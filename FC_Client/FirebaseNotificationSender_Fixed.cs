using Google.Apis.Auth.OAuth2;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Task s;

public class FirebaseNotificationSender
{
    private readonly string _serviceAccountPath;
    private readonly string _projectId;

    public FirebaseNotificationSender(string serviceAccountPath, string projectId)
    {
        _serviceAccountPath = serviceAccountPath;
        _projectId = projectId;
    }
    
    public async Task SendNotificationAsync(
        Dictionary<string, string> data,
        string topic = "alerts",
        string title = null,
        string body = null)
    {
        string[] scopes = { "https://www.googleapis.com/auth/firebase.messaging" };
        GoogleCredential credential = GoogleCredential
            .FromFile(_serviceAccountPath)
            .CreateScoped(scopes);

        string accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // יצירת הודעה משופרת
        var message = new
        {
            message = new
            {
                topic = topic,
                data = data,
                // הוספת הודעה גלויה רק אם יש title/body
                notification = !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body) ? new
                {
                    title = title,
                    body = body
                } : null,
                // הגדרות Android מיוחדות - התראה פתוחה ומורחבת
                android = new
                {
                    priority = "high",
                    notification = !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body) ? new
                    {
                        channel_id = "alerts_channel",
                        priority = "high",
                        sound = "default",
                        visibility = "public", // התראה פתוחה
                        importance = "max", // חשיבות מקסימלית
                        show_when = true,
                        ongoing = false,
                        auto_cancel = false, // לא נעלם אוטומטית
                        sticky = true, // נשאר על המסך
                        default_sound = true,
                        default_vibrate_timings = true,
                        default_light_settings = true,
                        notification_priority = "PRIORITY_MAX", // עדיפות מקסימלית
                        style = "bigtext", // סגנון טקסט גדול
                        big_text = body, // מציג את כל הטקסט
                        expand_large_icon = true // הרחבת אייקון
                    } : null
                },
                // הגדרות iOS מיוחדות  
                apns = new
                {
                    payload = new
                    {
                        aps = new
                        {
                            alert = !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body) ? new
                            {
                                title = title,
                                body = body
                            } : null,
                            badge = 1,
                            sound = "default",
                            content_available = 1 // זה חשוב לרקע
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        
        Console.WriteLine($"📤 Sending FCM message: {json}");
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";
        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"FCM send failed: {response.StatusCode}, {responseBody}");
        }
        
        Console.WriteLine($"✅ FCM message sent successfully!");
    }
    
    // פונקציה לשליחת התראת אזעקה עם מיקום מרחב מוגן
    public async Task SendAlertNotificationAsync(
        string userLocation = null,
        string alertType = "FIND_SHELTER",
        double shelterLatitude = 0.0,
        double shelterLongitude = 0.0,
        string shelterName = "מרחב מוגן",
        string shelterAddress = "")
    {
        var data = new Dictionary<string, string>
        {
            { "code", alertType },
            { "type", "trigger_location" },
            { "trigger_location", "true" },
            { "timestamp", DateTime.UtcNow.ToString("o") },
            { "latitude", shelterLatitude.ToString("F6") },
            { "longitude", shelterLongitude.ToString("F6") },
            { "shelter_name", shelterName },
            { "address", shelterAddress }
        };
        
        if (!string.IsNullOrEmpty(userLocation))
        {
            data["user_location"] = userLocation;
        }
        
        await SendNotificationAsync(
            data: data,
            topic: "alerts",
            title: "נמצא עבורך מרחב מוגן", 
            body: "לחץ לניווט"
        );
    }
}
