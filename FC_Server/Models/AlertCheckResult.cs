using System;

namespace FC_Server.Models
{
    public class AlertCheckResult
    {
        public bool IsInAlertZone { get; set; }
        public string ZoneName { get; set; }
        public int ResponseTime { get; set; }
        public bool HasActiveAlert { get; set; }
        public DateTime? AlertTime { get; set; }
        public string Message { get; set; }
    }
}