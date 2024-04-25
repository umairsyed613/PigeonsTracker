using AspNetMonsters.Blazor.Geolocation;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public class AppState
{
    public double ApplicationVersion { get; private set; } = 2.23;

    public OpenWeatherApiResult CachedOpenWeatherApiResult { get; set; }
    public Location Location { get; private set; }
    public DateTime LocationSetDatetime { get; set; }

    public void SetLocation(Location location, DateTime dateTime)
    {
        Location = location;
        LocationSetDatetime = dateTime;
    }

    public event Action OnChange;
    private void NotifyStateChanged() => OnChange?.Invoke();
    public string LanguageName { get; set; }

    public void ChangeLanguageName(string name)
    {
        LanguageName = name;
        NotifyStateChanged();
    }

    public bool IsLanguageUrdu() => !string.IsNullOrEmpty(LanguageName) && LanguageName.Equals("ur-PK");
}