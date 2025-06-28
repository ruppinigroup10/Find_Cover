namespace FC_Server.Models
{
    /// <summary>
    /// Extended Alert model to support database operations
    /// </summary>
    public class Alert
    {
        // Keep existing properties:
        public string notificationId { get; set; }
        public long time { get; set; }
        public int threat { get; set; }
        public bool isDrill { get; set; }
        public List<string> cities { get; set; }

        // Add these new properties for database compatibility:
        public int AlertId { get; set; }
        public string AlertType { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public DateTime AlertTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public string Data { get; set; }
        public string CreatedBy { get; set; }
        public string AffectedAreas { get; set; }

        // Computed property for compatibility
        public DateTime StartTime => AlertTime;
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

    /// <summary>
    /// Represents an active alert with additional properties for user allocation
    /// </summary>
    public class ActiveAlert
    {
        public int AlertId { get; set; }
        public DateTime AlertTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string AlertType { get; set; }
        public string Data { get; set; }
        public bool IsActive { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public string CreatedBy { get; set; }
        public string AreaName { get; set; }
        public int ResponseTimeSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Represents the status of an alert for user allocation
    /// </summary>
    public class AlertStatus
    {
        public int AlertId { get; set; }
        public DateTime AlertTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string AlertType { get; set; }
        public bool IsActive { get; set; }
    }
}