namespace Dintero.Checkout.Episerver.Helpers
{
    public static class DinteroAPIUrlHelper
    {
        private const string AccountTemplateUrl = "https://api.dintero.com/v1/accounts/{0}";
        private const string AuthTemplateUrl = "https://api.dintero.com/v1/accounts/{0}/auth/token";
        private const string NewSessionTemplateUrl = "https://checkout.dintero.com/v1/sessions-profile";
        private const string BaseTransactionUrlTemplateUrl = "https://checkout.dintero.com/v1/transactions/{0}/{1}";

        public static string GetAccountUrl(string accountId)
        {
            return string.Format(AccountTemplateUrl, accountId);
        }

        public static string GetAuthUrl(string accountId)
        {
            return string.Format(AuthTemplateUrl, accountId);
        }

        public static string GetNewSessionUrl()
        {
            return NewSessionTemplateUrl;
        }

        public static string GetTransactionDetailsUrl(string transactionId)
        {
            return string.Format(BaseTransactionUrlTemplateUrl, transactionId, "");
        }

        public static string GetTransactionCaptureUrl(string transactionId)
        {
            return string.Format(BaseTransactionUrlTemplateUrl, transactionId, "capture");
        }

        public static string GetTransactionRefundUrl(string transactionId)
        {
            return string.Format(BaseTransactionUrlTemplateUrl, transactionId, "refund");
        }

        public static string GetTransactionVoidUrl(string transactionId)
        {
            return string.Format(BaseTransactionUrlTemplateUrl, transactionId, "void");
        }
    }
}