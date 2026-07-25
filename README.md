<div align="center">

# SteamNexus

### Modern Steam Achievement Manager -- Built in C# / .NET 9

[.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) · [WPF Desktop](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/) · [Windows 10/11](https://www.microsoft.com/windows/windows-11) · [MIT](LICENSE) · v0.1.0

[Get Started](#get-started) · [Features](#features) · [How It Works](#how-it-works) · [Build from Source](#build-from-source)

</div>

---

## What is SteamNexus?

A modern Windows desktop application for managing Steam game achievements and statistics. Built with **C# / .NET 9** and the official [Steamworks SDK](https://partner.steamgames.com/doc/sdk/api) (`steam_api64.dll`), it provides a clean, fast interface for viewing, unlocking, and locking achievements across your entire game library.

SteamNexus replaces the aging [Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager) with a modern codebase — UI virtualization for large libraries, smart unlock with anti-detection delays, image caching, and a single portable executable.

No memory injection. No process hooking. No modified files. Just the official Steamworks API doing what it was designed to do.

---

## How It Works

1. Launch **SteamNexus** (Steam must be running and logged in)
2. Your game library loads with covers and playtime
3. Select a game to manage its achievements and stats
4. Toggle achievements, edit statistics, or use smart unlock
5. Changes are committed to Steam's servers via the official API

The app communicates directly with `steam_api64.dll` — Valve's official C API. No reverse engineering, no internal DLLs, no fragile hacks.

---

## Get Started

**Download a Release**

Grab the latest `SteamNexus.exe` from [GitHub Releases](https://github.com/ZavalaSebas/SteamNexus/releases). Self-contained, no .NET required. Just run it.

**Build from Source**

```bash
git clone https://github.com/ZavalaSebas/SteamNexus.git
cd SteamNexus
dotnet publish src/SteamNexus/SteamNexus.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Requirements

- **Windows 10 or 11** (x64)
- **[Steam client](https://store.steampowered.com/about/)** running and logged in
- **.NET 9 Runtime** (or self-contained publish)

---

## Features

- **Game Library** — Browse your entire Steam library with covers, playtime, and achievement progress
- **Achievement Manager** — Lock, unlock, or toggle individual achievements with one click
- **Smart Unlock** — Anti-detection delays (15-45s random) to protect your account from tracking sites
- **Stats Editor** — View and modify game statistics with protection warnings
- **Batch Operations** — Select multiple achievements and unlock/lock them all at once
- **Achievement Filters** — Filter by locked, unlocked, hidden, or search by name
- **Image Caching** — Game covers and achievement icons cached locally for fast loading
- **UI Virtualization** — Smooth performance even with 500+ games in your library
- **Dark Theme** — Modern dark UI with Mica, rounded corners, and smooth animations
- **Single Executable** — One portable `.exe`, no installation, no dependencies
- **Favorites** — Pin your most-used games to the top of the library

---

## Architecture

No bloated frameworks, no unnecessary dependencies — pure .NET with minimal packages.

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

- **P/Invoke** — Direct calls to `steam_api64.dll`, no wrapper libraries
- **MVVM** — CommunityToolkit.Mvvm source generators, no code-behind
- **Async** — UI stays fluid while Steam API calls run in background threads
- **Virtualized** — `VirtualizingStackPanel` renders only visible items

---

## Development

### Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# 12 |
| Runtime | .NET 9 |
| UI | WPF + WPFUI |
| MVVM | CommunityToolkit.Mvvm |
| Testing | xUnit |
| CI/CD | GitHub Actions |
| Steam API | `steam_api64.dll` via P/Invoke |

### Project Structure

```
SteamNexus/
├── src/SteamNexus/           # Main application
│   ├── Steam/                # Steam API integration layer
│   ├── Models/               # Data models
│   ├── ViewModels/           # MVVM ViewModels
│   ├── Views/                # WPF Views
│   ├── Controls/             # Custom controls
│   ├── Services/             # Business logic services
│   ├── Converters/           # Value converters
│   └── Helpers/              # Utilities
└── tests/SteamNexus.Tests/   # Unit tests
```

### Build & Test

```bash
# Build
dotnet build SteamNexus.slnx -c Release

# Test
dotnet test SteamNexus.slnx -c Release

# Publish (single exe)
dotnet publish src/SteamNexus/SteamNexus.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Roadmap

### v1.0 (Current)
- Game library browser with covers
- Achievement lock/unlock
- Stats editor
- Smart unlock with delays
- Image caching
- Favorites and search

### v2.0 (Planned)
- Multi-idling (rotate through games automatically)
- Achievement rarity percentages
- Friend activity
- Cloud save management

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Acknowledgments

Built with inspiration from [Gibbed's Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager) — the original tool that pioneered Steam achievement management.

---

<div align="center">

Made with care by [ZavalaSebas](https://github.com/ZavalaSebas)

</div>
