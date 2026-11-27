using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using biddingServer.services.xendit;

namespace biddingServer.services.xendit
{
    public class XenditService : IXenditService
    {
        private readonly HttpClient _http;
        private readonly string _secretKey;
        private readonly string _baseUrl;

        public XenditService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _secretKey = config["Xendit:SecretKey"] ?? throw new InvalidOperationException("Xendit:SecretKey not configured");
            _baseUrl = config["Xendit:BaseUrl"] ?? "https://api.xendit.co";
            _http.BaseAddress = new Uri(_baseUrl);
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_secretKey + ":"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        public async Task<XenditInvoiceResponse> CreateInvoiceAsync(decimal amount, string externalId, string description, string? successRedirectUrl = null, string? failureRedirectUrl = null)
        {
            var payload = new
            {
                external_id = externalId,
                amount = amount,
                description = description,
                success_redirect_url = successRedirectUrl,
                failure_redirect_url = failureRedirectUrl,
                currency = "PHP"
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("/v2/invoices", content);
            var respJson = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Xendit invoice create failed: {resp.StatusCode} {respJson}");
            using var doc = JsonDocument.Parse(respJson);
            var root = doc.RootElement;
            return new XenditInvoiceResponse
            {
                Id = root.GetProperty("id").GetString() ?? "",
                ExternalId = root.GetProperty("external_id").GetString() ?? "",
                InvoiceUrl = root.GetProperty("invoice_url").GetString() ?? "",
                Status = root.GetProperty("status").GetString() ?? "",
                Amount = root.GetProperty("amount").GetDecimal()
            };
        }
    }
}