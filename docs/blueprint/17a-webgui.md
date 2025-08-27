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

## 24.3. Solution Layout

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

## 24.4. Integration into Monolith (/SnapDog2)

### 24.4.1. Project References

In `SnapDog2.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="../SnapDog2.WebUi/SnapDog2.WebUi.csproj" />
  <ProjectReference Include="../SnapDog2.WebUi.ApiClient/SnapDog2.WebUi.ApiClient.csproj" />
  <ProjectReference Include="../SnapDog2.WebUi.Assets/SnapDog2.WebUi.Assets.csproj" />
</ItemGroup>
```

### 24.4.2. Program.cs (Host)

**Note:** The WebUi is built as a **Razor Class Library (RCL)** and has **no own Program.cs**. It is mapped only inside the monolith (`/SnapDog2`) via `MapRazorComponents<App>()`.

```csharp
using Microsoft.Extensions.FileProviders;
using SnapDog2.WebUi;
using SnapDog2.WebUi.Assets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddHttpClient<ISnapDogApi, SnapDogApi>(c =>
{
    c.BaseAddress = new Uri("/api/");
});

var app = builder.Build();

var assetsAssembly = typeof(Marker).Assembly;
var embedded = new ManifestEmbeddedFileProvider(assetsAssembly, "EmbeddedWebRoot");
app.UseStaticFiles(new StaticFileOptions { FileProvider = embedded });

if (builder.Configuration.GetValue("ASPNETCORE_FEATURE_WEBUI", true))
{
    app.MapRazorComponents<App>().WithStaticAssets();
}

app.Run();
```

### 24.4.3. SnapDog2.WebUi.Assets.csproj

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

## 24.5. Tests

- Continue using `/SnapDog2.Tests` but add two test tracks:
  - **bUnit** for Razor component unit tests → reference `/SnapDog2.WebUi`.
  - **Playwright** for E2E tests → launches `SnapDog2` as test host.

## 24.6. Advantages of Project Split

- Clear **separation**: host logic ≠ UI ≠ API client ≠ assets.
- **Clean build**: NSwag codegen decoupled from host.
- **Testability:** UI & integration tests run isolated.
- **Deployment** remains simple: all assemblies bundled in single‑file publish.

## 24.7. Folder Reality & Quickstart

- **Host project:** `/SnapDog2`
- **Tests:** `/SnapDog2.Tests`
- **New projects:** `/SnapDog2.WebUi`, `/SnapDog2.WebUi.ApiClient`, `/SnapDog2.WebUi.Assets`

**Quickstart**

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

## 24.8. Next Steps

- Scaffold projects (`dotnet new classlib` for Assets + ApiClient, `dotnet new razorclasslib` for WebUi).
- Reference them in `SnapDog2` and extend `Program.cs` with `MapRazorComponents<App>()`.
- Implement dummy components (`ZoneCard`, `ClientChip`) + embedded CSS.
- Extend CI build: NSwag codegen + bUnit + Playwright.
