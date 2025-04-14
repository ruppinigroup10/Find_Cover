namespace FC_Server.Models
{
    public class Shelter
    {
        private int shelter_id;
        private int provider_id;
        private string shelter_type;
        private string name;
        private float latitude;
        private float longitude;
        private string address;
        private int capacity;
        private bool is_accessible;
        private bool is_active;
        private DateTime created_at;
        private DateTime last_updated;
        private string additional_information;

        public int ShelterId { get => shelter_id; set => shelter_id = value; }
        public int ProviderId { get => provider_id; set => provider_id = value; }
        public string ShelterType { get => shelter_type; set => shelter_type = value; }
        public string Name { get => name; set => name = value; }
        public float Latitude { get => latitude; set => latitude = value; }
        public float Longitude { get => longitude; set => longitude = value; }
        public string Address { get => address; set => address = value; }
        public int Capacity { get => capacity; set => capacity = value; }
        public bool IsAccessible { get => is_accessible; set => is_accessible = value; }
        public bool IsActive { get => is_active; set => is_active = value; }
        public DateTime CreatedAt { get => created_at; set => created_at = value; }
        public DateTime LastUpdated { get => last_updated; set => last_updated = value; }
        public string AdditionalInformation { get => additional_information; set => additional_information = value; }

        // Constructor without parameters
        public Shelter()
        {
            this.shelter_id = 1;
            this.provider_id = 0;
            this.shelter_type = "";
            this.name = "";
            this.latitude = 0f;
            this.longitude = 0f;
            this.address = "";
            this.capacity = 0;
            this.is_accessible = false;
            this.is_active = true;
            this.created_at = DateTime.Now;
            this.last_updated = DateTime.Now;
            this.additional_information = "";
        }

        // Constructor with parameters
        public Shelter(int shelterId, int providerId, string shelterType, string name,
                   float latitude, float longitude, string address, int capacity,
                   bool isAccessible, bool isActive, DateTime createdAt,
                   DateTime lastUpdated, string additionalInformation)
        {
            this.shelter_id = shelterId;
            this.provider_id = providerId;
            this.shelter_type = shelterType;
            this.name = name;
            this.latitude = latitude;
            this.longitude = longitude;
            this.address = address;
            this.capacity = capacity;
            this.is_accessible = isAccessible;
            this.is_active = isActive;
            this.created_at = createdAt;
            this.last_updated = lastUpdated;
            this.additional_information = additionalInformation;
        }

        public static Shelter? AddShelter(string shelter_type, string name, float latitude, float longitude,
                                string address, int capacity, string additional_information, int provider_id)
        {
            DBservicesShelter dbs = new DBservicesShelter();
            return dbs.AddShelter(shelter_type, name, latitude, longitude, address, capacity, additional_information, provider_id);
        }

        public static Shelter? UpdateShelter(int shelter_id, string shelter_type, string name, float latitude, float longitude,
                                string address, int capacity, string additional_information, int provider_id)
        {
            DBservicesShelter dbs = new DBservicesShelter();
            return dbs.UpdateShelter(shelter_id, shelter_type, name, latitude, longitude, address, capacity, additional_information, provider_id);
        }

        public static Shelter? getShelter(int shelter_id)
        {
            DBservicesShelter dbs = new DBservicesShelter();
            return dbs.getShelter(shelter_id);
        }
    }
}
