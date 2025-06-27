using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FC_Server.Services
{
    public class FcmNotificationService
    {
        private readonly string _serviceAccountPath = "C:\\Users\\HP\\Documents\\GitHub\\Find_Cover\\FC_Server\\App_Data\\keys\\find-cover-34ca3bbecc34.json";
        private readonly string _projectId = "find-cover";

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
