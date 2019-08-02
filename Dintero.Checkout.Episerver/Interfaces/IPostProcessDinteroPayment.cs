using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;

namespace Dintero.Checkout.Episerver.Interfaces
{
    public interface IPostProcessDinteroPayment
    {
        void PostAuthorize(IPayment payment, string error, string transactionId, string orderNumber);

        void PostCapture(TransactionResult response, IPayment payment);

        void PostCredit(TransactionResult response, IPayment payment);
    }
}