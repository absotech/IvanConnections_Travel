namespace IvanConnections_Travel.Utils
{
    public static class LocationManagement
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public static async Task<Location> GetLocationAsync(
            GeolocationAccuracy geolocationAccuracy = GeolocationAccuracy.Medium,
            int timeOutSeconds = 10,
            Action<Location>? onUpdatedLocation = null)
        {
            Location cachedLocation = await GetCachedLocation();
            _ = Task.Run(async () =>
            {
                if (_semaphore.CurrentCount == 0)
                    return;

                await _semaphore.WaitAsync();
                try
                {
                    var request = new GeolocationRequest(geolocationAccuracy, TimeSpan.FromSeconds(timeOutSeconds));
                    Location? updatedLocation = await Geolocation.Default.GetLocationAsync(request);

                    if (updatedLocation != null &&
                        (updatedLocation.Latitude != cachedLocation.Latitude ||
                         updatedLocation.Longitude != cachedLocation.Longitude))
                    {
                        onUpdatedLocation?.Invoke(updatedLocation);
                    }
                }
                catch
                {
                    
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            return cachedLocation;
        }
        public static async Task<Location?> GetCurrentLocationAsync(
       GeolocationAccuracy geolocationAccuracy = GeolocationAccuracy.Lowest,
       int timeOutSeconds = 2)
        {
            await _semaphore.WaitAsync();
            try
            {
                var request = new GeolocationRequest(geolocationAccuracy, TimeSpan.FromSeconds(timeOutSeconds));
                Location? location = await Geolocation.Default.GetLocationAsync(request);
                return location;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to get current location: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        private static async Task<Location> GetCachedLocation()
        {
            try
            {
                Location? location = await Geolocation.Default.GetLastKnownLocationAsync();
                if (location != null)
                    return location;
            }
            catch
            {
                
            }
            return new Location(47.1585, 27.6014);
        }
    }
}
