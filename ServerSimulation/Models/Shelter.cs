using System;

namespace ServerSimulation.Models
{
    public class Shelter
    {
        public int shelter_id { get; set; }
        public int? provider_id { get; set; }
        public string shelter_type { get; set; }
        public string name { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string address { get; set; }
        public short capacity { get; set; }
        public bool? is_accessible { get; set; }
        public bool? is_active { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? last_updated { get; set; }
        public string additional_information { get; set; }
    }
}