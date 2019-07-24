using Dintero.Checkout.Episerver.Helpers;
using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Plugins.Payment;
using Mediachase.Data.Provider;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Mediachase.Commerce.Core.Features;
using Mediachase.Commerce.Extensions;

namespace Dintero.Checkout.Episerver
{
    public class DinteroPaymentGateway : AbstractPaymentGateway
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(DinteroPaymentGateway));

        private readonly IOrderRepository _orderRepository;
        private readonly IFeatureSwitch _featureSwitch;
        private readonly IInventoryProcessor _inventoryProcessor;
        private readonly DinteroRequestsHelper _requestsHelper;

        public DinteroPaymentGateway() : this(ServiceLocator.Current.GetInstance<IFeatureSwitch>(),
            ServiceLocator.Current.GetInstance<IInventoryProcessor>(),
            ServiceLocator.Current.GetInstance<IOrderRepository>()) { }

        public DinteroPaymentGateway(IFeatureSwitch featureSwitch, IInventoryProcessor inventoryProcessor,
            IOrderRepository orderRepository)
        {
            _featureSwitch = featureSwitch;
            _inventoryProcessor = inventoryProcessor;
            _orderRepository = orderRepository;
            _requestsHelper = new DinteroRequestsHelper();
        }

        public override bool ProcessPayment(Payment payment, ref string message)
        {
            Logger.Debug("Starting Dintero process payment.");
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
                return PaymentProcessingResult.CreateUnsuccessfulResult(
                    "There is no order form associated with payment.");
            }

            if (orderGroup is IPurchaseOrder purchaseOrder)
            {
                if (payment.TransactionType == TransactionType.Capture.ToString())
                {
                    // return true meaning the capture request is done,
                    // actual capturing must be done on Dintero.

                    var result = _requestsHelper.CaptureTransaction(payment, purchaseOrder);
                    if (result.Success)
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }

                    return PaymentProcessingResult.CreateUnsuccessfulResult(
                        $@"There was an error while capturing payment with Dintero:
                           code: {
                                result.ErrorCode
                            };
                           declineReason: {
                                result.Error
                            }");
                }

                if (payment.TransactionType == TransactionType.Void.ToString())
                {
                    var result = _requestsHelper.VoidTransaction(payment);
                    if (result.Success)
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }

                    return PaymentProcessingResult.CreateUnsuccessfulResult(
                        $@"There was an error while voiding payment with Dintero:
                           code: {
                                result.ErrorCode
                            };
                           declineReason: {
                                result.Error
                            }");
                }

                if (payment.TransactionType == TransactionType.Credit.ToString())
                {
                    var transactionId = payment.TransactionID;
                    if (string.IsNullOrEmpty(transactionId) || transactionId.Equals("0"))
                    {
                        return PaymentProcessingResult.CreateUnsuccessfulResult(
                            "TransactionID is not valid or the current payment method does not support this order type.");
                    }

                    // The transact must be captured before refunding
                    var result = _requestsHelper.RefundTransaction(payment, purchaseOrder);
                    if (result.Success)
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }

                    return PaymentProcessingResult.CreateUnsuccessfulResult(
                        $@"There was an error while refunding payment with Dintero:
                           code: {
                                result.ErrorCode
                            };
                           declineReason: {
                                result.Error
                            }");
                }

                // right now we do not support processing the order which is created by Commerce Manager
                return PaymentProcessingResult.CreateUnsuccessfulResult(
                    "The current payment method does not support this order type.");
            }

            if (orderGroup is ICart cart && cart.OrderStatus == OrderStatus.Completed)
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
            if (HttpContext.Current == null)
            {
                return cancelUrl;
            }

            Logger.Error($"Dintero transaction failed [{errorMessage}].");
            return UriUtil.AddQueryString(cancelUrl, "message", errorMessage);
        }

        /// <summary>
        /// Processes the successful transaction, will be called when Dintero server processes 
        /// the payment successfully and redirect back.
        /// </summary>
        /// <param name="cart">The cart that was processed.</param>
        /// <param name="payment">The order payment.</param>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="acceptUrl">The redirect url when finished.</param>
        /// <param name="cancelUrl">The redirect url when error happens.</param>
        /// <returns>The redirection url after processing.</returns>
        public string ProcessSuccessfulTransaction(ICart cart, IPayment payment, string transactionId,
            string orderNumber, string acceptUrl, string cancelUrl)
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
                    return UriUtil.AddQueryString(cancelUrl, "message",
                        string.Join(";", errorMessages.Distinct().ToArray()));
                }

                // Save the transact from Dintero to payment.
                payment.TransactionID = transactionId;

                var purchaseOrder = MakePurchaseOrder(cart, orderNumber);

                redirectionUrl = UpdateAcceptUrl(purchaseOrder, payment, acceptUrl);

                // Commit changes
                scope.Complete();
            }

            return redirectionUrl;
        }

        private IPurchaseOrder MakePurchaseOrder(ICart cart, string orderNumber)
        {
            var purchaseOrderLink = _orderRepository.SaveAsPurchaseOrder(cart);
            var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(purchaseOrderLink.OrderGroupId);

            _orderRepository.Delete(cart.OrderLink);

            purchaseOrder.OrderStatus = OrderStatus.InProgress;
            purchaseOrder.OrderNumber = orderNumber;

            UpdateLastOrderOfCurrentContact(CustomerContext.Current.CurrentContact, purchaseOrder.Created);

            _orderRepository.Save(purchaseOrder);

            return purchaseOrder;
        }

        private static void UpdateLastOrderOfCurrentContact(CustomerContact contact, DateTime datetime)
        {
            if (contact != null)
            {
                contact.LastOrder = datetime;
                contact.SaveChanges();
            }
        }

        private bool DoCompletingCart(ICart cart, IList<string> errorMessages)
        {
            var isSuccess = true;

            if (_featureSwitch.IsSerializedCartsEnabled())
            {
                var validationIssues = new Dictionary<ILineItem, IList<ValidationIssue>>();
                cart.AdjustInventoryOrRemoveLineItems(
                    (item, issue) => AddValidationIssues(validationIssues, item, issue), _inventoryProcessor);

                isSuccess = !validationIssues.Any();

                foreach (var issue in validationIssues.Values.SelectMany(x => x).Distinct())
                {
                    if (issue == ValidationIssue.RemovedDueToInsufficientQuantityInInventory)
                    {
                        errorMessages.Add("Not enough in stock.");
                    }
                    else
                    {
                        errorMessages.Add("Cart validation failure.");
                    }
                }

                return isSuccess;
            }

            var isIgnoreProcessPayment = new Dictionary<string, object> {{"PreventProcessPayment", true}};
            var workflowResults = OrderGroupWorkflowManager.RunWorkflow((OrderGroup) cart,
                OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, isIgnoreProcessPayment);

            if (workflowResults.OutputParameters["Warnings"] is StringDictionary warnings)
            {
                isSuccess = warnings.Count == 0;

                foreach (string message in warnings.Values)
                {
                    errorMessages.Add(message);
                }
            }

            return isSuccess;
        }

        private static void AddValidationIssues(IDictionary<ILineItem, IList<ValidationIssue>> issues,
            ILineItem lineItem, ValidationIssue issue)
        {
            if (!issues.ContainsKey(lineItem))
            {
                issues.Add(lineItem, new List<ValidationIssue>());
            }

            if (!issues[lineItem].Contains(issue))
            {
                issues[lineItem].Add(issue);
            }
        }

        private static string UpdateAcceptUrl(IPurchaseOrder purchaseOrder, IPayment payment, string acceptUrl)
        {
            var redirectionUrl = UriUtil.AddQueryString(acceptUrl, "success", "true");
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "contactId", purchaseOrder.CustomerId.ToString());
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "orderNumber", purchaseOrder.OrderNumber);
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "email", payment.BillingAddress.Email);
            return redirectionUrl;
        }
    }
}