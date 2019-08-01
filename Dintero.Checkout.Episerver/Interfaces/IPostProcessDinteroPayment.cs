using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;

namespace Dintero.Checkout.Episerver.Interfaces
{
    public interface IPostProcessDinteroPayment
    {
        void PostAuthorize(IPayment payment, string transactionId, string orderNumber);

        void PostCapture(TransactionResult response, IPayment payment);

        void PostCredit(TransactionResult response, IPayment payment);
    }
}