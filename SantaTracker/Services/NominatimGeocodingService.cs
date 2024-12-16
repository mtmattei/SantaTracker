using System.Text.Json;

namespace SantaTracker.Services
{
    public class NominatimGeocodingService : IGeocodingService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public NominatimGeocodingService()
        {
            _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SantaTracker/1.0");
        }

        public async Task<(double? lat, double? lon)> GeocodeAddress(string address)
        {
            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}";
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                if (doc.RootElement.GetArrayLength() > 0)
                {
                    var location = doc.RootElement[0];
                    return (
                        double.Parse(location.GetProperty("lat").GetString()),
                        double.Parse(location.GetProperty("lon").GetString())
                    );
                }
                return (null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<string> GetLocationName(double lat, double lon)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}&zoom=10";
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var displayName = doc.RootElement.GetProperty("display_name").GetString();
                return displayName?.Split(',')[0] ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
                return string.Empty;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }
    }
}
