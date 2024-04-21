using System.Globalization;
using Microsoft.AspNetCore.Components;
using PigeonsTracker.Services;

namespace PigeonsTracker.Shared;

public partial class CultureSelectorCmp
{
    [Inject] private AppState AppState { get; set; }

    public PigeonsTrackerCultureInfo[] PigeonsTrackerCultures = new[]
    {
        new PigeonsTrackerCultureInfo { CultureInfo = "en-US", DisplayName = "English" },
        new PigeonsTrackerCultureInfo { CultureInfo = "ur-PK", DisplayName = "Urdu" }
    };

    private string _selectedVal = CultureInfo.CurrentCulture.Name;

    public string SelectedValue
    {
        get => _selectedVal;
        set
        {
            _selectedVal = value;
            AppState.ChangeLanguageName(SelectedValue);
        }
    }
}

public sealed class PigeonsTrackerCultureInfo
{
    public string CultureInfo { get; set; }
    public string DisplayName { get; set; }
}