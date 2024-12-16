using SantaTracker.Models;

namespace SantaTracker.Services
{
    public class SantaService : IDisposable
    {
        private const double JITTER_RANGE = 0.05;
        private const double SPEED_VARIANCE = 500;
        private const int EARTH_RADIUS_KM = 6371;
        private const double JOURNEY_STEP = 0.02;
        private const double WAYPOINT_PROXIMITY_KM = 500;

        private readonly IGeocodingService _geocodingService;
        private readonly Random _random = new Random();
        private bool _disposed;

        private readonly List<(string name, double lat, double lon)> _waypoints = new()
        {
            ("North Pole", 90, 0),
            ("Anchorage", 61.2181, -149.9003),
            ("Tokyo", 35.6762, 139.6503),
            ("Sydney", -33.8688, 151.2093),
            ("Montreal", 45.5019, 73.5674),
            ("Mumbai", 19.0760, 72.8777),
            ("Moscow", 55.7558, 37.6173),
            ("Paris", 48.8566, 2.3522),
            ("London", 51.5074, -0.1278),
            ("New York", 40.7128, -74.0060),
            ("Los Angeles", 34.0522, -118.2437)
        };

        public double CurrentLatitude { get; private set; } = 90;
        public double CurrentLongitude { get; private set; } = 0;
        private int _currentWaypointIndex = 0;
        private int _nextWaypointIndex = 1;
        private double _journeyProgress = 0;

        public SantaService(IGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        public async Task<LocationUpdate> GetNextLocation(double? userLat = null, double? userLon = null)
        {
            var currentWaypoint = _waypoints[_currentWaypointIndex];
            var nextWaypoint = _waypoints[_nextWaypointIndex];

            var prevLat = CurrentLatitude;
            var prevLon = CurrentLongitude;

            // Calculate new position
            CurrentLatitude = currentWaypoint.lat + (nextWaypoint.lat - currentWaypoint.lat) * _journeyProgress;
            CurrentLongitude = currentWaypoint.lon + (nextWaypoint.lon - currentWaypoint.lon) * _journeyProgress;

            // Add some random movement
            CurrentLatitude += _random.NextDouble() * JITTER_RANGE * 2 - JITTER_RANGE;
            CurrentLongitude += _random.NextDouble() * JITTER_RANGE * 2 - JITTER_RANGE;

            // Calculate speed
            var distance = CalculateDistance(prevLat, prevLon, CurrentLatitude, CurrentLongitude);
            var speed = (distance / 1.0) * 3600; // Convert to km/h
            speed += (_random.NextDouble() * SPEED_VARIANCE * 2) - SPEED_VARIANCE;

            // Update progress
            _journeyProgress += JOURNEY_STEP;

            // Check if we've reached the next waypoint
            if (_journeyProgress >= 1.0)
            {
                _journeyProgress = 0;
                _currentWaypointIndex = _nextWaypointIndex;
                _nextWaypointIndex = (_nextWaypointIndex + 1) % _waypoints.Count;
            }

            var locationName = await GetLocationName(CurrentLatitude, CurrentLongitude);
            double? distanceFromUser = null;

            if (userLat.HasValue && userLon.HasValue)
            {
                distanceFromUser = CalculateDistance(CurrentLatitude, CurrentLongitude, userLat.Value, userLon.Value);
            }

            return new LocationUpdate
            {
                Latitude = CurrentLatitude,
                Longitude = CurrentLongitude,
                Speed = speed,
                LocationName = locationName,
                DistanceFromUser = distanceFromUser
            };
        }

        private string GetNearestWaypointName(double lat, double lon)
        {
            double shortestDistance = double.MaxValue;
            string nearestName = "In Transit";

            foreach (var waypoint in _waypoints)
            {
                var distance = CalculateDistance(lat, lon, waypoint.lat, waypoint.lon);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestName = waypoint.name;
                }
            }

            return shortestDistance < WAYPOINT_PROXIMITY_KM ? nearestName : $"Flying over {nearestName}";
        }

        private async Task<string> GetLocationName(double lat, double lon)
        {
            var name = await _geocodingService.GetLocationName(lat, lon);
            return string.IsNullOrEmpty(name) ? GetNearestWaypointName(lat, lon) : name;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EARTH_RADIUS_KM * c;
        }

        public double CalculateETA(double distance, double speed)
        {
            if (speed <= 0) return 0;
            return distance / speed;
        }

        private double ToRad(double degrees) => degrees * (Math.PI / 180);

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
