using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PigeonsTracker.Shared.Models;
using PigeonsTrackerApi.Mapper;
using PigeonsTrackerApi.Models;
using PigeonsTrackerApi.Services;

namespace PigeonsTrackerApi;

public class FireStoreFunction
{
    private readonly IFireStoreService<FsTournament> _fireStoreTournamentService;
    private readonly IFireStoreService<FsUserApproved> _fireStoreUserApprovedService;
    private readonly IFireStoreService<FsPigeonDiseaseAndCure> _firePigeonsDiseaseAndCureService;
    private readonly ILogger<FireStoreFunction> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FireStoreFunction(ILogger<FireStoreFunction> logger, IFireStoreService<FsTournament> fireStoreService, IFireStoreService<FsUserApproved> fireStoreUserApprovedService, IFireStoreService<FsPigeonDiseaseAndCure> firePigeonsDiseaseAndCureService)
    {
        _fireStoreTournamentService = fireStoreService ?? throw new ArgumentNullException(nameof(fireStoreService));
        _fireStoreUserApprovedService = fireStoreUserApprovedService;
        _firePigeonsDiseaseAndCureService = firePigeonsDiseaseAndCureService;
        _logger = logger;
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
}