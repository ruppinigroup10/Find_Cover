

namespace FC_Server.Models
{
    public class UserLocation
    {
        private int user_id;
        private double latitude;
        private double longitude;
        private DateTime createdAt;

        public int UserId { get => user_id; set => user_id = value; }
        public double Latitude { get => latitude; set => latitude = value; }
        public double Longitude { get => longitude; set => longitude = value; }
        public DateTime CreatedAt { get => createdAt; set => createdAt = value; }  // חדש


        public UserLocation()
        {
            user_id = 0;
            latitude = 0.0;
            longitude = 0.0;
            CreatedAt = DateTime.UtcNow; // ברירת מחדל
        }

        public UserLocation(int userId, double latitude, double longitude, DateTime createdAt)
        {
            this.user_id = userId;
            this.latitude = latitude;
            this.longitude = longitude;
            CreatedAt = createdAt;
        }
    }
}