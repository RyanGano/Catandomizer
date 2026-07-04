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
- The board seed is either caller-supplied (`id` route parameter) or a random int, and is echoed back in the response so a specific layout can be reproduced by requesting the same id.
- Water/harbor spaces are generated separately from `m_harborTypes`, interleaved with `null` (no-harbor) spaces.
- Domain model (`LandSpace`, `LandValue`, `WaterSpace`, enums `LandType`/`HarborType`) is separate from the wire format (`LandSpaceDto`, `WaterSpaceDto`, `BoardStateDto`), which converts enums to strings for JSON.

### API surface (Program.cs)

- `GET /getboard` — random board, random values.
- `GET /getboard/default` — fixed/"beginner" layout (`shuffleValues=false`), uses the default tile ordering as-is.
- `GET /getboard/{id}` — regenerate a specific board by seed.
- `GET /` — status/version string.
- CORS is locked down via `AllowCatandomizerApp` policy to `localhost:5052` and the deployed app origin; only the `/getboard` routes require CORS.

### Client rendering (CatandomizerApp)

- [Index.razor](CatandomizerApp/Pages/Index.razor) defines its own local DTO mirror classes (`BoardState`, `LandSpace`, `WaterSpace`) rather than sharing types with the service — keep both in sync manually if the service DTO shape changes.
- Board layout is rendered as absolutely-positioned rows of hex images; `boardSpaces`/`harborSpaces` arrays are indexed directly into fixed pixel-offset markup, so the land/harbor space ordering returned by the service must match the indices the markup expects.
- Land tile images map by `LandType` string to `wwwroot/assets/*.png`; harbor tiles similarly map by `HarborType` string to `*Harbor.png` assets (or `NoHarbor.png`).
