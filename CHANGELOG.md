# Changelog

All notable changes to SteamManager will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-07-30

### Added

- **App icon**: Custom icon (SM.ico/SM.png) now used for the executable, window taskbar, and About dialog.
- **WelcomeWindow WPFUI visual refresh**: Changed from plain `Window` to `FluentWindow` with Mica backdrop and custom title bar — consistent with the rest of the app's aesthetic.
- **Launcher visual refresh**: GamePickerView redesigned with toggle chips (pill-style filters), square refresh button (40×40), and standalone Add Game button.
- **Favorite star animation**: Pop-scale animation (1.0→1.35→1.0) when toggling favorite. Dynamic star icon — outline `&#xE734;` when not favorited, filled `&#xE735;` when favorited.
- **AboutDialog WPFUI visual refresh**: Converted from plain `Window` to `FluentWindow` with Mica backdrop and custom title bar. Redesigned layout with app info card, "How it works" section, credits, and support card (Ko-fi + GitHub Sponsor). Removed standalone disclaimer in favor of cleaner card-based layout.

### Changed

- **Header layout improvement**: GameManagerView header now uses a two-block layout — game title on the left with `MaxWidth="400"` and truncation, stats (circle + counter + percentage) on the right, top-aligned with the circle.
- **Progress circle clean-up**: Removed percentage text overlay from inside the 48×48 progress circle. Percentage now displayed below the achievement counter in smaller text (`FontSize="14"`, `SemiBold`, `#66C0F4`).
- **AchievementCard description space**: Removed `MaxWidth="350"` restrictions on both achievement name and description so they expand to fill available card width without being cut off.
- **Game title wraps to two lines**: Changed header to Grid layout so long game names wrap to a second line (`TextWrapping="Wrap"`, `MaxHeight="72"`) instead of being truncated or pushing stats off-screen.
- **Filter checkboxes → toggle chips**: Replaced flat checkboxes with styled `ToggleButton` pills — selected state shows accent background (`#2A4A6B`) and border (`#66C0F4`).
- **Improved text contrast**: Labels ("Show:", "Add:", tagline), search icon, and game count now use lighter shades (`#AAAAAA`, `#8A8A8A`, `#6B6B6B`) for readability on dark background.
- **Favorite glow simplified**: Removed gold gradient overlay on card cover; indicator now uses card border accent + star only.
- **WelcomeWindow content styling**: Cards, buttons, text, and checkbox now match the launcher's WPFUI aesthetic with consistent colors, shadows, and typography.

### Fixed

- **WelcomeWindow animation**: Removed stale `HeaderBorder` animation reference after the visual refresh.
- **AboutDialog icon**: Replaced generic Segoe MDL2 icon with the actual app icon (SM.png).
- **Tooltip font inheritance**: Heart button tooltip now uses `Segoe UI` explicitly instead of inheriting `Segoe MDL2 Assets` (was rendering squares).
- **Ko-fi icon rendering**: Fixed missing icon by using `Segoe MDL2 Assets` font for the icon character.
- **Add Game double-boxing**: Moved `+` button outside the input border.

---

## [1.2.0] - 2026-07-29

### Added

- **Achievement global rarity (rarity percentage)**: Each achievement card now shows the global percentage of players who have unlocked it. Data sourced from Steam's `ISteamUserStats013` API (`RequestGlobalAchievementPercentages` + `GetAchievementAchievedPercent`). Displayed with color coding: green (≥50%), yellow (10-50%), red (<10%).
- **Achievement search**: Search box to filter achievements by name or description.
- **Invert selection**: Button to quickly toggle the selection state of all achievements.
- **Achievement unlock date**: Each achievement card displays the unlock date/time when available.
- **Refresh achievements button**: Reload achievements without restarting the app.
- **Reset achievements button**: Reset all achievements back to locked state with double confirmation.
- **Refresh button in game picker**: Re-download the game list from gib.me without restarting the app.
- **Game type filters**: Filter games by type (games, demos, mods, junk) using checkboxes in the toolbar. Game type is retrieved from Steam API via `GetAppData(appId, "type")`.
- **Add Game by App ID**: Input field + button to manually add any owned game by its App ID. Validates ownership via `IsSubscribedApp()` before adding.
- **Welcome dialog**: First-run dialog showing "What's New" changelog. Reappears after each update.
- **Auto-updater**: Checks GitHub releases for new versions. Shows update dialog with progress bar and Skip/Update options.
- **Hamburger menu**: Title bar menu with About & Credits, Check for Updates, Ko-fi, and GitHub Sponsors options.
- **Heart in status bar**: Click the heart in the status bar to open Ko-fi donation page.

### Fixed

- **Achievement card click not working**: `MouseLeftButtonUp` event was on the inner status indicator border (28×28px) instead of the outer `CardBorder` — only clicking the small circle worked, not the whole card. Fixed by moving the handler to `CardBorder` so the entire card is clickable.
- **Progress circle and percentage text showing 0.0%**: `CompletionPercentage` property was never implemented in `GameManagerViewModel` — bindings in `GameManagerView.xaml` referenced a non-existent property. Added `PercentToArcMultiConverter` and `PercentToTextMultiConverter` (both using `CultureInfo.InvariantCulture` to avoid locale decimal separator issues) with `MultiBinding` to `UnlockedCount` and `TotalCount`.
- **Cover image not loading in game manager header**: Helper process (`--game <appId>`) created `GameInfo` with only `AppId` and `Name`, missing `CoverUrl`. Launcher had it but helper didn't. Fixed by setting `CoverUrl` on `GameInfo` in helper mode using the same CDN pattern as `SteamGameLibraryService`. Banner now uses `UrlToCachedImageConverter` with `ImageLoaded` event to update the `BitmapImage` asynchronously.
- **Locale bug in `Geometry.Parse`**: `ProgressToArcConverter` used string interpolation with floating-point numbers — on Spanish locale systems the decimal separator is comma (`,`) instead of period (`.`), causing `Geometry.Parse` to throw. Fixed by using `string.Format(CultureInfo.InvariantCulture, ...)` in both new converters.

### Known Limitations

- **Closing app during Smart Unlock cancels operation without rollback**: Hard-closing the app (X button, Alt+F4, or terminating the process) during a Smart Unlock operation prevents `StoreStats()` from executing — that method is called once in a `finally` block after the entire batch completes. A hard-close terminates the process before that call runs, so achievement changes are discarded and never persisted. Re-running Smart Unlock is safe since nothing was persisted.

## [1.1.0] - 2026-07-27

### Fixed

- **Protected achievement validation**: Achievements where `(Permission & 3) != 0` are now blocked from modification. Validation enforced at two layers: `SteamAchievements.SetAchievement/ClearAchievement` (service, business rule) and `GameManagerViewModel` (UX, user-facing messages). Handles three failure cases: schema load failure, individual achievement not found in schema, and confirmed protected status.
- **README Smart Unlock claim corrected**: Text now accurately reflects Smart Unlock is fully implemented and available in the UI.
- **PLAN.md SmartUnlockService status updated**: Marked as "UI integration pending" instead of fully implemented.

### Added

- **`GameSchemaService`** (ported from SAM): Reads `UserGameStatsSchema_{appId}.bin` from Steam's appcache. Extracts `Permission` flags for protected achievement detection. Attribution to SAM/Gibbed in ATTRIBUTIONS.md. KeyValue parser limitation (nested `Type.None` nodes) documented in DEVELOPMENT.md — validated against 355 real Steam schemas with zero real-world impact.
- **`PermissionVerified` field on `AchievementInfo`**: Distinguishes "confirmed unprotected" from "could not verify" — prevents silent fallback to unprotected behavior when schema match fails.
- **`SmartUnlockResult` record**: `UnlockAchievementsAsync` and `LockAchievementsAsync` now return `(Applied, Protected, Failed)` counts instead of ignoring results.

### Changed

- **`SetAchievement(name)` / `ClearAchievement(name)` removed**: Only overloads with `permission` parameter remain — no unvalidated code path exists.
- **`ISmartUnlockService` interface updated**: Methods now accept `(string Id, int Permission)` tuples and return `SmartUnlockResult`.

## [1.0.0] - 2026-07-25

### Added

- **Multi-selection for achievements**: Checkbox on each achievement card to select/deselect. Select All and Deselect buttons added.
- **Lock All / Unlock All with selection**: Lock/Unlock buttons now operate on selected achievements if any are selected, otherwise on all achievements.
- **Stats Editor**: Expandable panel to view and edit game stats. Includes predefined stats for popular games (TF2, CS2, Dota 2, Rust, etc.) and custom stat name entry.
- **Achievement filters**: Filter buttons (All, Unlocked, Locked, Hidden) with visual indicator for active filter.
- **Favorites**: Star button on game cards to mark games as favorites. Favorites persist across sessions and appear first in the game list.
- **Recent games ordering**: Games are ordered by: favorites first, then recently opened (up to 10), then alphabetically. Recent list persists across sessions.
- **Achievement icon refresh**: Icons update automatically when toggling between locked/unlocked states.

### Changed

- **Single exe, multi-process**: `SteamManager.exe` behaves differently based on command-line arguments:
  - No args: **Launcher mode** — initializes Steam with Spacewar AppId (480), shows game list
  - `--game <appId>`: **Helper mode** — initializes Steam with specific game AppId, shows achievements
- **Launcher stays open**: When helper opens, launcher window stays visible. When helper closes, launcher refreshes and continues.
- **Steam initialization order**: Steam now initializes completely before loading games list, ensuring proper game ordering on startup.

### Known Issues

None.

---

## [0.3.0] - 2026-07-25

### Fixed

- **Multi-process architecture now working**: Helper window now appears and displays correct achievements for each game. Previous attempts failed because `MainWindow` constructor was overwriting the passed DataContext.
- **Achievement icons**: Icons now load from Steam CDN when local handle is unavailable. Added `IconUrl` and `IconLockedUrl` properties to `AchievementInfo` for CDN-based icon loading.
- **Achievement icon refresh**: Icons update correctly when toggling between locked/unlocked states.
- **Back button**: Closes helper without affecting launcher. Launcher stays open while helper runs.
- **Navigation**: Multi-instance issue resolved. Only one launcher process runs; helper opens independently.

### Changed

- **Single exe, multi-process**: `SteamManager.exe` behaves differently based on command-line arguments:
  - No args: **Launcher mode** — initializes Steam with Spacewar AppId (480), shows game list
  - `--game <appId>`: **Helper mode** — initializes Steam with specific game AppId, shows achievements
- **`GamePickerViewModel.SelectGameCommand`**: Now launches helper process and awaits its exit. Launcher stays open.
- **`MainViewModel` navigation**: `NavigateToGame()` callback pattern removed in favor of multi-process
- **`MainWindow.xaml.cs`**: Constructor only sets DataContext from Services if DataContext is null (allows override)

### Known Issues

| Issue | Status |
|-------|--------|
| Loading status stays forever | Pending fix → ✅ Resolved in v1.0.0 |

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

- **Steam API approach**: Switched from `steam_api64.dll` (Steamworks SDK, distributed with games) to `steamclient.dll` (internal Steam client library, ships with every Steam installation). See [ADR-004](ARCHITECTURE.md#adr-004-use-steamclientdll-instead-of-steam_api64dll).
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
