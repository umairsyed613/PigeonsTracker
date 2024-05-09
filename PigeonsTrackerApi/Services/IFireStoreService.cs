using Google.Cloud.Firestore;
using PigeonsTrackerApi.Models;

namespace PigeonsTrackerApi.Services;

public interface IFireStoreService<T>
{
    Task<DocumentReference> AddDocumentAsync(T documentData);
    Task<FireStoreObjectResponse<T>> GetDocumentAsync(string documentId);
    Task UpdateDocumentAsync(string documentId,T updates);
    Task DeleteDocumentAsync(string documentId);
    Task<List<Dictionary<string, object>>> GetDocumentsAsync();
    Task<List<FireStoreObjectResponse<T>>> QueryDocumentsAsync(string fieldName, object fieldValue);
}