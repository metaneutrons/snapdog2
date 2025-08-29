# 24. SnapDog2 Web UI – Architecture & Implementation Blueprint (v3, English)

> Goal: A modern .NET web interface fully integrated into the monolithic application, supporting Dark/Light mode, zone control, client status & drag‑and‑drop reassignment, with **no external asset dependencies** (all static assets embedded). Blueprint is AI‑implementable (fine‑grained tasks, code skeletons, tests).

## 24.1. Product Goals & Principles

**Goals**

- Fast, robust control surface for SnapDog2 with reactive UI.
- Seamless integration into the existing C# monolith (no separate Node build, no runtime asset folders).
- Use of **modern ASP.NET Core Razor Components (SSR + Interactivity)** for low latency and high productivity.
- First‑class UX for **Zones** (Play/Pause/Next/Prev, Volume/Mute, Shuffle/Repeat, current track/playlist) and **Clients** (status, volume/mute, **drag‑and‑drop** between zones).
- **Dark/Light mode** with CSS variables (system auto + manual toggle, persisted).

**Non‑Goals (v1)**

- No separate frontend repository, no SPA‑only approach (focus on Server Rendering + Progressive Interactivity).
- No complex theming framework – lightweight design tokens (CSS vars) and accessible components.

## 24.2. Technology Stack & Packaging

- **Framework:** ASP.NET Core (Razor Components, SSR + interactive server components). No external Node build.
- **Runtime Model:** UI hosted **in‑process** with the monolith.
- **REST Access:** Strongly typed, generated API client (NSwag MSBuild) + Typed HttpClient + Polly (Retry/Timeout) + Cancellation.
- **Realtime/Freshness:** Optimized polling (500–2000 ms depending on info) + optimistic UI. Optional SignalR channel (v2) for push.
- **Theming:** CSS variables + `prefers-color-scheme`. Theme toggle (localStorage), semantic tokens (Success/Warning/Error).
- **Static Assets embedded:**
  - All CSS/Images/Icons as **EmbeddedResource** + **ManifestEmbeddedFileProvider**.
  - Optionally: **Razor Class Library (RCL)** as code carrier; assets still embedded (no external wwwroot).
- **Publish:** Single‑file, trim‑capable, ReadyToRun optional. No external webroot files.
- **Telemetry:** OpenTelemetry (tracing + metrics), Serilog/ETW, UI events (ZoneMove, VolumeChange) as custom events.

## 24.3. Visual Design & Typography

### 24.3.1. UI Layout Structure

The application uses a simple, single-layout design focused on audio control functionality.

#### 24.3.1.1. Main Interface Layout (`/`)

- Single-page application with vertical scrolling layout
- Each zone gets its own dedicated section, stacked vertically
- No separate routes for zones, clients, or settings
- Clean, focused interface optimized for audio control

#### 24.3.1.2. Zone Section Components

**Zone Header**

- Zone name displayed prominently using Orbitron font (text-h1)
- Clear visual separation between zones

**CD-Player Style Media Controls**

- Central media player section with classic CD player aesthetic
- Current track metadata display (artist, album, track title, duration)
- Transport controls: Play/Pause, Previous, Next, Stop
- Progress bar with seek functionality
- Volume and mute controls

**Playlist Management**

- Elegant playlist selector integrated above transport controls
- Dropdown or expandable list showing all available playlists from internal playlist manager
- Track listing from selected playlist with clickable track selection
- Visual indication of currently playing track

**Client Management Zone**

- Bottom section of each zone containing client chips
- Draggable client boxes with:
  - Client name (Orbitron font, text-h3)
  - Connection status indicator (red/green circle)
  - Drag handle affordance
- Drag-and-drop functionality between zones
- Real-time visual feedback during drag operations

#### 24.3.1.3. Additional Endpoints

**Status Page (`/status`)**

- System health and metrics overview
- Current configuration display (read-only, env-var based)
- Performance metrics and operational status
- Telemetry and diagnostic information
- No configuration editing capabilities (strict env-var based config)

### 24.3.2. Technical Implementation Details

#### 24.3.2.1. Drag-and-Drop Client Assignment

**API Integration**

- Uses `PUT /api/v1/clients/{clientIndex}/zone` endpoint
- Request body contains target zone index (1-based)
- Returns 204 No Content on success
- Error handling for invalid client/zone combinations

**User Experience Flow**

1. User drags client chip from source zone
2. Visual feedback shows valid drop zones during drag
3. Drop onto target zone triggers API call
4. Optimistic UI update with rollback on API failure
5. Real-time reflection of zone membership changes

**Technical Implementation**

- HTML5 Drag and Drop API with Blazor integration
- Polly retry policies for API resilience
- SignalR or polling for real-time state synchronization
- Smooth animations using CSS transitions

#### 24.3.2.2. Data Fetching Strategy

**Playlist Data**

- `GET /api/v1/media/playlists` - Fetch available playlists
- `GET /api/v1/media/playlists/{playlistIndex}/tracks` - Get tracks for playlist
- Cache playlist metadata with periodic refresh

**Zone State Polling**

- `GET /api/v1/zones/{zoneIndex}` - Full zone state
- `GET /api/v1/zones/{zoneIndex}/track/info` - Current track metadata
- Optimized polling intervals (500-2000ms based on activity)

**Client State Management**

- `GET /api/v1/clients` - All client states and assignments
- Real-time updates for connection status changes
- Efficient diff-based UI updates

### 24.3.3. Typography Foundation

#### 24.3.3.1. Primary Font: Orbitron

- **Source:** [Google Fonts Orbitron](https://fonts.google.com/specimen/Orbitron)
- **Character:** Futuristic, geometric sans-serif with distinctive digital/tech aesthetic
- **Weights:** 400 (Regular), 500 (Medium), 700 (Bold), 900 (Black)
- **Usage:** Headers, navigation, zone names, client indexentifiers, button labels
- **Embedding:** Via Google Fonts CDN or self-hosted for offline capability

#### 24.3.3.2. Secondary Font: System Font Stack

- **Fallback:** `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`
- **Usage:** Body text, descriptions, metadata, secondary information
- **Rationale:** Optimal readability for longer text content

### 24.3.4. Design Language

#### 24.3.4.1. Theme: Modern Audio Control Interface

- **Aesthetic:** Clean, minimalist with subtle sci-fi/digital influence from Orbitron
- **Visual Hierarchy:** Strong typography contrast (Orbitron headers vs. system body text)
- **Spacing:** Generous whitespace, consistent 8px grid system
- **Interaction:** Smooth transitions, hover states, progressive disclosure

#### 24.3.4.2. Component Styling Principles

- **Zone Cards:** Prominent Orbitron titles, clear status indicators, rounded corners
- **Client Chips:** Compact Orbitron labels, drag handles, status badges
- **Controls:** Geometric buttons echoing Orbitron's angular character
- **Navigation:** Clean Orbitron headers with subtle spacing

### 24.3.5. Color Palette & Dark/Light Mode

#### 24.3.5.1. Light Mode

```css
:root {
  --color-primary: #2563eb;        /* Blue accent */
  --color-background: #ffffff;     /* Clean white */
  --color-surface: #f8fafc;       /* Subtle gray */
  --color-text-primary: #1e293b;  /* Dark slate */
  --color-text-secondary: #64748b; /* Medium slate */
  --color-border: #e2e8f0;        /* Light border */
}
```

## 24.4. Dark Mode

```css
[data-theme="dark"] {
  --color-primary: #3b82f6;        /* Brighter blue */
  --color-background: #0f172a;     /* Deep slate */
  --color-surface: #1e293b;       /* Slate surface */
  --color-text-primary: #f1f5f9;  /* Light text */
  --color-text-secondary: #94a3b8; /* Muted text */
  --color-border: #334155;        /* Dark border */
}
```

## 24.5. Semantic Colors

- **Success:** `#10b981` (Green) - Playing state, connected clients
- **Warning:** `#f59e0b` (Amber) - Buffering, reconnecting
- **Error:** `#ef4444` (Red) - Disconnected, errors
- **Info:** `#06b6d4` (Cyan) - Information states

## 24.6. Typography Scale

```css
.text-display {
  font-family: 'Orbitron', sans-serif;
  font-size: 2.25rem;
  font-weight: 700;
  line-height: 1.2;
}

.text-h1 {
  font-family: 'Orbitron', sans-serif;
  font-size: 1.875rem;
  font-weight: 600;
  line-height: 1.3;
}

.text-h2 {
  font-family: 'Orbitron', sans-serif;
  font-size: 1.5rem;
  font-weight: 500;
  line-height: 1.4;
}

.text-h3 {
  font-family: 'Orbitron', sans-serif;
  font-size: 1.25rem;
  font-weight: 500;
  line-height: 1.4;
}

.text-body {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  font-size: 1rem;
  font-weight: 400;
  line-height: 1.6;
}

.text-small {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  font-size: 0.875rem;
  font-weight: 400;
  line-height: 1.5;
}
```

## 24.7. Component Visual Guidelines

## 24.8. Zone Cards

- Orbitron font for zone names (text-h2)
- System font for track metadata
- Rounded corners (8px)
- Subtle shadows in light mode, borders in dark mode

## 24.9. Client Management

- Orbitron font for client names (text-h3)
- Compact chips with clear drag affordances
- Status indicators using semantic colors
- Smooth drag-and-drop animations

## 24.10. Navigation & Controls

- Orbitron for primary navigation items
- Consistent button sizing and spacing
- Clear focus states for accessibility
- Hover effects that respect motion preferences

## 24.11. Solution Layout

```plaintext
SnapDog2.sln
├─ SnapDog2/                         # existing monolith (host)
├─ SnapDog2.Tests/                   # existing tests
├─ SnapDog2.WebUi/                   # Razor Components (UI Shell + Pages + Components)
├─ SnapDog2.WebUi.Assets/            # Embedded assets (css, icons, images)
├─ SnapDog2.WebUi.ApiClient/         # NSwag‑generated strong REST client
└─ build/tools/ ...                  # (optional) NSwag.json, codegen scripts
```

**Why split projects?**

- **Maintainability:** UI code, generated client, and assets are separated.
- **Build & CI:** NSwag codegen isolated, UI tests target `/SnapDog2.WebUi`.
- **Single‑file/Trim:** Assets embedded, no loose wwwroot.

## 24.12. Integration into Monolith (/SnapDog2)

### 24.12.1. Project References

In `SnapDog2.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="../SnapDog2.WebUi/SnapDog2.WebUi.csproj" />
  <ProjectReference Include="../SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj" />
  <ProjectReference Include="../SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj" />
</ItemGroup>
```

### 24.12.2. Program.cs (Host)

**Note:** The WebUi is built as a **Razor Class Library (RCL)** and has **no own Program.cs**. It is mapped only inside the monolith (`/SnapDog2`) via `MapRazorComponents<App>()`.

```csharp
using Microsoft.Extensions.FileProviders;
using SnapDog2.WebUi;
using SnapDog2.WebUi.Assets;
using SnapDog2.WebUi.ApiClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ISnapDogApiClient, SnapDogApiClient>(c =>
{
    c.BaseAddress = new Uri($"http://localhost:{snapDogConfig.Http.HttpPort}/api/v1/");
});

var app = builder.Build();

var assetsAssembly = typeof(Marker).Assembly;
var embedded = new ManifestEmbeddedFileProvider(assetsAssembly, "EmbeddedWebRoot");
app.UseStaticFiles(new StaticFileOptions { FileProvider = embedded });

if (snapDogConfig.Http.WebUiEnabled)
{
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
}

app.Run();
```

### 24.12.3. SnapDog2.WebUi.Assets.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
    <EnableDefaultItems>false</EnableDefaultItems>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="EmbeddedWebRoot/**/*" />
    <Compile Include="Marker.cs" />
  </ItemGroup>
</Project>
```

## 24.13. Tests

- Continue using `/SnapDog2.Tests` but add two test tracks:
  - **bUnit** for Razor component unit tests → reference `/SnapDog2.WebUi`.
  - **Playwright** for E2E tests → launches `SnapDog2` as test host.

## 24.14. Advantages of Project Split

- Clear **separation**: host logic ≠ UI ≠ API client ≠ assets.
- **Clean build**: NSwag codegen decoupled from host.
- **Testability:** UI & integration tests run isolated.
- **Deployment** remains simple: all assemblies bundled in single‑file publish.

## 24.15. Folder Reality & Quickstart

- **Host project:** `/SnapDog2`
- **Tests:** `/SnapDog2.Tests`
- **New projects:** `/SnapDog2.WebUi`, `/SnapDog2.WebUi.ApiClient`, `/SnapDog2.WebUi.Assets`

### 24.15.1. Quickstart

1. Create new projects: `SnapDog2.WebUi` (**Razor Class Library**), `SnapDog2.WebUi.ApiClient` (ClassLib), `SnapDog2.WebUi.Assets` (ClassLib).
2. Add project references (see 4.1) into `/SnapDog2.csproj`.
3. Wire `Program.cs` in monolith as in 4.2 (Razor Components + EmbeddedFileProvider for assets). **The RCL itself has no Program.cs.**
4. Set up NSwag codegen (`build/tools/NSwag.json`, runtime **Net90**) and run during build.
5. Extend test setup in `/SnapDog2.Tests`:
   - **bUnit:** `dotnet add SnapDog2.Tests package Bunit`
   - **Playwright:** `dotnet add SnapDog2.Tests package Microsoft.Playwright.MSTest` (or xUnit/NUnit) and `pwsh bin/Debug/net9.0/playwright.ps1 install`
   - Accessibility (optional): `dotnet add SnapDog2.Tests package Deque.AxeCore.Playwright`
6. Add first components (`ZoneCard`, `ClientChip`) + embedded CSS (RCL assets).
7. Run `dotnet run` in `/SnapDog2` → UI available, assets embedded, no external wwwroot.

## 24.16. Next Steps

- Scaffold projects (`dotnet new classlib` for Assets + ApiClient, `dotnet new razorclasslib` for WebUi).
- Reference them in `SnapDog2` and extend `Program.cs` with `MapRazorComponents<App>()`.
- Implement dummy components (`ZoneCard`, `ClientChip`) + embedded CSS.
- Extend CI build: NSwag codegen + bUnit + Playwright.

## 24.17. Implementation Guide

> **Critical for AI Success**: This section provides exact file contents, commands, and validation steps to ensure reliable AI implementation of the WebUI blueprint.

## 24.18. Project Creation Commands (Exact Sequence)

**Step 1: Create Projects**

```bash
# From SnapDog2 solution root
dotnet new razorclasslib -n SnapDog2.WebUi -o SnapDog2.WebUi
dotnet new classlib -n SnapDog2.WebUi.ApiClient -o SnapDog2.WebUi.ApiClient
dotnet new classlib -n SnapDog2.WebUi.Assets -o SnapDog2.WebUi.Assets

# Add to solution
dotnet sln add SnapDog2.WebUi/SnapDog2.WebUi.csproj
dotnet sln add SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj
dotnet sln add SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj
```

**Step 2: Add Project References**

```bash
# Add references to main project
dotnet add SnapDog2/SnapDog2.csproj reference SnapDog2.WebUi/SnapDog2.WebUi.csproj
dotnet add SnapDog2/SnapDog2.csproj reference SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj
dotnet add SnapDog2/SnapDog2.csproj reference SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj

# WebUi references ApiClient
dotnet add SnapDog2.WebUi/SnapDog2.WebUi.csproj reference SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj
```

## 24.19. Exact Project File Contents

**SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
    <EnableDefaultItems>false</EnableDefaultItems>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="EmbeddedWebRoot/**/*" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="Marker.cs" />
  </ItemGroup>
</Project>
```

**SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Http.Polly" />
    <PackageReference Include="NSwag.MSBuild">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>

  <!-- Enterprise-Grade NSwag API Client Generation -->
  <PropertyGroup>
    <OpenApiSpecPath>$(MSBuildProjectDirectory)/../SnapDog2/snapdog-api.json</OpenApiSpecPath>
    <GeneratedClientPath>$(MSBuildProjectDirectory)/Generated/SnapDogApiClient.cs</GeneratedClientPath>
  </PropertyGroup>

  <Target Name="ValidateOpenApiSpec" BeforeTargets="GenerateApiClient">
    <Error Text="OpenAPI specification not found at: $(OpenApiSpecPath). Run 'dotnet run --project SnapDog2.SwaggerGen' first."
           Condition="!Exists('$(OpenApiSpecPath)')" />
    <Message Text="✅ OpenAPI specification found: $(OpenApiSpecPath)" Importance="high" />
  </Target>

  <Target Name="GenerateApiClient"
          BeforeTargets="CoreCompile"
          DependsOnTargets="ValidateOpenApiSpec"
          Inputs="$(OpenApiSpecPath)"
          Outputs="$(GeneratedClientPath)">

    <Message Text="🔄 Generating API client from OpenAPI specification..." Importance="high" />

    <MakeDir Directories="$(MSBuildProjectDirectory)/Generated" />

    <Exec Command="$(NSwagExe_Net90) openapi2csclient /input:&quot;$(OpenApiSpecPath)&quot; /output:&quot;$(GeneratedClientPath)&quot; /namespace:SnapDog2.WebUi.ApiClient.Generated /className:GeneratedSnapDogClient /generateClientInterfaces:true /clientInterfaceName:IGeneratedSnapDogClient /injectHttpClient:true /useBaseUrl:false /generateExceptionClasses:true /exceptionClass:ApiException /wrapDtoExceptions:true /useHttpClientCreationMethod:false /generateOptionalParameters:true /generateJsonMethods:false /enforceFlagEnums:false /nullValue:Null /generateDefaultValues:true /generateDataAnnotations:false /excludedTypeNames: /handleReferences:false /generateImmutableArrayProperties:false /generateImmutableDictionaryProperties:false /jsonLibrary:SystemTextJson /arrayType:System.Collections.Generic.ICollection /dictionaryType:System.Collections.Generic.IDictionary /arrayInstanceType:System.Collections.Generic.List /dictionaryInstanceType:System.Collections.Generic.Dictionary /arrayBaseType:System.Collections.Generic.ICollection /dictionaryBaseType:System.Collections.Generic.IDictionary"
          ContinueOnError="false" />

    <Message Text="✅ API client generated successfully: $(GeneratedClientPath)" Importance="high" />
  </Target>

  <Target Name="CleanGeneratedClient" BeforeTargets="CoreClean">
    <Delete Files="$(GeneratedClientPath)" />
  </Target>
</Project>
```

**SnapDog2.WebUi/SnapDog2.WebUi.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
  </PropertyGroup>

  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj" />
  </ItemGroup>
</Project>
```

## 24.20. Required File Structure and Contents

**SnapDog2.WebUi.Assets/Marker.cs**

```csharp
namespace SnapDog2.WebUi.Assets;

/// <summary>
/// Marker class for embedded asset assembly identification.
/// </summary>
public static class Marker
{
    // This class serves as a type marker for the ManifestEmbeddedFileProvider
}
```

**SnapDog2.WebUi.Assets/EmbeddedWebRoot/css/app.css**

```css
@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@400;500;700;900&display=swap');

:root {
  --color-primary: #2563eb;
  --color-background: #ffffff;
  --color-surface: #f8fafc;
  --color-text-primary: #1e293b;
  --color-text-secondary: #64748b;
  --color-border: #e2e8f0;
  --font-orbitron: 'Orbitron', sans-serif;
  --font-system: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
}

[data-theme="dark"] {
  --color-primary: #3b82f6;
  --color-background: #0f172a;
  --color-surface: #1e293b;
  --color-text-primary: #f1f5f9;
  --color-text-secondary: #94a3b8;
  --color-border: #334155;
}

.text-h1 {
  font-family: var(--font-orbitron);
  font-size: 1.875rem;
  font-weight: 600;
  line-height: 1.3;
}

.text-h3 {
  font-family: var(--font-orbitron);
  font-size: 1.25rem;
  font-weight: 500;
  line-height: 1.4;
}

.zone-section {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  margin-bottom: 2rem;
  padding: 1.5rem;
}

.client-chip {
  display: inline-flex;
  align-items: center;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  padding: 0.5rem 1rem;
  margin: 0.25rem;
  cursor: grab;
  font-family: var(--font-orbitron);
}

.client-chip:active {
  cursor: grabbing;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-right: 0.5rem;
}

.status-connected {
  background-color: #10b981;
}

.status-disconnected {
  background-color: #ef4444;
}
```

## 24.21. Core Component Skeletons

**SnapDog2.WebUi/App.razor**

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>SnapDog2</title>
    <base href="/" />
    <link href="css/app.css" rel="stylesheet" />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

**SnapDog2.WebUi/Components/Routes.razor**

```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
    </Found>
    <NotFound>
        <h1>Not found</h1>
        <p>Sorry, there's nothing at this address.</p>
    </NotFound>
</Router>
```

**SnapDog2.WebUi/Components/Layout/MainLayout.razor**

```razor
@inherits LayoutView

<main>
    @Body
</main>
```

**SnapDog2.WebUi/Components/Pages/Home.razor**

```razor
@page "/"
@using SnapDog2.WebUi.ApiClient
@inject ISnapDogApiClient ApiClient

<PageTitle>SnapDog2</PageTitle>

<div class="zones-container">
    @if (zones != null)
    {
        @foreach (var zone in zones)
        {
            <ZoneSection Zone="zone" @key="zone.Index" />
        }
    }
</div>

@code {
    private List<ZoneState>? zones;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Load zones - implement according to your API structure
            // zones = await ApiClient.GetAllZonesAsync();
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }
}
```

## 24.22. API Client Integration Pattern

**SnapDog2.WebUi.ApiClient/ISnapDogApiClient.cs**

```csharp
namespace SnapDog2.WebUi.ApiClient;

/// <summary>
/// Business-focused API client interface for SnapDog operations.
/// Abstracts away generated client implementation details.
/// </summary>
public interface ISnapDogApiClient
{
    // Zone Operations
    Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default);
    Task<ZoneState> GetZoneAsync(int zoneIndex, CancellationToken cancellationToken = default);
    Task SetZoneVolumeAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
    Task ToggleZoneMuteAsync(int zoneIndex, CancellationToken cancellationToken = default);

    // Client Operations
    Task<ClientState[]> GetAllClientsAsync(CancellationToken cancellationToken = default);
    Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default);

    // Playlist Operations
    Task<PlaylistInfo[]> GetPlaylistsAsync(CancellationToken cancellationToken = default);
    Task SetZonePlaylistAsync(int zoneIndex, int playlistIndex, CancellationToken cancellationToken = default);
}
```

**SnapDog2.WebUi.ApiClient/SnapDogApiClient.cs**

```csharp
using Microsoft.Extensions.Logging;
using Polly;
using System.Net;

namespace SnapDog2.WebUi.ApiClient;

/// <summary>
/// Enterprise API client implementation with resilience, logging, and business logic.
/// Wraps the generated transport client with enterprise patterns.
/// </summary>
public partial class SnapDogApiClient : ISnapDogApiClient
{
    private readonly IGeneratedSnapDogClient _generatedClient;
    private readonly ILogger<SnapDogApiClient> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public SnapDogApiClient(
        IGeneratedSnapDogClient generatedClient,
        ILogger<SnapDogApiClient> logger)
    {
        _generatedClient = generatedClient;
        _logger = logger;
        _retryPolicy = CreateRetryPolicy();
    }

    public async Task<ZoneState[]> GetAllZonesAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            LogFetchingZones();
            var zones = await _generatedClient.GetZonesAsync(cancellationToken);
            LogRetrievedZones(zones.Count);
            return zones.ToArray();
        });
    }

    public async Task AssignClientToZoneAsync(int clientIndex, int zoneIndex, CancellationToken cancellationToken = default)
    {
        // Business validation
        if (clientIndex < 1) throw new ArgumentException("Client index must be >= 1", nameof(clientIndex));
        if (zoneIndex < 1) throw new ArgumentException("Zone index must be >= 1", nameof(zoneIndex));

        await _retryPolicy.ExecuteAsync(async () =>
        {
            LogAssigningClient(clientIndex, zoneIndex);

            try
            {
                await _generatedClient.AssignClientToZoneAsync(clientIndex, zoneIndex, cancellationToken);
                LogClientAssigned(clientIndex, zoneIndex);
            }
            catch (ApiException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                LogClientOrZoneNotFound(clientIndex, zoneIndex);
                throw new InvalidOperationException($"Client {clientIndex} or zone {zoneIndex} not found", ex);
            }
        });
    }

    private static IAsyncPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts
                });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching all zones")]
    private partial void LogFetchingZones();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Retrieved {ZoneCount} zones")]
    private partial void LogRetrievedZones(int ZoneCount);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Assigning client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogAssigningClient(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Successfully assigned client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogClientAssigned(int ClientIndex, int ZoneIndex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Client {ClientIndex} or zone {ZoneIndex} not found")]
    private partial void LogClientOrZoneNotFound(int ClientIndex, int ZoneIndex);

    // Implement other methods...
}
```

## 24.23. Program.cs Integration

**Add to SnapDog2/Program.cs (after existing services)**

```csharp
// WebUI Configuration (add after existing service registrations)
if (snapDogConfig.Http.WebUiEnabled)
{
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Register generated transport client
    builder.Services.AddHttpClient<IGeneratedSnapDogClient, GeneratedSnapDogClient>(client =>
    {
        var baseUrl = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? $"http://localhost:{snapDogConfig.Http.HttpPort}/api/v1/"
            : $"http://127.0.0.1:{snapDogConfig.Http.HttpPort}/api/v1/";

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "SnapDog2-WebUI/1.0");
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

    // Register business API client
    builder.Services.AddScoped<ISnapDogApiClient, SnapDogApiClient>();

    Log.Information("🌐 WebUI enabled with resilient API client configured");
}

// Add at the end of the app configuration
if (snapDogConfig.Http.WebUiEnabled)
{
    try
    {
        // Configure embedded assets
        var assetsAssembly = typeof(SnapDog2.WebUi.Assets.Marker).Assembly;
        var embedded = new ManifestEmbeddedFileProvider(assetsAssembly, "EmbeddedWebRoot");
        app.UseStaticFiles(new StaticFileOptions { FileProvider = embedded });

        // Map Razor components
        app.MapRazorComponents<SnapDog2.WebUi.App>()
            .AddInteractiveServerRenderMode();

        Log.Information("🌐 WebUI routes configured at {Path}", snapDogConfig.Http.WebUiPath);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to configure WebUI");
        throw;
    }
}

// Add these helper methods
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("API call retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(10); // 10 second timeout
}
```

## 24.24. Build and Validation Steps

**Step 1: Initial Build Test**

```bash
# Should build without errors
dotnet build SnapDog2.sln
```

**Step 2: Generate API Specification**

```bash
# Build main project first
dotnet build SnapDog2

# Generate swagger.json using dedicated tool
# Creates swagger.json in root of code base
dotnet run --project SnapDog2.SwaggerGen
```

**Step 3: Build API Client (with automatic generation)**

```bash
# Build triggers automatic API client generation with validation
dotnet build SnapDog2.WebUi.ApiClient

# Verify generated client exists
ls -la SnapDog2.WebUi.ApiClient/Generated/SnapDogApiClient.cs
```

**Step 4: Full Solution Build**

```bash
# Build entire solution with generated API client
dotnet build SnapDog2.sln --configuration Release
```

## 24.25. Implementation Success Factors

### 24.25.1. Requirements for AI Success

1. **Exact project references**: Follow the reference chain exactly as specified
2. **Embedded assets**: Ensure `GenerateEmbeddedFilesManifest=true` and correct EmbeddedResource pattern
3. **Configuration gating**: All WebUI services must be wrapped in `if (snapDogConfig.Http.WebUiEnabled)`
4. **API client generation**: NSwag must run after API specification is available
5. **Component hierarchy**: App.razor → Routes.razor → MainLayout.razor → Pages

### 24.25.2. Common Implementation Failures

- Missing ManifestEmbeddedFileProvider configuration
- Incorrect project reference order
- NSwag running before API spec generation
- Missing AddRazorComponents() service registration
- Forgetting InteractiveServerComponents configuration

### 24.25.3. Implementation Validation

- [ ] All projects build without errors
- [ ] Assets are properly embedded (check .deps.json for EmbeddedWebRoot)
- [ ] API client is generated with correct interface
- [ ] WebUI loads without 404 errors on assets
- [ ] Components render with proper Orbitron fonts
- [ ] Dark/light mode toggle works
- [ ] Drag-and-drop interaction responds

This implementation guide provides prescriptive instructions that eliminate common AI implementation failures through exact commands, file contents, and validation steps.

## 24.26. Blueprint Completion

The SnapDog2 WebUI blueprint is now complete and ready for AI-assisted implementation:

✅ **Orbitron Typography Integration** - Complete visual design specification with Google Fonts integration
✅ **Configuration Alignment** - Properly aligned with existing HttpConfig.WebUiEnabled pattern
✅ **UI Layout Design** - Single-page vertical zone layout with drag-and-drop client management
✅ **AI Implementation Guide** - Comprehensive guide with exact file contents and commands
✅ **Document Structure** - Proper markdown heading hierarchy and organization

The blueprint provides all necessary specifications, implementation patterns, and validation steps for reliable AI-generated WebUI implementation.

**Problem 4: API Client Interface Mismatch**

- **Solution**: Generate client first, then create matching interface
- **Pattern**: Let NSwag create the implementation, create minimal interface

This comprehensive guide should make the blueprint perfectly AI-implementable with exact commands, file contents, and validation steps!
