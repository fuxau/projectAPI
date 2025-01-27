namespace ville
{
    public class Result
    {
        public string code_uic { get; set; }
        public string libelle { get; set; }
    }

    public class Root
    {
        public List<Result> results { get; set; }
    }

    public class JourneyInfo
    {
        public TimeSpan Duration { get; set; }
        public double CarbonEmission { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }
}