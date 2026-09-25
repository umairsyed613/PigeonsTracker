window.blazorPwa = (function () {
    let registration = null;
    let refreshing = false;

    navigator.serviceWorker.register('service-worker.js').then(function (reg) {
        registration = reg;
    });

    navigator.serviceWorker.addEventListener('controllerchange', function () {
        if (refreshing) return;
        refreshing = true;
        window.location.reload();
    });

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            checkForUpdate();
        }
    });

    async function checkForUpdate() {
        if (!registration) return false;
        try {
            await registration.update();
        } catch (e) {
            // Network unavailable or update check failed; ignore.
        }

        const hasWaiting = !!registration.waiting;
        if (hasWaiting) {
            notifyBlazor();
        }
        return hasWaiting;
    }

    function activateUpdate() {
        if (registration && registration.waiting) {
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        }
    }

    function notifyBlazor() {
        DotNet.invokeMethodAsync('PigeonsTracker', 'PwaUpdateAvailable').catch(function () { });
    }

    return {
        checkForUpdate: checkForUpdate,
        activateUpdate: activateUpdate
    };
})();
