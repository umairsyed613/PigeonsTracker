using Google.Cloud.Firestore;

namespace PigeonsTrackerApi.Models;

[FirestoreData]
public class FsPigeonDiseaseAndCure
{
    [FirestoreProperty] public string Id { get; set; }
    [FirestoreProperty] public Dictionary<string, string> Title { get; set; } = new Dictionary<string, string>();
    [FirestoreProperty] public Dictionary<string, string> Content { get; set; } = new Dictionary<string, string>();
    [FirestoreProperty] public DateTime CreatedAt { get; set; }
    [FirestoreProperty] public DateTime? ModifiedAt { get; set; }
}