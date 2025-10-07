using Microsoft.Azure.Functions.Worker.Http;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_Retail.pt2.Functions.Helpers
{
    public static class HttpJson
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public static async Task<T?> ReadAsync<T>(HttpRequestData req)
        {
            using var sr = new StreamReader(req.Body, Encoding.UTF8);
            var body = await sr.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return default;
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }

        public static async Task<HttpResponseData> OkAsync<T>(HttpRequestData req, T data)
        {
            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(data, JsonOptions));
            return res;
        }

        public static async Task<HttpResponseData> CreatedAsync<T>(HttpRequestData req, T data)
        {
            var res = req.CreateResponse(HttpStatusCode.Created);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(data, JsonOptions));
            return res;
        }

        public static Task<HttpResponseData> NotFoundAsync(HttpRequestData req, string message)
        {
            var res = req.CreateResponse(HttpStatusCode.NotFound);
            res.WriteStringAsync(message);
            return Task.FromResult(res);
        }

        public static Task<HttpResponseData> BadAsync(HttpRequestData req, string message)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            res.WriteStringAsync(message);
            return Task.FromResult(res);
        }

        public static Task<HttpResponseData> NoContent(HttpRequestData req)
        {
            var res = req.CreateResponse(HttpStatusCode.NoContent);
            return Task.FromResult(res);
        }

        public static Task<HttpResponseData> Text(HttpRequestData req, HttpStatusCode code, string message)
        {
            var res = req.CreateResponse(code);
            res.WriteStringAsync(message);
            return Task.FromResult(res);
        }
    }
}
