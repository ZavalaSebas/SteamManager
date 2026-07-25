# SteamManager — Project Guide

This document serves as a guide to this specific project AND as a reference for the architecture, workflow, and decisions made during planning.

## Why SteamManager?

SteamManager is a modern rewrite of [Gibbed's Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager), originally built in 2008 with .NET Framework and Windows Forms. The original uses reverse-engineered access to Steam's internal `steamclient.dll`, has two separate executables, a broken image loading system, and a UI that hasn't aged well.

SteamManager replaces it with:
- **.NET 10 + WPF + WPFUI** — modern, GPU-accelerated UI with virtualization
- **Official Steamworks SDK** (`steam_api64.dll`) — stable, documented, no reverse engineering
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
│  GameLibraryService  ·  ConfigService           │
├─────────────────────────────────────────────────┤
│              Steam API Layer                    │
│  SteamClient  ·  SteamAchievements              │
│  SteamStats   ·  SteamApps  ·  SteamIcons       │
├─────────────────────────────────────────────────┤
│           P/Invoke (steam_api64.dll)            │
│  SteamNative.cs -- all DllImport declarations   │
└─────────────────────────────────────────────────┘
```

### Key Design Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Steam API | `steam_api64.dll` (official SDK) | Stable, documented, no reverse engineering |
| UI Framework | WPF + WPFUI | Modern look, single exe, GPU-accelerated |
| MVVM | CommunityToolkit.Mvvm | Source generators, minimal boilerplate |
| Image format | PNG/JPG | Native WPF support, no extra libraries |
| Persistence | JSON in `%LocalAppData%` | Simple, no database needed |
| Target | .NET 10 | Latest, stable, good WPF support |

### What we rejected

| Rejected | Why |
|----------|-----|
| `steamclient.dll` (internal) | Fragile, breaks with Steam updates, reverse-engineered |
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
│   │   ├── SteamNative.cs            # All P/Invoke declarations
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
│   ├── Helpers/                       # Utilities
│   └── Resources/                     # Styles, icons, images
├── SteamManager.Tests/                  # xUnit test project
├── .github/workflows/release.yml      # CI/CD pipeline
├── README.md
├── DEVELOPMENT.md                     # This file
├── CHANGELOG.md
└── LICENSE                            # GPL v3
```

## Steam API Integration

### How it works

SteamManager uses P/Invoke to call functions from `steam_api64.dll` directly. No wrapper libraries, no NuGet packages for Steam — just raw interop.

### Initialization sequence

```
1. Environment.SetEnvironmentVariable("SteamAppId", appId.ToString())
2. SteamAPI_RestartAppIfNecessary(appId) → if true, exit
3. SteamAPI_Init() → if false, error
4. SteamUserStats.RequestCurrentStats()
5. Wait for UserStatsReceived_t callback
6. Ready to read achievements and stats
```

### P/Invoke declarations (`SteamNative.cs`)

All DllImport declarations for `steam_api64.dll` go here. Key functions:

- **Lifecycle**: `SteamAPI_Init`, `SteamAPI_Shutdown`, `SteamAPI_RunCallbacks`
- **Stats**: `GetStat`, `SetStat`, `StoreStats`, `ResetAllStats`
- **Achievements**: `GetAchievement`, `SetAchievement`, `ClearAchievement`, `GetAchievementDisplayAttribute`, `GetAchievementAndUnlockTime`, `GetNumAchievements`, `GetAchievementName`, `GetAchievementIcon`
- **Utils**: `GetImageSize`, `GetImageRGBA` (for decoding achievement icons)
- **Apps**: `IsSubscribedApp` (game ownership)

### Callbacks

| Callback | ID | When |
|----------|-----|------|
| `UserStatsReceived_t` | 1101 | Stats loaded from server |
| `UserStatsStored_t` | 1102 | Stats saved to server |
| `UserAchievementStored_t` | 1103 | Individual achievement saved |
| `UserAchievementIconFetched_t` | 1109 | Achievement icon image ready |

### Achievement icon decoding

1. `GetAchievementIcon(name)` returns an image handle (int)
2. Handle 0 means not ready yet, wait for `UserAchievementIconFetched_t`
3. Use `GetImageSize(handle)` to get width/height
4. Use `GetImageRGBA(handle, buffer, size)` to get pixel data
5. Convert to `WriteableBitmap` for WPF binding
6. Cache to disk as PNG in `%LocalAppData%\SteamManager\cache\images\`

## Version Management

**Single source of truth**: `<Version>` in `SteamManager/SteamManager.csproj`

```xml
<Version>0.1.0</Version>
<AssemblyVersion>$(Version).0</AssemblyVersion>
```

- `AssemblyVersion` derives from `$(Version)` so assembly version is correct (e.g., `0.1.0.0`)
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
3. **Test** — `dotnet test SteamManager.slnx -c Release --no-build`
4. **Release** (only if version changed):
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

Run with: `dotnet test SteamManager.slnx -c Release`

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

// Real implementation uses P/Invoke
public class SteamAchievements : ISteamAchievements { }

// Test implementation returns predefined data
public class FakeSteamAchievements : ISteamAchievements { }
```

### Code Quality

Add to `.csproj` for consistent code style:

```xml
<PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

### WPFUI Navigation

Use WPFUI's `NavigationView` for page navigation:

```csharp
// MainViewModel
public void NavigateToGame(GameInfo game)
{
    // WPFUI handles page lifecycle and transitions
    _navigationService.NavigateTo(new GameManagerView(game));
}
```

### Key Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `WPF-UI` | 3.0.5 | Modern UI controls and theming |
| `CommunityToolkit.Mvvm` | 8.4.0 | MVVM source generators |
| `Microsoft.Extensions.DependencyInjection` | (add) | Service management |
| `Microsoft.Extensions.Logging` | (add) | Structured logging |
| `StyleCop.Analyzers` | 1.2.0-beta | Code style enforcement |

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
| IDE | Visual Studio 2022 / Rider / VS Code | Any works |
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

**Visual Studio 2022:**
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

**Status:** Accepted

**Context:**
The original SAM uses `steamclient.dll` which is reverse-engineered and breaks with Steam updates.

**Decision:**
Use `steam_api64.dll` from the official Steamworks SDK.

**Consequences:**
- ✅ Stable, documented, supported by Valve
- ✅ No reverse engineering required
- ❌ Only one AppID per process (cannot idle multiple games simultaneously)
- ❌ Some advanced features unavailable (e.g., internal Steam state)

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
- [ ] Commit with message: `bump vX.Y.Z`

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
| `SteamManager/SteamManager.csproj` | Version, target framework, NuGet packages |
| `SteamManager/Config.cs` | Centralized constants (URLs, paths, timeouts) |
| `SteamManager/Steam/SteamNative.cs` | All P/Invoke declarations for steam_api64.dll |
| `SteamManager/Steam/SteamClient.cs` | Steam API lifecycle (Init, Shutdown, RunCallbacks) |
| `SteamManager/Steam/SteamAchievements.cs` | Achievement read/write operations |
| `SteamManager/Steam/SteamStats.cs` | Stats read/write operations |
| `SteamManager/Services/SmartUnlockService.cs` | Anti-detection delay logic |
| `SteamManager/Services/ImageCacheService.cs` | Local image caching |
| `SteamManager/Services/ConfigService.cs` | Settings persistence |
| `SteamManager/Services/Updater.cs` | Update check, download, swap (v2.0) |
| `SteamManager/Services/NetworkHelper.cs` | HTTP client with User-Agent (v2.0) |
| `.github/workflows/release.yml` | CI/CD pipeline |
| `docs/index.html` | GitHub Pages landing page (v2.0) |
| `PLAN.md` | Full project plan with phases and features |

## Known Limitations

| Limitation | Reason | Workaround |
|------------|--------|------------|
| Single AppID per process | `steam_api64.dll` limitation | Future: multi-process idling |
| No cross-platform | Steam API is Windows-only | None (by design) |
| No WebP support | WPF doesn't decode WebP natively | Use PNG/JPG from Steam CDN |
| Achievement icons async | Steam API returns handle 0 initially, fetches in background | Wait for `UserAchievementIconFetched_t` callback |
| No auto-update | Not implemented in v1.0 | Manual download from GitHub Releases |
| No GitHub Pages | Not implemented in v1.0 | README serves as documentation |

## Known Issues & Resolutions

> **Document issues here as you find and fix them.** Include the symptoms, root cause, and how it was resolved. This helps future contributors avoid the same pitfalls.

| Issue | Root Cause | Resolution |
|-------|------------|------------|
| *No issues documented yet* | — | — |

---

Built with care by ZavalaSebas
