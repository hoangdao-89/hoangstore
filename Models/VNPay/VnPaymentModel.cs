using System;

namespace hoangstore.Models.VNPay // Thêm chữ .VNPay vào đây cho đúng folder
{
    public class VnPaymentRequest
    {
        public int OrderId { get; set; }
        public double Amount { get; set; }
        public string OrderDescription { get; set; }
        public string BankCode { get; set; }
    }

    public class VnPaymentResponse
    {
        public bool Success { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}