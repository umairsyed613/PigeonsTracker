using Blazored.LocalStorage;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Services;

public class SettingsService(ILocalStorageService localStorage)
{
    private const string SettingsKey = "A4BF6C42FC964935B15B12A1E05A3513";
    private ILocalStorageService LocalStorage { get; init; } = localStorage;

    public async Task UpsertSettings(SettingsModel settingsModel)
    {
        ArgumentNullException.ThrowIfNull(settingsModel);

        await LocalStorage.SetItemAsync(SettingsKey, settingsModel);
    }

    public async Task<SettingsModel> GetSettings()
    {
        if (await LocalStorage.ContainKeyAsync(SettingsKey))
        {
            return await LocalStorage.GetItemAsync<SettingsModel>(SettingsKey);
        }

        return null;
    }
}