namespace FC_Server.Models
{
    public class Alert
    {
        private int alert_id;
        private DateTime alert_time;
        private DateTime? end_time;
        private string alert_type;
        private string data;
        private bool is_active;

        public int AlertId { get => alert_id; set => alert_id = value; }
        public DateTime AlertTime { get => alert_time; set => alert_time = value; }
        public DateTime? EndTime { get => end_time; set => end_time = value; }
        public string AlertType { get => alert_type; set => alert_type = value; }
        public string Data { get => data; set => data = value; }
        public bool IsActive { get => is_active; set => is_active = value; }
    }
}
