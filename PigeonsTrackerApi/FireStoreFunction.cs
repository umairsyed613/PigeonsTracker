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
    private readonly IFireStoreService<FsTournament> _fireStoreService;
    private readonly ILogger<FireStoreFunction> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FireStoreFunction(ILogger<FireStoreFunction> logger, IFireStoreService<FsTournament> fireStoreService)
    {
        _fireStoreService = fireStoreService ?? throw new ArgumentNullException(nameof(fireStoreService));
        _logger = logger;
    }

    [Function("FireStoreCreateFunction")]
    public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Function, "post", Route = "data/create")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Creating tournament on firestore.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Tournament>(body, _jsonSerializerOptions);

        var resp = await _fireStoreService.AddDocumentAsync(data.Map());

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

        await _fireStoreService.UpdateDocumentAsync(data.FireStoreId, data.Map());

        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function("FireStoreGetAllFunction")]
    public async Task<HttpResponseData> GetAll([HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/getall")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Getting all tournaments from firestore.");

        var dd = await _fireStoreService.GetDocumentsAsync();
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

        var storeObjectResponse = await _fireStoreService.GetDocumentAsync(id);

        if (storeObjectResponse == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(storeObjectResponse.Data.Map(storeObjectResponse.Id));

        return response;
    }
}