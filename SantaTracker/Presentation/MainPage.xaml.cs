using Mapsui.Tiling;
using Uno.Extensions.Navigation;
using SantaTracker.Services;

namespace SantaTracker.Presentation
{
    public sealed partial class MainPage : Page, IDisposable
    {
        private bool _disposed;
        private DispatcherTimer _santaTimer;
        private readonly SantaService _santaService;
        private readonly IGeocodingService _geocodingService;
        private double? _destinationLat;
        private double? _destinationLon;

        public MainPage()
        {
            this.InitializeComponent();
            _geocodingService = new NominatimGeocodingService();
            _santaService = new SantaService(_geocodingService);

            SantaMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            AddressTextBox.TextChanged += AddressTextBox_TextChanged;

            StartSantaTracking();
        }

        private void StartSantaTracking()
        {
            try
            {
                _santaTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _santaTimer.Tick += SantaTimer_Tick;
                _santaTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start tracking: {ex.Message}");
            }
        }

        private async void SantaTimer_Tick(object sender, object e)
        {
            var update = await _santaService.GetNextLocation(_destinationLat, _destinationLon);

            DispatcherQueue.TryEnqueue(() =>
            {
                var locationText = $"{update.LocationName} ({update.Latitude:F1}°N, {update.Longitude:F1}°E)";
                if (update.DistanceFromUser.HasValue)
                {
                    var distanceText = update.DistanceFromUser >= 1000
                        ? $"{update.DistanceFromUser.Value / 1000:F1}k km away"
                        : $"{(int)update.DistanceFromUser.Value} km away";
                    locationText += $" - {distanceText}";
                }
                LocationText.Text = locationText;
                SpeedText.Text = $"{(int)update.Speed:N0} km/h";

                if (_destinationLat.HasValue && update.DistanceFromUser.HasValue && update.Speed > 0)
                {
                    var hoursToDestination = update.DistanceFromUser.Value / update.Speed;
                    var timeToDestination = TimeSpan.FromHours(hoursToDestination);

                    ETAText.Text = timeToDestination.TotalHours >= 1
                        ? $"{timeToDestination.TotalHours:F1} hours"
                        : $"{timeToDestination.TotalMinutes:F0} min";
                }
                else
                {
                    ETAText.Text = "--:--";
                }
            });
        }

        private async void AddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AddressTextBox.Text))
            {
                var (lat, lon) = await _geocodingService.GeocodeAddress(AddressTextBox.Text);
                _destinationLat = lat;
                _destinationLon = lon;
            }
        }

        public new void Dispose()
        {
            if (!_disposed)
            {
                _santaTimer?.Stop();
                _geocodingService?.Dispose();
                _santaService?.Dispose();
                _disposed = true;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
    }
}
