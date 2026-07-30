# SteamManager — Development Guide

This document describes the **processes, conventions, and operational rules** for working on this project.

For architecture, design decisions, and historical context, see [ARCHITECTURE.md](ARCHITECTURE.md).

---

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
│   │   ├── NativeMethods.cs          # P/Invoke for kernel32
│   │   ├── NativeStrings.cs          # UTF-8 marshaling utilities
│   │   ├── ISteamClient018.cs        # Vtable struct + wrapper
│   │   ├── ISteamUserStats013.cs     # Vtable struct + wrapper
│   │   ├── ISteamUser012.cs          # Vtable struct + wrapper
│   │   ├── ISteamApps008.cs          # Vtable struct + wrapper
│   │   ├── ISteamApps001.cs          # Vtable struct + wrapper
│   │   ├── ISteamInterfaces.cs       # Interface registration
│   │   ├── ISteamUtils005.cs         # Vtable struct + wrapper
│   │   ├── SteamClient.cs            # Init, Shutdown, RunCallbacks
│   │   ├── SteamAchievements.cs      # Achievement read/write
│   │   ├── SteamStats.cs             # Stats read/write
│   │   ├── SteamApps.cs              # Game library, ownership
│   │   ├── SteamIcons.cs             # Icon download and caching
│   │   ├── SteamCallbackHandler.cs   # Callback system
│   │   ├── SteamCallbacks.cs         # Callback message envelopes
│   │   ├── SteamContext.cs           # Session state model
│   │   ├── KeyValue.cs               # Binary KV parser (ported from SAM)
│   │   ├── KeyValueSerializer.cs     # Binary reading helpers
│   │   ├── KeyValueType.cs           # KV type enum
│   │   └── UserStatType.cs           # Stat type enum
│   ├── Models/                        # Data models
│   │   ├── GameInfo.cs
│   │   ├── AchievementInfo.cs
│   │   ├── StatInfo.cs
│   │   ├── SchemaAchievementDefinition.cs
│   │   ├── SchemaStatDefinition.cs
│   │   ├── SchemaIntegerStatDefinition.cs
│   │   └── SchemaFloatStatDefinition.cs
│   ├── ViewModels/                    # MVVM ViewModels
│   │   ├── MainViewModel.cs
│   │   ├── GamePickerViewModel.cs
│   │   └── GameManagerViewModel.cs
│   ├── Views/                         # WPF Views
│   │   ├── MainWindow.xaml / .cs
│   │   ├── GamePickerView.xaml / .cs
│   │   └── GameManagerView.xaml / .cs
│   ├── Controls/                      # Custom controls
│   │   ├── GameCard.xaml / .cs
│   │   ├── AchievementCard.xaml / .cs
│   │   └── SkeletonCard.xaml / .cs
│   ├── Dialogs/                       # Dialog windows
│   │   ├── SmartUnlockDialog.xaml / .cs
│   │   ├── SmartUnlockResultDialog.xaml / .cs
│   │   ├── ProgressOverlay.xaml / .cs
│   │   ├── UpdateWindow.xaml / .cs
│   │   ├── WelcomeWindow.xaml / .cs
│   │   ├── AboutDialog.xaml / .cs
│   │   └── InfoDialog.xaml / .cs
│   ├── Services/                      # Business logic
│   │   ├── IGameLibraryService.cs
│   │   ├── SteamGameLibraryService.cs
│   │   ├── IImageCacheService.cs
│   │   ├── ImageCacheService.cs
│   │   ├── ISmartUnlockService.cs
│   │   ├── SmartUnlockService.cs
│   │   ├── IConfigService.cs
│   │   ├── ConfigService.cs
│   │   ├── GameSchemaService.cs
│   │   ├── GameStats.cs
│   │   ├── Updater.cs
│   │   ├── NetworkHelper.cs
│   │   ├── MessageBoxService.cs
│   │   └── FileLogger.cs
│   ├── Converters/                    # Value converters
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── UrlToCachedImageConverter.cs
│   │   ├── ProgressToArcConverter.cs
│   │   ├── PercentToArcMultiConverter.cs
│   │   ├── ProgressToWidthConverter.cs
│   │   ├── GlobalPercentageConverters.cs
│   │   ├── FilterToBackgroundConverter.cs
│   │   ├── SelectedBorderConverter.cs
│   │   ├── BoolToCheckmarkConverter.cs
│   │   ├── BoolToFavoriteColorConverter.cs
│   │   ├── AchievementBackgroundConverter.cs
│   │   └── VisibilityConverters.cs
│   └── Resources/                     # Styles
│       └── Styles.xaml
├── SteamManager.Tests/                  # xUnit test project
│   ├── AchievementInfoTests.cs
│   ├── ConverterTests.cs
│   ├── GameManagerViewModelTests.cs
│   ├── GameStatsTests.cs
│   ├── KeyValueTests.cs
│   ├── KeyValueSerializerTests.cs
│   ├── SchemaModelTests.cs
│   ├── SmartUnlockServiceTests.cs
│   ├── SmartUnlockProgressPropertyChangedTests.cs
│   └── XmlParsingTests.cs
├── .github/workflows/release.yml      # CI/CD pipeline
├── README.md
├── ARCHITECTURE.md
├── DEVELOPMENT.md                     # This file
├── CHANGELOG.md
└── LICENSE                            # GPL v3
```

---

## Version Management

**Single source of truth**: `<Version>` in `SteamManager/SteamManager.csproj`

```xml
<Version>1.2.0</Version>
<AssemblyVersion>$(Version).0</AssemblyVersion>
```

- `AssemblyVersion` derives from `$(Version)` so assembly version is correct (e.g., `1.2.0.0`)
- The Updater compares remote tag vs local version using `Version.TryParse`

**To bump the version**: edit `<Version>` in the csproj, commit with a descriptive message, push to `main`.

### Constants Pattern (`Config.cs`)

Prefer keeping constants centralized in `Config.cs` rather than scattered across classes. This includes URLs, paths, timeouts, and other magic values.

---

## Semantic Versioning (SemVer)

Always follow SemVer for version numbers.

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

---

## Release Process (CI/CD)

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
- Tests are disabled in CI: GitHub Actions uses a 64-bit Windows runner and .NET 10 doesn't ship a 32-bit runtime, so the x86 test process cannot run. Tests are verified locally before each release.

---

## Solution File (.slnx)

```xml
<Solution>
  <Project Path="SteamManager/SteamManager.csproj" />
  <Project Path="SteamManager.Tests/SteamManager.Tests.csproj" />
</Solution>
```

Benefits: human-readable, merge-friendly, no VS-generated garbage.

---

## Tests

Run locally with: `dotnet test SteamManager.slnx -c Release`

> Tests require `<RuntimeIdentifier>win-x86</RuntimeIdentifier>` to run (both main and test projects). CI cannot run tests because GitHub Actions uses a 64-bit Windows runner and .NET 10 doesn't ship a 32-bit runtime — the test process fails to start with `hostfxr.dll` loading error. Tests are verified locally before each release.

### Test categories

| Category | Files | Coverage |
|----------|-------|----------|
| **SmartUnlock** | `SmartUnlockServiceTests.cs`, `SmartUnlockProgressPropertyChangedTests.cs` | Smart unlock delay logic, cancellation, progress reporting, progress property change notifications |
| **Achievement/ViewModel** | `AchievementInfoTests.cs`, `GameManagerViewModelTests.cs` | Model creation, state transitions, ViewModel behavior |
| **Schema/KV Parser** | `SchemaModelTests.cs`, `KeyValueTests.cs`, `KeyValueSerializerTests.cs` | Binary KeyValue parsing, schema model mapping, `Permission` field extraction |
| **Game Stats** | `GameStatsTests.cs` | Predefined stat definitions |
| **Converters** | `ConverterTests.cs` | Value converter behavior |
| **XML Parsing** | `XmlParsingTests.cs` | `games.xml` parsing edge cases (entity encoding, malformed entries) |

### Test conventions

- One `[Fact]` per test method (no `[Theory]` unless data-driven)
- No test dependencies — each test is independent
- Arrange → Act → Assert pattern
- Test class name = Service/Class name + "Tests" (e.g., `SmartUnlockServiceTests`)
- Namespace mirrors source: `SteamManager.Tests.Services.SmartUnlockServiceTests`
- Method naming: `MethodName_Condition_ExpectedResult` (e.g., `SetAchievement_ValidId_ReturnsTrue`)

---

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

---

## Git Best Practices

Follow these conventions for clean git history.

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

---

## Coding Standards

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

---

## Development Environment Setup

For new contributors. What you need to get started.

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

---

## Git Hooks

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

> The `.git/hooks/` directory is not version-controlled, so each developer must install this manually. There is no automated setup script — copy the script above into `.git/hooks/pre-commit` and make it executable:

```bash
# Copy the script to your local hooks directory (Git Bash on Windows)
chmod +x .git/hooks/pre-commit
```

---

## Release Checklist

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

---

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
| `.github/workflows/release.yml` | CI/CD pipeline (`win-x86`, triggers on push/PR to main) |
| `CHANGELOG.md` | Version history (v0.1.0 → v1.2.0) |

---

## Known Limitations

| Limitation | Reason | Workaround |
|------------|--------|------------|
| Single AppID per process | `steamclient.dll` limitation (same as original SAM) | Future: multi-process idling |
| No cross-platform | Steam API is Windows-only | None (by design) |
| No WebP support | WPF doesn't decode WebP natively | Use PNG/JPG from Steam CDN |
| Achievement icons async | Steam API returns handle 0 initially, fetches in background | Wait for `UserAchievementIconFetched_t` callback |

| Uses internal Steam API | `steamclient.dll` is not officially documented | Same approach as original SAM, proven stable |
| **32-bit (x86) platform only** | Steam ships only a 32-bit `steamclient.dll`; Windows cannot load a 32-bit DLL into a 64-bit process | Project targets `win-x86` (`<RuntimeIdentifier>win-x86</RuntimeIdentifier>`); publish native exe with `dotnet publish -r win-x86` |
| **vtable layouts byte-aligned to SAM** | `steamclient.dll` is a C++ object with a per-version vtable; padding/extra entries from one SDK version break ours | Vtable structs in `ISteam*.cs` are copied 1-to-1 from gibbed/SAM and must NOT be reordered or padded. See `SAM.API/Interfaces/` |
| **No playtime, no achievement details from library** | SAM approach (`games.xml` + `IsSubscribedApp`) only tells if user owns a game — no per-user data (playtime, achievements, stats) | Steam Web API `GetOwnedGames` returns playtime but requires API key. Alternative: parse `steamcommunity.com/profiles/{id}/games/?xml=1` with session cookies (requires login flow). See `SteamWebApiKey` constant in `Config.cs` — requires Steam Web API key setup. |
| **KeyValue parser: nested Type.None nodes** | The binary KeyValue parser does not correctly handle `Type.None` parent nodes containing `Type.None` children — the inner termination marker is misinterpreted | Validated against 355 real schemas with zero impact (no real Steam schema uses this pattern). Risk is very low. See `KeyValue.ReadAsBinary()` and skipped test `ReadAsBinary_ParsesNestedKeyValue`. |
| **Stats editor not refreshed after ResetAllStats** | No observable stats collection exists in the ViewModel — `GetStat(name, out value)` reads one stat at a time, no callback updates UI | After reset, user must re-query each stat individually. Future: add an observable `Statistics` collection that gets updated when `RequestStats()` callback fires. |
| **Smart Unlock UI requires manual smoke test before release** | `steamclient.dll` is not available in the test environment; no automated smoke test can open the three Smart Unlock dialogs (SmartUnlockDialog, ProgressOverlay, SmartUnlockResultDialog) or verify the dropdown entry point | Before any release, verify manually with Steam running: (1) dropdown appears in game manager toolbar, (2) SmartUnlockDialog opens with correct defaults (15-45s), (3) Smart Unlock execution shows ProgressOverlay with live counters, (4) result dialog shows correct icon and auto-dismiss behavior. No automated CI coverage possible. |
| **Closing app during Smart Unlock cancels operation without rollback** | Hard-closing the app (clicking the window's X button, Alt+F4, or terminating the process) during a Smart Unlock operation prevents `StoreStats()` from executing — that method is called once in a `finally` block after the entire batch completes, flushing buffered local achievement state to Steam's server. A hard-close terminates the process before that call runs, so achievement changes are discarded and never persisted. | User must not close the app during Smart Unlock. If closed accidentally, re-run Smart Unlock — the operation is idempotent and safe to re-run in full, since nothing may have persisted if the app was closed before the batch's `StoreStats()` call. Does not affect games with no in-progress work. |
