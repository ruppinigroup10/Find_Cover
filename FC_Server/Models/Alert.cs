namespace FC_Server.Models
{
    public class Alert
    {
        public string notificationId { get; set; }
        public long time { get; set; }
        public int threat { get; set; }
        public bool isDrill { get; set; }
        public List<string> cities { get; set; }
    }
}