using Google.Cloud.Firestore;
using PigeonsTrackerApi.Models;

namespace PigeonsTrackerApi.Services;

public class FireStoreService<T> : IFireStoreService<T>
{
    private readonly FirestoreDb _db;
    private readonly CollectionReference _collection;

    public FireStoreService(FirestoreDb firestoreDb, string collectionPath)
    {
        ArgumentNullException.ThrowIfNull(collectionPath);
        _db = firestoreDb ?? throw new ArgumentNullException(nameof(firestoreDb));
        _collection = _db.Collection(collectionPath);
    }

    public async Task<DocumentReference> AddDocumentAsync(T documentData)
    {
        return await _collection.AddAsync(documentData);
    }

    public async Task UpdateDocumentAsync(string documentId, T updates)
    {
        var docRef = _collection.Document(documentId);
        var updatesDict = updates.GetType()
            .GetProperties()
            .Where(prop => prop.GetValue(updates, null) != null)
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(updates, null));

        await docRef.UpdateAsync(updatesDict);
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        var docRef = _collection.Document(documentId);
        await docRef.DeleteAsync();
    }

    public async Task<FireStoreObjectResponse<T>> GetDocumentAsync(string documentId)
    {
        var docRef = _collection.Document(documentId);
        var snapshot = await docRef.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            var data = snapshot.ConvertTo<T>();
            return new FireStoreObjectResponse<T>
            {
                Id = snapshot.Id,
                Data = data
            };
        }

        return null;
    }

    public async Task<List<Dictionary<string, object>>> GetDocumentsAsync()
    {
        var querySnapshot = await _collection.GetSnapshotAsync();
        return querySnapshot.Documents.Select(snapshot => snapshot.ToDictionary()).ToList();
    }

    public async Task<List<Dictionary<string, object>>> QueryDocumentsAsync(string fieldName, object fieldValue)
    {
        var query = _collection.WhereEqualTo(fieldName, fieldValue);
        var querySnapshot = await query.GetSnapshotAsync();
        return querySnapshot.Documents.Select(doc => doc.ToDictionary()).ToList();
    }
}