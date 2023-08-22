using System;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PigeonsTracker
{
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

                if (string.IsNullOrEmpty(result)) { await jsInterop.InvokeVoidAsync("blazorCulture.set", defaultCul); }

                var culture = !string.IsNullOrEmpty(result) ? new CultureInfo(result) : new CultureInfo(defaultCul);

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}