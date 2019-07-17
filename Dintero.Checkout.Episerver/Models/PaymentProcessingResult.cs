namespace Dintero.Checkout.Episerver.Models
{
    //
    // Summary:
    //     Represents results when processing an EPiServer.Commerce.Order.IPayment.
    public class PaymentProcessingResult
    {
        //
        // Summary:
        //     Gets the flag indicating whether the processing is successful.
        public bool IsSuccessful { get; private set; }
        //
        // Summary:
        //     Gets the message during processing.
        public string Message { get; private set; }
        //
        // Summary:
        //     Gets the redirect url.
        public string RedirectUrl { get; private set; }

        //
        // Summary:
        //     Creates successful processing result.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        // Returns:
        //     Instance of EPiServer.Commerce.Order.PaymentProcessingResult
        public static PaymentProcessingResult CreateSuccessfulResult(string message)
        {
            return new PaymentProcessingResult()
            {
                IsSuccessful = true,
                Message = message,
                RedirectUrl = string.Empty
            };
        }
        //
        // Summary:
        //     Creates successful processing result with specific action.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   redirectUrl:
        //     The redirect url.
        //
        // Returns:
        //     Instance of EPiServer.Commerce.Order.PaymentProcessingResult
        public static PaymentProcessingResult CreateSuccessfulResult(string message, string redirectUrl)
        {
            return new PaymentProcessingResult()
            {
                IsSuccessful = true,
                Message = message,
                RedirectUrl = redirectUrl
            };
        }
        //
        // Summary:
        //     Creates unsuccessful processing result.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        // Returns:
        //     Instance of EPiServer.Commerce.Order.PaymentProcessingResult
        public static PaymentProcessingResult CreateUnsuccessfulResult(string message)
        {
            return new PaymentProcessingResult()
            {
                IsSuccessful = false,
                Message = message
            };
        }
    }
}