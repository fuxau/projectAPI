using Newtonsoft.Json;

namespace NolanReymondMapApi.Models
{
    public class Route
    {
        public int DistanceMeters { get; set; }

        [JsonProperty("duration")]
        public string? DurationRaw { get; set; } // Nullable

        public Polyline? Polyline { get; set; } // Nullable

        public TimeSpan Duration
        {
            get
            {
                if (string.IsNullOrEmpty(DurationRaw))
                {
                    return TimeSpan.Zero;
                }

                if (DurationRaw.EndsWith("s") && int.TryParse(DurationRaw.TrimEnd('s'), out int seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }

                Console.WriteLine($"Erreur : Format incorrect pour DurationRaw : {DurationRaw}");
                return TimeSpan.Zero;
            }
        }
    }

    public class Polyline
    {
        [JsonProperty("encodedPolyline")]
        public string? EncodedPolyline { get; set; } // Nullable
    }

    public class RoutesResponse
    {
        [JsonProperty("routes")]
        public List<Route>? Routes { get; set; } = new List<Route>(); // Nullable avec initialisation
    }
}