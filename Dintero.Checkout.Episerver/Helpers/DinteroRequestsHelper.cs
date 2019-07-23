using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using EPiServer.Logging;
using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Helpers
{
    public class DinteroRequestsHelper
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(DinteroRequestsHelper));
        private readonly IOrderNumberGenerator _orderNumberGenerator;

        public DinteroConfiguration Configuration { get; }

        public DinteroRequestsHelper() : this(new DinteroConfiguration()) { }

        public DinteroRequestsHelper(DinteroConfiguration configuration) : this(
            ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(), configuration) { }

        public DinteroRequestsHelper(IOrderNumberGenerator orderNumberGenerator, DinteroConfiguration configuration)
        {
            _orderNumberGenerator = orderNumberGenerator;
            Configuration = configuration;
        }

        /// <summary>
        /// Authorize and create Dintero session transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="currentCart"></param>
        /// <returns></returns>
        public DinteroCreateSessionResponse CreateTransaction(IPayment payment, ICart currentCart)
        {
            return SendAuthorizedRequest(token => CreateCheckoutSession(payment, currentCart, token));
        }

        /// <summary>
        /// Retrieve access token and create a transaction with the order details
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {
            string token = null;

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetAuthUrl(Configuration.AccountId);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var response = SendRequest<DinteroAuthResponse>(url, "Basic",
                        $"{Configuration.ClientId}:{Configuration.ClientSecretId}",
                        new
                        {
                            grant_type = "client_credentials",
                            audience = DinteroAPIUrlHelper.GetAccountUrl(Configuration.AccountId)
                        });

                    if (response != null)
                    {
                        token = response.AccessToken;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred during requesting access token.", e);
                    throw;
                }

            }

            return token;
        }

        /// <summary>
        /// Create Dintero session
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="currentCart"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public DinteroCreateSessionResponse CreateCheckoutSession(IPayment payment, ICart currentCart,
            string accessToken)
        {
            DinteroCreateSessionResponse sessionData = null;

            if (Configuration.IsValid() && !string.IsNullOrWhiteSpace(accessToken))
            {
                var url = DinteroAPIUrlHelper.GetNewSessionUrl();

                if (!Configuration.IsValid())
                {
                    throw new Exception("Dintero configuration is not valid!");
                }

                try
                {
                    var orderForm = currentCart.Forms.FirstOrDefault(f => f.Payments.Contains(payment));

                    if (orderForm != null)
                    {
                        var shippingAddress = orderForm.Shipments.First().ShippingAddress;

                        var orderNumber = _orderNumberGenerator.GenerateOrderNumber(currentCart);

                        var request = new DinteroCreateSessionRequest
                        {
                            UrlSetting =
                                new DinteroUrlSetting
                                {
                                    ReturnUrl =
                                        UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentPage", true),
                                    CallbackUrl =
                                        UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentPage", true)
                                },
                            Customer = new DinteroCustomer
                            {
                                Email = payment.BillingAddress.Email,
                                PhoneNumber = payment.BillingAddress.DaytimePhoneNumber
                            },
                            Order = new DinteroOrder
                            {
                                Amount =
                                    CurrencyHelper.CurrencyToInt(payment.Amount, currentCart.Currency.CurrencyCode),
                                VatAmount = CurrencyHelper.CurrencyToInt(payment.Amount,
                                    currentCart.Currency.CurrencyCode), // TODO: resolve VAT,
                                Currency = currentCart.Currency.CurrencyCode,
                                MerchantReference = orderNumber,
                                BillingAddress =
                                    new DinteroAddress
                                    {
                                        FirstName = payment.BillingAddress.FirstName,
                                        LastName = payment.BillingAddress.LastName,
                                        AddressLine =
                                            $"{payment.BillingAddress.Line1} {payment.BillingAddress.Line2}",
                                        PostalCode = payment.BillingAddress.PostalCode,
                                        PostalPlace = payment.BillingAddress.City,
                                        Country = payment.BillingAddress.CountryCode
                                    },
                                ShippingAddress = new DinteroAddress
                                {
                                    FirstName = shippingAddress.FirstName,
                                    LastName = shippingAddress.LastName,
                                    AddressLine = $"{shippingAddress.Line1} {shippingAddress.Line2}",
                                    PostalCode = shippingAddress.PostalCode,
                                    PostalPlace = shippingAddress.City,
                                    Country = shippingAddress.CountryCode
                                },
                                Items = ConvertOrderLineItems(currentCart),
                                PartialPayment = false
                            },
                            ProfileId = Configuration.ProfileId
                        };

                        var response = SendRequest<DinteroCreateSessionResponse>(url, "Bearer", accessToken, request);

                        if (response != null)
                        {
                            sessionData = response;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred during initializing payment session.", e);
                    throw;
                }

            }

            return sessionData;
        }

        /// <summary>
        /// Capture transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        /// <returns></returns>
        public DinteroCaptureResponse CaptureTransaction(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            return SendAuthorizedRequest(token => CaptureTransaction(payment, purchaseOrder, token));
        }

        /// <summary>
        /// Capture transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public DinteroCaptureResponse CaptureTransaction(IPayment payment, IPurchaseOrder purchaseOrder,
            string accessToken)
        {
            DinteroCaptureResponse result = null;

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetTransactionCaptureUrl(payment.TransactionID);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var request = new DinteroCaptureRequest
                    {
                        Amount = CurrencyHelper.CurrencyToInt(payment.Amount, purchaseOrder.Currency.CurrencyCode),
                        CaptureReference = purchaseOrder.OrderNumber,
                        Items = ConvertOrderLineItems(purchaseOrder)
                    };

                    var response = SendRequest<DinteroCaptureResponse>(url, "Bearer", accessToken, request);

                    if (response != null)
                    {
                        result = response;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred during capturing transaction.", e);
                    throw;
                }

            }

            return result;
        }

        /// <summary>
        /// Capture transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <returns></returns>
        public DinteroVoidResponse VoidTransaction(IPayment payment)
        {
            return SendAuthorizedRequest(token => VoidTransaction(payment, token));
        }

        /// <summary>
        /// Void transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public DinteroVoidResponse VoidTransaction(IPayment payment, string accessToken)
        {
            DinteroVoidResponse result = null;

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetTransactionVoidUrl(payment.TransactionID);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var response = SendRequest<DinteroVoidResponse>(url, "Bearer", accessToken, null);

                    if (response != null)
                    {
                        result = response;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred during voiding transaction.", e);
                    throw;
                }

            }

            return result;
        }

        /// <summary>
        /// Refund transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        /// <returns></returns>
        public DinteroRefundResponse RefundTransaction(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            return SendAuthorizedRequest(token => RefundTransaction(payment, purchaseOrder, token));
        }

        /// <summary>
        /// Refund transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public DinteroRefundResponse RefundTransaction(IPayment payment, IPurchaseOrder purchaseOrder,
            string accessToken)
        {
            DinteroRefundResponse result = null;

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetTransactionRefundUrl(payment.TransactionID);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var request = new DinteroRefundRequest
                    {
                        Amount = CurrencyHelper.CurrencyToInt(payment.Amount, purchaseOrder.Currency.CurrencyCode),
                        Reason = "Refund", // TODO: set reason,
                        Items = ConvertOrderLineItems(purchaseOrder)
                    };

                    var response = SendRequest<DinteroRefundResponse>(url, "Bearer", accessToken, request);

                    if (response != null)
                    {
                        result = response;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred during refunding transaction.", e);
                    throw;
                }

            }

            return result;
        }

        private T SendAuthorizedRequest<T>(Func<string, T> func)
        {
            var result = default(T);

            var token = GetAccessToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                result = func(token);
            }

            return result;
        }

        private static T SendRequest<T>(string url, string tokenType, string token, object data)
        {
            var http = (HttpWebRequest) WebRequest.Create(new Uri(url));
            http.Accept = "application/json";
            http.ContentType = "application/json";
            http.Method = "POST";

            var authorizationHeader = string.Empty;

            if (tokenType == "Basic")
            {
                authorizationHeader = $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{token}"))}";
            }
            else if (tokenType == "Bearer")
            {
                authorizationHeader = $"Bearer {token}";
            }

            http.Headers.Add("Authorization", authorizationHeader);

            var encoding = new ASCIIEncoding();
            var bytes = encoding.GetBytes(JsonConvert.SerializeObject(data));

            using (var newStream = http.GetRequestStream())
            {
                var result = default(T);

                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                var response = http.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd();

                        result = JsonConvert.DeserializeObject<T>(content);
                    }
                }

                return result;
            }
        }

        private static List<DinteroOrderLine> ConvertOrderLineItems(IOrderGroup currentCart)
        {
            var items = new List<DinteroOrderLine>();

            foreach (var item in currentCart.GetAllLineItems().Select((value, i) => new {Index = i, Value = value}))
            {
                items.Add(new DinteroOrderLine
                {
                    Id = item.Value.LineItemId.ToString(),
                    Groups = new List<DinteroOrderLineGroup>(),
                    LineId = item.Index.ToString(),
                    Description = item.Value.DisplayName,
                    Quantity = item.Value.Quantity,
                    Amount = CurrencyHelper.CurrencyToInt(item.Value.GetExtendedPrice(currentCart.Currency).Amount,
                        currentCart.Currency.CurrencyCode),
                    VatAmount = 50, // TODO: fill property
                    Vat = 20 // TODO: fill property
                });
            }

            return items;
        }
    }
}