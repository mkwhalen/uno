#if __IOS__ || __MACOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreLocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private CLLocationManager _locationManager;

		public Geolocator()
		{
			_locationManager = new CLLocationManager
			{
				DesiredAccuracy = DesiredAccuracy == PositionAccuracy.Default ? 10 : 1,
			};

#if __IOS__ //required only for iOS
			_locationManager.RequestWhenInUseAuthorization();
#endif

			_locationManager.LocationsUpdated += _locationManager_LocationsUpdated;

			_locationManager.StartUpdatingLocation();
		}

		private void _locationManager_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
		{
			BroadcastStatus(PositionStatus.Ready);
			this._positionChanged?.Invoke(this, new PositionChangedEventArgs(ToGeoposition(e.Locations.Last())));
		}

		partial void StartPositionChanged()
		{
			BroadcastStatus(PositionStatus.Initializing);
		}

#if __IOS__
		public async Task<Geoposition> GetGeopositionAsync() //will be removed with #2240
#else
		public async IAsyncOperation<Geoposition> GetGeopositionAsync()
#endif
		{
			BroadcastStatus(PositionStatus.Initializing);
			var location = _locationManager.Location;
			BroadcastStatus(PositionStatus.Ready);
			return ToGeoposition(location);
		}

		private static Geoposition ToGeoposition(CLLocation location)
			=> new Geoposition(
				new Geocoordinate(
					altitude: location.Altitude,
					longitude: location.Coordinate.Longitude,
					latitude: location.Coordinate.Latitude,
					accuracy: location.HorizontalAccuracy,
					altitudeAccuracy: location.VerticalAccuracy,
					speed: location.Speed,
					point: new Geopoint(
						new BasicGeoposition
						{
							Altitude = location.Altitude,
							Latitude = location.Coordinate.Latitude,
							Longitude = location.Coordinate.Longitude
						}
					),
					timestamp: (DateTime)location.Timestamp
				)
			);

#if __IOS__
		public async Task<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout) //will be removed with #2240
#else
		public async IAsyncOperation<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
#endif
			=> await GetGeopositionAsync();

		private static List<CLLocationManager> _requestManagers = new List<CLLocationManager>();

#if __IOS__
		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
#else
		public static async IAsyncOperation<GeolocationAccessStatus> RequestAccessAsync()
#endif
		{
			var mgr = new CLLocationManager();

			lock (_requestManagers)
			{
				_requestManagers.Add(mgr);
			}

			try
			{
				GeolocationAccessStatus accessStatus;
				var cts = new TaskCompletionSource<CLAuthorizationStatus>();

				mgr.AuthorizationChanged += (s, e) =>
				{

					if (e.Status != CLAuthorizationStatus.NotDetermined)
					{
						cts.TrySetResult(e.Status);
					}
				};

#if __IOS__ //required only for iOS
				mgr.RequestWhenInUseAuthorization();
#endif

				if (CLLocationManager.Status != CLAuthorizationStatus.NotDetermined)
				{
					accessStatus = TranslateStatus(CLLocationManager.Status);
				}

				var cLAuthorizationStatus = await cts.Task;

				accessStatus = TranslateStatus(cLAuthorizationStatus);
				
				//if geolocation is not well accessible, default geoposition should be recommended
				if (accessStatus != GeolocationAccessStatus.Allowed)
				{
					IsDefaultGeopositionRecommended = true;
				}

				return accessStatus;
			}
			finally
			{
				lock (_requestManagers)
				{
					_requestManagers.Remove(mgr);
				}
			}
		}

		public static async Task<IList<Geoposition>> GetGeopositionHistoryAsync(DateTime startTime) { return new List<Geoposition>(); }

		public static async Task<IList<Geoposition>> GetGeopositionHistoryAsync(DateTime startTime, TimeSpan duration) { return new List<Geoposition>(); }

		private static GeolocationAccessStatus TranslateStatus(CLAuthorizationStatus status)
		{
			switch (status)
			{
				// These two constants are set by value based on https://developer.apple.com/library/ios/documentation/CoreLocation/Reference/CLLocationManager_Class/index.html#//apple_ref/c/tdef/CLAuthorizationStatus
				// This is for the compatibility with iOS 8 and the introduction of AuthorizedWhenInUse.
				// This can be replaced with proper enum values when upgrading to iOS 8.0 SDK.
				case (CLAuthorizationStatus)4: // CLAuthorizationStatus.AuthorizedWhenInUse:
				case (CLAuthorizationStatus)3: // CLAuthorizationStatus.AuthorizedAlways:
					return GeolocationAccessStatus.Allowed;

				case CLAuthorizationStatus.NotDetermined:
					return GeolocationAccessStatus.Unspecified;

				default:
				case CLAuthorizationStatus.Restricted:
				case CLAuthorizationStatus.Denied:
					return GeolocationAccessStatus.Denied;
			}
		}
	}
}
#endif
