namespace SantaTracker.Models
{
    public class LocationUpdate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public string LocationName { get; set; }
        public double? DistanceFromUser { get; set; }
    }
}
