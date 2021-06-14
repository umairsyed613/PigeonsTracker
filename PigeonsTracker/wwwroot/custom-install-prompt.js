function showAddToHomeScreen() {
    //console.log("Calling Install Prompt!");
    // These assembly and method names must match the app name and the method in the MainLayout.razor
    const blazorAssembly = 'PigeonsTracker';
    const blazorInstallMethod = 'PWAInstallable';

    DotNet.invokeMethodAsync(blazorAssembly, blazorInstallMethod)
        .then(function () { }, function (er) {
            //console.log(er);
            //setTimeout(showAddToHomeScreen, 1000);
        });
}
function OnPwaInstallClick() {
    if (window.PWADeferredPrompt) {
        // Fires the browser prompt. Invoked from the custom installation UI through JS Interop
        window.PWADeferredPrompt.prompt();
        window.PWADeferredPrompt.userChoice
            .then(function (choiceResult) {
                window.PWADeferredPrompt = null;
            });
    }
}
window.addEventListener('beforeinstallprompt', function (e) {
    // Prevent Chrome 67 and earlier from automatically showing the prompt
    e.preventDefault();
    // Stash the event so it can be triggered later.
    window.PWADeferredPrompt = e;
    // Show custom installation prompt (which will trigger the built-in one, of course, when confirmed by the user)
    const checkExist = setInterval(function () {
        const indexPage = document.getElementById("indexPage");
        if (!!indexPage) {
            showAddToHomeScreen();
            clearInterval(checkExist);
        }
    }, 100);
});