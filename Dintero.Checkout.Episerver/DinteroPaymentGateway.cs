using Dintero.Checkout.Episerver.Helpers;
using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Plugins.Payment;
using Mediachase.Data.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dintero.Checkout.Episerver
{
    public class DinteroPaymentGateway : AbstractPaymentGateway
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(DinteroPaymentGateway));

        private PaymentMethodDto _payment;
        private readonly IOrderRepository _orderRepository;
        private readonly DinteroRequestsHelper _requestsHelper;

        public DinteroPaymentGateway() : this(ServiceLocator.Current.GetInstance<IOrderRepository>())
        {
        }

        public DinteroPaymentGateway(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            _requestsHelper = new DinteroRequestsHelper();
        }

        public override bool ProcessPayment(Payment payment, ref string message)
        {
            var orderGroup = payment.Parent.Parent;

            var paymentProcessingResult = ProcessPayment(orderGroup, payment);

            if (!string.IsNullOrEmpty(paymentProcessingResult.RedirectUrl))
            {
                HttpContext.Current.Response.Redirect(paymentProcessingResult.RedirectUrl);
            }
            message = paymentProcessingResult.Message;
            return paymentProcessingResult.IsSuccessful;
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="orderGroup">The order group.</param>
        /// <param name="payment">The payment.</param>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            if (HttpContext.Current == null)
            {
                return PaymentProcessingResult.CreateSuccessfulResult("Http context is null.");
            }

            if (payment == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("Payment was not specified.");
            }

            var orderForm = orderGroup.Forms.FirstOrDefault(f => f.Payments.Contains(payment));
            if (orderForm == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("There is no order form assosiated with payment.");
            }

            var purchaseOrder = orderGroup as IPurchaseOrder;
            if (purchaseOrder != null)
            {
                if (payment.TransactionType == TransactionType.Capture.ToString())
                {
                    // return true meaning the capture request is done,
                    // actual capturing must be done on Dintero.

                    var result = _requestsHelper.CaptureTransaction(payment, purchaseOrder);
                    if (result.Error == null)
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }

                    return PaymentProcessingResult.CreateUnsuccessfulResult(
                        $@"There was an error while capturing payment with Dintero.
                        code: {result.Error.Code}
                        declineReason: {result.Error.Message}");
                }

                if (payment.TransactionType == TransactionType.Credit.ToString())
                {
                    // TODO: add refund logic
                }

                // right now we do not support processing the order which is created by Commerce Manager
                return PaymentProcessingResult.CreateUnsuccessfulResult("The current payment method does not support this order type.");
            }

            var cart = orderGroup as ICart;
            if (cart != null && cart.OrderStatus == OrderStatus.Completed)
            {
                return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
            }
            _orderRepository.Save(orderGroup);

            var redirectUrl = UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentPage");

            return PaymentProcessingResult.CreateSuccessfulResult(string.Empty, redirectUrl);
        }

        // <summary>
        // Processes the unsuccessful transaction.
        // </summary>
        // <param name = "cancelUrl" > The cancel url.</param>
        // <param name = "errorMessage" > The error message.</param>
        // <returns>The url redirection after process.</returns>
        public string ProcessUnsuccessfulTransaction(string cancelUrl, string errorMessage)
        {
            return UriUtil.AddQueryString(cancelUrl, "message", errorMessage);
        }

        /// <summary>
        /// Processes the successful transaction, will be called when Dintero server processes 
        /// the payment successfully and redirect back.
        /// </summary>
        /// <param name="cart">The cart that was processed.</param>
        /// <param name="payment">The order payment.</param>
        /// <param name="transactionID">The transaction id.</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="acceptUrl">The redirect url when finished.</param>
        /// <param name="cancelUrl">The redirect url when error happens.</param>
        /// <returns>The redirection url after processing.</returns>
        public string ProcessSuccessfulTransaction(ICart cart, IPayment payment, string transactionId, string orderNumber, string acceptUrl, string cancelUrl)
        {
            if (cart == null)
            {
                return cancelUrl;
            }

            string redirectionUrl;
            using (var scope = new TransactionScope())
            {
                // Change status of payments to processed.
                // It must be done before execute workflow to ensure payments which should mark as processed.
                // To avoid get errors when executed workflow.
                PaymentStatusManager.ProcessPayment(payment);

                var errorMessages = new List<string>();
                var cartCompleted = DoCompletingCart(cart, errorMessages);

                if (!cartCompleted)
                {
                    return UriUtil.AddQueryString(cancelUrl, "message", string.Join(";", errorMessages.Distinct().ToArray()));
                }

                // Save the transact from Dintero to payment.
                payment.TransactionID = transactionId;

                var purchaseOrder = MakePurchaseOrder(cart, orderNumber);

                // TODO: add query parameters to redirect url to acceptUrl
                redirectionUrl = acceptUrl;

                // Commit changes
                scope.Complete();
            }

            return redirectionUrl;
        }

        private IPurchaseOrder MakePurchaseOrder(ICart cart, string orderNumber)
        {
            // Save changes
            //this might cause problem when checkout using multiple shipping address because ECF workflow does not handle it. Modify the workflow instead of modify in this payment
            var purchaseOrderLink = _orderRepository.SaveAsPurchaseOrder(cart);
            var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(purchaseOrderLink.OrderGroupId);

            UpdateOrder(purchaseOrder, orderNumber);

            UpdateLastOrderOfCurrentContact(CustomerContext.Current.CurrentContact, purchaseOrder.Created);

            _orderRepository.Save(purchaseOrder);

            // Remove old cart
            _orderRepository.Delete(cart.OrderLink);

            return purchaseOrder;
        }

        private void UpdateOrder(IPurchaseOrder purchaseOrder, string orderNumber)
        {
            purchaseOrder.OrderStatus = OrderStatus.InProgress;
            purchaseOrder.OrderNumber = orderNumber;
        }

        /// <summary>
        /// Update last order time stamp which current user completed.
        /// </summary>
        /// <param name="contact">The customer contact.</param>
        /// <param name="datetime">The order time.</param>
        private void UpdateLastOrderOfCurrentContact(CustomerContact contact, DateTime datetime)
        {
            if (contact != null)
            {
                contact.LastOrder = datetime;
                contact.SaveChanges();
            }
        }

        /// <summary>
        /// Validates and completes a cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="errorMessages">The error messages.</param>
        private bool DoCompletingCart(ICart cart, IList<string> errorMessages)
        {
            var isSuccess = true;

            // TODO: run cart workflow

            var isIgnoreProcessPayment = new Dictionary<string, object> { { "PreventProcessPayment", true } };
            var workflowResults = OrderGroupWorkflowManager.RunWorkflow((OrderGroup)cart, OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, isIgnoreProcessPayment);

            return isSuccess;
        }
    }
}