using Microsoft.AspNetCore.Http;
using hoangstore.Models.VNPay; // Gọi đúng đến folder VNPay mới của Hoàng

namespace hoangstore.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequest request);
        VnPaymentResponse PaymentExecute(IQueryCollection collections);
    }
}