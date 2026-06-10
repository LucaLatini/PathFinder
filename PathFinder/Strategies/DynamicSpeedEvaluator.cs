using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PathFinder.Interfaces;
using PathFinder.Models;

namespace PathFinder.Strategies
{
    public class DynamicSpeedEvaluator : ISpeedEvaluator
    {
        private List<SpeedProfile> _profiles;
        private readonly double _minRequiredClearance;

        public DynamicSpeedEvaluator()
        {
            _profiles = new List<SpeedProfile>();
            LoadProfiles();
            
            _minRequiredClearance = _profiles.Count > 0 
                ? _profiles.Min(p => p.RequiredClearanceMeters) 
                : 1.2; // Valore di default basato sul profilo minimo dell'utente
        }

        private void LoadProfiles()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "PathFinder", "wwwroot", "speed_profiles.json");
            
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "speed_profiles.json");
            }

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                _profiles = JsonSerializer.Deserialize<List<SpeedProfile>>(json) ?? new List<SpeedProfile>();
            }
            else
            {
                _profiles = new List<SpeedProfile>
                {
                    new SpeedProfile { Speed = 1.0, RequiredClearanceMeters = 1.9 },
                    new SpeedProfile { Speed = 0.9, RequiredClearanceMeters = 1.7 },
                    new SpeedProfile { Speed = 0.7, RequiredClearanceMeters = 1.5 },
                    new SpeedProfile { Speed = 0.5, RequiredClearanceMeters = 1.3 },
                    new SpeedProfile { Speed = 0.3, RequiredClearanceMeters = 1.2 }
                };
            }

            _profiles = _profiles.OrderByDescending(p => p.Speed).ToList();
        }

        public double GetMaxSpeedForDistance(double distanceMeters)
        {
            foreach (var profile in _profiles)
            {
                if (distanceMeters >= profile.RequiredClearanceMeters)
                {
                    return profile.Speed;
                }
            }

            return 0.0; // Troppo stretto, il robot deve fermarsi
        }

        public bool IsDistanceSafe(double distanceMeters)
        {
            return distanceMeters >= _minRequiredClearance;
        }

        public List<SpeedProfile> GetProfiles()
        {
            return _profiles;
        }
    }
}
