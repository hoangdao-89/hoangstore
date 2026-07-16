using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using hoangstore.Models.VNPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace hoangstore.Models.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequest request)
        {
            var config = _configuration.GetSection("Vnpay");
            var baseUrl = GetRequiredConfig(config, "BaseUrl");
            var tmnCode = GetRequiredConfig(config, "TmnCode");
            var hashSecret = GetRequiredConfig(config, "HashSecret");
            var returnUrl = GetRequiredConfig(config, "ReturnUrl");

            var vietnamTime = GetVietnamTime();
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", config["Version"] ?? "2.1.0");
            vnpay.AddRequestData("vnp_Command", config["Command"] ?? "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(request.Amount * 100m)).ToString(CultureInfo.InvariantCulture));
            vnpay.AddRequestData("vnp_CreateDate", vietnamTime.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", config["CurrCode"] ?? "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", config["Locale"] ?? "vn");
            vnpay.AddRequestData("vnp_OrderInfo", string.IsNullOrWhiteSpace(request.OrderDescription)
                ? $"ThanhToanDonHang{request.OrderId}"
                : request.OrderDescription);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", $"{request.OrderId}_{DateTime.UtcNow.Ticks}");
            vnpay.AddRequestData("vnp_ExpireDate", vietnamTime.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            if (!string.IsNullOrWhiteSpace(request.BankCode))
            {
                vnpay.AddRequestData("vnp_BankCode", request.BankCode);
            }

            return vnpay.CreateRequestUrl(baseUrl, hashSecret);
        }

        public VnPaymentResponse PaymentExecute(IQueryCollection collections)
        {
            var config = _configuration.GetSection("Vnpay");
            var hashSecret = GetRequiredConfig(config, "HashSecret");
            var vnpay = new VnPayLibrary();

            foreach (var item in collections)
            {
                if (!string.IsNullOrWhiteSpace(item.Key) &&
                    item.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                {
                    vnpay.AddResponseData(item.Key, item.Value.ToString());
                }
            }

            var secureHash = collections["vnp_SecureHash"].ToString();

            if (string.IsNullOrWhiteSpace(secureHash) ||
                !vnpay.ValidateSignature(secureHash, hashSecret))
            {
                return new VnPaymentResponse { Success = false };
            }

            decimal.TryParse(
                vnpay.GetResponseData("vnp_Amount"),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var amountInMinorUnit);

            return new VnPaymentResponse
            {
                Success = true,
                PaymentMethod = "VNPAY",
                OrderDescription = vnpay.GetResponseData("vnp_OrderInfo"),
                OrderId = vnpay.GetResponseData("vnp_TxnRef"),
                TransactionId = vnpay.GetResponseData("vnp_TransactionNo"),
                VnPayResponseCode = vnpay.GetResponseData("vnp_ResponseCode"),
                TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus"),
                Amount = amountInMinorUnit / 100m
            };
        }

        private static DateTime GetVietnamTime()
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
        }

        private static string GetRequiredConfig(IConfigurationSection section, string key)
        {
            var value = section[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Thiếu cấu hình Vnpay:{key}.");
            }

            return value.Trim();
        }
    }

    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(StringComparer.Ordinal);
        private readonly SortedList<string, string> _responseData = new(StringComparer.Ordinal);

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) _requestData[key] = value;
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) _responseData[key] = value;
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var queryString = BuildQueryString(_requestData, false);
            var secureHash = Utils.HmacSHA512(hashSecret, queryString);
            return $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var responseData = BuildQueryString(_responseData, true);
            var calculatedHash = Utils.HmacSHA512(secretKey, responseData);

            return string.Equals(
                calculatedHash,
                inputHash,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildQueryString(
            IEnumerable<KeyValuePair<string, string>> data,
            bool excludeSecureHash)
        {
            var parts = data
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Where(item =>
                    !excludeSecureHash ||
                    (item.Key != "vnp_SecureHash" &&
                     item.Key != "vnp_SecureHashType"))
                .Select(item =>
                    $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}");

            return string.Join("&", parts);
        }
    }

    public static class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            var hashValue = hmac.ComputeHash(inputBytes);

            return Convert.ToHexString(hashValue).ToLowerInvariant();
        }

        public static string GetIpAddress(HttpContext context)
        {
            try
            {
                var remoteIp = context.Connection.RemoteIpAddress;

                if (remoteIp == null) return "127.0.0.1";
                if (remoteIp.IsIPv4MappedToIPv6) remoteIp = remoteIp.MapToIPv4();
                if (remoteIp.AddressFamily != AddressFamily.InterNetwork) return "127.0.0.1";

                return remoteIp.ToString();
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}