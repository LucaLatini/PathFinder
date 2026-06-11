# PathFinder

A web-based pathfinding engine built with .NET 10. It processes map images and metadata to generate optimized paths for navigation, using A* for search and Raycasting for smoothing.

## Core Features

*   **Grid-based Pathfinding:** Uses A* algorithm with customizable cell size.
*   **Path Optimization:** Post-processing via Raycasting to remove redundant nodes while maintaining line-of-sight.
*   **Navigation Logic:** Dynamic heading calculation (Stop-and-Turn) and speed evaluation based on configurable profiles.
*   **Image Processing:** Integrates SkiaSharp for map analysis and visualization.
*   **Localization Friendly:** Configured with `InvariantCulture` to handle standard W3C number inputs consistently across different server locales.

## Tech Stack

*   **Backend:** ASP.NET Core 10 (MVC)
*   **Graphics:** SkiaSharp
*   **Architecture:** Strategy Pattern for decoupled engine components (A*, LOS, Optimizers).
*   **Frontend:** Razor Views, Vanilla CSS, jQuery.

## Getting Started

### Prerequisites
*   .NET 10 SDK

### Installation
1. Clone the repository.
2. Navigate to the project folder: `cd PathFinder`.
3. Restore dependencies: `dotnet restore`.
4. Run the application: `dotnet run --project PathFinder`.

## Configuration

*   `wwwroot/speed_profiles.json`: Defines speed limits and acceleration curves.
*   `map_meta.json`: Reference metadata for map scaling and coordinate transformation.
