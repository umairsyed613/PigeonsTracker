namespace PigeonsTrackerApi.Models;

public class FireStoreObjectResponse<T>
{
    public string Id { get; set; }
    public T Data { get; set; }
}