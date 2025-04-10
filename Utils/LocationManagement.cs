namespace IvanConnections_Travel.Utils
{
    public static class LocationManagement
    {
        private static CancellationTokenSource _cancelTokenSource;
        private static bool _isCheckingLocation;

        public static async Task<Location> GetLocationAsync(
            GeolocationAccuracy geolocationAccuracy = GeolocationAccuracy.Medium,
            int timeOutSeconds = 10,
            Action<Location>? onUpdatedLocation = null)
        {
            Location cachedLocation = await GetCachedLocation();

            // Start updating current location in background
            _ = Task.Run(async () =>
            {
                Location? updatedLocation = await TryGetCurrentLocationAsync(geolocationAccuracy, timeOutSeconds);

                if (updatedLocation != null &&
                    (updatedLocation.Latitude != cachedLocation.Latitude ||
                     updatedLocation.Longitude != cachedLocation.Longitude))
                {
                    onUpdatedLocation?.Invoke(updatedLocation);
                }
            });

            return cachedLocation;
        }

        private static async Task<Location> GetCachedLocation()
        {
            try
            {
                Location location = await Geolocation.Default.GetLastKnownLocationAsync();

                if (location != null)
                    return location;
            }
            catch
            {
                // Log or handle specific exceptions if needed
            }

            return new Location(47.1585, 27.6014); // Fallback
        }

        private static async Task<Location?> TryGetCurrentLocationAsync(
            GeolocationAccuracy geolocationAccuracy,
            int timeOutSeconds)
        {
            try
            {
                _isCheckingLocation = true;

                GeolocationRequest request = new(geolocationAccuracy, TimeSpan.FromSeconds(timeOutSeconds));
                _cancelTokenSource = new CancellationTokenSource();

                Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
                return location;
            }
            catch
            {
                // Log or handle specific exceptions if needed
                return null;
            }
            finally
            {
                _isCheckingLocation = false;
            }
        }

        public static void CancelRequest()
        {
            if (_isCheckingLocation && _cancelTokenSource?.IsCancellationRequested == false)
                _cancelTokenSource.Cancel();
        }
    }
}
