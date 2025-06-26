using System;

namespace FC_Server.Models
{
    public class UserVisit
    {
        public int visit_id { get; set; }
        public int user_id { get; set; }
        public int shelter_id { get; set; }
        public int alert_id { get; set; }
        public DateTime? arrival_time { get; set; }
        public DateTime? departure_time { get; set; }
        public string status { get; set; }
        public double? distance_to_shelter { get; set; }
        public bool confirmed_arrival { get; set; }
        public double? walking_distance { get; set; }
        public string route_polyline { get; set; }
    }
}
