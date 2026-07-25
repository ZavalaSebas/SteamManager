# SteamNexus — Project Guide

This document serves as a guide to this specific project AND as a reference for the architecture, workflow, and decisions made during planning.

## Why SteamNexus?

SteamNexus is a modern rewrite of [Gibbed's Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager), originally built in 2008 with .NET Framework and Windows Forms. The original uses reverse-engineered access to Steam's internal `steamclient.dll`, has two separate executables, a broken image loading system, and a UI that hasn't aged well.

SteamNexus replaces it with:
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
SteamNexus/
├── SteamNexus.slnx                    # Solution file (.slnx format)
├── src/SteamNexus/                     # Main WPF application
│   ├── SteamNexus.csproj              # Version, target framework, packages
│   ├── App.xaml / App.xaml.cs         # Application entry, theme setup
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
├── tests/SteamNexus.Tests/            # xUnit test project
├── .github/workflows/release.yml      # CI/CD pipeline
├── README.md
├── DEVELOPMENT.md                     # This file
├── CHANGELOG.md
└── LICENSE                            # MIT
```

## Steam API Integration

### How it works

SteamNexus uses P/Invoke to call functions from `steam_api64.dll` directly. No wrapper libraries, no NuGet packages for Steam — just raw interop.

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
6. Cache to disk as PNG in `%LocalAppData%\SteamNexus\cache\images\`

## Version Management

**Single source of truth**: `<Version>` in `src/SteamNexus/SteamNexus.csproj`

```xml
<Version>0.1.0</Version>
<AssemblyVersion>$(Version).0</AssemblyVersion>
```

- `AssemblyVersion` derives from `$(Version)` so assembly version is correct (e.g., `0.1.0.0`)
- The Updater (future) will compare local vs remote version using `Version.TryParse`

**To bump the version**: edit `<Version>` in the csproj, commit with a descriptive message, push to `main`.

## Release Process (CI/CD)

On push to `main`, `.github/workflows/release.yml` runs:

1. **Check version change** — compares `<Version>` in HEAD vs HEAD~1
2. **Build** — `dotnet build SteamNexus.slnx -c Release`
3. **Test** — `dotnet test SteamNexus.slnx -c Release --no-build`
4. **Release** (only if version changed):
   - `dotnet publish` as self-contained single-file
   - Generate body from commit message
   - Create tag + release with `.exe`

### Critical workflow details
- `fetch-depth: 0` — required so `git show HEAD~1:path` can access the parent commit
- `permissions: contents: write` — required for `softprops/action-gh-release`
- Csproj path: `src/SteamNexus/SteamNexus.csproj`
- Release body comes from the **commit body** — write it with `### Added/Fixed/Changed` sections

## Solution File (.slnx)

```xml
<Solution>
  <Project Path="src/SteamNexus/SteamNexus.csproj" />
  <Project Path="tests/SteamNexus.Tests/SteamNexus.Tests.csproj" />
</Solution>
```

Benefits: human-readable, merge-friendly, no VS-generated garbage.

## Tests

Run with: `dotnet test SteamNexus.slnx -c Release`

### Planned test categories

- **SteamNativeTests** — validate P/Invoke declarations compile correctly
- **SmartUnlockTests** — verify delay logic, cancellation, progress reporting
- **AchievementTests** — mock Steam API responses, verify model creation
- **ConfigTests** — verify AssemblyVersion format and csproj consistency

## Caching System

All cache files stored in `%LocalAppData%\SteamNexus\`:

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
2. Hits `https://api.github.com/repos/ZavalaSebas/SteamNexus/releases/latest`
3. Compares remote tag version vs local `AssemblyVersion`
4. If newer, finds the first `.exe` asset and returns download URL
5. `UpdateWindow` shows progress, calls `Updater.DownloadAndApplyUpdateAsync()`
6. Download swaps: `SteamNexus.exe` → `SteamNexus.exe.old`, new → `SteamNexus.exe`, starts new process, exits
7. On next launch, `Updater.CleanupOldExe()` deletes the `.old` file

### Requirements
- `NetworkHelper` **must** set `User-Agent` header — GitHub API returns 403 without it
- HTTP client timeout: 10 seconds
- Assembly version must match csproj `<Version>` or update check compares wrong values

### Planned implementation
- `Services/Updater.cs` — update check, download, swap, cleanup
- `Services/NetworkHelper.cs` — HTTP client with User-Agent, JSON fetching
- `UI/Windows/UpdateWindow.xaml` — download progress UI
- Config keys: `GitHubApiUrl`, `RequestTimeout`

## GitHub Pages (v2.0)

> NOT implemented in v1.0. Documented here for future development.

### What it is

A landing page at `https://zavalasebas.github.io/SteamNexus/` showing version, download link, and release info. Deployed automatically on push to `main`.

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

7. **Always build before pushing** — run `dotnet build SteamNexus.slnx -c Release` and make sure there are no errors.

8. **Always test before pushing** — run `dotnet test SteamNexus.slnx -c Release` and make sure all tests pass.

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

## Key Files Quick Reference

| File | Purpose |
|------|---------|
| `src/SteamNexus/SteamNexus.csproj` | Version, target framework, NuGet packages |
| `src/SteamNexus/Steam/SteamNative.cs` | All P/Invoke declarations for steam_api64.dll |
| `src/SteamNexus/Steam/SteamClient.cs` | Steam API lifecycle (Init, Shutdown, RunCallbacks) |
| `src/SteamNexus/Steam/SteamAchievements.cs` | Achievement read/write operations |
| `src/SteamNexus/Steam/SteamStats.cs` | Stats read/write operations |
| `src/SteamNexus/Services/SmartUnlockService.cs` | Anti-detection delay logic |
| `src/SteamNexus/Services/ImageCacheService.cs` | Local image caching |
| `src/SteamNexus/Services/ConfigService.cs` | Settings persistence |
| `src/SteamNexus/Services/Updater.cs` | Update check, download, swap (v2.0) |
| `src/SteamNexus/Services/NetworkHelper.cs` | HTTP client with User-Agent (v2.0) |
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

---

Built with care by ZavalaSebas
