using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using hoangstore.Models.VNPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace hoangstore.Models.Services // Đã khớp với thư mục Models/Services của Hoàng
{
    // =======================================================================
    // 1. LỚP INTERFACE & DỊCH VỤ CHÍNH (Controller sẽ gọi cái này)
    // =======================================================================
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequest request);
        VnPaymentResponse PaymentExecute(IQueryCollection collections);
    }

    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequest request)
        {
            var vnpayConfig = _configuration.GetSection("Vnpay");
            var vnpay = new VnPayLibrary(); // Lớp này nằm ngay bên dưới file này

            string ipAddress = Utils.GetIpAddress(context);
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress.Contains(":"))
            {
                ipAddress = "127.0.0.1";
            }

            vnpay.AddRequestData("vnp_Version", vnpayConfig["Version"] ?? "2.1.0");
            vnpay.AddRequestData("vnp_Command", vnpayConfig["Command"] ?? "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnpayConfig["TmnCode"].Trim());
            vnpay.AddRequestData("vnp_Amount", ((long)(request.Amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", vnpayConfig["CurrCode"] ?? "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", vnpayConfig["Locale"] ?? "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "ThanhToanDonHang" + request.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");

            // Link ngrok 
            string returnUrl = vnpayConfig["ReturnUrl"];
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);

            vnpay.AddRequestData("vnp_TxnRef", request.OrderId.ToString() + DateTime.Now.Ticks.ToString());
            vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            if (!string.IsNullOrEmpty(request.BankCode))
            {
                vnpay.AddRequestData("vnp_BankCode", request.BankCode);
            }

            return vnpay.CreateRequestUrl(vnpayConfig["BaseUrl"].Trim(), vnpayConfig["HashSecret"].Trim());
        }

        public VnPaymentResponse PaymentExecute(IQueryCollection collections)
        {
            var vnpayConfig = _configuration.GetSection("Vnpay");
            var vnpay = new VnPayLibrary();

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            string vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnpayConfig["HashSecret"].Trim());

            if (!checkSignature)
            {
                return new VnPaymentResponse { Success = false };
            }

            return new VnPaymentResponse
            {
                Success = true,
                PaymentMethod = "VNPAY",
                OrderDescription = vnpay.GetResponseData("vnp_OrderInfo"),
                OrderId = vnpay.GetResponseData("vnp_TxnRef"),
                TransactionId = vnpay.GetResponseData("vnp_TransactionNo"),
                VnPayResponseCode = vnpay.GetResponseData("vnp_ResponseCode")
            };
        }
    }

    // =======================================================================
    // 2. LỚP VNPAY LIBRARY ĐÃ ĐƯỢC FIX LỖI URI.ESCAPEDATASTRING BÊN TRONG 
    // =======================================================================
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _requestData.Add(key, value);
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _responseData.Add(key, value);
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();
            foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                // 🔥 ĐÂY LÀ ĐIỂM "ĂN TIỀN": Ép mã hóa chuẩn chữ HOA (%3A thay vì %3a) cho Link Ngrok
                data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            }

            var querystring = data.ToString();
            baseUrl += "?" + querystring;
            var signData = querystring;
            if (signData.Length > 0) signData = signData.Remove(data.Length - 1, 1);

            var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType")) _responseData.Remove("vnp_SecureHashType");
            if (_responseData.ContainsKey("vnp_SecureHash")) _responseData.Remove("vnp_SecureHash");

            foreach (var (key, value) in _responseData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            }

            if (data.Length > 0) data.Remove(data.Length - 1, 1);
            return data.ToString();
        }
    }

    // =======================================================================
    // 3. LỚP UTILS BĂM SHA512 VÀ XỬ LÝ SO SÁNH
    // =======================================================================
    public class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue) hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string GetIpAddress(HttpContext context)
        {
            var ipAddress = string.Empty;
            try
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress != null)
                {
                    if (remoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        remoteIpAddress = Dns.GetHostEntry(remoteIpAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    }
                    if (remoteIpAddress != null) ipAddress = remoteIpAddress.ToString();
                    return ipAddress;
                }
            }
            catch (Exception)
            {
                return "127.0.0.1";
            }
            return "127.0.0.1";
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}