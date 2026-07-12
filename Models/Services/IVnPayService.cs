using hoangstore.Models.VNPay;
using Microsoft.AspNetCore.Http;

namespace hoangstore.Models.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(
            HttpContext context,
            VnPaymentRequest request);

        VnPaymentResponse PaymentExecute(
            IQueryCollection collections);
    }
}