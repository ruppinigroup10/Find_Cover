using System.Reflection.Metadata.Ecma335;

namespace FC_Server.Models;

public class UserPreferences
{

    private int preference_id;
    private int user_id;
    private string shelter_type;
    private bool accessibility_needed;
    private int num_default_people;
    private bool pets_allowed;
    private DateTime last_update;

    public int PreferenceId { get => preference_id; set => preference_id = value; }
    public int UserId { get => user_id; set => user_id = value; }
    public string ShelterType { get => shelter_type; set => shelter_type = value; }
    public bool AccessibilityNeeded { get => accessibility_needed; set => accessibility_needed = value; }
    public int NumDefaultPeople { get => num_default_people; set => num_default_people = value; }
    public bool PetsAllowed { get => pets_allowed; set => pets_allowed = value; }
    public DateTime LastUpdate { get => last_update; set => last_update = value; }

    // Constructor without parameters
    public UserPreferences()
    {
        this.preference_id = 1;
        this.user_id = 0;
        this.shelter_type = "";
        this.accessibility_needed = false;
        this.num_default_people = 0;
        this.pets_allowed = false;
        this.last_update = DateTime.Now;
    }
    // Constructor with parameters
    public UserPreferences(int preferenceId, int userId, string shelterType,
                           bool accessibilityNeeded, int numDefaultPeople,
                           bool petsAllowed, DateTime lastUpdate)
    {
        this.preference_id = preferenceId;
        this.user_id = userId;
        this.shelter_type = shelterType;
        this.accessibility_needed = accessibilityNeeded;
        this.num_default_people = numDefaultPeople;
        this.pets_allowed = petsAllowed;
        this.last_update = lastUpdate;
    }

    //יצירת פונקציה סטטית שבתוכה יש קריאה לפונקציה לא סטטית
    public static UserPreferences? GetUserPreferences(int user_id)
    {
        DBservices dbs = new DBservices();
        return dbs.GetUserPreferences(user_id);
    }

    public static UserPreferences? UpdateUserPreferences(int preference_id, int user_id, string shelter_type, bool accessibility_needed, int num_default_people, bool pets_allowed, DateTime last_update)
    {
        DBservices dbs = new DBservices();
        return dbs.UpdateUserPreferences(preference_id, user_id, shelter_type, accessibility_needed, num_default_people, pets_allowed, last_update);
    }

    public static UserPreferences? AddPreference(int user_id,string shelter_type, bool accessibility_needed, int num_default_people, bool pets_allowed)
    {  DBservices dbs = new DBservices();
       return dbs.AddPreference( user_id, shelter_type, accessibility_needed, num_default_people, pets_allowed);
    }

}
