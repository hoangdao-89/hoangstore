using System;

namespace hoangstore.Models.VNPay 
{
    public class VnPaymentRequest
    {
        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        public string OrderDescription { get; set; } = string.Empty;

        public string BankCode { get; set; } = string.Empty;
    }

    public class VnPaymentResponse
    {
        public bool Success { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string OrderDescription { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public string VnPayResponseCode { get; set; } = string.Empty;

        public string TransactionStatus { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}