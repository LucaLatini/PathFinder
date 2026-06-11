# PathFinder: Advanced Path Planning for Autonomous Mobile Robots

PathFinder is a sophisticated web-based orchestration engine designed to solve complex navigation tasks for **Autonomous Mobile Robots (AMR)**. Built on the cutting-edge **.NET 10** platform, it transforms static architectural planimetries into actionable, optimized, and kinematically-feasible trajectories for industrial robotic fleets.

## The Problem It Solves

Standard pathfinding algorithms often produce "staircase" paths or routes that ignore the physical constraints of a robot (such as its footprint, rotation capabilities, and acceleration curves). PathFinder addresses these challenges by implementing a multi-stage pipeline that refines a raw mathematical path into a professional robotic mission.

## Core Functional Pipeline

### 1. Intelligent Grid Generation (`GridFactory`)
The engine utilizes **SkiaSharp** to ingest high-resolution map images. It doesn't just look at pixels; it analyzes occupancy data to build a traversable grid.
*   **Custom Resolution:** Users can define the `cellSize` (e.g., 10px or 5cm per cell), allowing the engine to balance between computational speed and navigation precision.
*   **Collision Awareness:** The factory identifies obstacles and generates a binary search space for the pathfinding engine.

### 2. Pathfinding Engine (`AStarEngine`)
At its core, the project uses a highly optimized **A* (A-Star)** algorithm. 
*   **Heuristic-Driven:** It finds the most efficient route from start to finish based on Euclidean distance.
*   **Segmented Routing:** Supports multi-waypoint missions, calculating the optimal sequence of segments to complete a complex path.

### 3. Post-Processing & Smoothing (`RaycastingPathOptimizer`)
Raw A* paths are often inefficient for robots. PathFinder applies a **Raycasting** pass using the **Bresenham algorithm**:
*   **Redundancy Removal:** It identifies nodes that can be skipped if there is a clear "Line of Sight" (LOS) between non-adjacent points.
*   **Trajectory Smoothing:** This transforms a jagged, pixel-perfect path into a series of long, smooth vectors, drastically reducing the number of required stop-and-turn maneuvers.

### 4. Robotic Kinematics & Orientation
Unlike generic pathfinders, this system understands that a robot has an orientation (**Pose**).
*   **Heading Calculation (`StopAndTurnHeadingCalculator`):** It computes the required angle for each node, ensuring the robot faces the direction of travel.
*   **Final Alignment:** The system allows for a specific `finalAngleDeg` at the destination, essential for docking or cargo pickup.

### 5. Velocity & Speed Profiling (`DynamicSpeedEvaluator`)
The engine generates a `SpeedProfile` for the entire journey:
*   **Profile-Based Logic:** Using `speed_profiles.json`, it calculates maximum safe velocities for each segment.
*   **Acceleration Handling:** It considers the robot's physical limits to ensure that speed transitions are smooth and safe for the hardware.

## Architectural Design

The project is built on the **Strategy Pattern**, ensuring that every core component is interchangeable and testable:
- **IPathfindingEngine**: Easily switch from A* to Dijkstra or RRT.
- **ILineOfSightChecker**: Swap the LOS logic for different sensor simulations.
- **ISpeedEvaluator**: Update speed logic without touching the pathfinding core.

## Technical Stack
- **Framework:** ASP.NET Core 10 (MVC)
- **Image Processing:** SkiaSharp
- **Serialization:** System.Text.Json (configured for high-precision decimal handling)
- **Infrastructure:** `InvariantCulture` enforced for consistent cross-system coordinate parsing.

## Getting Started

### Prerequisites
- .NET 10 SDK

### Run the Engine
```bash
git clone <repository-url>
cd PathFinder
dotnet restore
dotnet run --project PathFinder
```

The application exposes a POST endpoint `/Home/CalculatePath` which accepts map images, metadata, and waypoints to return a fully optimized robotic trajectory.

## Configuration
Calibration is handled via JSON files:
- `wwwroot/speed_profiles.json`: Define linear/angular velocity limits.
- `map_meta.json`: Essential for mapping pixel coordinates to real-world metric units.
