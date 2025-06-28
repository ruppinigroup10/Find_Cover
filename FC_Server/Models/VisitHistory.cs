namespace FC_Server.Models
{
    public class VisitHistory
    {
        public int VisitId { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public string ShelterName { get; set; }
        public string ShelterAddress { get; set; }
        public string AdditionalInformation { get; set; }

        public VisitHistory()
        {
            ShelterName = "";
            ShelterAddress = "";
            AdditionalInformation = "";
        }

        public static List<VisitHistory> GetUserVisitHistory(int user_id)
        {
            DBservices dbs = new DBservices();
            return dbs.GetUserVisitHistory(user_id);
        }
    }
}
