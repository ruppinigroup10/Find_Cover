namespace FC_Server.Models;

public class UserPreferences
{
    public int Id { get; set; }
    public string ShelterType { get; set; }

    public int MyProperty { get; set; }
    public int MyProperty { get; set; }
    public int MyProperty { get; set; }

    public static UserPreferences GetPreferences(int user_id)
    {
        return new UserPreferences();//todo inmplement
    }

    public static UserPreferences UpdatePreferences(int user_id,UserPreferences preferences)
    {
        return new UserPreferences();//todo inmplement
    }

}
