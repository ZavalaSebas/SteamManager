# SteamManager - Plan de Proyecto

## Decisión Técnica Final

| Aspecto | Decisión |
|---------|----------|
| Nombre | **SteamManager** |
| Runtime | .NET 10 |
| UI | WPF + WPFUI |
| Steam API | `steam_api64.dll` via P/Invoke directo (NO steamclient.dll) |
| Patrón | MVVM con CommunityToolkit.Mvvm |
| Empaquetado | `PublishSingleFile` + `SelfContained` = 1 exe portable |
| Caché | `%LocalAppData%\SteamManager\` |

---

## Alcance: Fase Actual vs Futuro

### Fase Actual (v1.0) - Tier 1
Replicar y mejorar las capacidades core del SAM original:

- Gestión de logros (lock/unlock con smart delays)
- Editor de estadísticas
- Explorador de biblioteca con carátulas
- Búsqueda y filtros de juegos
- Favorites (juegos anclados)
- Desbloqueo inteligente con delays aleatorios (anti-detección)
- Caché de imágenes local
- UI moderna con WPFUI (Mica, esquinas redondeadas, grid virtualizado)

### Futuro (v2.0+) - Tier 2, 3 y 4
> Documentado aquí para desarrollo futuro. NO implementar en v1.0.

**Tier 2 - Multi-Idling:**
- Multi-idling por rotación automática (un juego a la vez, rota cada N minutos)
- O multi-proceso (lanzar instancias separadas por juego)
- Requiere investigación de viabilidad con `steam_api64.dll`

**Tier 3 - Funcionalidades sociales y datos:**
- Porcentaje global de obtención por logro
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

```
SteamManager/
├── SteamManager.sln
├── src/
│   └── SteamManager/
│       ├── SteamManager.csproj
│       ├── App.xaml / App.xaml.cs
│       │
│       ├── Steam/                          # Capa de integración con Steam
│       │   ├── SteamNative.cs              # P/Invoke declarations (steam_api64.dll)
│       │   ├── SteamClient.cs              # Wrapper: Init, Shutdown, RunCallbacks
│       │   ├── SteamAchievements.cs        # Lectura/escritura de logros
│       │   ├── SteamStats.cs               # Lectura/escritura de estadísticas
│       │   ├── SteamApps.cs                # Lista de juegos, ownership, playtime
│       │   ├── SteamIcons.cs               # Descarga y caché de iconos/logos
│       │   ├── SteamCallbackHandler.cs     # Sistema de callbacks
│       │   └── SteamContext.cs             # Modelo de sesión activa para un AppID
│       │
│       ├── Models/                         # Modelos de datos
│       │   ├── GameInfo.cs                 # AppId, Name, Playtime, CoverUrl, IsFavorite
│       │   ├── AchievementInfo.cs          # Id, Name, Desc, IsUnlocked, UnlockTime, Icon
│       │   ├── StatInfo.cs                 # Name, Type, Value, Min, Max, Permission
│       │   └── AppSettings.cs              # Configuración persistente
│       │
│       ├── ViewModels/                     # ViewModels MVVM
│       │   ├── MainViewModel.cs            # Shell: navega entre vistas
│       │   ├── GamePickerViewModel.cs      # Selector de juegos con búsqueda
│       │   ├── GameManagerViewModel.cs     # Editor de logros/stats de 1 juego
│       │   ├── AchievementViewModel.cs     # VM de un logro individual
│       │   ├── StatViewModel.cs            # VM de una stat individual
│       │   └── SettingsViewModel.cs        # VM de configuración
│       │
│       ├── Views/                          # Vistas WPF
│       │   ├── MainWindow.xaml             # Ventana principal (shell)
│       │   ├── GamePickerView.xaml         # Grid virtualizado de carátulas
│       │   ├── GameManagerView.xaml        # Editor de logros + stats
│       │   └── SettingsView.xaml           # Configuración
│       │
│       ├── Controls/                       # Controles custom
│       │   ├── GameCard.xaml               # Tarjeta de juego (portada + nombre)
│       │   ├── AchievementCard.xaml        # Tarjeta de logro (icono + nombre + check)
│       │   └── ProgressOverlay.xaml        # Overlay de progreso de desbloqueo
│       │
│       ├── Services/                       # Servicios de negocio
│       │   ├── IGameLibraryService.cs      # Interfaz: obtener juegos del usuario
│       │   ├── GameLibraryService.cs       # Implementación: llama a SteamApps
│       │   ├── IImageCacheService.cs       # Interfaz: caché de imágenes
│       │   ├── ImageCacheService.cs        # Descarga + caché en disco
│       │   ├── IAchievementUnlocker.cs     # Interfaz: desbloqueo con lógica anti-ban
│       │   ├── SmartUnlockService.cs       # Desbloqueo inteligente con delays
│       │   └── ConfigService.cs            # Persistencia de settings (JSON)
│       │
│       ├── Converters/                     # Value converters
│       │   ├── BoolToVisibilityConverter.cs
│       │   ├── AchievementToIconConverter.cs
│       │   └── PercentageToColorConverter.cs
│       │
│       ├── Helpers/                        # Utilidades
│       │   ├── Paths.cs                    # Rutas de caché, datos, etc.
│       │   └── NativeMethods.cs            # P/Invoke auxiliares
│       │
│       ├── Resources/                      # Recursos estáticos
│       │   ├── Styles.xaml                 # Estilos globales
│       │   ├── Icons.xaml                  # Iconos vectoriales
│       │   └── Images/                     # Placeholder images
│       │
│       ├── app.manifest
│       └── launchSettings.json
│
└── tests/
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
| `SteamNative.cs` | Todas las declaraciones DllImport de `steam_api64.dll` |
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

**P/Invoke declarations (steam_api64.dll):**

```csharp
// Lifecycle
SteamAPI_Init() → bool
SteamAPI_Shutdown() → void
SteamAPI_RunCallbacks() → void
SteamAPI_RestartAppIfNecessary(uint appId) → bool

// UserStats
SteamAPI_ISteamUserStats_RequestCurrentStats(IntPtr) → bool
SteamAPI_ISteamUserStats_GetStat(IntPtr, string, ref int) → bool
SteamAPI_ISteamUserStats_GetStat(IntPtr, string, ref float) → bool
SteamAPI_ISteamUserStats_SetStat(IntPtr, string, int) → bool
SteamAPI_ISteamUserStats_SetStat(IntPtr, string, float) → bool
SteamAPI_ISteamUserStats_StoreStats(IntPtr) → bool
SteamAPI_ISteamUserStats_ResetAllStats(IntPtr, bool) → bool

// Achievements
SteamAPI_ISteamUserStats_GetNumAchievements(IntPtr) → uint
SteamAPI_ISteamUserStats_GetAchievementName(IntPtr, uint) → IntPtr (string)
SteamAPI_ISteamUserStats_GetAchievement(IntPtr, string, out bool) → bool
SteamAPI_ISteamUserStats_SetAchievement(IntPtr, string) → bool
SteamAPI_ISteamUserStats_ClearAchievement(IntPtr, string) → bool
SteamAPI_ISteamUserStats_GetAchievementAndUnlockTime(IntPtr, string, out bool, out uint) → bool
SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute(IntPtr, string, string) → IntPtr (string)
SteamAPI_ISteamUserStats_GetAchievementIcon(IntPtr, string) → int (handle)
SteamAPI_ISteamUserStats_IndicateAchievementProgress(IntPtr, string, uint cur, uint max) → bool

// Utils (iconos)
SteamAPI_ISteamUtils_GetImageSize(IntPtr, int, out uint, out uint) → bool
SteamAPI_ISteamUtils_GetImageRGBA(IntPtr, int, byte[], int) → bool

// Apps (biblioteca)
SteamAPI_ISteamApps_IsSubscribedApp(IntPtr, uint) → bool
```

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
2. SteamAPI_RestartAppIfNecessary(appId) → si true, salir
3. SteamAPI_Init() → si false, error
4. SteamUserStats.RequestCurrentStats()
5. Esperar callback UserStatsReceived_t
6. Ya se puede leer logros y stats
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
        └── SettingsView
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
| **Porcentaje de obtención** | Mostrar % global de cada logro |
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
| Publish | `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` |
| Icono | Icono custom de app (.ico) |
| README | Instrucciones de uso |
| Licencia | MIT |

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
│  GameLibraryService    ConfigService            │
├─────────────────────────────────────────────────┤
│              Steam API Layer                    │
│  SteamClient  SteamAchievements  SteamStats     │
│  SteamApps    SteamIcons  SteamCallbackHandler  │
├─────────────────────────────────────────────────┤
│           P/Invoke (steam_api64.dll)            │
│  SteamNative.cs — todas las DllImport           │
└─────────────────────────────────────────────────┘
```

---

## Orden de Construcción

### Sprint 1 - Core Steam (sin UI)
1. Crear solution y proyecto `SteamManager`
2. `SteamNative.cs` — todos los P/Invoke
3. `SteamClient.cs` — Init/Shutdown/RunCallbacks
4. `SteamAchievements.cs` — lectura de logros
5. `SteamStats.cs` — lectura de stats
6. `SteamApps.cs` — lista de juegos
7. `SteamCallbackHandler.cs` — sistema de callbacks
8. `SteamContext.cs` — modelo de sesión
9. **Test**: Conectar con Steam, leer logros de Spacewar (AppID 480) por consola

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
