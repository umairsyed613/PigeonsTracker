using AspNetMonsters.Blazor.Geolocation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PigeonsTracker.Shared.Models;

namespace PigeonsTracker.Services;

public class AppState
{
    public double ApplicationVersion { get; private set; } = 3.21;
    public bool IsMobile { get; set; }
    public OpenWeatherApiResult CachedOpenWeatherApiResult { get; set; }
    public Location Location { get; private set; }
    public DateTime LocationSetDatetime { get; set; }

    public void SetLocation(Location location, DateTime dateTime)
    {
        Location = location;
        LocationSetDatetime = dateTime;
    }

    public event Action OnLanguageChange;
    private void NotifyStateChanged() => OnLanguageChange?.Invoke();
    public string LanguageName { get; set; }

    public void ChangeLanguageName(string name)
    {
        var notify = LanguageName != name;
        LanguageName = name;
        if (notify)
        {
            NotifyStateChanged();
        }
    }

    public bool IsLanguageUrdu() => !string.IsNullOrEmpty(LanguageName) && LanguageName.Equals("ur-PK");
}