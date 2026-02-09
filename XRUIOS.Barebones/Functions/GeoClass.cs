using GeoCoordinatePortable;
using Microsoft.Maui.Devices.Sensors;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.XRUIOS;


namespace XRUIOS.Barebones
{
    public static class GeoClass
    {


        public struct Coordinate
        {
            public double X, Y;

            public Coordinate() { }
            public Coordinate(double x, double y)
            {
                X = x; Y = y;
            }
        }

        public struct LocationPoint
        {
            public DateTime TimeStamp;
            public double Latitude;
            public double Longitude;

            public LocationPoint() { }


            public LocationPoint(DateTime timestamp, double latitude, double longitude)
            {
                TimeStamp = timestamp;
                Latitude = latitude;
                Longitude = longitude;
            }
        }

        //Use GeoClue on Linux
        public static async Task<Coordinate> GetExactCoordinates()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));

                // Use Geolocation.Default.GetLocationAsync for MAUI/Xamarin Essentials
                Microsoft.Maui.Devices.Sensors.Location location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    var locationPoint = new LocationPoint
                    {
                        TimeStamp = DateTime.UtcNow,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    };

                    // Use the properties from the 'location' object you just fetched
                    await SaveLocationHistory(locationPoint);
                    return new Coordinate(location.Latitude, location.Longitude);
                }

                // Must return something if location is null
                return default;
            }
            catch (Exception ex)
            {
                // For production, catch specific exceptions like PermissionException
                throw new InvalidOperationException($"Unable to get location: {ex.Message}");
            }
        }

        //Exact Location
        private static async Task SaveLocationHistory(LocationPoint newLocation)
        {
            var directoryPath = Path.Combine(DataPath, "Coords");

            var json = await JSONDataHandler.LoadJsonFile("LocationData", directoryPath);

            var locationHistory = (List<LocationPoint>)await JSONDataHandler.GetVariable<List<LocationPoint>>(json, "Data", encryptionKey);

            if (locationHistory.Count >= 20)
            {
                locationHistory.RemoveAt(0);
            }


            json = await JSONDataHandler.UpdateJson<List<LocationPoint>>(json, "Data", locationHistory, encryptionKey);

            locationHistory.Add(newLocation);
            json = await JSONDataHandler.UpdateJson<List<LocationPoint>>(json, "Data", locationHistory, encryptionKey);
            await JSONDataHandler.SaveJson(json);
        }

        public static async Task<List<LocationPoint>> GetRecentLocations()
        {
            var directoryPath = Path.Combine(DataPath, "Coords");

            var json = await JSONDataHandler.LoadJsonFile("LocationData", directoryPath);

            var locationHistory = (List<LocationPoint>)await JSONDataHandler.GetVariable<List<LocationPoint>>(json, "Data", encryptionKey);

            return locationHistory;
        }

        public static async Task ClearLocationHistory(LocationPoint newLocation)
        {
            var directoryPath = Path.Combine(DataPath, "Coords");

            var json = await JSONDataHandler.LoadJsonFile("LocationData", directoryPath);

            json = await JSONDataHandler.UpdateJson<List<LocationPoint>>(json, "Data", new List<LocationPoint>(), encryptionKey);

            await JSONDataHandler.SaveJson(json);

        }

        //Relative Location


        public struct RelativePoint
        {
            public double latmin;
            public double latmax;
            public double longmin;
            public double longmax;
            public RelativePoint() { }


            public RelativePoint(double latmin, double latmax, double longmin, double longmax)
            {
                this.latmin = latmin;
                this.latmax = latmax;
                this.longmin = longmin;
                this.longmax = longmax;
            }
        }

        public struct RelativeLocationPoint
        {
            public DateTime Timestamp;
            public RelativePoint Area;

            public RelativeLocationPoint() { }


            public RelativeLocationPoint(DateTime timestamp, RelativePoint area)
            {
                Timestamp = timestamp;
                Area = area;
            }
        }


        private static readonly Random _rng = new Random();


        public static async Task<RelativePoint> GetRelativeCoordinates()
        {
            try
            {
                // Use GeoCoordinate to ensure input is a valid physical location
                var baseCoord = await GetExactCoordinates();

                // 1. Calculate Jittered Minima (using a small offset for 'near' AR points)
                // Range 0.0001 is ~11 meters. Use 0.03 if you really want 3.3km away.
                double latJitter = 0;
                double longJitter = 0;

                for (int i = 0; i < 5; i++)
                {
                    latJitter += (_rng.NextDouble() * 0.0002) - 0.0001;
                    longJitter += (_rng.NextDouble() * 0.0002) - 0.0001;
                }

                // Fix: Add base coordinate ONCE, not 5 times
                double latMin = baseCoord.Y + (latJitter / 5);
                double longMin = baseCoord.X + (longJitter / 5);

                // 2. Calculate Maxima (wider bounds for the 'zone')
                double latMax = baseCoord.Y + ((_rng.NextDouble() * 0.001) - 0.0005);
                double longMax = baseCoord.X + ((_rng.NextDouble() * 0.001) - 0.0005);

                // 3. Create and return the RelativePoint
                RelativePoint rp = new RelativePoint(latMin, latMax, longMin, longMax);

                // Update history
                await SaveRelativeLocationHistory(new RelativeLocationPoint(DateTime.UtcNow, rp));

                return rp;
            }
            catch (Exception ex)
            {
                throw new Exception($"Location Error: {ex.Message}");
            }
        }


        public static async Task<RelativePoint> ConvertToRelativeCoordinates(double latitude, double longitude)
        {
            try
            {
                // Use GeoCoordinate to ensure input is a valid physical location
                var baseCoord = new GeoCoordinate(latitude, longitude);

                // 1. Calculate Jittered Minima (using a small offset for 'near' AR points)
                // Range 0.0001 is ~11 meters. Use 0.03 if you really want 3.3km away.
                double latJitter = 0;
                double longJitter = 0;

                for (int i = 0; i < 5; i++)
                {
                    latJitter += (_rng.NextDouble() * 0.0002) - 0.0001;
                    longJitter += (_rng.NextDouble() * 0.0002) - 0.0001;
                }

                // Fix: Add base coordinate ONCE, not 5 times
                double latMin = baseCoord.Latitude + (latJitter / 5);
                double longMin = baseCoord.Longitude + (longJitter / 5);

                // 2. Calculate Maxima (wider bounds for the 'zone')
                double latMax = baseCoord.Latitude + ((_rng.NextDouble() * 0.001) - 0.0005);
                double longMax = baseCoord.Longitude + ((_rng.NextDouble() * 0.001) - 0.0005);

                // 3. Create and return the RelativePoint
                RelativePoint rp = new RelativePoint(latMin, latMax, longMin, longMax);

                // Update history
                await SaveRelativeLocationHistory(new RelativeLocationPoint(DateTime.UtcNow, rp));

                return rp;
            }
            catch (Exception ex)
            {
                throw new Exception($"Location Error: {ex.Message}");
            }
        }

        private static async Task SaveRelativeLocationHistory(RelativeLocationPoint newLocation)
        {
            var directoryPath = Path.Combine(DataPath, "Coords");

            var json = await JSONDataHandler.LoadJsonFile("RelativeLocationData", directoryPath);

            var locationHistory = (List<RelativeLocationPoint>)await JSONDataHandler.GetVariable<List<RelativeLocationPoint>>(json, "Data", encryptionKey);

            if (locationHistory.Count >= 40)
            {
                locationHistory.RemoveAt(39);
            }

            locationHistory.Add(newLocation);
            json = await JSONDataHandler.UpdateJson<List<LocationPoint>>(json, "Data", locationHistory, encryptionKey);
            await JSONDataHandler.SaveJson(json);


            await JSONDataHandler.SaveJson(json);

        }

        public static async Task<List<RelativeLocationPoint>> GetRecentRelativeLocations()
        {
            var directoryPath = Path.Combine(DataPath, "Coords");

            var json = await JSONDataHandler.LoadJsonFile("RelativeLocationData", directoryPath);

            var locationHistory = (List<RelativeLocationPoint>)await JSONDataHandler.GetVariable<List<RelativeLocationPoint>>(json, "Data", encryptionKey);

            return locationHistory;
        }

        public static async Task ClearRelativeLocationHistory(RelativeLocationPoint newLocation)
        {
            var directoryPath = Path.Combine(DataPath, "Coords");

            var json = await JSONDataHandler.LoadJsonFile("RelativeLocationData", directoryPath);

            json = await JSONDataHandler.UpdateJson<List<RelativeLocationPoint>>(json, "Data", new List<RelativeLocationPoint>(), encryptionKey);

            await JSONDataHandler.SaveJson(json);

        }



    }
}
