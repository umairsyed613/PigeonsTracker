namespace PigeonsTracker.Helper;

public class IdGenerator
{
    public static string GetNewId()
    {
            return Guid.NewGuid().ToString("N");
        }
}