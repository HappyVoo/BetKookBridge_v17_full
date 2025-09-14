using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BetKookBridge
{
    internal static class KookClient
    {
        private const string API = "https://www.kookapp.cn/api/v3/message/create";
        private static readonly HttpClient _http = new HttpClient();

        public static async Task<(bool ok, string body)> SendCardsAsync(string token, string channelId, List<object> cards)
        {
            var payload = new {
                target_id = channelId,
                type = 10,
                content = JsonSerializer.Serialize(cards)
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, API);
            req.Headers.TryAddWithoutValidation("Authorization", $"Bot {token}");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, $"HTTP {(int)resp.StatusCode} {body}");
        }
    }
}
