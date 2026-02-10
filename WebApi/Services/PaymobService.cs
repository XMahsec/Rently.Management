using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Rently.Management.WebApi.Services
{
    public class PaymobService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PaymobService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> AuthenticateAsync()
        {
            var apiKey = _configuration["Paymob:ApiKey"] ?? "";
            var client = _httpClientFactory.CreateClient("paymob");
            var res = await client.PostAsJsonAsync("/auth/tokens", new { api_key = apiKey });
            var auth = await res.Content.ReadFromJsonAsync<AuthRes>();
            return auth?.token ?? "";
        }

        public async Task<(string orderId, string paymentToken, string url)> InitiateAsync(int amountCents, string currency, string email, string name, string phone, string integrationId, bool useIframe)
        {
            var token = await AuthenticateAsync();
            var client = _httpClientFactory.CreateClient("paymob");

            var orderReq = new
            {
                auth_token = token,
                delivery_needed = false,
                amount_cents = amountCents,
                currency = currency,
                items = Array.Empty<object>()
            };
            var orderRes = await client.PostAsJsonAsync("/ecommerce/orders", orderReq);
            var order = await orderRes.Content.ReadFromJsonAsync<OrderRes>();

            var redirectionUrl = _configuration["Paymob:RedirectionUrl"] ?? "";
            var billingData = new
            {
                apartment = "NA",
                email = email,
                floor = "NA",
                first_name = name,
                street = "NA",
                building = "NA",
                phone_number = phone,
                shipping_method = "NA",
                postal_code = "NA",
                city = "NA",
                country = "NA",
                last_name = name,
                state = "NA"
            };
            var keyReq = new
            {
                auth_token = token,
                amount_cents = amountCents,
                expiration = 3600,
                order_id = order?.id,
                billing_data = billingData,
                currency = currency,
                integration_id = integrationId,
                lock_order_when_paid = true,
                redirection_url = redirectionUrl
            };
            var keyRes = await client.PostAsJsonAsync("/acceptance/payment_keys", keyReq);
            var key = await keyRes.Content.ReadFromJsonAsync<PaymentKeyRes>();
            var iframeId = _configuration["Paymob:IframeId"] ?? "";
            var baseUrl = _configuration["Paymob:BaseUrl"] ?? "";
            var root = baseUrl.Replace("/api", "");
            var url = useIframe
                ? $"{root}/acceptance/iframes/{iframeId}?payment_token={key?.token}"
                : $"{root}/acceptance/pay?payment_token={key?.token}";
            return (order?.id?.ToString() ?? "", key?.token ?? "", url);
        }

        public bool ValidateHmac(Dictionary<string, string> fields, string hmac)
        {
            var secret = _configuration["Paymob:HmacSecret"] ?? "";
            var keys = new[]
            {
                "amount_cents","created_at","currency","error_occured","has_parent_transaction","id","integration_id","is_3d_secure","is_auth","is_capture","is_refunded","is_standalone_payment","is_voided","order.id","owner","pending","source_data.pan","source_data.sub_type","source_data.type","success"
            };
            var sb = new StringBuilder();
            foreach (var k in keys)
            {
                if (fields.TryGetValue(k, out var v))
                    sb.Append(v);
            }
            var data = sb.ToString();
            using var sha = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return string.Equals(computed, hmac, StringComparison.OrdinalIgnoreCase);
        }

        private class AuthRes { public string? token { get; set; } }
        private class OrderRes { public int? id { get; set; } }
        private class PaymentKeyRes { public string? token { get; set; } }
    }
}
