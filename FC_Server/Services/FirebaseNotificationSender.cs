using Google.Apis.Auth.OAuth2;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class FirebaseNotificationSender
{
    private readonly string _serviceAccountPath;
    private readonly string _projectId;

    public FirebaseNotificationSender(string serviceAccountPath, string projectId)
    {
        _serviceAccountPath = serviceAccountPath;
        _projectId = projectId;
    }

    public async Task SendNotificationAsync(string title, string body, string topic = "alerts")
    {
        string[] scopes = { "https://www.googleapis.com/auth/firebase.messaging" };
        GoogleCredential credential = GoogleCredential
            .FromFile(_serviceAccountPath)
            .CreateScoped(scopes);


        string accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var message = new
            {
                message = new
                {
                    topic = topic, // ניתן גם להחליף ל"token" אם שולחים לטוקן ספציפי
                    notification = new { title, body },
                    data = new
                    {
                        type = "alert",
                        city = "באר שבע"
                    }
                }
            };

            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";
            var response = await client.PostAsync(url, content);

            string responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"FCM send failed: {response.StatusCode}, {responseBody}");
            }
        }
    }
}


/*using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FC_Server.Services
{
    public class FcmNotificationService
    {
        private readonly string _serviceAccountPath;
        private readonly string _projectId;

        public FcmNotificationService(IConfiguration configuration)
        {
            // works on both Mac and Windows
            _serviceAccountPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                configuration["Firebase:ServiceAccountPath"]
            );
            _projectId = configuration["Firebase:ProjectId"];
        }

        public async Task SendNotificationAsync(string title, string body, List<string> topicsOrTokens)
        {
            string[] scopes = { "https://www.googleapis.com/auth/firebase.messaging" };
            GoogleCredential credential = GoogleCredential
                .FromFile(_serviceAccountPath)
                .CreateScoped(scopes);

            string accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                foreach (var target in topicsOrTokens)
                {
                    var message = new
                    {
                        message = new
                        {
                            topic = target,
                            notification = new { title, body },
                            data = new
                            {
                                alert_type = "rocket",
                                source = "FC_Server"
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(message);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";
                    var response = await client.PostAsync(url, content);

                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[FCM] {response.StatusCode}: {responseBody}");
                }
            }
        }
    }
}
*/