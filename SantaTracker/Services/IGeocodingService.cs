// Services/IGeocodingService.cs
namespace SantaTracker.Services
{
    public interface IGeocodingService : IDisposable
    {
        Task<(double? lat, double? lon)> GeocodeAddress(string address);
        Task<string> GetLocationName(double lat, double lon);
    }
}
