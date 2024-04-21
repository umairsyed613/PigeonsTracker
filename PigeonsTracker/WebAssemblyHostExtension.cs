using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using Microsoft.JSInterop;
using PigeonsTracker.Services;

namespace PigeonsTracker;

public static class WebAssemblyHostExtension
{
    public static async Task SetDefaultCulture(this WebAssemblyHost host)
    {
        try
        {
            var jsInterop = host.Services.GetRequiredService<IJSRuntime>();
            var result = await jsInterop.InvokeAsync<string>("blazorCulture.get");

            //Console.WriteLine(result);

            var defaultCul = "en-US";

            if (string.IsNullOrEmpty(result))
            {
                await jsInterop.InvokeVoidAsync("blazorCulture.set", defaultCul);
            }

            var culture = !string.IsNullOrEmpty(result) ? new CultureInfo(result) : new CultureInfo(defaultCul);


            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            var appSate = host.Services.GetRequiredService<AppState>();
            appSate.LanguageName = !string.IsNullOrEmpty(result) ? result : defaultCul;
            Console.WriteLine($@"Language is set to {appSate.LanguageName}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}