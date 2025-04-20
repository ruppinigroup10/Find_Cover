namespace FC_Server.Models;

public class KnownLocation
{
    private int location_id;
    private int user_id;
    private float latitude;
    private float longitude;
    private float radius;
    private string address;
    private string location_name;
    private string nickname;
    private DateTime added_at;

    public int LocationId { get => location_id; set => location_id = value; }
    public int UserId { get => user_id; set => user_id = value; }
    public float Latitude { get => latitude; set => latitude = value; }
    public float Longitude { get => longitude; set => longitude = value; }
    public float Radius { get => radius; set => radius = value; }
    public string Address { get => address; set => address = value; }
    public string LocationName { get => location_name; set => location_name = value; }
    public string Nickname { get => nickname; set => nickname = value; }
    public DateTime AddedAt { get => added_at; set => added_at = value; }

    // Constructor without parameters
    public KnownLocation()
    {
        this.location_id = 1;
        this.user_id = 0;
        this.latitude = 0f;
        this.longitude = 0f;
        this.radius = 0f;
        this.address = "";
        this.location_name = "";
        this.nickname = "";
        this.added_at = DateTime.Now;
    }
    // Constructor with parameters
    public KnownLocation(int locationId, int userId, float latitude, float longitude,
                         float radius, string address, string locationName,
                         string nickname, DateTime addedAt)
    {
        this.location_id = locationId;
        this.user_id = userId;
        this.latitude = latitude;
        this.longitude = longitude;
        this.radius = radius;
        this.address = address;
        this.location_name = locationName;
        this.nickname = nickname;
        this.added_at = addedAt;
    }
    //יצירת פונקציה סטטית שבתוכה יש קריאה לפונקציה לא סטטית
    public static KnownLocation? GetKnownLocation(int user_id)
    {
        DBservices dbs = new DBservices();
        return dbs.GetKnownLocation(user_id);
    }
    public static KnownLocation? UpdateKnownLocation(int location_id, int user_id, float latitude, float longitude, float radius, string address, string location_name, string nickname, DateTime added_at)
    {
        DBservices dbs = new DBservices();
        return dbs.UpdateKnownLocation(location_id, user_id, latitude, longitude, radius, address, location_name, nickname, added_at);
    }
    public static KnownLocation? AddKnownLocation(int location_id, int user_id, float latitude, float longitude, float radius, string address, string location_name, string nickname)
    {
        DBservices dbs = new DBservices();
        return dbs.AddKnownLocation(location_id, user_id, latitude, longitude, radius, address, location_name, nickname);
    }
}


