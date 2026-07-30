<div align="center">

# SteamManager

### Your Steam library, your rules.

[![License: GPL v3](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078d4.svg)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20WPFUI-9b59b6.svg)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)

Manage your Steam achievements and statistics with a modern, clean interface.

[Get Started](#get-started) · [Features](#features) · [How It Works](#how-it-works) · [Build from Source](#build-from-source)

</div>

---

## What is SteamManager?

A modern Windows desktop application for managing Steam game achievements and statistics. Built with **C# / .NET 10** and the same `steamclient.dll` approach used by the original SAM, it provides a clean, fast interface for viewing, unlocking, and locking achievements across your entire game library.

SteamManager replaces the aging [Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager) with a modern codebase — UI virtualization for large libraries, smart unlock with anti-detection delays, image caching, and a single portable executable.

No memory injection. No process hooking. No modified files. Just the official Steamworks API doing what it was designed to do.

> Born from the idea behind Steam Achievement Manager by Gibbed, rebuilt from scratch in .NET 10 with a native WPF interface and the internal `steamclient.dll` library.

---

## How It Works

The way Steam manages achievements is through its internal client library. SteamManager loads `steamclient.dll` from your Steam installation, initializes a session for a specific game, reads all achievement and stat data, displays it in a modern UI, and writes changes back through the same internal API that the original SAM uses.

1. Launch **SteamManager** (Steam must be running and logged in)
2. Your game library loads with covers
3. Select a game to manage its achievements and stats
4. Toggle achievements, edit statistics, or use smart unlock
5. Changes are committed to Steam's servers via the official API

---

## Features

- **Game Library** — Browse your entire Steam library with covers and achievement progress
- **Achievement Manager** — Lock, unlock, or toggle individual achievements with one click
- **Smart Unlock** — Delays between operations to reduce detection risk. Configure delay range and track progress in real time.
- **Protected Achievement Validation** — Automatically detects and blocks modification of developer-protected achievements
- **Achievement Global Rarity** — Each achievement shows the global unlock percentage with color coding
- **Stats Editor** — View and modify game statistics with protection warnings
- **Batch Operations** — Select multiple achievements and unlock/lock them all at once
- **Achievement Filters** — Filter by locked, unlocked, hidden, or search by name
- **Invert Selection** — Quickly toggle the selection state of all achievements
- **Achievement Search** — Filter achievements by name or description
- **Image Caching** — Game covers and achievement icons cached locally for fast loading
- **UI Virtualization** — Smooth performance even with 500+ games in your library
- **Dark Theme** — Modern dark UI with Mica, rounded corners, and smooth animations
- **Single Executable** — One portable `.exe`, no installation, no dependencies
- **Favorites** — Pin your most-used games to the top of the library
- **Game Type Filters** — Filter games by type (games, demos, mods, junk)
- **Add Game by App ID** — Manually add any owned game by entering its App ID
- **Auto-Updater** — Checks for new versions on launch with one-click update
- **Welcome Dialog** — "What's New" dialog shown after each update

---

## Get Started

**Download a Release**

Grab the latest `SteamManager.exe` from [GitHub Releases](https://github.com/ZavalaSebas/SteamManager/releases). Self-contained, no .NET required. Just run it.

**Build from Source**

```bash
git clone https://github.com/ZavalaSebas/SteamManager.git
cd SteamManager
dotnet publish SteamManager/SteamManager.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
```

---

## Requirements

- **Windows 10 or 11** (x86 — 32-bit, required because Steam ships a 32-bit `steamclient.dll`)
- **[Steam client](https://store.steampowered.com/about/)** running and logged in
- **.NET 10 Runtime** (or self-contained publish — note: publish with `-r win-x86`)

---

## Architecture

Modern .NET with a focused tech stack — no unnecessary dependencies.

- **Native Interop** — Loads `steamclient.dll` from Steam installation, calls via vtable
- **MVVM** — CommunityToolkit.Mvvm source generators, no code-behind
- **Async** — UI stays fluid while Steam API calls run in background threads
- **Virtualized** — `VirtualizingStackPanel` renders only visible items

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
│           Native Interop Layer                  │
│  NativeWrapper.cs — vtable extraction           │
│  SteamLoader.cs — DLL loading from registry     │
│  steamclient.dll (from Steam installation)      │
└─────────────────────────────────────────────────┘
```

---

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for workflow rules, test conventions, release process, and coding standards.
See [ARCHITECTURE.md](ARCHITECTURE.md) for design decisions, ADRs, and integration details.

---

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

---

## Acknowledgments

Built with inspiration from [Gibbed's Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager) — the original tool that pioneered Steam achievement management.

---

## Sponsor

If you find SteamManager useful, consider supporting the project:

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20Me-ff5e5b?logo=ko-fi&logoColor=white)](https://ko-fi.com/sebastianzavala82573)
[![GitHub Sponsors](https://img.shields.io/badge/GitHub%20Sponsors-Support%20Me-ea4aaa?logo=github-sponsors&logoColor=white)](https://github.com/sponsors/ZavalaSebas)

---

<div align="center">

Made with care by [ZavalaSebas](https://github.com/ZavalaSebas)

</div>
