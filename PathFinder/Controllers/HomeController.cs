using Microsoft.AspNetCore.Mvc;
using PathFinder.Models;
using PathFinder.Services;
using PathFinder.Strategies;
using System.Text.Json;

namespace PathFinder.Controllers
{
    public class HomeController : Controller
    {
        private readonly PathfindingService _pathfindingService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(PathfindingService pathfindingService, ILogger<HomeController> logger)
        {
            _pathfindingService = pathfindingService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CalculatePath(
            IFormFile image,
            IFormFile metadataFile,
            string waypointsJson,
            int cellSize = 10,
            double robotRadiusMeters = 0.5,
            double? finalAngleDeg = null)
        {
            if (image == null || image.Length == 0) return BadRequest("Immagine planimetria mancante.");
            if (metadataFile == null || metadataFile.Length == 0) return BadRequest("File metadati mancante.");
            if (string.IsNullOrEmpty(waypointsJson)) return BadRequest("Punti mancanti.");
            if (cellSize <= 0) cellSize = 1;

            _logger.LogInformation("=== [PATHFINDER] Nuova richiesta ===");
            _logger.LogInformation("[INPUT] robotRadiusMeters = {RobotRadiusMeters} m", robotRadiusMeters);
            _logger.LogInformation("[INPUT] cellSize           = {CellSize} px", cellSize);
            _logger.LogInformation("[INPUT] finalAngleDeg      = {FinalAngleDeg}°", finalAngleDeg?.ToString() ?? "non specificato");
            _logger.LogInformation("[INPUT] CurrentCulture     = {Culture}", Thread.CurrentThread.CurrentCulture.Name);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var inputPoints = JsonSerializer.Deserialize<List<PointDTO>>(waypointsJson, options);

            if (inputPoints == null || inputPoints.Count < 2)
                return BadRequest("Sono necessari almeno un punto di partenza e uno di arrivo.");

            MapMetadata metadata;
            try
            {
                using var metaStream = metadataFile.OpenReadStream();
                metadata = await JsonSerializer.DeserializeAsync<MapMetadata>(metaStream);
                if (metadata == null) return BadRequest("Formato JSON metadati non valido.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Errore metadati: {ex.Message}");
            }

            _logger.LogInformation("[METADATA] resolution = {Resolution} m/px", metadata.resolution);
            _logger.LogInformation("[METADATA] img size   = {W}x{H} px", metadata.img_width_px, metadata.img_height_px);
            _logger.LogInformation("[METADATA] world size = {Wm}x{Hm} m", metadata.width_m, metadata.height_m);
            _logger.LogInformation("[METADATA] origin     = ({Ox}, {Oy})", metadata.origin_x, metadata.origin_y);

            if (metadata.img_width_px > 0 && metadata.width_m > 0)
            {
                double expectedResolution = metadata.width_m / metadata.img_width_px;
                double resolutionMismatch = Math.Abs(metadata.resolution - expectedResolution);
                if (resolutionMismatch > 0.01)
                {
                    _logger.LogWarning("[METADATA] ⚠️ INCONGRUENZA: resolution dichiarata={Declared}, attesa={Expected}. Diff={Diff}",
                        metadata.resolution, expectedResolution, resolutionMismatch);
                }
            }

            // --- CONVERSIONE METRI -> CELLE ---
            double gridCellSizeInMeters = metadata.resolution * cellSize;
            int robotRadiusInCells = (int)Math.Ceiling(robotRadiusMeters / gridCellSizeInMeters);
            if (robotRadiusInCells < 1) robotRadiusInCells = 1;

            _logger.LogInformation("[CONV] gridCellSizeInMeters = {CellM} m/cella", gridCellSizeInMeters);
            _logger.LogInformation("[CONV] robotRadiusInCells   = {RadiusCells} celle", robotRadiusInCells);
            if (robotRadiusInCells > 10)
            {
                _logger.LogWarning("[CONV] ⚠️ robotRadiusInCells={RadiusCells} > 10!", robotRadiusInCells);
            }

            var transformer = new CoordinateTransformer(metadata);

            double? canvasFinalAngleRad = null;
            if (finalAngleDeg.HasValue)
            {
                canvasFinalAngleRad = -(finalAngleDeg.Value * (Math.PI / 180.0));
            }

            var targetCells = inputPoints
                .Select(p => new Coordinate(p.X / cellSize, p.Y / cellSize))
                .ToList();

            for (int i = 0; i < inputPoints.Count; i++)
            {
                _logger.LogInformation("[WAYPOINTS] [{I}] pixel=({Px},{Py}) → cella=({Cx},{Cy})",
                    i, inputPoints[i].X, inputPoints[i].Y, targetCells[i].X, targetCells[i].Y);
            }

            using var imageStream = image.OpenReadStream();

            var finalPoses = await _pathfindingService.CalculatePathAsync(
                imageStream,
                targetCells,
                cellSize,
                robotRadiusInCells,
                canvasFinalAngleRad);

            _logger.LogInformation("[RISULTATO] Pose generate: {Count}", finalPoses.Count);
            if (finalPoses.Count == 0)
            {
                _logger.LogWarning("[RISULTATO] ⚠️ Nessun percorso! robotRadiusInCells={R}", robotRadiusInCells);
            }

            // ═══════════════════════════════════════════════════════════════════
            //  COSTRUZIONE OUTPUT VDA5050 + CANVAS
            // ═══════════════════════════════════════════════════════════════════

            // 1) Calcoliamo le posizioni AMR per ogni posa
            var amrData = finalPoses.Select(p =>
            {
                double pixelX = (p.X * cellSize) + (cellSize / 2.0);
                double pixelY = (p.Y * cellSize) + (cellSize / 2.0);
                var amrPose = transformer.PixelsToAmrPose(pixelX, pixelY, p.Theta);
                return new
                {
                    pixelX,
                    pixelY,
                    canvasTheta = p.Theta,
                    amrX = amrPose.X,
                    amrY = amrPose.Y,
                    amrTheta = amrPose.Theta,
                    speed = p.speed
                };
            }).ToList();

            // 2) Calcoliamo gli heading ASSOLUTI nel frame AMR
            var absoluteHeadings = new double[amrData.Count];
            for (int i = 0; i < amrData.Count - 1; i++)
            {
                absoluteHeadings[i] = Math.Atan2(
                    amrData[i + 1].amrY - amrData[i].amrY,
                    amrData[i + 1].amrX - amrData[i].amrX);
            }
            if (amrData.Count > 1)
            {
                absoluteHeadings[amrData.Count - 1] = finalAngleDeg.HasValue
                    ? finalAngleDeg.Value * Math.PI / 180.0
                    : absoluteHeadings[amrData.Count - 2];
            }

            // 3) Nodi VDA5050 (incluso sequenceId e released = true)
            var vda5050Nodes = amrData.Select((p, i) => new
            {
                nodeId = i.ToString(),
                sequenceId = i * 2, // I nodi sono sempre pari
                nodeDescription = "",
                released = true,
                nodePosition = new
                {
                    x = Math.Round(p.amrX, 3),
                    y = Math.Round(p.amrY, 3),
                    theta = Math.Round(absoluteHeadings[i], 3),
                    mapId = "",
                    mapDescription = "",
                    allowedDeviationXY = 0.1,
                    allowedDeviationTheta = 0.1
                },
                actions = new object[] { }
            }).ToList();

            // 4) Edge VDA5050 (incluso sequenceId e released = true)
            var vda5050Edges = new List<object>();
            for (int i = 0; i < amrData.Count - 1; i++)
            {
                double orientation = Math.Atan2(
                    amrData[i + 1].amrY - amrData[i].amrY,
                    amrData[i + 1].amrX - amrData[i].amrX);

                vda5050Edges.Add(new
                {
                    edgeId = i.ToString(),
                    sequenceId = (i * 2) + 1, // Gli edge sono sempre dispari
                    edgeDescription = "",
                    released = true,
                    startNodeId = i.ToString(),
                    endNodeId = (i + 1).ToString(),
                    maxSpeed = Math.Round(amrData[i].speed, 2),
                    orientation = Math.Round(orientation, 3),
                    rotationAllowed = true,
                    actions = new object[] { }
                });
            }

            // 5) Path canvas per il rendering frontend
            var canvasPath = amrData.Select(p => new
            {
                canvas = new { x = p.pixelX, y = p.pixelY, theta = p.canvasTheta },
                amr = new { x = p.amrX, y = p.amrY, theta = p.amrTheta, speed = p.speed }
            }).ToList();

            // 6) Creazione della struttura VDA5050 standard completa
            long headerId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.00Z");

            var vda5050Payload = new
            {
                headerId = headerId,
                timestamp = timestamp,
                version = "2.0.0",
                manufacturer = "bosch",
                serialNumber = "0", // Come standard VDA5050 il serialNumber è sempre stringa
                orderId = headerId.ToString(), // Utilizziamo l'headerId come orderId
                orderUpdateId = 0,
                zoneSetId = "",
                nodes = vda5050Nodes,
                edges = vda5050Edges
            };

            _logger.LogInformation("[VDA5050] Generati {Nodes} nodi, {Edges} edge (Order ID: {OrderId})",
                vda5050Nodes.Count, vda5050Edges.Count, vda5050Payload.orderId);

            return Json(new
            {
                success = true,
                path = canvasPath,
                vda5050 = vda5050Payload
            });
        }
    }
}
