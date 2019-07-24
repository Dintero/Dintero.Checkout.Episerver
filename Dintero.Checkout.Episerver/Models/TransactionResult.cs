namespace Dintero.Checkout.Episerver.Models
{
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string ErrorCode { get; set; }
    }
}