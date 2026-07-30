# SteamManager — Architecture

This document records the architectural decisions, design rationale, and historical context of the SteamManager project. It describes what the system **is** and **why** it was built this way.

For development workflow and operational rules, see [DEVELOPMENT.md](DEVELOPMENT.md).

---

## 1. Architecture Overview

SteamManager is a modern rewrite of [Gibbed's Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager), originally built in 2008 with .NET Framework and Windows Forms. The original uses reverse-engineered access to Steam's internal `steamclient.dll`, has two separate executables, a broken image loading system, and a UI that hasn't aged well.

SteamManager replaces it with:
- **.NET 10 + WPF + WPFUI** — modern, GPU-accelerated UI with virtualization
- **`steamclient.dll`** — the same internal Steam library used by the original SAM, loaded from the user's Steam installation via Windows Registry
- **Single executable** — portable, no installation, no dependencies
- **MVVM architecture** — clean separation of concerns with CommunityToolkit.Mvvm
- **Smart unlock** — anti-detection delays to protect user accounts

```
┌─────────────────────────────────────────────────┐
│                   UI (WPF + WPFUI)              │
│  MainWindow  ·  GamePickerView  ·  ManagerView  │
│  ViewModels (MVVM with CommunityToolkit)        │
├─────────────────────────────────────────────────┤
│              Services (Business Logic)          │
│  SmartUnlockService  ·  ImageCacheService       │
│  SteamGameLibraryService  ·  ConfigService      │
├─────────────────────────────────────────────────┤
│              Steam API Layer                    │
│  SteamClient  ·  SteamAchievements              │
│  SteamStats   ·  SteamApps  ·  SteamIcons       │
├─────────────────────────────────────────────────┤
│           Native Interop Layer                  │
│  NativeWrapper.cs — vtable extraction           │
│  SteamLoader.cs — DLL loading from registry     │
│  steamclient.dll (from Steam installation)      │
└─────────────────────────────────────────────────┘
```

---

## 2. Key Design Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Steam API | `steamclient.dll` (internal Steam library) | Same approach as original SAM, fully portable, no external DLLs needed |
| UI Framework | WPF + WPFUI | Modern look, single exe, GPU-accelerated |
| MVVM | CommunityToolkit.Mvvm | Source generators, minimal boilerplate |
| Image format | PNG/JPG | Native WPF support, no extra libraries |
| Persistence | JSON in `%LocalAppData%` | Simple, no database needed |
| Target | .NET 10 | Latest, stable, good WPF support |

### What we rejected

| Rejected | Why |
|----------|-----|
| `steam_api64.dll` (Steamworks SDK) | Not included with Steam, requires external DLL, distribution issues, licensing concerns |
| WinUI 3 | Packaging complexity, MSIX requirement breaks portability |
| Avalonia | Cross-platform unnecessary (Steam API is Windows-only) |
| MVVM frameworks (Prism, etc.) | Overkill, CommunityToolkit is enough |
| WebP images | WPF doesn't support it natively, PNG works fine |

---

## 3. Steam API Integration

### How it works

SteamManager uses `steamclient.dll` — the internal Steam client library that comes with every Steam installation. This is the same approach used by the original SAM. The DLL is loaded dynamically at runtime from the user's Steam installation directory (found via Windows Registry).

No wrapper libraries, no NuGet packages for Steam — just raw interop via vtable/COM-style calls.

### Initialization sequence

```
1. Environment.SetEnvironmentVariable("SteamAppId", appId.ToString())
2. Read Steam install path from registry: HKLM\Software\Valve\Steam\InstallPath
3. SetDllDirectory(steamPath + ";" + steamPath + "\bin")
4. LoadLibraryEx(steamPath + "\steamclient.dll")
5. Resolve 3 exports: CreateInterface, Steam_BGetCallback, Steam_FreeLastCallback
6. CreateInterface("SteamClient018") → root client object
7. CreateSteamPipe() → IPC pipe to Steam
8. ConnectToGlobalUser(pipe) → connect to logged-in user
9. GetISteamUserStats013(user, pipe) → stats interface
10. RequestUserStats() → request stats from server
11. Wait for UserStatsReceived_t callback
12. Ready to read achievements and stats
```

### Native interop layer (`SteamLoader.cs` + `NativeWrapper.cs`)

**SteamLoader.cs** handles:
- Reading Steam install path from Windows Registry
- Loading `steamclient.dll` via `LoadLibraryEx`
- Resolving 3 exported functions via `GetProcAddress`
- Setting DLL search directories

**NativeWrapper.cs** handles:
- Extracting vtable from COM-style C++ objects
- Converting vtable function pointers to callable .NET delegates
- String marshaling (UTF-8 managed ↔ native)

### Interface objects

Each Steam interface is represented as a struct of `IntPtr` fields (vtable slots):

| Interface | Version | Purpose |
|-----------|---------|---------|
| `ISteamClient018` | 018 | Root client, pipe/user management |
| `ISteamUserStats013` | 013 | Achievement and stat operations |
| `ISteamApps008` | 008 | Game ownership checks |
| `ISteamUtils005` | 005 | Image decoding for icons |

### Callbacks

| Callback | ID | When |
|----------|-----|------|
| `UserStatsReceived_t` | 1101 | Stats loaded from server |
| `UserStatsStored_t` | 1102 | Stats saved to server |
| `UserAchievementStored_t` | 1103 | Individual achievement saved |
| `UserAchievementIconFetched_t` | 1109 | Achievement icon image ready |

Callbacks are dispatched via `Steam_BGetCallback` polling. The callback timer fires on the UI thread, matching `CallbackMessage.Id` against registered `ICallback` implementations.

---

## 4. Architecture Decision Records (ADRs)

### ADR-001: Use `steam_api64.dll` instead of `steamclient.dll`

**Status:** Deprecated — superseded by [ADR-004](#adr-004-use-steamclientdll-instead-of-steam_api64dll)

**Context:**
The original SAM uses `steamclient.dll` which is reverse-engineered and breaks with Steam updates.

**Decision:**
Use `steam_api64.dll` from the official Steamworks SDK.

**Consequences:**
- ✅ Stable, documented, supported by Valve
- ✅ No reverse engineering required
- ❌ Only one AppID per process (cannot idle multiple games simultaneously)
- ❌ Some advanced features unavailable (e.g., internal Steam state)

**Reason for deprecation:** `steam_api64.dll` is not included with Steam installation. It's distributed with individual games as part of the Steamworks SDK. This creates distribution problems — the app crashes if the DLL is not present, and we cannot legally bundle it.

---

### ADR-002: WPF + WPFUI instead of WinUI 3 or Avalonia

**Status:** Accepted

**Context:**
Need a modern UI framework for Windows desktop.

**Decision:**
Use WPF with WPFUI for modern styling.

**Consequences:**
- ✅ Mature, well-documented, large ecosystem
- ✅ Single-file publishing works
- ✅ WPFUI provides modern Fluent design
- ❌ No cross-platform (but Steam API is Windows-only anyway)
- ❌ XAML can be verbose

---

### ADR-003: Single executable instead of installer

**Status:** Accepted

**Context:**
Users want portability and easy distribution.

**Decision:**
Publish as self-contained single-file executable.

**Consequences:**
- ✅ No installation required
- ✅ Easy to distribute (one .exe file)
- ✅ Portable (can run from USB)
- ❌ Larger file size (~60-80MB with runtime)
- ❌ No automatic updates (need custom implementation)

---

### ADR-004: Use `steamclient.dll` instead of `steam_api64.dll`

**Status:** Accepted — supersedes [ADR-001](#adr-001-use-steam_api64dll-instead-of-steamclientdll)

**Context:**
During early development we discovered that `steam_api64.dll` (Steamworks SDK) is NOT included with the Steam installation. It's distributed with individual games as part of the Steamworks SDK, and each game ships its own copy in its installation directory. This creates several critical problems:

1. **Runtime failure**: The app crashes with `DllNotFoundException` if `steam_api64.dll` is not in the application directory or system PATH
2. **Distribution problem**: We cannot legally bundle `steam_api64.dll` (Valve's license restricts redistribution)
3. **User friction**: Users would need to manually find and copy the DLL from some game installation
4. **Inconsistency with original SAM**: The original project uses `steamclient.dll` which IS always available

**Decision:**
Switch to `steamclient.dll` — the internal Steam client library that ships with every Steam installation. This is the exact same approach used by the original SAM project (proven by 15+ years of use).

**Technical approach:**
1. Find Steam install path via Windows Registry (`HKLM\Software\Valve\Steam\InstallPath`)
2. Load `steamclient.dll` via `LoadLibraryEx` with `LOAD_WITH_ALTERED_SEARCH_PATH`
3. Resolve 3 exported functions: `CreateInterface`, `Steam_BGetCallback`, `Steam_FreeLastCallback`
4. Create interface objects via `CreateInterface("SteamClient018")`
5. Call methods via vtable/COM-style `CallingConvention.ThisCall`

**Consequences:**
- ✅ Fully portable — no external DLLs needed
- ✅ No distribution/licensing issues
- ✅ Same proven approach as the original SAM
- ✅ Zero configuration — finds Steam automatically
- ✅ Release size stays small (~55KB vs ~60MB with bundled DLL)
- ❌ Uses internal Steam API (could break with Steam updates)
- ❌ More complex interop code (vtable extraction vs simple P/Invoke)
- ❌ Requires Windows Registry access (standard for Windows apps)

**Files affected:**
- `SteamNative.cs` → deleted (P/Invoke for steam_api64.dll, obsolete)
- `SteamCallbackIds.cs` → deleted (absorbed into `SteamCallbacks.cs`)
- `SteamLoader.cs` → new — loads steamclient.dll from registry
- `NativeMethods.cs` → new — `LoadLibraryExW`, `GetProcAddress`, `SetDllDirectoryW` P/Invoke
- `NativeStrings.cs` → new — UTF-8 marshaling utilities
- `NativeWrapper.cs` → new — generic vtable extraction base class
- `ISteamClient018.cs` → new — vtable struct + wrapper class
- `ISteamUserStats013.cs` → new — vtable struct + wrapper class
- `ISteamApps008.cs` → new — vtable struct + wrapper class
- `ISteamUtils005.cs` → new — vtable struct + wrapper class
- `SteamCallbacks.cs` → new — callback message envelopes + `EResult` enum
- `SteamClient.cs` → updated — initialization sequence now gets `ISteamUtils` first for AppId verification
- `SteamCallbackHandler.cs` → updated — uses `Steam_BGetCallback` polling instead of `SteamAPI_RunCallbacks`

---

### ADR-005: Port KeyValue binary parser from SAM vs. reimplement from scratch

**Status:** Accepted

**Context:**
SteamManager needed to read `UserGameStatsSchema_{appId}.bin` files to extract `Permission` flags for protected achievements. A binary Key-Value format parser was required.

**Decision:**
Port the KeyValue parser from Gibbed's SAM (zlib license, GPL-compatible) instead of writing from scratch.

**Reasons:**
- SAM's parser is proven: 15+ years of use, handles all real Steam schema variants
- Tested against 355 real schemas during development — zero failures
- `Permission` field extraction verified on real games (1134700, 1203220, etc.)
- zlib license requires attribution only (see ATTRIBUTIONS.md)
- Reimplementing would introduce bugs in an obscure binary format with no test vectors

**Consequences:**
- ✅ Proven correct parser, no R&D needed
- ✅ Attribution in ATTRIBUTIONS.md satisfies zlib requirements
- ✅ Same approach as original SAM (which works correctly)
- ⚠️ One known limitation: nested `Type.None` parent→child not handled (see Known Limitations in DEVELOPMENT.md)

---

### ADR-006: SmartUnlockService — fully implemented (core + UI)

**Status:** Complete

**Context:**
`SmartUnlockService` provides anti-detection delays between unlock/lock operations. The core logic was implemented during the protected achievements work. In v1.1.0, the full UI was connected.

**Decision:**
Implement both the core service and its UI integration:
- `SmartUnlockDialog`: configure delay range (seconds), shows overlay option
- `ProgressOverlay`: real-time progress with cancel support
- `SmartUnlockResultDialog`: applied/protected/failed counts, auto-dismiss on clean run
- Dropdown toolbar button with "Unlock All" / "Smart Unlock..." options
- Full orchestration via `GameManagerViewModel` with `CancellationToken` handling

**Consequences:**
- ✅ Core smart unlock logic is tested and correct
- ✅ UI fully integrated and functional
- ⚠️ Smoke test requires manual verification with Steam running (steamclient.dll not available in test environment) — documented in Known Limitations (DEVELOPMENT.md)

**See:** `SmartUnlockService.cs`, `ISmartUnlockService.cs`, `SmartUnlockResult`, `Dialogs/`

---

## 5. Protected Achievement Validation

Steam schemas can mark achievements as protected via a `Permission` field in `UserGameStatsSchema_{appId}.bin`. SteamManager validates protection at two layers:

**Layer 1 — Service (`SteamAchievements.cs`)**
`SetAchievement(name, permission)` and `ClearAchievement(name, permission)` check `(permission & 3) != 0` before calling UserStats. Returns `false` if protected. This is the business rule enforcement.

**Layer 2 — UX (`GameManagerViewModel.cs`)**
`ToggleAchievement`, `LockAll`, and `UnlockAll` check `_schemaLoadFailed`, `IsProtected`, and `IsUnverified` before any Steam call. Shows clear user-facing messages for each case:
- Schema load failed: "Could not verify achievement protection status - schema not loaded"
- Achievement unverified (ApiName not found in schema): "Could not verify protection status for '{name}' - skipping"
- Achievement protected: "Achievement '{name}' is protected and cannot be modified"

**Schema loading (`GameSchemaService.cs`)**
Loads `UserGameStatsSchema_{appId}.bin` from Steam's appcache via the ported KeyValue binary parser. Matches achievements by `ApiName`. Sets `PermissionVerified = true` on match, `false` on miss. Logs unmatched achievements via `ILogger`.

---

## 6. Caching System

All cache files stored in `%LocalAppData%\SteamManager\`:

| Path | Content | TTL |
|------|---------|-----|
| `cache/images/` | Achievement icons and game covers (PNG) | 7 days |
| `config.json` | User preferences, favorites, theme | Never |

### Image cache flow

```
GetOrDownloadAsync(url):
  1. Compute filename from URL hash
  2. Check cache directory for file
  3. If exists and < 7 days old → return cached
  4. If not → download, save to cache, return
```

---

## 7. Auto-Update System

Implemented in **v1.2.0**.

### How it works

1. On launch, `MainViewModel` calls `Updater.CheckForUpdateAsync()`
2. Hits `https://api.github.com/repos/ZavalaSebas/SteamManager/releases/latest`
3. Compares remote tag version vs local `AssemblyVersion`
4. If newer, finds the first `.exe` asset and returns download URL
5. `UpdateWindow` shows progress, calls `Updater.DownloadAndApplyUpdateAsync()`
6. Download swaps: `SteamManager.exe` → `SteamManager.exe.old`, new → `SteamManager.exe`, starts new process, exits
7. On next launch, `Updater.CleanupOldExe()` deletes the `.old` file

### Requirements
- `NetworkHelper` **must** set `User-Agent` header — GitHub API returns 403 without it
- HTTP client timeout: 10 seconds
- Assembly version must match csproj `<Version>` or update check compares wrong values

### Implementation
- `Services/Updater.cs` — update check, download, swap, cleanup
- `Services/NetworkHelper.cs` — HTTP client with User-Agent, JSON fetching
- `Views/UpdateWindow.xaml` — download progress UI
- Config keys: `GitHubApiUrl`, `RequestTimeout`

### Welcome Dialog

Added in **v1.2.0**. A per-version "What's New" dialog shown on first launch of a new version:

1. App reads `ConfigService.Settings.WelcomeShownVersion`
2. If different from `Config.AssemblyVersion`, show `WelcomeWindow`
3. User clicks "Continue", save current version to settings

---

## 8. Research Notes: IsSubscribedApp and Family Sharing

### How IsSubscribedApp behaves with Family Sharing

`IsSubscribedApp(appId)` returns ownership status based on the active Steam session. Testing across multiple game types reveals:

| Game type | Example | IsSubscribedApp result | Notes |
|-----------|---------|----------------------|-------|
| Direct purchase | Portal (440) | **True** | License owned by account |
| Family Shared — exists in BOTH accounts | Half-Life 2 (220), Dark Souls II (335300), Dark Souls III (374320) | **True** | Game is on both accounts, so API sees ownership on the active session |
| Family Shared — ONLY on lending account | 100% Orange Juice (282280) | **False** | Active account has no direct license; lending account's license is not exposed via this API |
| Key activation (non-Store) | Halo Spartan Assault (391659) | **False** | Likely registered as different license type not detected by `IsSubscribedApp` |
| Non-Store purchase | RE4 Remake (2276120) | **False** | May be key-activated or missing from active session |

### Key implications

- `IsSubscribedApp` is **not** a reliable way to enumerate Family Shared games exclusively from the borrower side
- Games that appear in both accounts (lender + borrower) return `True` because the active session has a direct license
- Games exclusive to the lending account return `False` from the borrower's session — this is why SteamManager shows fewer games than the full family library
- SAM also uses `IsSubscribedApp()` and shows more games because it iterates a larger set of appIds — some indirectly owned games happen to return `True` in that particular Steam session state

### What this means for SteamManager

- SteamManager's library list is **correct** for the current Steam session state
- Adding family shared games to the displayed list would require detecting and including games exclusively on lending accounts, which `IsSubscribedApp` does not expose reliably
- `IsSubscribedFromFamilySharing(appId)` exists but returns `False` for games only on the lending account (since the borrower has no shared license record in this session)

This is an API limitation, not a bug in SteamManager. The library correctly reflects what Steam's internal API reports for the current session.

---

## 9. Development Patterns

### Dependency Injection

Use `Microsoft.Extensions.DependencyInjection` for service management. Register services in `App.xaml.cs`:

```csharp
var services = new ServiceCollection();
services.AddSingleton<SteamClient>();
services.AddSingleton<SmartUnlockService>();
services.AddSingleton<ConfigService>();
services.AddSingleton<ImageCacheService>();
ServiceProvider = services.BuildServiceProvider();
```

**Benefits**: Testable, loose coupling, clear service lifetimes.

### Async/Await Patterns

All Steam API calls should be async. Use `Task<T>` return types:

```csharp
public async Task<bool> SetAchievementAsync(string name);
public async Task<List<AchievementInfo>> GetAchievementsAsync();
```

### Threading Model

Steam API callbacks arrive on a background thread. Marshal to UI thread using WPFUI's dispatcher:

```csharp
_dispatcherQueue.TryEnqueue(() =>
{
    Achievements = new ObservableCollection<AchievementInfo>(achievements);
});
```

### Error Handling

Use structured error handling:

```csharp
// Option 1: Try-catch for expected errors
try
{
    await _steamClient.SetAchievementAsync("achievement_name");
}
catch (SteamApiException ex)
{
    _logger.LogError(ex, "Failed to set achievement");
    ShowErrorNotification($"Failed: {ex.Message}");
}

// Option 2: Result type for user-facing errors
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
}
```

### Logging

Use `Microsoft.Extensions.Logging` for all services:

```csharp
public SmartUnlockService(ILogger<SmartUnlockService> logger)
{
    _logger = logger;
}

_logger.LogInformation("Starting smart unlock for {Count} achievements", achievements.Count);
_logger.LogWarning("Achievement {Name} already unlocked, skipping", name);
_logger.LogError(ex, "Failed to unlock achievement {Name}", name);
```

### Steam API Testing

Steam API cannot be mocked directly. Use interfaces:

```csharp
public interface ISteamAchievements
{
    Task<bool> SetAsync(string name);
    Task<List<AchievementInfo>> GetAllAsync();
}

// Real implementation uses vtable calls via NativeWrapper
public class SteamAchievements : ISteamAchievements { }

// Test implementation returns predefined data
public class FakeSteamAchievements : ISteamAchievements { }
```

### Code Quality

Code style is enforced through the project's coding standards and conventions documented in DEVELOPMENT.md. Consistent naming, file organization, and async patterns are described throughout.

### View Navigation

Navigation is handled via `MainViewModel.CurrentViewModel` — swap the ViewModel and WPF's DataTemplates render the appropriate View:

```csharp
public void SelectGame(GameInfo game)
{
    CurrentViewModel = new GameManagerViewModel(game);
}
```

WPF automatically renders the correct View via `DataTemplate` mappings in `App.xaml`.

### Key Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `WPF-UI` | 3.0.5 | Modern UI controls and theming |
| `CommunityToolkit.Mvvm` | 8.4.0 | MVVM source generators |
| `Microsoft.Extensions.DependencyInjection` | (add) | Service management |
| `Microsoft.Extensions.Logging` | (add) | Structured logging |
