using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using EPiServer.Events.Clients;
using EPiServer.Logging;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Orders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                    var response = SendRequest<DinteroAuthResponse>(url,
                        $"{Configuration.ClientId}:{Configuration.ClientSecretId}", "Basic",
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
                                    currentCart.Currency.CurrencyCode),
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
                                Items = ConvertOrderLineItems(currentCart.Forms, currentCart.Currency),
                                PartialPayment = false
                            },
                            ProfileId = Configuration.ProfileId
                        };

                        var response = SendRequest<DinteroCreateSessionResponse>(url, accessToken, "Bearer", request);

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
        /// Retrieve transaction details
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public DinteroTransactionActionResponse GetTransactionDetails(string transactionId, string accessToken)
        {
            var result = new DinteroTransactionActionResponse();

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetTransactionDetailsUrl(transactionId);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var response = (JObject)SendRequest<object>(url, accessToken, requestMethod: "GET");

                    result.Id = response["id"]?.ToString().ToUpper();
                    result.Currency = response["currency"]?.ToString().ToUpper();
                    result.Status = response["status"]?.ToString().ToUpper();
                    result.Items = response["items"]?.ToObject<List<DinteroOrderLine>>();
                    result.Events = new List<DinteroTransactionEvent>();

                    var events = response["events"];
                    if (events != null)
                    {
                        foreach (var transactionEvent in events)
                        {
                            var eventObj = new DinteroTransactionEvent
                            {
                                Id = transactionEvent["id"]?.ToString(),
                                Event = transactionEvent["event"]?.ToString().ToUpper(),
                                Items = response["items"]?.ToObject<List<DinteroOrderLine>>(),
                                Success = response["success"]?.ToString().ToLower() == "true"
                            };

                            if (int.TryParse(response["amount"]?.ToString(), out var eventAmount))
                            {
                                eventObj.Amount = eventAmount;
                            }

                            result.Events.Add(eventObj);
                        }
                    }

                    if (int.TryParse(response["amount"]?.ToString(), out var amount))
                    {
                        result.Amount = amount;
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
        /// <param name="purchaseOrder"></param>
        /// <returns></returns>
        public TransactionResult CaptureTransaction(IPayment payment, IPurchaseOrder purchaseOrder)
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
        public TransactionResult CaptureTransaction(IPayment payment, IPurchaseOrder purchaseOrder, string accessToken)
        {
            var result = new TransactionResult();

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
                        Items = ConvertOrderLineItems(purchaseOrder.Forms, purchaseOrder.Currency)
                    };

                    result = SendTransactionRequest(url, "Bearer", accessToken, request, new List<string> {"CAPTURED"});
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
        public TransactionResult VoidTransaction(IPayment payment)
        {
            return SendAuthorizedRequest(token => VoidTransaction(payment, token));
        }

        /// <summary>
        /// Void transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public TransactionResult VoidTransaction(IPayment payment, string accessToken)
        {
            var result = new TransactionResult();

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetTransactionVoidUrl(payment.TransactionID);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    result = SendTransactionRequest(url, "Bearer", accessToken, null,
                        new List<string> {"AUTHORIZATION_VOIDED"});
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
        /// <param name="returnForm"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public TransactionResult RefundTransaction(IPayment payment, IEnumerable<IOrderForm> returnForms, Currency currency)
        {
            return SendAuthorizedRequest(token => RefundTransaction(payment, returnForms, currency, token));
        }

        /// <summary>
        /// Refund transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="returnForms"></param>
        /// <param name="currency"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public TransactionResult RefundTransaction(IPayment payment, IEnumerable<IOrderForm> returnForms,
            Currency currency, string accessToken)
        {
            var result = new TransactionResult();

            if (!Configuration.IsValid())
            {
                throw new Exception("Dintero configuration is not valid!");
            }

            var url = DinteroAPIUrlHelper.GetTransactionRefundUrl(payment.TransactionID);

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var transaction = GetTransactionDetails(payment.TransactionID, accessToken);

                    if (transaction == null)
                    {
                        throw new Exception("Dintero transaction can't be loaded!");
                    }

                    var request = new DinteroRefundRequest
                    {
                        Amount = CurrencyHelper.CurrencyToInt(payment.Amount, currency.CurrencyCode),
                        Reason = "Refund", // TODO: set reason
                    };

                    var returnForm = GetCurrentReturnForm(returnForms, transaction);

                    if (returnForm != null)
                    {
                        request.Items = ConvertRefundOrderLineItems(returnForm, transaction, currency);
                    }

                    result = SendTransactionRequest(url, "Bearer", accessToken, request,
                        new List<string> {"PARTIALLY_REFUNDED", "PARTIALLY_CAPTURED_REFUNDED", "REFUNDED"});
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred during refunding transaction.", e);
                    throw;
                }

            }

            return result;
        }

        private static TransactionResult SendTransactionRequest(string url, string tokenType, string accessToken,
            object data, ICollection<string> successStatuses)
        {
            var result = new TransactionResult();
            var response = (JObject) SendRequest<object>(url, accessToken, tokenType, data);

            if (response != null)
            {
                if (response["status"] == null && !successStatuses.Contains(response["status"]?.ToString().ToUpper()))
                {
                    var jsonError = response["error"]?.ToObject<DinteroResponseError>();

                    if (jsonError != null && !string.IsNullOrWhiteSpace(jsonError.Message))
                    {
                        result.Error = jsonError.Message;
                        result.ErrorCode = jsonError.Code;
                    }
                }
                else
                {
                    result.Success = true;
                }
            }

            if (!result.Success && string.IsNullOrWhiteSpace(result.Error))
            {
                result.Error = "Response is empty or has incorrect format";
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

        private static T SendRequest<T>(string url, string token, string tokenType = "Bearer", object data = null,
            string requestMethod = "POST")
        {
            var http = (HttpWebRequest) WebRequest.Create(new Uri(url));
            http.Accept = "application/json";
            http.ContentType = "application/json";
            http.Method = requestMethod;

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

            if (data != null && requestMethod == "POST")
            {
                var encoding = new ASCIIEncoding();
                var bytes = encoding.GetBytes(JsonConvert.SerializeObject(data));

                using (var newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();
                }
            }

            var result = default(T);

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

        private static IOrderForm GetCurrentReturnForm(IEnumerable<IOrderForm> returnForms,
            DinteroTransactionActionResponse transaction)
        {
            IOrderForm returnForm = null;

            if (returnForms != null)
            {
                var forms = returnForms.ToList();

                if (forms.Count == 1)
                {
                    returnForm = forms.First();
                }
                else
                {
                    var refunds = transaction.Events.Where(e => e.Success && e.Event == "REFUND").ToList();

                    foreach (var form in forms)
                    {
                        if (!refunds.Any(refund => HaveEqualLineItems(refund.Items, form.GetAllLineItems().ToList())))
                        {
                            returnForm = form;
                            break;
                        }
                    }
                }
            }

            return returnForm;
        }

        private static bool HaveEqualLineItems(ICollection<DinteroOrderLine> refundItems,
            ICollection<ILineItem> formItems)
        {
            return refundItems.Count == formItems.Count && refundItems.All(refundItem =>
                       formItems.Any(formItem =>
                           formItem.Code == refundItem.Id && formItem.Quantity == refundItem.Quantity));
        }

        private static List<DinteroOrderLine> ConvertRefundOrderLineItems(IOrderForm orderForm,
            DinteroTransactionActionResponse transaction, Currency currency)
        {
            // TODO: resolve address
            var shippingAddress = orderForm.Shipments.FirstOrDefault();
            return orderForm.GetAllLineItems().Select(lineItem => TransformLineItem(currency, lineItem,
                shippingAddress?.ShippingAddress, ResolveLineItemDinteroId(transaction, lineItem.Code))).ToList();
        }

        private static int ResolveLineItemDinteroId(DinteroTransactionActionResponse transaction, string code)
        {
            var dinteroItem = transaction.Items.FirstOrDefault(item => item.Id == code);
            if (dinteroItem != null && int.TryParse(dinteroItem.LineId, out var dinteroId))
            {
                return dinteroId;
            }
            return 0;
        }

        private static List<DinteroOrderLine> ConvertOrderLineItems(IEnumerable<IOrderForm> orderForms,
            Currency currency)
        {
            var items = new List<DinteroOrderLine>();

            var index = 0;

            foreach (var orderForm in orderForms)
            {
                foreach (var s in orderForm.Shipments)
                {
                    foreach (var item in s.LineItems.Select((value, i) => new {Index = i, Value = value}))
                    {
                        index = item.Index + 1;
                        items.Add(TransformLineItem(currency, item.Value, s.ShippingAddress, index));
                    }

                    index++;
                    var shipment = (Shipment) s;

                    items.Add(new DinteroOrderLine
                    {
                        Id = shipment.Id.ToString(),
                        Groups = new List<DinteroOrderLineGroup>(),
                        LineId = index.ToString(),
                        Description = shipment.ShippingMethodName,
                        Quantity = 1,
                        Amount = CurrencyHelper.CurrencyToInt(shipment.ShippingTotal, currency.CurrencyCode),
                        VatAmount = GetVatAmount(shipment.ShippingTotal, shipment.ShippingTax,
                            currency.CurrencyCode),
                        Vat = Convert.ToInt32(shipment.ShippingTax)
                    });
                }
            }

            return items;
        }

        private static DinteroOrderLine TransformLineItem(Currency currency, ILineItem lineItem,
            IOrderAddress orderAddress, int index)
        {
            var dinteroItem = new DinteroOrderLine
            {
                Id = lineItem.Code,
                Groups = new List<DinteroOrderLineGroup>(),
                LineId = index.ToString(),
                Description = lineItem.DisplayName,
                Quantity = lineItem.Quantity
            };

            AdjustPriceAndTaxes(currency, lineItem, dinteroItem, orderAddress);

            return dinteroItem;
        }

        private static void AdjustPriceAndTaxes(Currency currency, ILineItem lineItem,
            DinteroOrderLine dinteroItem, IOrderAddress orderAddress)
        {
            var amount = lineItem.GetExtendedPrice(currency).Amount;
            double vat = 0;

            var entryDto = CatalogContext.Current.GetCatalogEntryDto(lineItem.Code,
                new CatalogEntryResponseGroup(CatalogEntryResponseGroup.ResponseGroup.CatalogEntryFull));
            if (entryDto.CatalogEntry.Count > 0)
            {
                CatalogEntryDto.VariationRow[] variationRows = entryDto.CatalogEntry[0].GetVariationRows();
                if (variationRows.Length > 0)
                {
                    var taxCategory = CatalogTaxManager.GetTaxCategoryNameById(variationRows[0].TaxCategoryId);
                    var taxes = OrderContext.Current.GetTaxes(Guid.Empty, taxCategory,
                        Thread.CurrentThread.CurrentCulture.Name, orderAddress).ToList();

                    foreach (var tax in taxes)
                    {
                        if (tax.TaxType == TaxType.SalesTax)
                        {
                            vat = tax.Percentage;
                        }
                    }
                }
            }

            dinteroItem.Amount = CurrencyHelper.CurrencyToInt(amount, currency.CurrencyCode);
            dinteroItem.Vat = Convert.ToInt32(vat);
            dinteroItem.VatAmount = GetVatAmount(amount, vat, currency.CurrencyCode);
        }

        private static int GetVatAmount(decimal amount, decimal vat, string currencyCode)
        {
            return GetVatAmount(amount, Convert.ToDouble(vat), currencyCode);
        }

        private static int GetVatAmount(decimal amount, double vat, string currencyCode)
        {
            return CurrencyHelper.CurrencyToInt(Convert.ToDouble(amount) * vat / (100 + vat), currencyCode);
        }
    }
}