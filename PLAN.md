# SteamManager - Plan de Proyecto

## Estado Actual de Implementación

> Última actualización: 2026-07-27 (v1.1.0)

### Fase 1 (Core Steam API) — ✅ Completada
- `SteamLoader.cs` ✅ — carga de `steamclient.dll` desde registro
- `NativeWrapper.cs` ✅ — extracción de vtable y llamadas nativas
- `SteamClient.cs` ✅ — Init/Shutdown/RunCallbacks
- `SteamAchievements.cs` ✅ — lectura/escritura de logros
- `SteamStats.cs` ✅ — lectura/escritura de estadísticas
- `SteamApps.cs` ✅ — verificación de suscripción
- `SteamIcons.cs` ✅ — decodificación RGBA de iconos
- `SteamCallbackHandler.cs` ✅ — sistema de callbacks
- `SteamContext.cs` ✅ — modelo de sesión

**Archivos adicionales de interop** (no estaban en el plan original, copiados de gibbed/SAM):
- `NativeMethods.cs` — P/Invoke para kernel32
- `NativeStrings.cs` — marshaling UTF-8 para cadenas nativas
- `ISteamClient018.cs` — layout de vtable + wrapper
- `ISteamUserStats013.cs` — layout de vtable + wrapper
- `ISteamApps008.cs` — layout de vtable + wrapper
- `ISteamUtils005.cs` — layout de vtable + wrapper
- `SteamCallbacks.cs` — structs de callbacks + CallbackMessage

### Fase 2 (UI WPF + WPFUI) — ✅ Completada
- `App.xaml` ✅ — tema WPFUI Dark + DataTemplates
- `MainWindow.xaml/.cs` ✅ — shell con navegación
- `MainViewModel.cs` ✅ — shell ViewModel
- `GamePickerViewModel.cs` + `GamePickerView.xaml` ✅
- `GameManagerViewModel.cs` + `GameManagerView.xaml` ✅
- `GameCard.xaml` ✅
- `AchievementCard.xaml` ✅

### Fase 3 (Servicios de Negocio) — ✅ Completada
- `SteamGameLibraryService.cs` ✅ — implementación real usando approach de SAM
- `SmartUnlockService`, `ImageCacheService`, `ConfigService` ✅ — todos implementados

### Plataforma: win-x86 (32-bit)
-steamclient.dll de Steam es de 32 bits — el proyecto usa `<RuntimeIdentifier>win-x86</RuntimeIdentifier>`

---

## Problema Encontrado y Cambio de Arquitectura

### Problema Original
El proyecto usaba `steam_api64.dll` (Steamworks SDK) via P/Invoke directo. Este DLL:
- **No viene con Steam** — se distribuye con cada juego individual
- **No está en el directorio de Steam** — el usuario no tiene acceso directo a él
- **Problemas de distribución** — no se puede redistribuir legalmente (licencia Valve)
- **Dependencia externa** — la app crashea si el DLL no está presente

### Solución Adoptada
Cambiar a `steamclient.dll` (la biblioteca interna de Steam), igual que el proyecto original SAM:
- **Siempre disponible** — viene con la instalación de Steam
- **No necesita distribución** — el usuario ya lo tiene
- **Portabilidad completa** — la app es un exe autocontenido
- **Mismo approach que el original** — probado por años de uso

### Documentación del Cambio
Ver [ADR-004](DEVELOPMENT.md#adr-004-use-steamclientdll-instead-of-steam_api64dll) para el análisis completo.

---

## Enumeración de Juegos (Phase 3)

### Problema: Steam Web API requiere API Key

Intentamos usar el endpoint `GetOwnedGames` de la API de Steam (`api.steampowered.com`) pero:
- **Requiere API Key** — Devuelve **404 Not Found** sin ella
- La API key de Steam es por desarrollador, no por usuario

Intentamos también `steamcommunity.com/profiles/{id}/games/?xml=1`:
- **Requiere cookies de sesión** — Devuelve página HTML "Sign In"
- **Perfiles privados** — Devuelven HTML aunque haya sesión

### Solución: Approach de SAM

SAM usa un enfoque simple y robusto:
1. Descarga `games.xml` desde `https://gib.me/sam/games.xml` (lista maestra de appIds)
2. Itera por cada appId conocido
3. Para cada uno, llama `IsSubscribedApp(appId)` vía `steamclient.dll` local
4. Si el usuario lo posee, obtiene metadata con `GetAppData(appId, "name")`

```
PARA CADA appId EN games.xml:
    SI steamClient.IsSubscribedApp(appId) == true:
        AGREGAR a lista de juegos con nombre de GetAppData()
```

**Ventajas:**
- Sin API key
- Perfil privado no importa
- No necesita login de Steam Community
- Consulta directo al cliente Steam local (sesión ya autenticada)
- Funciona out-of-the-box

**Desventaja:**
- La lista `games.xml` es estática (puede no incluir juegos muy nuevos)
- Si Gibbed deja de mantener `games.xml`, habría que hostear nuestra propia copia

### Implementación Actual

| Componente | Estado |
|------------|--------|
| `SteamGameLibraryService.GetOwnedGamesAsync()` | ✅ Implementado |
| `ISteamUser012` wrapper (`GetSteamId()`) | ✅ Implementado |
| `ISteamApps001` wrapper (`GetAppData()`) | ✅ Implementado |
| `SteamContext.SteamId` | ✅ Implementado |
| Fallback si `games.xml` falla | ✅ Spacewar (480) |
| `ImageCacheService` | ✅ Implementado |
| `SmartUnlockService` | ✅ Implementado — core + UI completos (72 tests passing) |
| `ConfigService` | ✅ Implementado |

### Arquitectura Multi-Proceso

**Problema**: `steamclient.dll` es un singleton por proceso. No se puede cambiar el AppId en el mismo proceso.

**Solución implementada** (v1.0):
- **Launcher** (`SteamManager.exe` sin args): Inicializa Steam con Spacewar (AppId=480), muestra lista de juegos. Permanece abierto mientras el helper corre.
- **Helper** (`SteamManager.exe --game <appId>`): Inicializa Steam con el AppId específico del juego, muestra logros. Cierra independently.

```
Launcher                          Helper
+-----------+                     +-----------+
| AppId=480 |                     | AppId=X  |
| Lista de  | --click juego-->   | Logros X |
| juegos    |    (permanece)      | Unlock/  |
+-----------+  <--vuelve          | Lock     |
                                   +-----------+
```

---

## Decisión Técnica Final

| Aspecto | Decisión |
|---------|----------|
| Nombre | **SteamManager** |
| Runtime | .NET 10 |
| UI | WPF + WPFUI |
| Steam API | `steamclient.dll` (biblioteca interna de Steam) via vtable/COM-style |
| Patrón | MVVM con CommunityToolkit.Mvvm |
| Empaquetado | `PublishSingleFile` + `SelfContained` = 1 exe portable |
| Caché | `%LocalAppData%\SteamManager\` |

---

## Alcance: Fase Actual vs Futuro

### Fase Actual (v1.0) - ✅ Completada
Replicar y mejorar las capacidades core del SAM original:

- ✅ Gestión de logros (lock/unlock con UI que actualiza iconos)
- ✅ Lock All / Unlock All con soporte multi-selección
- ✅ Editor de estadísticas (expandible, con stats predefinidos para juegos populares)
- ✅ Explorador de biblioteca con carátulas
- ✅ Búsqueda de juegos por nombre
- ✅ Filtros de logros (All, Unlocked, Locked, Hidden)
- ✅ Favorites (juegos anclados con estrella, persistidos)
- ✅ Orden por favoritos y recientes
- ✅ Desbloqueo inteligente con delays aleatorios (SmartUnlockService — core + UI implementados, 72 tests)
- ✅ Caché de imágenes local
- ✅ UI moderna con WPFUI (Dark theme, Fluent design)
- ✅ Multi-proceso (launcher + helper independientes)

### Futuro (v2.0+) - Tier 2, 3 y 4
> Documentado aquí para desarrollo futuro. NO implementar en v1.0.

**Tier 2 - Multi-Idling:**
- Multi-idling por rotación automática (un juego a la vez, rota cada N minutos)
- O multi-proceso (lanzar instancias separadas por juego)
- Requiere investigación de viabilidad con `steamclient.dll`

**Tier 3 - Funcionalidades sociales y datos:**
- Porcentaje global de obtención por logro (requiere Steam Web API key o scraping de community pages; no disponible sin autenticación ni API key pública)
- Progreso de logros por juego (X/50 desbloqueados)
- Amigos en línea + avatares
- Rich Presence de amigos
- Dashboard con estadísticas personales de Steam (total logros, horas, etc.)

**Tier 4 - Suite completa:**
- Gestión de cloud saves (backup/restore)
- Historial de cambios realizados
- Gestión de screenshots
- Info de DLCs por juego
- Logging detallado de acciones

---

## Estructura del Proyecto

> **Nota histórica**: Esta es la estructura planificada originally. No todos los archivos se implementaron como se indicaba. La estructura real del proyecto se encuentra en [DEVELOPMENT.md](DEVELOPMENT.md#project-structure).

```
SteamManager/
├── SteamManager.slnx
├── SteamManager/
│   ├── SteamManager.csproj
│   ├── App.xaml / App.xaml.cs
│   │
│   ├── Steam/                          # Capa de integración con Steam
│   │   ├── SteamLoader.cs             # Carga de steamclient.dll via registry
│   │   ├── NativeWrapper.cs           # Extracción de vtable y llamadas nativas
│   │   ├── SteamClient.cs             # Init, Shutdown, RunCallbacks
│   │   ├── SteamAchievements.cs       # Lectura/escritura de logros
│   │   ├── SteamStats.cs              # Lectura/escritura de estadísticas
│   │   ├── SteamApps.cs               # Lista de juegos, ownership, playtime
│   │   ├── SteamIcons.cs              # Descarga y caché de iconos/logos
│   │   ├── SteamCallbackHandler.cs    # Sistema de callbacks
│   │   └── SteamContext.cs            # Modelo de sesión activa para un AppID
│   │
│   ├── Models/                         # Modelos de datos
│   │   ├── GameInfo.cs                 # AppId, Name, Playtime, CoverUrl, IsFavorite
│   │   ├── AchievementInfo.cs          # Id, Name, Desc, IsUnlocked, UnlockTime, Icon
│   │   ├── StatInfo.cs                 # Name, Type, Value, Min, Max, Permission
│   │   └── AppSettings.cs              # Configuración persistente
│   │
│   ├── ViewModels/                     # ViewModels MVVM
│   │   ├── MainViewModel.cs            # Shell: navega entre vistas
│   │   ├── GamePickerViewModel.cs      # Selector de juegos con búsqueda
│   │   ├── GameManagerViewModel.cs     # Editor de logros/stats de 1 juego
│   │   ├── AchievementViewModel.cs     # VM de un logro individual
│   │   ├── StatViewModel.cs            # VM de una stat individual
│   │   └── SettingsViewModel.cs        # VM de configuración
│   │
│   ├── Views/                          # Vistas WPF
│   │   ├── MainWindow.xaml             # Ventana principal (shell)
│   │   ├── GamePickerView.xaml         # Grid virtualizado de carátulas
│   │   ├── GameManagerView.xaml        # Editor de logros + stats
│   │   └── SettingsView.xaml           # Configuración
│   │
│   ├── Controls/                       # Controles custom
│   │   ├── GameCard.xaml               # Tarjeta de juego (portada + nombre)
│   │   ├── AchievementCard.xaml        # Tarjeta de logro (icono + nombre + check)
│   │   └── ProgressOverlay.xaml        # Overlay de progreso de desbloqueo ✅ implementado en Dialogs/
│   │
│   ├── Services/                       # Servicios de negocio
│   │   ├── IGameLibraryService.cs      # Interfaz: obtener juegos del usuario
│   │   ├── GameLibraryService.cs       # Implementación: llama a SteamApps
│   │   ├── IImageCacheService.cs       # Interfaz: caché de imágenes
│   │   ├── ImageCacheService.cs        # Descarga + caché en disco
│   │   ├── IAchievementUnlocker.cs     # Interfaz: desbloqueo con lógica anti-ban
│   │   ├── SmartUnlockService.cs       # Desbloqueo inteligente con delays
│   │   └── ConfigService.cs            # Persistencia de settings (JSON)
│   │
│   ├── Converters/                     # Value converters
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── AchievementToIconConverter.cs
│   │   └── PercentageToColorConverter.cs
│   │
│   ├── Helpers/                        # Utilidades
│   │   ├── Paths.cs                    # Rutas de caché, datos, etc. (not implemented — see Config.cs)
│   │   └── NativeMethods.cs            # P/Invoke auxiliares (moved to Steam/)
│   │
│   ├── Resources/                      # Recursos estáticos
│   │   ├── Styles.xaml                 # Estilos globales
│   │   ├── Icons.xaml                  # Iconos vectoriales
│   │   └── Images/                     # Placeholder images
│   │
│   ├── app.manifest
│   └── launchSettings.json
│
└── SteamManager.Tests/
    ├── SteamNativeTests.cs
    └── SmartUnlockTests.cs
```

---

## Fases de Desarrollo

### FASE 1: Core - Integración con Steam API
> Objetivo: Conectar con Steam, leer logros y stats de un juego

**Archivos a crear:**

| Archivo | Responsabilidad |
|---------|----------------|
| `SteamLoader.cs` | Carga `steamclient.dll` desde el directorio de Steam via registro de Windows |
| `NativeWrapper.cs` | Extracción de vtable y llamadas a funciones nativas via `CallingConvention.ThisCall` |
| `SteamClient.cs` | `Init(appId)`, `Shutdown()`, `RunCallbacks()` |
| `SteamAchievements.cs` | `GetAchievementCount()`, `GetAchievementName(i)`, `GetAchievement(name)`, `SetAchievement(name)`, `ClearAchievement(name)`, `GetAchievementDisplayAttribute(name, key)`, `GetAchievementAndUnlockTime(name)` |
| `SteamStats.cs` | `GetStat(name, out int)`, `GetStat(name, out float)`, `SetStat(name, value)`, `StoreStats()`, `ResetAllStats(achievementsToo)` |
| `SteamApps.cs` | `GetOwnedGames()`, `IsSubscribedApp(appId)`, `GetAppData(appId, key)` |
| `SteamIcons.cs` | `GetAchievementIcon(name)` → bytes RGBA → ImageSource → caché |
| `SteamCallbackHandler.cs` | Registro de callbacks, `RunCallbacks()` en timer, dispatch a eventos |
| `SteamContext.cs` | Modelo que agrupa estado de sesión Steam activa |
| `GameInfo.cs` | Modelo de juego |
| `AchievementInfo.cs` | Modelo de logro |
| `StatInfo.cs` | Modelo de stat |

**Carga de steamclient.dll:**

```csharp
// 1. Leer ruta de Steam desde registro
string steamPath = Registry.GetValue(
    @"HKEY_LOCAL_MACHINE\Software\Valve\Steam", "InstallPath") as string;

// 2. Agregar directorio de DLLs al search path
SetDllDirectory(steamPath + ";" + Path.Combine(steamPath, "bin"));

// 3. Cargar steamclient.dll
IntPtr module = LoadLibraryEx(
    Path.Combine(steamPath, "steamclient.dll"),
    IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);

// 4. Resolver 3 funciones exportadas
CreateInterface = GetProcAddress(module, "CreateInterface");
Steam_BGetCallback = GetProcAddress(module, "Steam_BGetCallback");
Steam_FreeLastCallback = GetProcAddress(module, "Steam_FreeLastCallback");
```

**Creación de interfaces (vtable-based):**

```csharp
// Crear objeto raíz SteamClient018
IntPtr clientObj = CreateInterface("SteamClient018", IntPtr.Zero);

// Obtener pipe y usuario
int pipe = SteamClient.CreateSteamPipe();
int user = SteamClient.ConnectToGlobalUser(pipe);

// Obtener interfaces específicas
IntPtr userStats = SteamClient.GetISteamUserStats013(user, pipe);
IntPtr apps = SteamClient.GetISteamApps008(user, pipe);

// Cada interfaz tiene una vtable con punteros a funciones
// Las llamadas usan CallingConvention.ThisCall
```

**Funciones por interfaz:**

| Interfaz | Funciones | Versión |
|----------|-----------|---------|
| `ISteamClient018` | CreateSteamPipe, ConnectToGlobalUser, GetISteamUserStats, GetISteamApps | 018 |
| `ISteamUserStats013` | GetStat, SetStat, GetAchievement, SetAchievement, ClearAchievement, StoreStats, ResetAllStats, GetNumAchievements, GetAchievementName, GetAchievementIcon, GetAchievementDisplayAttribute, GetAchievementAndUnlockTime, RequestUserStats | 013 |
| `ISteamApps008` | IsSubscribedApp | 008 |
| `ISteamUtils005` | GetImageSize, GetImageRGBA | 005 |

**Callbacks a manejar:**

| Callback | ID | Uso |
|----------|-----|-----|
| `UserStatsReceived_t` | 1101 | Stats cargados desde servidor |
| `UserStatsStored_t` | 1102 | Stats guardados |
| `UserAchievementStored_t` | 1103 | Logro individual guardado |
| `UserAchievementIconFetched_t` | 1109 | Icono de logro listo |

**Flujo de inicialización:**

```
1. Environment.SetEnvironmentVariable("SteamAppId", appId.ToString())
2. Leer ruta de Steam desde registro (HKLM\Software\Valve\Steam\InstallPath)
3. SetDllDirectory(steamPath + ";" + steamPath + "\bin")
4. LoadLibraryEx(steamPath + "\steamclient.dll")
5. CreateInterface("SteamClient018") → cliente raíz
6. CreateSteamPipe() → crear pipe IPC
7. ConnectToGlobalUser(pipe) → conectar al usuario
8. GetISteamUserStats013(user, pipe) → interfaz de stats
9. RequestUserStats() → solicitar stats del servidor
10. Esperar callback UserStatsReceived_t
11. Ya se puede leer logros y stats
```

---

### FASE 2: UI - WPF + WPFUI
> Objetivo: Interfaz moderna con selector de juegos y editor de logros

**Dependencias NuGet:**

```xml
<PackageReference Include="WPF-UI" Version="3.*" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
```

**Vistas a crear:**

| Vista | Función |
|-------|---------|
| `MainWindow.xaml` | Shell con navegación lateral o top bar |
| `GamePickerView.xaml` | Grid virtualizado de carátulas de juego |
| `GameManagerView.xaml` | Editor de logros (izq) + stats (der) |
| `IdlerView.xaml` | Panel de multi-idling con lista compacta |
| `GameCard.xaml` | Card con portada, nombre, botón "Administrar" |
| `AchievementCard.xaml` | Icono + nombre + checkbox + descripción |

**Patrón de navegación:**

```
MainWindow
  └── ContentControl (bound a CurrentView)
        ├── GamePickerView (por defecto)
        ├── GameManagerView (al seleccionar juego)
        └── SettingsView (not implemented)
```

El `MainViewModel` tiene un `CurrentView` que cambia via `ObservableProperty`. Sin Frame, sin URIs, swap directo de ViewModels.

**GamePickerView - Virtualización:**

```xml
<ItemsControl ItemsSource="{Binding Games}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingPanel IsVirtualizing="True"
                               VirtualizationMode="Recycling" />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <local:GameCard />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Descarga de imágenes:**

1. `GamePickerViewModel` llama a `IGameLibraryService.GetOwnedGamesAsync()`
2. Cada juego tiene un `CoverUrl` del CDN de Steam
3. `IImageCacheService.GetOrDownloadAsync(url)` → verifica caché → descarga si falta
4. Binding: `ImageSource="{Binding CachedCover}"`

---

### FASE 3: Servicios de Negocio
> Objetivo: Desbloqueo inteligente, caché, config

**SmartUnlockService:**

```csharp
public async Task UnlockAchievementsAsync(
    IEnumerable<string> achievementIds,
    TimeSpan minDelay,
    TimeSpan maxDelay,
    IProgress<int> progress,
    CancellationToken cancellationToken)
{
    var random = new Random();
    var total = achievementIds.Count();
    var current = 0;

    foreach (var id in achievementIds)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SteamAchievements.SetAchievement(id);
        SteamStats.StoreStats();

        current++;
        progress.Report((int)((double)current / total * 100));

        var delay = TimeSpan.FromSeconds(
            random.Next((int)minDelay.TotalSeconds, (int)maxDelay.TotalSeconds));
        await Task.Delay(delay, cancellationToken);
    }
}
```

**ImageCacheService:**

```
Ruta: %LocalAppData%\SteamManager\cache\images\
Formato: PNG
Clave: {appId}_{achievementId or "cover"}.png
Limpieza: Borrar archivos > 7 días
```

**ConfigService:**

```
Ruta: %LocalAppData%\SteamManager\config.json
Contenido:
- FavoriteGameIds (lista de IDs fijados arriba)
- DefaultUnlockDelay (min/max seconds)
- Theme (Dark/Light/System)
- LastSelectedGameId
```

---

### FASE 4: Mejoras sobre el Original
> Objetivo: Features que el SAM original no tiene (v1.0)

| Feature | Descripción |
|---------|-------------|
| **Desbloqueo inteligente** | Delay aleatorio entre 15-45s por logro |
| **Multi-selección** | Seleccionar múltiples logros y desbloquear en lote |
| **Filtros de logros** | Secretos, desbloqueados, bloqueados |
| **Favorites** | Anclar juegos frecuentes arriba |
| **Búsqueda predictiva** | Filtrado instantáneo de juegos por nombre |
| **Stats editor** | Habilitar/deshabilitar con disclaimer |
| **Reset con confirmación** | Triple confirmación para reset de stats |

---

### FASE 5: Polish y Distribución
> Objetivo: Producción, testing, distribución

| Tarea | Detalle |
|-------|---------|
| Tests unitarios | `SteamNativeTests`, `SmartUnlockTests`, `IdlerTests` |
| Publish | `dotnet publish -r win-x86 --self-contained -p:PublishSingleFile=true` |
| Icono | Icono custom de app (.ico) |
| README | Instrucciones de uso |
| Licencia | GPL v3 |

---

## Diagrama de Capas

```
┌─────────────────────────────────────────────────┐
│                   UI (WPF + WPFUI)              │
│  MainWindow                                     │
│  GamePickerView  GameManagerView                │
│  ViewModels (MVVM bindings)                     │
├─────────────────────────────────────────────────┤
│              Services (Negocio)                 │
│  SmartUnlockService    ImageCacheService        │
│  SteamGameLibraryService    ConfigService     │
├─────────────────────────────────────────────────┤
│              Steam API Layer                    │
│  SteamClient  SteamAchievements  SteamStats     │
│  SteamApps    SteamIcons  SteamCallbackHandler  │
├─────────────────────────────────────────────────┤
│           Native Interop Layer                  │
│  NativeWrapper.cs — vtable extraction           │
│  SteamLoader.cs — DLL loading from registry     │
│  steamclient.dll (from Steam installation)      │
└─────────────────────────────────────────────────┘
```

---

## Orden de Construcción

### Sprint 1 - Core Steam (sin UI)
1. Crear solution y proyecto `SteamManager`
2. `SteamLoader.cs` — carga de `steamclient.dll` desde registro
3. `NativeWrapper.cs` — extracción de vtable y llamadas nativas
4. `SteamClient.cs` — Init/Shutdown/RunCallbacks
5. `SteamAchievements.cs` — lectura de logros
6. `SteamStats.cs` — lectura de stats
7. `SteamApps.cs` — lista de juegos
8. `SteamCallbackHandler.cs` — sistema de callbacks
9. `SteamContext.cs` — modelo de sesión
10. **Test**: Conectar con Steam, leer logros de Spacewar (AppID 480) por consola

### Sprint 2 - UI Básica
10. Crear estructura WPF (App.xaml, MainWindow)
11. `MainViewModel` + shell de navegación
12. `GamePickerViewModel` + `GamePickerView` + `GameCard`
13. Navegación: Click en juego → `GameManagerView`

### Sprint 3 - Editor de Logros
14. `GameManagerViewModel` + `GameManagerView`
15. `AchievementViewModel` + `AchievementCard`
16. `StatViewModel` + `StatInfo`
17. Toggle de logros + StoreStats
18. Stats editor con protección

### Sprint 4 - Servicios
19. `ImageCacheService` — descarga de portadas/iconos
20. `SmartUnlockService` — desbloqueo con delays
21. `ConfigService` — persistencia de settings
22. Filtros, búsqueda, favorites

### Sprint 5 - Polish
23. Tests unitarios
24. Animaciones y transiciones WPF
25. Icono de app, nombre, branding
26. Publish como single exe
27. README
