using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PigeonsTracker.Shared.Models;
using PigeonsTracker.Shared.Requests;
using PigeonsTrackerApi.Mapper;
using PigeonsTrackerApi.Models;
using PigeonsTrackerApi.Services;

namespace PigeonsTrackerApi;

public class FireStoreFunction
{
    private readonly IFireStoreService<FsTournament> _fireStoreTournamentService;
    private readonly IFireStoreService<FsPublicTournament> _fireStorePublicTournamentService;
    private readonly IFireStoreService<FsUserApproved> _fireStoreUserApprovedService;
    private readonly IFireStoreService<FsPigeonDiseaseAndCure> _firePigeonsDiseaseAndCureService;
    private readonly ILogger<FireStoreFunction> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FireStoreFunction(
        ILogger<FireStoreFunction> logger,
        IFireStoreService<FsTournament> fireStoreService,
        IFireStoreService<FsPublicTournament> fireStorePublicTournamentService,
        IFireStoreService<FsUserApproved> fireStoreUserApprovedService,
        IFireStoreService<FsPigeonDiseaseAndCure> firePigeonsDiseaseAndCureService)
    {
        _fireStoreTournamentService = fireStoreService ?? throw new ArgumentNullException(nameof(fireStoreService));
        _fireStorePublicTournamentService = fireStorePublicTournamentService ?? throw new ArgumentNullException(nameof(fireStorePublicTournamentService));
        _fireStoreUserApprovedService = fireStoreUserApprovedService;
        _firePigeonsDiseaseAndCureService = firePigeonsDiseaseAndCureService;
        _logger = logger;
    }

    [Function("FireStoreCreatePublicTournamentFunction")]
    public async Task<HttpResponseData> CreatePublicTournament(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "publictournament/create")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Creating public tournament on firestore.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<PublicTournament>(body, _jsonSerializerOptions);

        if (data is null || string.IsNullOrWhiteSpace(data.Name))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        data.Id = string.IsNullOrWhiteSpace(data.Id) ? Guid.NewGuid().ToString("N") : data.Id;
        data.CreatedAt = data.CreatedAt == default ? DateTime.Now : data.CreatedAt;
        data.DayRecords ??= [];

        if (string.IsNullOrWhiteSpace(data.ManagerCode))
        {
            data.ManagerCode = await GenerateUniqueCodeByFieldAsync("ManagerCode");
        }

        if (string.IsNullOrWhiteSpace(data.ManagerRecoveryKey))
        {
            data.ManagerRecoveryKey = await GenerateUniqueRecoveryKeyByFieldAsync("ManagerRecoveryKey");
        }

        data.Lofts ??= [];
        var loftCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var loft in data.Lofts)
        {
            loft.LoftId = string.IsNullOrWhiteSpace(loft.LoftId) ? Guid.NewGuid().ToString("N") : loft.LoftId;
            loft.CreatedAt = loft.CreatedAt == default ? DateTime.Now : loft.CreatedAt;

            if (string.IsNullOrWhiteSpace(loft.LoftCode))
            {
                loft.LoftCode = GenerateNumericCode();
            }

            while (!loftCodes.Add(loft.LoftCode))
            {
                loft.LoftCode = GenerateNumericCode();
            }
        }

        var resp = await _fireStorePublicTournamentService.AddDocumentAsync(data.Map());
        data.FireStoreId = resp.Id;

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    [Function("FireStoreGetPublicTournamentFunction")]
    public async Task<HttpResponseData> GetPublicTournament(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "publictournament/get/{id}")] HttpRequestData req,
        string id,
        FunctionContext executionContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        var stored = await GetPublicTournamentStoreByTournamentId(id);
        if (stored is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(stored.Data.Map(stored.Id));
        return response;
    }

    [Function("FireStoreGetAllPublicTournamentFunction")]
    public async Task<HttpResponseData> GetAllPublicTournament(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "publictournament/getall")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var docs = await _fireStorePublicTournamentService.GetDocumentObjectsAsync();
        var data = docs.Select(s => s.Data.Map(s.Id))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    [Function("FireStoreUpsertPublicTournamentDayRecordFunction")]
    public async Task<HttpResponseData> UpsertPublicTournamentDayRecord(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "publictournament/dayrecord/upsert")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<PublicTournamentUpsertDayRecordRequest>(body, _jsonSerializerOptions);

        if (request is null || string.IsNullOrWhiteSpace(request.TournamentId) || string.IsNullOrWhiteSpace(request.LoftId) || string.IsNullOrWhiteSpace(request.AccessCode))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var stored = await GetPublicTournamentStoreByTournamentId(request.TournamentId);
        if (stored is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var tournament = stored.Data.Map(stored.Id);
        var targetLoft = tournament.Lofts.FirstOrDefault(f => f.LoftId == request.LoftId);
        if (targetLoft is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var isManager = request.AccessCode == tournament.ManagerCode || request.AccessCode == tournament.ManagerRecoveryKey;
        var isLoftManager = request.AccessCode == targetLoft.LoftCode;

        if (!isManager && !isLoftManager)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        request.LoftRecord ??= new PublicTournamentLoftDayRecord();
        request.LoftRecord.LoftId = request.LoftId;
        request.LoftRecord.LoftName = string.IsNullOrWhiteSpace(request.LoftRecord.LoftName) ? targetLoft.LoftName : request.LoftRecord.LoftName;
        if (request.LoftRecord.StartTime == default)
        {
            request.LoftRecord.StartTime = request.DateOfFlying.Date + tournament.FlyingStartTime;
        }

        CalculateLoftRecordAggregates(request.LoftRecord, targetLoft.BirdCount, targetLoft.HasBabyPigeon);

        var normalizedDate = request.DateOfFlying.Date;
        var dayRecord = tournament.DayRecords.FirstOrDefault(f => f.Date.Date == normalizedDate);
        if (dayRecord is null)
        {
            dayRecord = new PublicTournamentDayRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = normalizedDate,
                CreatedAt = DateTime.Now,
                LoftRecords = []
            };
            tournament.DayRecords.Add(dayRecord);
        }

        var existingLoftRecord = dayRecord.LoftRecords.FirstOrDefault(f => f.LoftId == request.LoftId);
        if (existingLoftRecord is null)
        {
            dayRecord.LoftRecords.Add(request.LoftRecord);
        }
        else
        {
            dayRecord.LoftRecords.Remove(existingLoftRecord);
            dayRecord.LoftRecords.Add(request.LoftRecord);
        }

        await _fireStorePublicTournamentService.UpdateDocumentAsync(stored.Id, tournament.Map());

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dayRecord);
        return response;
    }

    [Function("FireStorePublicTournamentRegenerateCodesFunction")]
    public async Task<HttpResponseData> RegeneratePublicTournamentCodes(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "publictournament/codes/regenerate")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<PublicTournamentRegenerateCodesRequest>(body, _jsonSerializerOptions);

        if (request is null || string.IsNullOrWhiteSpace(request.TournamentId) || string.IsNullOrWhiteSpace(request.ManagerAccessCodeOrRecovery))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var stored = await GetPublicTournamentStoreByTournamentId(request.TournamentId);
        if (stored is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var tournament = stored.Data.Map(stored.Id);
        var isManager = request.ManagerAccessCodeOrRecovery == tournament.ManagerCode || request.ManagerAccessCodeOrRecovery == tournament.ManagerRecoveryKey;
        if (!isManager)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        if (request.RegenerateManagerCode)
        {
            tournament.ManagerCode = await GenerateUniqueCodeByFieldAsync("ManagerCode");
        }

        if (request.RegenerateRecoveryKey)
        {
            tournament.ManagerRecoveryKey = await GenerateUniqueRecoveryKeyByFieldAsync("ManagerRecoveryKey");
        }

        if (request.RegenerateAllLoftCodes)
        {
            var generated = new HashSet<string>(StringComparer.Ordinal);
            foreach (var loft in tournament.Lofts)
            {
                var next = GenerateNumericCode();
                while (!generated.Add(next))
                {
                    next = GenerateNumericCode();
                }

                loft.LoftCode = next;
            }
        }

        tournament.CodeVersion = Math.Max(1, tournament.CodeVersion + 1);
        tournament.LastCodeRegeneratedAt = DateTime.Now;

        await _fireStorePublicTournamentService.UpdateDocumentAsync(stored.Id, tournament.Map());

        var responseBody = new PublicTournamentRegenerateCodesResponse
        {
            TournamentId = tournament.Id,
            CodeVersion = tournament.CodeVersion,
            RegeneratedAt = tournament.LastCodeRegeneratedAt ?? DateTime.Now,
            ManagerCode = tournament.ManagerCode,
            ManagerRecoveryKey = tournament.ManagerRecoveryKey,
            LoftCodesByLoftId = tournament.Lofts.ToDictionary(k => k.LoftId, v => v.LoftCode)
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(responseBody);
        return response;
    }

    [Function("FireStorePublicTournamentBirdIndexSummaryFunction")]
    public async Task<HttpResponseData> GetPublicTournamentBirdIndexSummary(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "publictournament/summary/birdindex/{id}")] HttpRequestData req,
        string id,
        FunctionContext executionContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        var stored = await GetPublicTournamentStoreByTournamentId(id);
        if (stored is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var tournament = stored.Data.Map(stored.Id);
        var maxBirdCount = tournament.Lofts.DefaultIfEmpty().Max(m => m?.BirdCount ?? 0);
        var rows = new List<PublicTournamentBirdIndexSummaryRow>();

        foreach (var day in tournament.DayRecords.OrderBy(o => o.Date))
        {
            foreach (var loftRecord in day.LoftRecords.OrderBy(o => o.LoftName))
            {
                var perBirdTicks = Enumerable.Repeat<long?>(null, maxBirdCount).ToList();
                foreach (var bird in loftRecord.BirdRecords)
                {
                    if (bird.BirdIndex > 0 && bird.BirdIndex <= maxBirdCount)
                    {
                        perBirdTicks[bird.BirdIndex - 1] = bird.TotalBirdFlyingTime?.Ticks;
                    }
                }

                var totalTicks = perBirdTicks.Where(w => w.HasValue).Select(s => s!.Value).Sum();
                totalTicks += loftRecord.BabyBird?.TotalBirdFlyingTime?.Ticks ?? 0;

                rows.Add(new PublicTournamentBirdIndexSummaryRow
                {
                    LoftName = loftRecord.LoftName,
                    DateOfFlying = day.Date,
                    BirdHoursTicks = perBirdTicks,
                    TotalSumOfDayTicks = totalTicks
                });
            }
        }

        var responseBody = new PublicTournamentBirdIndexSummaryResponse
        {
            MaxBirdCount = maxBirdCount,
            Rows = rows
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(responseBody);
        return response;
    }

    [Function("FireStorePublicTournamentTotalsSummaryFunction")]
    public async Task<HttpResponseData> GetPublicTournamentTotalsSummary(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "publictournament/summary/totals/{id}")] HttpRequestData req,
        string id,
        FunctionContext executionContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        var stored = await GetPublicTournamentStoreByTournamentId(id);
        if (stored is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var tournament = stored.Data.Map(stored.Id);
        var rows = tournament.DayRecords
            .OrderBy(o => o.Date)
            .SelectMany(day => day.LoftRecords.Select(loftRecord => new PublicTournamentTotalsSummaryRow
            {
                LoftName = loftRecord.LoftName,
                DateOfFlying = day.Date,
                TotalLanded = loftRecord.TotalLanded,
                TotalNotLanded = loftRecord.TotalNotLanded,
                BabyPigeonSumTicks = loftRecord.BabyPigeonSum?.Ticks ?? 0,
                TotalHoursTicks = loftRecord.TotalHours?.Ticks ?? 0
            }))
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(rows);
        return response;
    }

    [Function("FireStoreCreateFunction")]
    public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Function, "post", Route = "data/create")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Creating tournament on firestore.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Tournament>(body, _jsonSerializerOptions);

        var resp = await _fireStoreTournamentService.AddDocumentAsync(data.Map());

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new FireStoreUpsertResponse()
        {
            Id = resp.Id
        });

        return response;
    }

    [Function("FireStoreUpdateFunction")]
    public async Task<HttpResponseData> Update([HttpTrigger(AuthorizationLevel.Function, "post", Route = "data/update")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Updating tournament on firestore.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Tournament>(body, _jsonSerializerOptions);

        if (data.FireStoreId is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        await _fireStoreTournamentService.UpdateDocumentAsync(data.FireStoreId, data.Map());

        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function("FireStoreGetAllFunction")]
    public async Task<HttpResponseData> GetAll([HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/getall")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Getting all tournaments from firestore.");

        var dd = await _fireStoreTournamentService.GetDocumentsAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(dd);

        return response;
    }

    [Function("FireStoreGetFunction")]
    public async Task<HttpResponseData> GetOne([HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/get/{id}")] HttpRequestData req,
        string id, FunctionContext executionContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _logger.LogInformation("Getting tournaments from firestore.");

        var storeObjectResponse = await _fireStoreTournamentService.GetDocumentAsync(id);

        if (storeObjectResponse == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(storeObjectResponse.Data.Map(storeObjectResponse.Id));

        return response;
    }

    [Function("FireStoreUserApprovedFunction")]
    public async Task<HttpResponseData> IsUserApproved([HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/approved/user/{id}")] HttpRequestData req,
        string id, FunctionContext executionContext)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _logger.LogInformation("Getting ApprovedUser from firestore.");

        var storeObjectResponse = await _fireStoreUserApprovedService.QueryDocumentsAsync("UserId", id);

        if (storeObjectResponse == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var first = storeObjectResponse.Single();
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(first.Data);

        return response;
    }

    [Function("FireStorePigeonsDisease")]
    public async Task<HttpResponseData> AddPigeonsDiseaseAndCure([HttpTrigger(AuthorizationLevel.Function, "post", Route = "data/pigeonsdisease/add")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Creating tournament on firestore.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<PigeonDiseaseAndCure>(body, _jsonSerializerOptions);

        var resp = await _firePigeonsDiseaseAndCureService.AddDocumentAsync(data.Map());

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new FireStoreUpsertResponse()
        {
            Id = resp.Id
        });

        return response;
    }

    private async Task<FireStoreObjectResponse<FsPublicTournament>> GetPublicTournamentStoreByTournamentId(string tournamentId)
    {
        var response = await _fireStorePublicTournamentService.QueryDocumentsAsync("Id", tournamentId);
        if (response is not { Count: > 0 })
        {
            return null;
        }

        return response.First();
    }

    private static void CalculateLoftRecordAggregates(PublicTournamentLoftDayRecord record, int expectedBirdCount, bool hasBabyPigeon)
    {
        record.BirdRecords ??= [];

        var landedBirds = record.BirdRecords.Count(c => c.EndTime.HasValue);
        var birdsTotalTicks = record.BirdRecords
            .Where(w => w.TotalBirdFlyingTime.HasValue)
            .Select(s => s.TotalBirdFlyingTime!.Value.Ticks)
            .DefaultIfEmpty(0)
            .Sum();

        var babyTicks = hasBabyPigeon
            ? record.BabyBird?.TotalBirdFlyingTime?.Ticks ?? 0
            : 0;

        record.TotalLanded = landedBirds;
        record.TotalNotLanded = Math.Max(0, expectedBirdCount - landedBirds);
        record.BabyPigeonSum = TimeSpan.FromTicks(Math.Max(0, babyTicks));
        record.TotalHours = TimeSpan.FromTicks(Math.Max(0, birdsTotalTicks + babyTicks));
        record.UpdatedAt = DateTime.Now;
    }

    private async Task<string> GenerateUniqueCodeByFieldAsync(string fieldName)
    {
        for (var i = 0; i < 25; i++)
        {
            var code = GenerateNumericCode();
            var existing = await _fireStorePublicTournamentService.QueryDocumentsAsync(fieldName, code);
            if (existing is not { Count: > 0 })
            {
                return code;
            }
        }

        throw new InvalidOperationException($"Unable to generate unique code for field '{fieldName}'.");
    }

    private async Task<string> GenerateUniqueRecoveryKeyByFieldAsync(string fieldName)
    {
        for (var i = 0; i < 25; i++)
        {
            var key = GenerateNumericKey(24);
            var existing = await _fireStorePublicTournamentService.QueryDocumentsAsync(fieldName, key);
            if (existing is not { Count: > 0 })
            {
                return key;
            }
        }

        throw new InvalidOperationException($"Unable to generate unique recovery key for field '{fieldName}'.");
    }

    private static string GenerateNumericCode()
    {
        var parts = new string[3];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = string.Concat(Enumerable.Range(0, 3).Select(_ => RandomNumberGenerator.GetInt32(0, 10).ToString()));
        }

        return string.Join("-", parts);
    }

    private static string GenerateNumericKey(int length)
    {
        return string.Concat(Enumerable.Range(0, length).Select(_ => RandomNumberGenerator.GetInt32(0, 10).ToString()));
    }
}