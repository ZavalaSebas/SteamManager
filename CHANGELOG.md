# Changelog

All notable changes to SteamManager will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
