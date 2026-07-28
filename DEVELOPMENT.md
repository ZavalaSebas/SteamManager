# SteamManager — Project Guide

This document serves as a guide to this specific project AND as a reference for the architecture, workflow, and decisions made during planning.

## Why SteamManager?

SteamManager is a modern rewrite of [Gibbed's Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager), originally built in 2008 with .NET Framework and Windows Forms. The original uses reverse-engineered access to Steam's internal `steamclient.dll`, has two separate executables, a broken image loading system, and a UI that hasn't aged well.

SteamManager replaces it with:
- **.NET 10 + WPF + WPFUI** — modern, GPU-accelerated UI with virtualization
- **`steamclient.dll`** — the same internal Steam library used by the original SAM, loaded from the user's Steam installation via Windows Registry
- **Single executable** — portable, no installation, no dependencies
- **MVVM architecture** — clean separation of concerns with CommunityToolkit.Mvvm
- **Smart unlock** — anti-detection delays to protect user accounts

## Architecture Overview

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

### Key Design Decisions

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

## Project Structure

```
SteamManager/
├── SteamManager.slnx                    # Solution file (.slnx format)
├── SteamManager/                         # Main WPF application
│   ├── SteamManager.csproj              # Version, target framework, packages
│   ├── App.xaml / App.xaml.cs         # Application entry, theme setup
│   ├── Config.cs                      # Centralized constants (URLs, paths, timeouts)
│   ├── Steam/                         # Steam API integration layer
│   │   ├── SteamLoader.cs            # DLL loading from registry
│   │   ├── NativeWrapper.cs          # Vtable extraction and native calls
│   │   ├── SteamClient.cs            # Init, Shutdown, RunCallbacks
│   │   ├── SteamAchievements.cs      # Achievement read/write
│   │   ├── SteamStats.cs             # Stats read/write
│   │   ├── SteamApps.cs              # Game library, ownership
│   │   ├── SteamIcons.cs             # Icon download and caching
│   │   ├── SteamCallbackHandler.cs   # Callback system
│   │   └── SteamContext.cs           # Session state model
│   ├── Models/                        # Data models
│   ├── ViewModels/                    # MVVM ViewModels
│   ├── Views/                         # WPF Views
│   ├── Controls/                      # Custom controls
│   ├── Services/                      # Business logic
│   ├── Converters/                    # Value converters
│   └── Resources/                     # Styles (Styles.xaml)
├── SteamManager.Tests/                  # xUnit test project
├── .github/workflows/release.yml      # CI/CD pipeline
├── README.md
├── DEVELOPMENT.md                     # This file
├── CHANGELOG.md
└── LICENSE                            # GPL v3
```

## Steam API Integration

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

## Version Management

**Single source of truth**: `<Version>` in `SteamManager/SteamManager.csproj`

```xml
<Version>1.0.0</Version>
<AssemblyVersion>$(Version).0</AssemblyVersion>
```

- `AssemblyVersion` derives from `$(Version)` so assembly version is correct (e.g., `1.0.0.0`)
- The Updater (future) will compare local vs remote version using `Version.TryParse`

**To bump the version**: edit `<Version>` in the csproj, commit with a descriptive message, push to `main`.

### Constants Pattern (`Config.cs`)

**Prefer keeping constants centralized** in a dedicated `Config.cs` file rather than scattered across classes. This includes URLs, paths, timeouts, and other magic values.

```csharp
// Example structure (to be implemented)
public static class Config
{
    public const string GitHubApiUrl = "https://api.github.com/repos/ZavalaSebas/SteamManager/releases/latest";
    public const int RequestTimeoutSeconds = 10;
    public const string CachePath = "%LocalAppData%\\SteamManager";
    // ... etc
}
```

> **Why this matters**: Centralizing constants prevents typos, makes values easy to find/change, and follows the pattern validated in OrbSpoofer.

### Welcome Sentinel (v2.0)

> **Remember to implement this when building the settings/config system.**

A per-version flag that shows a "What's New" dialog on first launch of a new version. OrbSpoofer uses this pattern successfully:

1. App reads `App.Settings.WelcomeShownVersion`
2. If different from `Config.AssemblyVersion`, show `WelcomeWindow`
3. User clicks "Continue", save current version to settings

This is a nice UX touch that helps users discover new features. Defer implementation until the config system is in place.

## Semantic Versioning (SemVer)

> **Always follow SemVer for version numbers.**

Format: `MAJOR.MINOR.PATCH`

```
MAJOR.MINOR.PATCH
  │     │     │
  │     │     └── Fixes, bugs, security patches
  │     └──────── New features (backwards compatible)
  └────────────── Breaking changes (incompatible with previous)
```

### When to bump

| Change Type | Bump | Example |
|-------------|------|---------|
| Fix bug | PATCH | `0.1.0` → `0.1.1` |
| New feature | MINOR | `0.1.1` → `0.2.0` |
| Breaking change | MAJOR | `0.2.0` → `1.0.0` |
| Pre-release | Suffix | `1.0.0-beta.1` |

### Rules

1. **Start at 0.x.y** — while in development, MAJOR is 0
2. **Once 1.0.0** — public API is stable
3. **Never reuse versions** — if you delete a release, don't reuse that version number
4. **Update CHANGELOG.md** — document what changed in each version

## Release Process (CI/CD)

> **DISABLED for development** — workflow only runs on manual trigger (`workflow_dispatch`). Re-enable push/PR triggers in `.github/workflows/release.yml` when ready for production.

On push to `main`, `.github/workflows/release.yml` runs:

1. **Check version change** — compares `<Version>` in HEAD vs HEAD~1
2. **Build** — `dotnet build SteamManager.slnx -c Release`
3. **Release** (only if version changed):
   - `dotnet publish` as self-contained single-file
   - Generate body from commit message
   - Create tag + release with `.exe`

### Critical workflow details
- `fetch-depth: 0` — required so `git show HEAD~1:path` can access the parent commit
- `permissions: contents: write` — required for `softprops/action-gh-release`
- Csproj path: `SteamManager/SteamManager.csproj`
- Release body comes from the **commit body** — write it with `### Added/Fixed/Changed` sections

## Solution File (.slnx)

```xml
<Solution>
  <Project Path="SteamManager/SteamManager.csproj" />
  <Project Path="SteamManager.Tests/SteamManager.Tests.csproj" />
</Solution>
```

Benefits: human-readable, merge-friendly, no VS-generated garbage.

## Tests

Run locally with: `dotnet test SteamManager.slnx -c Release`

> **Note**: Tests require `<RuntimeIdentifier>win-x86</RuntimeIdentifier>` to run (both main and test projects). Locally tests pass. **CI cannot run tests** because GitHub Actions uses a 64-bit Windows runner and .NET 10 doesn't ship a 32-bit runtime - the test process fails to start with `hostfxr.dll` loading error. Tests are verified locally before each release.

### Planned test structure

Tests will follow the pattern validated in OrbSpoofer (21 xUnit tests). Categories and specific tests will be defined as we implement each feature:

| Category | Purpose | When to implement |
|----------|---------|-------------------|
| **SteamNativeTests** | Validate P/Invoke declarations compile correctly, test marshaling | Phase 2 (Steam API) |
| **SmartUnlockTests** | Verify delay logic, cancellation token handling, progress reporting | Phase 2 (Smart Unlock) |
| **AchievementTests** | Mock Steam API responses, verify model creation and state transitions | Phase 3 (Game Manager) |
| **ConfigTests** | Verify `AssemblyVersion` matches `<Version>` in csproj, test config persistence | Phase 1 (Foundation) |
| **ImageCacheTests** | Test cache hit/miss, TTL expiration, disk write/read | Phase 2 (Image Cache) |
| **IconDecoderTests** | Test RGBA to Bitmap conversion, handle 0 retry logic | Phase 2 (Image Cache) |

> **Note**: Add specific test names here as they are implemented. Follow the pattern: `MethodName_Condition_ExpectedResult` (e.g., `SetAchievement_ValidId_ReturnsTrue`).

### Test conventions

- One `[Fact]` per test method (no `[Theory]` unless data-driven)
- No test dependencies — each test is independent
- Arrange → Act → Assert pattern
- Test class name = Service/Class name + "Tests" (e.g., `SmartUnlockServiceTests`)
- Namespace mirrors source: `SteamManager.Tests.Services.SmartUnlockServiceTests`

## Caching System

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

## Auto-Update System (v2.0)

> NOT implemented in v1.0. Documented here for future development.

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

### Planned implementation
- `Services/Updater.cs` — update check, download, swap, cleanup
- `Services/NetworkHelper.cs` — HTTP client with User-Agent, JSON fetching
- `Views/UpdateWindow.xaml` — download progress UI
- Config keys: `GitHubApiUrl`, `RequestTimeout`

## GitHub Pages (v2.0)

> NOT implemented in v1.0. Documented here for future development.

### What it is

A landing page at `https://zavalasebas.github.io/SteamManager/` showing version, download link, and release info. Deployed automatically on push to `main`.

### Setup
- `docs/index.html` — landing page
- Repo Settings → Pages → Source: "GitHub Actions"
- CTA download button auto-updates version from GitHub Releases API

### Structure
```
docs/
├── index.html          # Landing page
├── sitemap.xml
├── og-image.svg        # Social preview
└── assets/
    ├── favicon.ico
    ├── logo.png
    └── screenshot.png
```

## Workflow Rules

**These are strict rules that must always be followed:**

1. **NEVER commit without verifying first** — always run `git status` and `git diff` before staging. Check what files changed, read the diffs, make sure no secrets, no debug code, no broken files are included.

2. **NEVER push without verifying first** — after committing, always run `git log --oneline -3` and `git diff HEAD~1` to confirm the commit looks correct before pushing.

3. **NEVER force push without explicit permission** — force push destroys history. Only use for cleanup of test commits, and only when the user says yes.

4. **NEVER commit secrets, API keys, or tokens** — check all staged files for hardcoded credentials before committing.

5. **Multiple commits are fine for progress**, but group them meaningfully. Don't push 50 tiny commits. Squash related work into logical commits with clear descriptions.

6. **Commit messages matter** — subject line ≤72 chars, body describes what was done and why. For version bumps, body becomes release notes with `### Added / Fixed / Changed` sections.

7. **Always build before pushing** — run `dotnet build SteamManager.slnx -c Release` and make sure there are no errors.

8. **Always test before pushing** — run `dotnet test SteamManager.slnx -c Release` and make sure all tests pass.

Good commit structure:
```
bump v0.2.0

### Added
- Smart unlock with random delays
- Image caching for achievement icons

### Fixed
- Achievement icon not loading on first try

### Changed
- Replaced WinForms ListView with WPF VirtualizingPanel
```

## Git Best Practices

> **Follow these conventions for clean git history.**

### Branch Naming

| Pattern | Purpose | Example |
|---------|---------|---------|
| `feat/feature-name` | New feature | `feat/smart-unlock` |
| `fix/bug-description` | Bug fix | `fix/achievement-icon` |
| `docs/topic` | Documentation | `docs/update-readme` |
| `refactor/what` | Code refactoring | `refactor/steam-client` |
| `test/what` | Adding tests | `test/smart-unlock` |
| `hotfix/vX.Y.Z` | Critical fix | `hotfix/v1.0.1` |

### Commit Atomicity

One logical change per commit:

```bash
# ✅ Good — one feature
git add SteamNative.cs
git commit -m "feat: add SteamNative P/Invoke declarations"

# ❌ Bad — unrelated changes mixed
git add SteamNative.cs MainWindow.xaml README.md
git commit -m "misc stuff"
```

### Commit Message Format

```
<type>: <description>

[optional body]
```

| Type | When to use |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting (no code change) |
| `refactor` | Code restructuring |
| `test` | Adding tests |
| `chore` | Build, CI, tooling |

### Workflow

```bash
# 1. Create feature branch
git checkout -b feat/my-feature

# 2. Work and commit atomically
git add file1.cs
git commit -m "feat: add service interface"

git add file2.cs
git commit -m "feat: implement service"

# 3. Push branch
git push -u origin feat/my-feature

# 4. Create PR (or merge directly if solo)

# 5. Delete branch after merge
git branch -d feat/my-feature
git push origin --delete feat/my-feature
```

### Never Do This

- ❌ Commit directly to `main` (always use branches)
- ❌ Force push shared branches
- ❌ Commit secrets or API keys
- ❌ Commit generated files (`bin/`, `obj/`, `.vs/`)
- ❌ Write essays in commit messages (keep it concise)

## Development Patterns

> **Follow these patterns when implementing features.** Documented before coding starts to ensure consistency.

### Dependency Injection

Use `Microsoft.Extensions.DependencyInjection` for service management. Register services in `App.xaml.cs`:

```csharp
// App.xaml.cs
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
// ✅ Correct
public async Task<bool> SetAchievementAsync(string name);
public async Task<List<AchievementInfo>> GetAchievementsAsync();

// ❌ Avoid
public bool SetAchievement(string name);  // Blocks UI thread
public void SetAchievement(string name);  // No way to report errors
```

### Threading Model

Steam API callbacks arrive on a background thread. Marshal to UI thread using WPFUI's dispatcher:

```csharp
// In ViewModel or Service
_dispatcherQueue.TryEnqueue(() =>
{
    // Update UI properties here
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
// Constructor injection
public SmartUnlockService(ILogger<SmartUnlockService> logger)
{
    _logger = logger;
}

// Usage
_logger.LogInformation("Starting smart unlock for {Count} achievements", achievements.Count);
_logger.LogWarning("Achievement {Name} already unlocked, skipping", name);
_logger.LogError(ex, "Failed to unlock achievement {Name}", name);
```

### Steam API Testing

Steam API cannot be mocked directly. Use interfaces:

```csharp
// Interface for testability
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

Code style is enforced through the project's coding standards and conventions documented in this file. Consistent naming, file organization, and async patterns are described throughout.

### View Navigation

Navigation is handled via `MainViewModel.CurrentViewModel` — swap the ViewModel and WPF's DataTemplates render the appropriate View:

```csharp
// MainViewModel
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

## Coding Standards

> **Follow these conventions for consistent code style.**

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `SmartUnlockService` |
| Interfaces | `I` prefix | `ISteamAchievements` |
| Methods | PascalCase | `SetAchievementAsync()` |
| Async methods | `Async` suffix | `GetAchievementsAsync()` |
| Properties | PascalCase | `IsUnlocked` |
| Private fields | `_camelCase` | `_logger` |
| Parameters | camelCase | `achievementName` |
| Local variables | camelCase | `unlockTime` |
| Constants | PascalCase | `MaxRetryCount` |
| Files | Match class name | `SmartUnlockService.cs` |

### File Organization

```csharp
// 1. Using directives
using System;
using System.Collections.Generic;

// 2. Namespace
namespace SteamManager.Services;

// 3. Class declaration
public class SmartUnlockService
{
    // 4. Private fields
    private readonly ILogger<SmartUnlockService> _logger;
    
    // 5. Constructor
    public SmartUnlockService(ILogger<SmartUnlockService> logger)
    {
        _logger = logger;
    }
    
    // 6. Public properties
    public bool IsRunning { get; private set; }
    
    // 7. Public methods
    public async Task UnlockAsync(List<AchievementInfo> achievements) { }
    
    // 8. Private methods
    private void DelayBetweenUnlocks() { }
}
```

## Development Environment Setup

> **For new contributors.** What you need to get started.

### Requirements

| Requirement | Version | Notes |
|-------------|---------|-------|
| OS | Windows 10/11 | Steam API is Windows-only |
| .NET SDK | 10.0 | `dotnet --version` to verify |
| IDE | Visual Studio 2022 17.10+ / Visual Studio 2026 / Rider / VS Code | .NET 10 requires VS 2022 17.10+ or VS 2026 |
| Steam | Installed | Required for testing |
| Git | Latest | For version control |

### First Steps

```bash
# 1. Clone the repo
git clone https://github.com/ZavalaSebas/SteamManager.git
cd SteamManager

# 2. Restore packages
dotnet restore

# 3. Build
dotnet build SteamManager.slnx -c Release

# 4. Run tests
dotnet test SteamManager.slnx -c Release

# 5. Run the app (requires Steam running)
dotnet run --project SteamManager/SteamManager.csproj
```

### IDE Setup

**Visual Studio 2022 (17.10+) or Visual Studio 2026:**
- Install ".NET desktop development" workload
- Install "WPF" component

**Rider:**
- Install "WPF" plugin (usually included)

**VS Code:**
- Install C# Dev Kit extension
- Install .NET Extension Pack

## Git Hooks

> **Automated validation before every commit.**

### Pre-commit Hook

Create `.git/hooks/pre-commit`:

```bash
#!/bin/sh
# Build
dotnet build SteamManager.slnx -c Release --no-restore
if [ $? -ne 0 ]; then
  echo "❌ Build failed. Commit aborted."
  exit 1
fi

# Test
dotnet test SteamManager.slnx -c Release --no-build
if [ $? -ne 0 ]; then
  echo "❌ Tests failed. Commit aborted."
  exit 1
fi

echo "✅ Build and tests passed."
```

### Setup

```bash
# Make hook executable (Git Bash on Windows)
chmod +x .git/hooks/pre-commit
```

## Architecture Decision Records (ADR)

> **Document why we made certain decisions.** Future contributors will thank you.

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

### Creating New ADRs

When making a significant decision, create a new file `docs/adr/004-decision-title.md`:

```markdown
# ADR-004: [Decision Title]

**Status:** Proposed | Accepted | Deprecated | Superseded by [ADR-XXX]

**Context:**
[What is the issue?]

**Decision:**
[What did we decide?]

**Consequences:**
- ✅ [Positive outcomes]
- ❌ [Negative outcomes]
```

### ADR-004: Use `steamclient.dll` instead of `steam_api64.dll`

**Status:** Accepted — supersedes [ADR-001](#adr-001-use-steam_api64dll-instead-of-steamclientdll)

**Context:**
During Phase 1 development, we discovered that `steam_api64.dll` (Steamworks SDK) is NOT included with the Steam installation. It's distributed with individual games as part of the Steamworks SDK, and each game ships its own copy in its installation directory. This creates several critical problems:

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
- ⚠️ One known limitation: nested `Type.None` parent→child not handled (see Known Limitations)

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
- ⚠️ Smoke test requires manual verification with Steam running (steamclient.dll not available in test environment) — documented in Known Limitations

**See:** `SmartUnlockService.cs`, `ISmartUnlockService.cs`, `SmartUnlockResult`, `Dialogs/`

## Protected Achievement Validation

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

## Release Checklist

> **Follow this checklist before every release.**

### Pre-release

- [ ] All features for this version are complete
- [ ] All tests pass locally
- [ ] No compiler warnings (or warnings documented)
- [ ] Code reviewed (if working with others)

### Version Bump

- [ ] Update `<Version>` in `SteamManager/SteamManager.csproj`
- [ ] Update `CHANGELOG.md` with new version and changes
- [ ] Commit with subject `bump vX.Y.Z — <short summary>` and body with `### Added / Fixed / Changed` sections (the commit body becomes the GitHub Release body)

### Commit & Push

- [ ] `git status` — no unexpected changes
- [ ] `git diff` — review all changes
- [ ] `git log --oneline -3` — verify commit history
- [ ] `git push origin main`

### Post-release

- [ ] Verify GitHub Actions workflow completed
- [ ] Check release page on GitHub
- [ ] Test downloaded .exe works
- [ ] Update documentation if needed

### Hotfix Process

If critical bug found after release:

1. Create branch `hotfix/vX.Y.Z`
2. Fix the bug
3. Bump patch version (e.g., `1.0.0` → `1.0.1`)
4. Commit and push
5. Run workflow manually
6. Merge back to `main`

## Key Files Quick Reference

| File | Purpose |
|------|---------|
| `SteamManager/SteamManager.csproj` | Version, target framework, NuGet packages, `RuntimeIdentifier=win-x86` |
| `SteamManager/Config.cs` | Centralized constants (SteamDll, registry keys, timeouts, AppIds, SteamCommunityUrl) |
| `SteamManager/Steam/SteamLoader.cs` | Loads steamclient.dll from registry, resolves `CreateInterface`/`GetCallback`/`FreeLastCallback` |
| `SteamManager/Steam/NativeMethods.cs` | `LoadLibraryExW`, `GetProcAddress` (ANSI), `SetDllDirectoryW` P/Invoke |
| `SteamManager/Steam/NativeStrings.cs` | UTF-8 marshaling utilities (`StringToStringHandle`, `PointerToString`) |
| `SteamManager/Steam/NativeWrapper.cs` | Generic vtable extraction base class; `GetFunction<T>()` + `Call<>()` |
| `SteamManager/Steam/ISteamClient018.cs` | vtable struct + `SteamClient018` wrapper (vtable indices 0–39, added `GetISteamUser`) |
| `SteamManager/Steam/ISteamUserStats013.cs` | vtable struct + `SteamUserStats013` wrapper (achievements/stats) |
| `SteamManager/Steam/ISteamUser012.cs` | vtable struct + `SteamUser012` wrapper (SteamID retrieval via `GetSteamId()`) |
| `SteamManager/Steam/ISteamApps008.cs` | vtable struct + `SteamApps008` wrapper (game ownership via `IsSubscribedApp()`) |
| `SteamManager/Steam/ISteamApps001.cs` | vtable struct + `SteamApps001` wrapper (app metadata via `GetAppData()`) |
| `SteamManager/Steam/ISteamUtils005.cs` | vtable struct + `SteamUtils005` wrapper (AppId, image RGBA) |
| `SteamManager/Steam/SteamClient.cs` | Steam API lifecycle (Init → GetPipe → ConnectUser → get interfaces); exposes `User` (SteamUser012) and `Apps001` (SteamApps001) |
| `SteamManager/Steam/SteamContext.cs` | Groups SteamClient + Achievements/Stats/Apps; exposes `SteamId` (user's SteamID64); runs callbacks on timer |
| `SteamManager/Steam/SteamAchievements.cs` | Achievement read/write via `SteamUserStats013` wrapper |
| `SteamManager/Steam/SteamStats.cs` | Stats read/write via `SteamUserStats013` wrapper |
| `SteamManager/Steam/SteamApps.cs` | App subscription check + `GetAppData()` via `SteamApps008` and `SteamApps001` wrappers |
| `SteamManager/Steam/SteamIcons.cs` | RGBA image decoding via `SteamUtils005.GetImageSize/GetImageRGBA` |
| `SteamManager/Steam/SteamCallbacks.cs` | `CallbackMessage` struct + `UserStatsReceived_t` etc. + `EResult` enum |
| `SteamManager/Steam/SteamCallbackHandler.cs` | Polls `Steam_BGetCallback` and dispatches to registered handlers |
| `SteamManager/Models/GameInfo.cs` | `AppId`, `Name`, `PlaytimeMinutes`, `CoverUrl`, `HeaderImageUrl`, `LogoUrl`, `ImgIconUrl`, `IsFavorite` |
| `SteamManager/Models/AchievementInfo.cs` | `Id`, `Name`, `Description`, `IsUnlocked`, `UnlockTime`, `Icon` |
| `SteamManager/Models/StatInfo.cs` | `Name`, `Type`, `Value`, `Min`, `Max`, `Permission` |
| `SteamManager/ViewModels/MainViewModel.cs` | Shell navigation + `CurrentViewModel` |
| `SteamManager/ViewModels/GamePickerViewModel.cs` | Game list, search, favorites, `LoadGamesCommand` |
| `SteamManager/ViewModels/GameManagerViewModel.cs` | Selected game achievements/stats management |
| `SteamManager/Views/GamePickerView.xaml/.cs` | Grid of game cards (UserControl, auto-mapped via DataTemplate) |
| `SteamManager/Views/GameManagerView.xaml/.cs` | Achievement list + stats editor (UserControl, auto-mapped via DataTemplate) |
| `SteamManager/Controls/GameCard.xaml/.cs` | Game card with cover image and name |
| `SteamManager/Controls/AchievementCard.xaml/.cs` | Achievement card with icon, name, unlock toggle |
| `SteamManager/Services/IGameLibraryService.cs` | Interface: `GetOwnedGamesAsync()` returns `List<GameInfo>` |
| `SteamManager/Services/SteamGameLibraryService.cs` | Downloads `games.xml` from `gib.me/sam/`, iterates appIds, calls `IsSubscribedApp()` + `GetAppData()` for owned games |
| `SteamManager/App.xaml` | WPF-UI theme (`Dark`) + DataTemplates for ViewModel→View mapping |
| `SteamManager/App.xaml.cs` | DI setup, async Steam init, callback DispatcherTimer |
| `SteamManager/MainWindow.xaml/.cs` | Shell window — `ContentControl` bound to `MainViewModel.CurrentViewModel` |
| `SteamManager.Tests/` | xUnit test project covering core models, converters, and services (verified locally, not CI) |
| `.github/workflows/release.yml` | CI/CD pipeline (`win-x86`, `workflow_dispatch` only) |
| `PLAN.md` | Full project plan with phases and features |
| `CHANGELOG.md` | Version history (v0.1.0 → v1.0.0 with full feature set) |

## Known Limitations

| Limitation | Reason | Workaround |
|------------|--------|------------|
| Single AppID per process | `steamclient.dll` limitation (same as original SAM) | Future: multi-process idling |
| No cross-platform | Steam API is Windows-only | None (by design) |
| No WebP support | WPF doesn't decode WebP natively | Use PNG/JPG from Steam CDN |
| Achievement icons async | Steam API returns handle 0 initially, fetches in background | Wait for `UserAchievementIconFetched_t` callback |
| No auto-update | Not implemented in v1.0 | Manual download from GitHub Releases |
| No GitHub Pages | Not implemented in v1.0 | README serves as documentation |
| Uses internal Steam API | `steamclient.dll` is not officially documented | Same approach as original SAM, proven stable |
| **32-bit (x86) platform only** | Steam ships only a 32-bit `steamclient.dll`; Windows cannot load a 32-bit DLL into a 64-bit process | Project targets `win-x86` (`<RuntimeIdentifier>win-x86</RuntimeIdentifier>`); publish native exe with `dotnet publish -r win-x86` |
| **vtable layouts byte-aligned to SAM** | `steamclient.dll` is a C++ object with a per-version vtable; padding/extra entries from one SDK version break ours | Vtable structs in `ISteam*.cs` are copied 1-to-1 from gibbed/SAM and must NOT be reordered or padded. See `SAM.API/Interfaces/` |
| **No playtime, no achievement details from library** | SAM approach (`games.xml` + `IsSubscribedApp`) only tells if user owns a game — no per-user data (playtime, achievements, stats) | Steam Web API `GetOwnedGames` returns playtime but requires API key. Alternative: parse `steamcommunity.com/profiles/{id}/games/?xml=1` with session cookies (requires login flow). See `SteamWebApiKey` constant in `Config.cs` for future implementation. |
| **KeyValue parser: nested Type.None nodes** | The binary KeyValue parser does not correctly handle `Type.None` parent nodes containing `Type.None` children — the inner termination marker is misinterpreted | Validated against 355 real schemas with zero impact (no real Steam schema uses this pattern). Risk is very low. See `KeyValue.ReadAsBinary()` and skipped test `ReadAsBinary_ParsesNestedKeyValue`. |
| **Stats editor not refreshed after ResetAllStats** | No observable stats collection exists in the ViewModel — `GetStat(name, out value)` reads one stat at a time, no callback updates UI | After reset, user must re-query each stat individually. Future: add an observable `Statistics` collection that gets updated when `RequestStats()` callback fires. |
| **Smart Unlock UI requires manual smoke test before release** | `steamclient.dll` is not available in the test environment; no automated smoke test can open the three Smart Unlock dialogs (SmartUnlockDialog, ProgressOverlay, SmartUnlockResultDialog) or verify the dropdown entry point | Before any release, verify manually with Steam running: (1) dropdown appears in game manager toolbar, (2) SmartUnlockDialog opens with correct defaults (15-45s), (3) Smart Unlock execution shows ProgressOverlay with live counters, (4) result dialog shows correct icon and auto-dismiss behavior. No automated CI coverage possible. |

## Known Issues & Resolutions

> **Document issues here as you find and fix them.** Include the symptoms, root cause, and how it was resolved. This helps future contributors avoid the same pitfalls.

| Issue | Root Cause | Resolution |
|-------|------------|------------|
| `DllNotFoundException: steam_api64.dll` | `steam_api64.dll` is not included with Steam installation — it's distributed with individual games | Switched to `steamclient.dll` approach (see ADR-004) |
| `steam_api64.dll` not found in Steam directory | Steam doesn't ship this DLL — it's a Steamworks SDK DLL for game developers | Same resolution as above |
| App hangs on "Connecting to Steam" | `InitializeSteam()` ran synchronously on the UI thread before `mainWindow.Show()` | Moved Steam init to `Task.Run` and only dispatch state updates back to UI thread (see `App.xaml.cs:InitializeSteamAsync`) |
| Blank/dark screen, WPFUI resources missing | `App.xaml` had no `<ui:ThemesDictionary>` / `<ui:ControlsDictionary>` | Added `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"` + merged WPF-UI theme dictionaries |
| ContentControl shows nothing (no game picker) | No `DataTemplate` mapping `GamePickerViewModel` → `GamePickerView`; WPF didn't know how to render the VM | Added `DataTemplate` entries for both view models in `App.xaml:Application.Resources` |
| `EntryPointNotFoundException: 'SetDllDirectory'` | `LibraryImport` does NOT auto-append `W`; entry point is `SetDllDirectoryW` (UTF-16) | Explicit `EntryPoint = "SetDllDirectoryW"` + `StringMarshalling.Utf16` on `NativeMethods.SetDllDirectory` |
| `LoadLibraryEx` returns 0 / `Win32 Error 0` | Steam's `steamclient.dll` is **32-bit** and the project was `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` | Changed main + test projects to `win-x86` / `<PlatformTarget>x86</PlatformTarget>` |
| `GetISteamUserStats` returns IntPtr.Zero | `ISteamClient018` vtable layout was guessed (GetISteamUserStats at index 19); real index in SAM is **13** | Rewrote `ISteamClient018` struct 1-to-1 from `gibbed/SteamAchievementManager/SAM.API/Interfaces/ISteamClient018.cs` — `GetISteamUserStats` is at index 13 |
| Native interface version strings garbled | `string` parameter marshals as **UTF-16**; Steam expects **UTF-8** | Pass version strings as `IntPtr` via `NativeStrings.StringToStringHandle` (which produces UTF-8 bytes). Used in `SteamClient018.GetISteamUserStats/GetISteamApps/GetISteamUtils` |
| `IsSubscribedApp` not found in vtable | Field was named `BIsSubscribedApp`; SAM names it `IsSubscribedApp` at the same index | Renamed field to `IsSubscribedApp` in `ISteamApps008` |
| `MainViewModel.StatusMessage` stuck on "Connecting to Steam" | `InitializeSteam()` swallowed exceptions to `Debug.WriteLine`; status never went back to the VM | `App.xaml.cs:InitializeSteamAsync` now sets `mainViewModel.StatusMessage` on success/failure and calls `LoadGamesCommand` to refresh |
| No Steam callbacks fire | `SteamClient.RunCallbacks()` was never called | Added a `DispatcherTimer` (`Config.CallbackTimerMs` = 100 ms) in `App.xaml.cs:StartCallbackTimer` that ticks `SteamContext.RunCallbacks()` |
| **"0 games loaded" after XML parse error** | `XDocument.Parse()` threw `XmlException: An error occurred while parsing EntityName` — games with `&` in name (e.g., "Age of Empires II & Conquerors") break the parser | Replaced with manual string parsing and `XmlReader` with `DtdProcessing.Ignore` |
| **"0 games loaded" after "Sign In" HTML** | `api.steampowered.com/IPlayerService/GetOwnedGames` requires **API key** (returns 404); `steamcommunity.com/profiles/{id}/games/?xml=1` requires session cookies + returns HTML for private profiles | Switched to SAM approach: download `games.xml` from `https://gib.me/sam/games.xml`, iterate all known appIds, call `IsSubscribedApp()` per-game via `steamclient.dll` |
| **Same 5 achievements (Spacewar) for all games** | `steamclient.dll` is a **process-level singleton**. When Steam client is running with AppId=X, initializing with AppId=Y in the same process either fails or returns Spacewar achievements. `ChangeAppId()` re-created interfaces but they all pointed to the same global Steam state. | Multi-process architecture: launcher (`SteamManager.exe`) shows game list without Steam init. Helper (`SteamManager.exe --game <appId>`) initializes Steam with the specific game AppId in its own process. Each process has an isolated `steamclient.dll` state. |
| **Launcher freezes showing 40902 games** | When Steam was not initialized in the launcher, `GetOwnedGamesAsync()` returned all games from `games.xml` without filtering by ownership. | Launcher now initializes Steam with Spacewar AppId (480) first, then filters by ownership via `IsSubscribedApp()`. Shows 189 owned games instead of all 40902. |
| **Helper window doesn't appear** | `MainWindow` constructor was overwriting DataContext passed from `App.StartGameHelperMode` with a new instance from Services | Added null check in `MainWindow` constructor: only set DataContext from Services if DataContext is null |
| **Achievement icons not loading** | Not yet implemented - `AchievementInfo` has `IconHandle` but no icon download/display | Fixed — Icons now load from Steam CDN when local handle unavailable, with `IconUrl` and `IconLockedUrl` properties |
| **Back button doesn't work in helper** | Helper mode doesn't have navigation back to launcher - it's a separate process | Fixed — Multi-process architecture: helper closes independently, launcher stays open and refreshes on return |
| **Loading status stays forever in helper** | Steam init happens on main thread but loading is async; status never updates after initial message | Fixed — Steam initialization now completes before loading games list, proper async flow |

---

Built with care by ZavalaSebas
