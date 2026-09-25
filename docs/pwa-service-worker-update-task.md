# PigeonsTracker — PWA Service Worker Update Fix

Repo: https://github.com/umairsyed613/PigeonsTracker
Stack: Blazor WebAssembly PWA, hosted on Azure Static Web Apps, deployed via GitHub Actions (`Azure/static-web-apps-deploy`)

## Problem

After publishing new code to Azure Static Web Apps, the app installed on an
Android phone (via Chrome "Add to Home Screen") does not pick up the update.
The old cached version keeps loading.

## Root causes to check / fix

1. **Service worker script itself may be cached by the CDN.**
   Azure SWA's edge can cache static files aggressively. `service-worker.js`
   (and `service-worker.published.js`) must be served with
   `Cache-Control: no-cache` so the browser always re-checks it.

   Fix: add a route override in `staticwebapp.config.json`:
   ```json
   {
     "routes": [
       {
         "route": "/service-worker.js",
         "headers": { "cache-control": "no-cache" }
       }
     ]
   }
   ```

2. **No manual/forced update path exists in the app.**
   Even when a new service worker installs, it sits in the `waiting` state
   until all tabs/instances close (standard SW lifecycle) — so on a phone
   where the PWA is rarely fully closed, updates never apply.

   Fix: add `skipWaiting()` + `clients.claim()` support to the service
   worker, JS interop to trigger an update check, and a UI button in Blazor
   to apply it immediately.

3. **Verify the GitHub Actions deploy is actually producing a new asset hash.**
   Blazor's `service-worker-assets.js` manifest only triggers a refetch when
   file hashes change. Confirm the latest workflow run uploaded a
   `service-worker.published.js` / manifest that differs from the previous
   deployed version (check the Actions run artifacts/logs, or diff the
   deployed file against git history).

## Implementation tasks for Claude Code

### 1. `wwwroot/index.html` (or a new `wwwroot/js/pwa-update.js` referenced from it)
Add JS interop helpers:
- `blazorPwa.checkForUpdate()` — calls `registration.update()`, then resolves
  `true`/`false` depending on whether a new worker is waiting/installing.
- `blazorPwa.activateUpdate()` — posts `{ type: 'SKIP_WAITING' }` to the
  waiting worker.
- A `navigator.serviceWorker.addEventListener('controllerchange', ...)`
  listener that reloads the page once (guard with a `refreshing` flag) after
  the new worker takes control.

### 2. `wwwroot/service-worker.published.js`
- Add a `message` event listener that calls `self.skipWaiting()` when it
  receives `{ type: 'SKIP_WAITING' }`.
- Ensure the `activate` event handler calls `event.waitUntil(self.clients.claim())`
  (add it if missing).

### 3. New Blazor component (e.g. `Shared/UpdateChecker.razor` or inline in a settings page)
- Button: "Check for Updates" → calls `checkForUpdate()` via `IJSRuntime`.
- If an update is found, show an alert/banner with an "Update Now" button →
  calls `activateUpdate()` (page reloads automatically via the
  `controllerchange` listener — no extra code needed).
- Optional: call `checkForUpdate()` automatically in `App.razor`'s
  `OnInitializedAsync` so users are prompted without needing to press the
  button, and/or auto-call `activateUpdate()` immediately when an update is
  found for a fully silent auto-update flow.

### 4. `staticwebapp.config.json`
- Add the `no-cache` route override for `/service-worker.js` shown above
  (and consider doing the same for `/service-worker.published.js` and
  `/service-worker-assets.js` if they aren't already excluded from caching).

## How to verify the fix

1. Deploy the change via the existing GitHub Actions workflow.
2. On an Android phone, open the installed PWA (don't reinstall).
3. Use `chrome://inspect/#devices` from a desktop Chrome connected via USB
   (with USB debugging enabled on the phone) to open DevTools against the
   live PWA tab.
4. In DevTools → Application → Service Workers, confirm a new worker
   installs and shows as "waiting", then confirm pressing "Update Now" in
   the app activates it and reloads to the new version.
5. Check the `service-worker.js` request in the Network tab — confirm its
   `Cache-Control` response header is `no-cache`.
