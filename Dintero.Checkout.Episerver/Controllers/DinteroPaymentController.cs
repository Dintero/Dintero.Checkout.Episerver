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
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Dintero.Checkout.Episerver.Controllers
{
    public class DinteroPaymentController : PageController<DinteroPage>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly DinteroRequestsHelper _requestsHelper;

        public DinteroPaymentController() : this(ServiceLocator.Current.GetInstance<IOrderRepository>(),
            new DinteroRequestsHelper()) { }

        public DinteroPaymentController(IOrderRepository orderRepository, DinteroRequestsHelper requestsHelper)
        {
            _orderRepository = orderRepository;
            _requestsHelper = requestsHelper;
        }

        public async Task<ActionResult> Index(string error, string transaction_id, string merchant_reference)
        {
            if (PageEditing.PageIsInEditMode)
            {
                return new EmptyResult();
            }

            var currentCart =
                _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(), Cart.DefaultName);
            if (!currentCart.Forms.Any() || !currentCart.GetFirstForm().Payments.Any())
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Exception");
            }

            var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c =>
                c.PaymentMethodId.Equals(_requestsHelper.Configuration.PaymentMethodId));
            if (payment == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Payment was not specified");
            }

            InitializeResponse();

            if (string.IsNullOrWhiteSpace(transaction_id) && string.IsNullOrWhiteSpace(error))
            {
                var sessionData = await _requestsHelper.CreateTransaction(payment, currentCart);

                if (sessionData != null)
                {
                    return Redirect(sessionData.CheckoutUrl);
                }
            }
            else
            {
                var cancelUrl =
                    UriUtil.GetUrlFromStartPageReferenceProperty("CheckoutPage");
                cancelUrl = UriUtil.AddQueryString(cancelUrl, "success", "false");
                cancelUrl = UriUtil.AddQueryString(cancelUrl, "paymentMethod", "dintero");
                var gateway = new DinteroPaymentGateway();

                string redirectUrl;

                if (string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(transaction_id) &&
                    !string.IsNullOrWhiteSpace(merchant_reference))
                {
                    var acceptUrl = UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentLandingPage");
                    redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, transaction_id,
                        merchant_reference, acceptUrl, cancelUrl);
                }
                else
                {
                    TempData["Message"] = "Cancel message";
                    redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, "Cancel message");
                }

                return Redirect(redirectUrl);
            }

            return new EmptyResult();
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