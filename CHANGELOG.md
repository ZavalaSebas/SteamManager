# Changelog

All notable changes to SteamManager will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-07-25

### Fixed

- **Multi-process architecture now working**: Helper window now appears and displays correct achievements for each game. Previous attempts failed because `MainWindow` constructor was overwriting the passed DataContext.

### Changed

- **Single exe, multi-process**: `SteamManager.exe` behaves differently based on command-line arguments:
  - No args: **Launcher mode** — initializes Steam with Spacewar AppId (480), shows game list
  - `--game <appId>`: **Helper mode** — initializes Steam with specific game AppId, shows achievements
- **`GamePickerViewModel.SelectGameCommand`**: Now launches helper process and calls `Application.Current.Shutdown()` on the launcher
- **`MainViewModel` navigation**: `NavigateToGame()` callback pattern removed in favor of multi-process
- **`MainWindow.xaml.cs`**: Constructor only sets DataContext from Services if DataContext is null (allows override)

### Known Issues

| Issue | Status |
|-------|--------|
| Achievement icons not loading | Pending fix |
| Back button doesn't work in helper | Pending fix |
| Loading status stays forever | Pending fix |

### Technical Notes

**Why multi-process works**: `steamclient.dll` is a process-level singleton. When Steam client is running with AppId=X, initializing with AppId=Y in the same process either fails or returns wrong data (usually Spacewar achievements). By launching a new process with `--game <appId>`, each game gets its own isolated Steam context.

**Launcher shutdown timing**: Calling `Application.Current.Shutdown()` immediately after `Process.Start()` ensures the launcher window closes before the helper window appears, giving a seamless experience.

---

## [0.2.2] - 2026-07-25

### Changed

- **Multi-process architecture**: Launcher (`SteamManager.exe` without args) shows the game list. Game helper (`SteamManager.exe --game <appId>`) shows achievements for a specific game. This solves the `steamclient.dll` singleton problem — each game runs in its own process with its own AppId.
- **Launcher mode** (`App.StartLauncherMode`): Initializes Steam with Spacewar AppId (480) for ownership verification. Downloads `games.xml` from gib.me and displays owned games only.
- **Helper mode** (`App.StartGameHelperMode`): Initializes Steam with the specific game AppId, loads and displays achievements for that game only.
- **`GamePickerViewModel.SelectGameCommand`**: Launches a new process (`SteamManager.exe --game <appId>`) instead of navigating within the same window.
- **`GameCard` event routing**: Changed from `MouseLeftButtonUp` to a custom `GameSelected` routed event for reliable click handling.

### Known Issues

| Issue | Root Cause | Workaround |
|-------|------------|------------|
| **Helper window doesn't appear** | WPF + `steamclient.dll` multi-process has COM threading issues. The helper process starts and creates the window, but Steam initialization on a background thread doesn't properly update the UI. | None yet — requires investigation into WPF COM threading model with native DLL interop. |

### Technical Notes

**Why multi-process?** `steamclient.dll` maintains global state per process. When Steam is running (AppId=X) and we try to initialize with AppId=Y in the same process, Steam rejects it or returns wrong data. SAM solves this by having two executables: `SAM.Picker.exe` (lists games) and `SAM.Game.exe <appId>` (manages achievements for that specific game). We replicate this with a single exe that behaves differently based on command-line arguments.

**Launcher** (`--game` absent): Initializes Steam with AppId=480 (Spacewar), downloads game list, filters by ownership via `IsSubscribedApp()`, displays owned games.

**Helper** (`--game <appId>`): Creates window first, then initializes Steam with the game AppId on a background thread. **Known issue**: The background thread approach causes the window to appear but Steam initialization doesn't complete properly.

---

## [0.2.1] - 2026-07-25

### Changed

- **Game enumeration approach**: Switched from Steam Web API (`GetOwnedGames`) to SAM's `games.xml` + `IsSubscribedApp()` approach. The Web API approach failed because `api.steampowered.com` requires an API key for `GetOwnedGames`, and the Steam Community XML (`/profiles/{id}/games/?xml=1`) requires authenticated session cookies.
- **Game cover URLs**: Changed CDN hostname from `steamcdn.fra1.cdn.digitaloceantl.com` to `steamcdn-a.akamaihd.net` (the correct Steam CDN).
- **GameCard**: Removed playtime display — SAM approach doesn't provide playtime data. Requires Steam Web API key for `GetOwnedGames` with playtime.

### Fixed

- **"0 games loaded" after XML parse error**: `XDocument.Parse()` threw `XmlException: An error occurred while parsing EntityName` on games with `&` in their name (e.g., "Age of Empires II & Conquerors"). Replaced with manual string parsing and `XmlReader` with `DtdProcessing.Ignore`.
- **Steam Community XML returns "Sign In" page**: The endpoint requires Steam session cookies which `HttpClient` doesn't have. Additionally, private profiles return HTML instead of XML regardless of authentication state.
- **Game covers not loading**: Wrong CDN hostname prevented images from downloading. Fixed URL pattern.
- **UI theme**: MainWindow changed from `Window` to `FluentWindow` (WPF-UI) with dark background (#1A1A1A).

### Added

- **`IImageCacheService.cs`**: Interface for caching downloaded images locally with TTL.
- **`ImageCacheService.cs`**: Implementation with memory + disk cache (7-day TTL), MD5 hash filenames, async download.
- **`ISmartUnlockService.cs`** and **`SmartUnlockService.cs`**: Anti-detection delay system for unlock/lock operations.
- **`IConfigService.cs`** and **`ConfigService.cs`**: JSON-based settings persistence (favorites, unlock delays, theme).
- **`GameInfo.CoverImage`**: `ObservableProperty` for async image loading with UI binding support.
- **`GamePickerViewModel.LoadCoversAsync()`**: Background loading of game covers after library loads.
- **`UrlToCachedImageConverter.cs`**: WPF value converter for URL-to-cached-image conversion.
- **`BoolToVisibilityConverter.cs`**: WPF converter for loading state visibility.
- **`ISteamUser012.cs`**: Interface wrapper to get user's SteamID via `GetSteamId()`.
- **`ISteamApps001.cs`**: Interface wrapper for `GetAppData(appId, key)`.
- **`SteamContext.SteamId`**: Public property returning the logged-in user's SteamID64.
- **`SteamApps.GetAppData()`**: New method exposed via `SteamApps` wrapper.
- **`SteamWebApiKey` constant**: Placeholder for future API key support (currently unused).
- **Navigation**: Game selection navigates to `GameManagerViewModel`; `BackToGamesCommand` returns to picker.

### Known Issues Resolved

| Issue | Root Cause | Resolution |
|-------|------------|------------|
| "0 games loaded" after XML parse error | `XDocument.Parse()` fails on `&` entities in game names | Manual string parsing + `XmlReader` with `DtdProcessing.Ignore` |
| "0 games loaded" after "Sign In" HTML | Steam Community XML requires session cookies + private profile returns HTML | Switched to SAM approach: `games.xml` + `IsSubscribedApp()` |
| Game covers not showing | Wrong CDN hostname (`steamcdn.fra1.cdn.digitaloceantl.com` doesn't resolve) | Changed to `steamcdn-a.akamaihd.net` |
| Window background white despite dark theme | `Window` doesn't apply WPF-UI theme; needed `FluentWindow` | Changed `MainWindow` base class to `FluentWindow` |

---

## [0.2.0] - 2026-07-25

### Changed

- **Steam API approach**: Switched from `steam_api64.dll` (Steamworks SDK, distributed with games) to `steamclient.dll` (internal Steam client library, ships with every Steam installation). See [ADR-004](DEVELOPMENT.md#adr-004-use-steamclientdll-instead-of-steam_api64dll).
- **Target platform**: Changed from `win-x64` to `win-x86` (32-bit). Steam ships only a 32-bit `steamclient.dll`; Windows cannot load a 32-bit DLL into a 64-bit process.
- **Project structure**: Separated interface vtable definitions from wrapper classes. Each Steam interface (`ISteamClient018`, `ISteamUserStats013`, `ISteamApps008`, `ISteamUtils005`) now has its own file matching gibbed/SAM structure.
- **String marshaling**: Version strings passed to `steamclient.dll` now use `IntPtr` + `NativeStrings.StringToStringHandle()` (UTF-8) instead of `string` (UTF-16) to match Steam's expected encoding.
- **`NativeMethods.cs`**: Corrected entry points — `SetDllDirectoryW` (UTF-16) for `SetDllDirectory`, `[MarshalAs(LPStr)]` for `GetProcAddress`.
- **`App.xaml.cs`**: Steam initialization now runs asynchronously via `Task.Run` instead of blocking the UI thread before `mainWindow.Show()`.
- **Callback system**: Added `DispatcherTimer` polling at 100ms intervals (`StartCallbackTimer`) to dispatch Steam callbacks — previously `RunCallbacks()` was never called.

### Added

- **WPF-UI theme**: `App.xaml` now includes `<ui:ThemesDictionary Theme="Dark"/>` and `<ui:ControlsDictionary/>` — required for all `DynamicResource` brushes to resolve.
- **DataTemplate mappings**: `App.xaml:Application.Resources` now maps `GamePickerViewModel` → `GamePickerView` and `GameManagerViewModel` → `GameManagerView` so the `ContentControl` can render them.
- **New interop files** (matched from gibbed/SAM source):
  - `Steam/ISteamClient018.cs` — vtable struct + `SteamClient018` wrapper
  - `Steam/ISteamUserStats013.cs` — vtable struct + `SteamUserStats013` wrapper
  - `Steam/ISteamApps008.cs` — vtable struct + `SteamApps008` wrapper
  - `Steam/ISteamUtils005.cs` — vtable struct + `SteamUtils005` wrapper (added `GetAppId()`)
  - `Steam/NativeMethods.cs` — `LoadLibraryExW`, `GetProcAddress`, `SetDllDirectoryW` P/Invoke
  - `Steam/NativeStrings.cs` — UTF-8 string marshaling utilities
  - `Steam/NativeWrapper.cs` — generic vtable extraction base class
  - `Steam/SteamLoader.cs` — DLL loading from registry + `CreateInterface`/`GetCallback`/`FreeLastCallback`
- **`Controls/` directory**: `GameCard.xaml`, `AchievementCard.xaml` (placeholders matching PLAN.md)
- **`Services/` directory**: `IGameLibraryService.cs`, `SteamGameLibraryService.cs` (placeholder — returns hardcoded Spacewar game)
- **`ViewModels/` directory**: `MainViewModel.cs`, `GamePickerViewModel.cs`, `GameManagerViewModel.cs`
- **`Views/` directory**: `GamePickerView.xaml/.xaml.cs`, `GameManagerView.xaml/.xaml.cs`
- **`Known Issues & Resolutions` section** in `DEVELOPMENT.md` documenting all bugs found and fixed during development.

### Removed

- **`Steam/SteamNative.cs`**: Original P/Invoke declarations for `steam_api64.dll` — obsolete after ADR-004 switch.
- **`Steam/SteamCallbackIds.cs`**: Callback ID constants — functionality absorbed into `SteamCallbacks.cs`.

### Fixed

- `EntryPointNotFoundException: 'SetDllDirectory'` — `LibraryImport` does not auto-append `W`; explicit `EntryPoint = "SetDllDirectoryW"` required.
- `LoadLibraryEx` returns 0 / `Win32 Error 0` — was caused by architecture mismatch (32-bit DLL, 64-bit process).
- `GetISteamUserStats` returns `IntPtr.Zero` — `ISteamClient018` vtable layout was wrong. Real index for `GetISteamUserStats` is **13** (not 19 as previously assumed). All vtable structs rewritten 1-to-1 from `gibbed/SteamAchievementManager/SAM.API/Interfaces/`.
- Native interface version strings garbled — `string` marshals as UTF-16 but Steam expects UTF-8; now using `NativeStrings.StringToStringHandle()`.
- `IsSubscribedApp` not found in vtable — field was named `BIsSubscribedApp`; SAM names it `IsSubscribedApp`.
- `MainViewModel.StatusMessage` stuck on "Connecting to Steam" — `InitializeSteam()` swallowed exceptions and status never propagated to the VM.
- Blank/dark screen with WPFUI resources missing — `App.xaml` had no theme dictionaries loaded.
- ContentControl shows nothing — no `DataTemplate` mapping ViewModels to Views.

---

## [0.1.0] - 2026-07-24

### Added

- Initial project setup and configuration
- Steam API integration via P/Invoke (`steam_api64.dll`) — **deprecated**, see v0.2.0 change log
- Project architecture and development guide
