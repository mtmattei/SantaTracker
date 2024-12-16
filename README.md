# Santa Tracker Component Documentation

## 📁 Project Structure
```
SantaTracker/
├── Services/
│   ├── IGeocodingService.cs
│   ├── NominatimGeocodingService.cs
│   └── SantaService.cs
├── Models/
│   └── LocationUpdate.cs
└── Presentation/
    ├── MainPage.xaml
    └── MainPage.xaml.cs
```

## 🔍 Component Details

### `IGeocodingService.cs`

**Purpose**: Interface for geocoding operations
```csharp
public interface IGeocodingService : IDisposable
{
    Task<(double? lat, double? lon)> GeocodeAddress(string address);
    Task<string> GetLocationName(double lat, double lon);
}
```
- Handles address-to-coordinate conversion
- Provides reverse geocoding
- Ensures proper resource cleanup

### `NominatimGeocodingService.cs`

**Purpose**: OpenStreetMap Nominatim implementation
- Uses HTTP requests to Nominatim API
- 10-second timeout for reliability
- JSON response processing
- Error handling with fallbacks

### `SantaService.cs`

**Purpose**: Core Santa tracking logic
- Waypoint navigation system
- Realistic movement patterns
- Real-time location updates
- Distance and speed calculations

**Key Constants**:
```csharp
JITTER_RANGE = 0.05
SPEED_VARIANCE = 500
EARTH_RADIUS_KM = 6371
JOURNEY_STEP = 0.02
```

### `LocationUpdate.cs`

**Purpose**: Data model for position updates
```csharp
public class LocationUpdate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public string LocationName { get; set; }
    public double? DistanceFromUser { get; set; }
}
```

### `MainPage.xaml.cs`

**Purpose**: UI controller and coordination
- Service initialization
- Map display updates
- User input handling
- Timer management
- Display formatting

## 🛠️ Dependencies
- Mapsui.UI.WinUI
- OpenStreetMap/Nominatim

## 🚀 Usage

### Initialization
```csharp
var geocodingService = new NominatimGeocodingService();
var santaService = new SantaService(geocodingService);
```

### Location Updates
- Automatic updates every second
- Real-time distance calculations
- Dynamic ETA updates
- Fallback location names when needed

## ⚠️ Error Handling
1. Geocoding failures → Use nearest waypoint
2. Network timeouts → 10-second limit
3. Resource cleanup → IDisposable implementation
4. Debug logging for issues
