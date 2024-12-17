using BruTile.Predefined;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Limiting;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.Widgets;
using Mapsui.Widgets.ButtonWidgets;
using SantaTracker.Services;

namespace SantaTracker.Presentation;

public partial class MainPage : Page, IDisposable
{
	private const string FollowActiveIcon = "Assets.Maps.following.svg";
	private const string FollowInactiveIcon = "Assets.Maps.nofollow.svg";

	private bool _isFollowing;
	private bool _disposed;
	private DispatcherTimer? _santaTimer;
	private MemoryLayer? _pinsLayer;
	private PointFeature? _santaPin;
	private readonly SantaService _santaService;
	private double? _destinationLat;
	private double? _destinationLon;
	private Map? _map;

	public MainPage()
	{
		this.InitializeComponent();
		_santaService = new SantaService();
#if HAS_UNO
		this.Loaded += (sender, e) => InitializeMap();
#else
		InitializeMap();
#endif

	}

	private void InitializeMap()
	{
		_map = SantaMap.Map;
		_map.Info += MapOnInfo;
		_map.Layers.Add(OpenStreetMap.CreateTileLayer());
		AddWidgets();

		_map.Navigator.Limiter = new ViewportLimiterKeepWithinExtent();
		StartSantaTracking();
	}

	private void AddWidgets()
	{
		if (_map is not { } map)
			return;

		map.Widgets.Add(CreateImageButtonWidget(GetFollowIcon(), OnButtonWidgetTapped));
		map.Widgets.Add(new ZoomInOutWidget { Margin = new MRect(36) });
	}

	private string GetFollowIcon()
	{
		return _isFollowing ? FollowActiveIcon : FollowInactiveIcon;
	}

	private bool OnButtonWidgetTapped(ImageButtonWidget sender, WidgetEventArgs e)
	{
		_isFollowing = !_isFollowing;

		sender.ImageSource = typeof(MainPage).LoadImageSource(GetFollowIcon()).ToString();

		_map?.RefreshGraphics();
		return false;
	}

	private void AddPinsLayer(MPoint initialPoint)
	{
		_santaPin = new PointFeature(initialPoint);
		_santaPin.Styles.Add(CreateCalloutStyle(title: "Santa Claus", subtitle: "Ho Ho Ho!"));

		_pinsLayer = new MemoryLayer
		{
			Name = "Santa Pin Layer",
			IsMapInfoLayer = true,
			Features = [_santaPin],
			Style = new SymbolStyle()
			{
				ImageSource = typeof(MainPage).LoadImageSource(@"Assets.Maps.santahat.svg").ToString(),
				SymbolScale = 1,
				SymbolOffset = new RelativeOffset(new Offset(x: 0.0, y: 0.5))
			}
		};

		_map?.Layers.Add(_pinsLayer);
	}

	private ImageButtonWidget CreateImageButtonWidget(string iconPath, Func<ImageButtonWidget, WidgetEventArgs, bool> tapped) => new()
	{
		ImageSource = typeof(MainPage).LoadImageSource(iconPath).ToString(),
		VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
		HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Right,
		Margin = new MRect(30),
		Padding = new MRect(10, 8),
		CornerRadius = 8,
		Envelope = new MRect(0, 0, 64, 64),
		Tapped = tapped
	};

	private static CalloutStyle CreateCalloutStyle(string title, string subtitle)
	{
		return new CalloutStyle
		{
			Title = title,
			TitleFont = { Size = 14 },
			TitleFontColor = Color.Black,
			Subtitle = subtitle,
			SubtitleFont = { Size = 12 },
			SubtitleFontColor = Color.FromArgb(97, 28, 27, 31),
			Type = CalloutType.Detail,
			MaxWidth = 120,
			Enabled = false,
			SymbolOffset = new Offset(0, SymbolStyle.DefaultHeight * 1f)
		};
	}

	private void StartSantaTracking()
	{
		try
		{
			_santaTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(5)
			};
			_santaTimer.Tick += SantaTimer_Tick;

			_santaTimer.Start();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to start tracking: {ex.Message}");
		}
	}

	private async void SantaTimer_Tick(object? sender, object e)
	{
		var update = await _santaService.GetNextLocation(_destinationLat, _destinationLon);
		var newPoint = SphericalMercator.FromLonLat(update.Longitude, update.Latitude).ToMPoint();

		DispatcherQueue.TryEnqueue(() =>
		{
			if (_map is { } map)
			{
				if (_pinsLayer is null)
				{
					AddPinsLayer(newPoint);
					map.Navigator.CenterOnAndZoomTo(newPoint, map.Navigator.Viewport.Resolution);
				}
				else if (_santaPin is { } pin)
				{
					pin.Modified();
					pin.Point.X = newPoint.X;
					pin.Point.Y = newPoint.Y;

					if (_isFollowing)
					{
						map.Navigator.CenterOnAndZoomTo(newPoint, map.Navigator.Viewport.Resolution);
					}
				}	
			}

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
		});
	}

	public new void Dispose()
	{
		if (!_disposed)
		{
			_santaTimer?.Stop();
			_santaService?.Dispose();
			_disposed = true;
		}
	}

	private static void MapOnInfo(object? sender, MapInfoEventArgs e)
	{
		var calloutStyle = e.MapInfo?.Feature?.Styles.Where(s => s is CalloutStyle).Cast<CalloutStyle>().FirstOrDefault();
		if (calloutStyle != null)
		{
			calloutStyle.Enabled = !calloutStyle.Enabled;
			e.MapInfo?.Layer?.DataHasChanged();
		}
	}

	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		Dispose();
	}
}
