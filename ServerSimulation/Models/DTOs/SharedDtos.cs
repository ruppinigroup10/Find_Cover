namespace ServerSimulation.Models.DTOs
{
    public class PersonDto
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? NearestShelterId { get; set; }
        public double? NearestShelterDistance { get; set; }
        public bool IsManual { get; set; } = false;
    }

    public class ShelterDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Capacity { get; set; }
    }

    public class AssignmentDto
    {
        public int PersonId { get; set; }
        public int ShelterId { get; set; }
        public double Distance { get; set; }
        public bool IsWalkingDistance { get; set; } = false;
        public string RoutePolyline { get; set; } // for the encoded route
        public List<double[]> RouteCoordinates { get; set; } // for decoded coordinates
    }
}