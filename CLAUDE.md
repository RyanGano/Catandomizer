# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Catandomizer generates random (valid) board layouts for the Settlers of Catan tabletop game. It is split into two independent .NET 7 projects with no solution file linking them:

- **CatandomizerService** — an ASP.NET Core minimal API (`Microsoft.NET.Sdk.Web`) that computes a board layout and serves it as JSON.
- **CatandomizerApp** — a Blazor WebAssembly client (`Microsoft.NET.Sdk.BlazorWebAssembly`) that calls the service and renders the board using hex tile images.

There are no automated tests in this repo.

## Common commands

Run from within each project directory (there is no top-level `.sln`):

```
# Service
cd CatandomizerService
dotnet run                 # serves on http://localhost:5149 (see launchSettings.json)
dotnet build

# App
cd CatandomizerApp
dotnet run                 # serves on http://localhost:5052 (see launchSettings.json)
dotnet build
```

The app is hardcoded to call the deployed service at `https://catandomizerservice.azurewebsites.net/getboard` (see [Index.razor](CatandomizerApp/Pages/Index.razor)). To test against a local service instance, uncomment the `localhost:5149` line in `RecalculateAsync` and comment out the production URL.

## Architecture

### Board generation (CatandomizerService)

- `BoardState.CreateAsync(seed, shuffleValues)` is the entry point. It builds 19 `LandSpace` hexes with a fixed adjacency graph (hardcoded index-based connections mirroring a standard Catan board layout), optionally shuffles land types/values/harbor types using a seeded `Random`, then assigns number values so that no two adjacent tiles share a value or have two adjacent "red" (6/8) values.
- Value assignment (`AddLandValues`/`FirstAvailableValue`/`ReplaceWithValues`) is a constraint-satisfaction pass: it tries to place each remaining value on a tile whose neighbors all tolerate it (`LandValue.CanBeNextTo`); if no value works directly, it swaps values between tiles to resolve the conflict.
- `BoardState`'s constructor re-validates the whole board (`Validate`) and throws if any adjacency constraint is violated or a non-desert tile has no value — this is the correctness invariant for the whole generator.
- Every response's `Id` is a self-contained **board code** produced by `BoardCode.Encode`: a ~30-char Base64url string bit-packing `[4b version][4b game set][19×3b land types][18×4b values][9×3b harbors]` plus a trailing CRC-8 byte. Requesting `/getboard/{code}` rebuilds the exact board via `BoardState.CreateFromCode` — no RNG involved. `BoardCode.Decode` validates length, CRC, version, game set (`GameSet.Base` only for now), enum ranges, and that tile/value/harbor multisets match the standard Catan distribution; the `BoardState` constructor then re-validates adjacency. Legacy integer seeds are still accepted on the same route (non-negative `int.TryParse` → seed path).
- Water/harbor spaces are generated separately from `m_harborTypes`, interleaved with `null` (no-harbor) spaces.
- Domain model (`LandSpace`, `LandValue`, `WaterSpace`, enums `LandType`/`HarborType`) is separate from the wire format (`LandSpaceDto`, `WaterSpaceDto`, `BoardStateDto`), which converts enums to strings for JSON.

### API surface (Program.cs)

- `GET /getboard` — random board, random values.
- `GET /getboard/default` — fixed/"beginner" layout (`shuffleValues=false`), uses the default tile ordering as-is.
- `GET /getboard/{id}` — regenerate a specific board from a board code (or a legacy integer seed); invalid codes return 400 with a reason.
- `GET /` — status/version string.
- CORS is locked down via `AllowCatandomizerApp` policy to `localhost:5052` and the deployed app origin; only the `/getboard` routes require CORS.

### Client rendering (CatandomizerApp)

- [Models/BoardModels.cs](CatandomizerApp/Models/BoardModels.cs) holds the client's own mirror of the wire format (`BoardState`, `LandSpace`, `WaterSpace`) plus the view models `BoardSpace`/`HarborSpace`. These are **not** shared with the service — keep them in sync manually if the service DTO shape changes. The land-type strings the client switches on (`"Mountain"`, `"Hill"`, …) must match what `LandSpaceDto` emits; the service deliberately sends plain land names (not `"Ore / Mountain"`).
- `BoardSpace`/`HarborSpace` derive everything the UI needs from `Type`/`Value` in their constructors: the tile image, a resource emoji + name, `IsRed` (6/8), `PipCount` (roll probability), and harbor trade ratio (`2:1`/`3:1`). No service change is needed to add these — they are purely client-side.
- Tiles are drawn by the [BoardTile](CatandomizerApp/Shared/BoardTile.razor) and [HarborTile](CatandomizerApp/Shared/HarborTile.razor) components, which layer the resource emoji, number token, and pip dots on top of the hex PNG (styles live in [wwwroot/css/app.css](CatandomizerApp/wwwroot/css/app.css)). A legend below the board maps each emoji to its resource/land and explains harbor ratios.
- Board layout is rendered as absolutely-positioned rows in [Index.razor](CatandomizerApp/Pages/Index.razor); `boardSpaces`/`harborSpaces` arrays are indexed directly into fixed pixel-offset markup, so the land/harbor space ordering returned by the service must match the indices the markup expects. The board wrapper has an explicit height because its rows are absolutely positioned (otherwise following content overlaps it).
- Land tile images map by `LandType` string to `wwwroot/assets/*.png`; harbor tiles similarly map by `HarborType` string to `*Harbor.png` assets (or `NoHarbor.png`).

## Deployment

Both projects deploy to Azure App Service (Windows) via GitHub Actions on push to `master`:

| Project | App Service | URL | Workflow | Publish-profile secret |
| --- | --- | --- | --- | --- |
| CatandomizerService | `CatandomizerService` | catandomizerservice.azurewebsites.net | [.github/workflows/deploy-service.yml](.github/workflows/deploy-service.yml) | `AZURE_SERVICE_PUBLISH_PROFILE` |
| CatandomizerApp | `Catandomizer` | catandomizer.azurewebsites.net | [.github/workflows/deploy-app.yml](.github/workflows/deploy-app.yml) | `AZURE_APP_PUBLISH_PROFILE` |

- Each workflow is **path-filtered** to its own project, so editing one app does not redeploy the other. Both also support manual `workflow_dispatch` runs.
- The Blazor app publishes to a folder containing `web.config` + `wwwroot/`; the whole folder is deployed to the site root and `web.config` rewrites requests into `wwwroot/`.
- The service's CORS policy (`Program.cs`) must include the deployed app origin (`https://catandomizer.azurewebsites.net`) and the app's service URL (`Index.razor`) must point at the deployed service — update both if an App Service is renamed.
- Secrets are set under GitHub repo **Settings → Secrets and variables → Actions**; get each publish profile from the Azure Portal App Service **Overview → Get publish profile**.
