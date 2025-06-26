namespace FC_Server.Models
{
    /// <summary>
    /// External API model - represents alerts from tzevaadom.co.il API
    /// </summary>
    public class Alert
    {
        public string notificationId { get; set; }
        public long time { get; set; }
        public int threat { get; set; }
        public bool isDrill { get; set; }
        public List<string> cities { get; set; }
    }

    /// <summary>
    /// Internal database model - represents alerts stored in your database
    /// </summary>
    public class AlertRecord
    {
        public int alert_id { get; set; }
        public string alert_type { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? end_time { get; set; }
        public bool is_active { get; set; }
        public string created_by { get; set; }
        public string alert_source { get; set; }
    }
}