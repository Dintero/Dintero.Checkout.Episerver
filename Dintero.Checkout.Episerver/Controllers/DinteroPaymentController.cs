using Dintero.Checkout.Episerver.Helpers;
using Dintero.Checkout.Episerver.PageTypes;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Security;
using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EPiServer.Logging;

namespace Dintero.Checkout.Episerver.Controllers
{
    public class DinteroPaymentController : PageController<DinteroPage>
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(DinteroPaymentController));

        private readonly IOrderRepository _orderRepository;
        private readonly DinteroRequestsHelper _requestsHelper;


        public DinteroPaymentController() : this(ServiceLocator.Current.GetInstance<IOrderRepository>(),
            new DinteroRequestsHelper()) { }

        public DinteroPaymentController(IOrderRepository orderRepository, DinteroRequestsHelper requestsHelper)
        {
            _orderRepository = orderRepository;
            _requestsHelper = requestsHelper;
        }

        public ActionResult Index(string error, string transaction_id, string session_id, string merchant_reference, string trackingNumber)
        {
            if (PageEditing.PageIsInEditMode)
            {
                return new EmptyResult();
            }

            string redirectUrl = null;
            Logger.Debug($"HttpContext.Request.RawUrl: {HttpContext.Request.RawUrl}.");
            Logger.Debug($"Dintero payment error: {error}; transaction_id: {transaction_id}; session_id: {session_id}; merchant_reference: {merchant_reference}; trackingNumber: {trackingNumber}");

            var cancelUrl = UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentCancelPage");
            var acceptUrl = UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentLandingPage");
            cancelUrl = UriUtil.AddQueryString(cancelUrl, "success", "false");
            var orderNumber = merchant_reference + trackingNumber;

            InitializeResponse();

            Logger.Debug($"Lock {orderNumber}");
            LockHelper.Lock(orderNumber);

            var gateway = ServiceLocator.Current.GetInstance<DinteroPaymentGateway>();

            Logger.Debug($"Dintero payment {orderNumber} start processing");

            try
            {
                ICart currentCart;
                if (string.IsNullOrWhiteSpace(session_id))
                {
                    // redirect_url is called
                    currentCart =
                        _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(), Cart.DefaultName);

                    // in case it's redirect from Dintero, but for some reason cookies are not set OR it's redirect from checkout(trackingNumber not empty)
                    if (currentCart == null && (!string.IsNullOrWhiteSpace(transaction_id) || !string.IsNullOrEmpty(trackingNumber) ))
                    {
                        var predictableOrderId = string.IsNullOrEmpty(trackingNumber) ? merchant_reference : trackingNumber;
                        Logger.Debug($"Trying to get cart by predictable order Id predictableOrderId: {predictableOrderId}");
                        currentCart = OrderHelper.GetCartByPredictableOrderId(predictableOrderId);
                        if (currentCart != null)
                        {
                            Logger.Debug($"Cart has been loaded by predictableOrderId: {predictableOrderId}");
                        }
                    }

                    Logger.Debug($"CurrentPrincipal {PrincipalInfo.CurrentPrincipal.GetContactId()}");
                    
                    if (currentCart == null)
                    {
                        // check if cart was processed by return_url / callback_url
                        var order = OrderHelper.GetOrderByTrackingNumber(merchant_reference);
                        if (order == null)
                        {
                            Logger.Debug(
                                $"Dintero payment {orderNumber} Cart cannot be loaded!!! contactId: {PrincipalInfo.CurrentPrincipal.GetContactId()}.");

                            throw new PaymentException(PaymentException.ErrorType.ProviderError, "",
                                "Cart cannot be loaded!!!");
                        }

                        Logger.Debug(
                            $"Dintero payment {orderNumber} redirect to accept Url. Order status - {((IOrderGroup) order).OrderStatus}");
                        return Redirect(DinteroPaymentGateway.UpdateAcceptUrl(order, acceptUrl,
                            ((IOrderGroup) order).OrderStatus == OrderStatus.OnHold));
                    }
                    else
                    {
                        Logger.Debug($"CurrentCart.OrderFormId= {currentCart.Forms.First().OrderFormId}");
                        Logger.Debug($"Principal From Cart {currentCart.CustomerId}");
                    }

                    if (!currentCart.Forms.Any() || !currentCart.GetFirstForm().Payments.Any())
                    {
                        Logger.Debug($"Dintero payment {orderNumber} Cart is invalid!");
                        throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Cart is invalid!");
                    }

                    Logger.Debug($"Dintero payment {orderNumber} retrieved cart for current principal");
                }
                else
                {
                    // if session_id parameter is set it means that it is either Dintero callback was sent (in pair with redirect)
                    // or on_hold transaction was processed
                    // in the first case we need to check whether redirect was processed if yes then skip
                    // in the second we need to release order (check transaction if it is authorised or failed)

                    // check if cart has been already processed by redirect_url
                    currentCart = OrderHelper.GetCartByDinteroSessionId(session_id);
                    if (currentCart == null)
                    {
                        var order = OrderHelper.GetOrderByTrackingNumber(merchant_reference);
                        if (order != null && ((IOrderGroup)order).OrderStatus == OrderStatus.OnHold)
                        {
                            Logger.Debug($"Processing OnHold Order.");
                            var result = gateway.ProcessOnHoldOrder(order.Id, transaction_id);
                            if (result == null)
                            {
                                Logger.Debug("Unable to release OnHold order.");
                            }
                            else
                            {
                                Logger.Debug($"Processing OnHold Order completed: {result.OrderStatus}.");
                            }
                        }
                        else
                        {
                            Logger.Debug($"Dintero payment {orderNumber} cart is already processed");
                        }
                        return new HttpStatusCodeResult(HttpStatusCode.OK);
                    }

                    Logger.Debug($"CurrentPrincipal {currentCart.CustomerId}");
                    Logger.Debug($"CurrentCart.OrderFormId= {currentCart.Forms.First().OrderFormId}");
                    Logger.Debug($"Dintero payment {orderNumber} cart as retrieved by session Id");
                }

                var formPayments = currentCart.Forms.SelectMany(f => f.Payments).Select(p => p.PaymentId);

                var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c =>
                    c.PaymentMethodId.Equals(_requestsHelper.Configuration.PaymentMethodId));

                if (payment == null)
                {
                    Logger.Debug($"Dintero payment {orderNumber} payment is null");

                    throw new PaymentException(PaymentException.ErrorType.ProviderError, "",
                        $"Payment was not specified ({_requestsHelper.Configuration.PaymentMethodId}). Form payments: {string.Join(";", formPayments)}");
                }

                if (string.IsNullOrWhiteSpace(transaction_id) && string.IsNullOrWhiteSpace(error))
                {
                    var sessionData = _requestsHelper.CreateTransaction(payment, currentCart, trackingNumber);

                    if (sessionData != null)
                    {
                        var cart = _orderRepository.LoadCart<Cart>(currentCart.CustomerId, Cart.DefaultName);
                        cart[DinteroConstants.DinteroSessionMetaField] = sessionData.SessionId;
                        cart.OrderForms[0][DinteroConstants.DinteroSessionMetaField] = sessionData.SessionId;
                        cart.AcceptChanges();

                        Logger.Debug($"Dintero payment {orderNumber} redirect to checkout");

                        return Redirect(sessionData.CheckoutUrl);
                    }

                    Logger.Debug($"Dintero payment {orderNumber} redirect to cancel");

                    return Redirect(cancelUrl);
                }

                cancelUrl = UriUtil.AddQueryString(cancelUrl, "paymentMethod", "dintero");

                if (string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(transaction_id) &&
                    !string.IsNullOrWhiteSpace(merchant_reference))
                {
                    payment.TransactionID = transaction_id;
                    payment.TransactionType = TransactionType.Authorization.ToString();

                    redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, transaction_id,
                        merchant_reference, acceptUrl, cancelUrl);
                }
                else
                {
                    TempData["Message"] = error;
                    redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, error);
                }

                Logger.Debug($"Dintero payment {orderNumber} call post authorize");

                DinteroPaymentGateway.PostProcessPayment.PostAuthorize(payment, error, transaction_id,
                    merchant_reference);
            }
            catch (Exception e)
            {
                Logger.Error($"Dintero payment {orderNumber} failed {e}");
            }
            finally
            {
                Logger.Debug("Release");
                LockHelper.Release(orderNumber);
            }

            if (string.IsNullOrWhiteSpace(session_id))
            {
                Logger.Debug($"Dintero payment {orderNumber} redirect to {redirectUrl}");
                return Redirect(redirectUrl);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private void InitializeResponse()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Pragma", "no-cache");
        }
    }
}