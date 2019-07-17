using Dintero.Checkout.Episerver.Models;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dintero.Checkout.Episerver.Helpers
{
    public class DinteroRequestsHelper
    {
        private readonly IOrderNumberGenerator _orderNumberGenerator;

        public DinteroConfiguration Configuration { get; }

        public DinteroRequestsHelper() : this(new DinteroConfiguration())
        {
        }

        public DinteroRequestsHelper(DinteroConfiguration configuration)
            : this(ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(), configuration)
        {
        }

        public DinteroRequestsHelper(IOrderNumberGenerator orderNumberGenerator, DinteroConfiguration configuration)
        {
            _orderNumberGenerator = orderNumberGenerator;
            Configuration = configuration;
        }

        /// <summary>
        /// Authorise and create Dintero session transaction
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="currentCart"></param>
        /// <returns></returns>
        public async Task<DinteroCreateSessionResponse> CreateTransaction(IPayment payment, ICart currentCart)
        {
            DinteroCreateSessionResponse sessionData = null;

            var token = await GetAccessToken();

            if (!string.IsNullOrWhiteSpace(token))
            {
                sessionData = await CreateCheckoutSession(payment, currentCart, token);
            }

            return sessionData;
        }

        /// <summary>
        /// Retrieve access token and create a transaction with the order details
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAccessToken()
        {
            string token = null;

            if (Configuration.IsValid())
            {
                var url = DinteroAPIUrlHelper.GetAuthUrl(Configuration.AccountId);

                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        var client = GetHttpClient("Basic", $"{Configuration.ClientId}:{Configuration.ClientSecretId}");

                        var response = await client.PostAsJsonAsync(url, new
                        {
                            grant_type = "client_credentials",
                            audience = DinteroAPIUrlHelper.GetAccountUrl(Configuration.AccountId)
                        });

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var responseObj = await response.Content.ReadAsAsync<DinteroAuthResponse>();
                            token = responseObj.AccessToken;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
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
        public async Task<DinteroCreateSessionResponse> CreateCheckoutSession(IPayment payment, ICart currentCart,
            string accessToken)
        {
            DinteroCreateSessionResponse sessionData = null;

            if (Configuration.IsValid() && !string.IsNullOrWhiteSpace(accessToken))
            {
                var url = DinteroAPIUrlHelper.GetNewSessionUrl();

                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        var orderForm = currentCart.Forms.FirstOrDefault(f => f.Payments.Contains(payment));
                        var shippingAddress = orderForm.Shipments.First().ShippingAddress;

                        var client = GetHttpClient("Bearer", accessToken);

                        var orderNumber = _orderNumberGenerator.GenerateOrderNumber(currentCart);

                        var request = new DinteroCreateSessionRequest
                        {
                            UrlSetting = new DinteroUrlSetting
                            {
                                ReturnUrl = UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentPage", true),
                                CallbackUrl = UriUtil.GetUrlFromStartPageReferenceProperty("DinteroPaymentPage", true)
                            },
                            Customer = new DinteroCustomer
                            {
                                Email = payment.BillingAddress.Email,
                                PhoneNumber = payment.BillingAddress.DaytimePhoneNumber
                            },
                            Order = new DinteroOrder
                            {
                                Amount = payment.Amount,
                                VatAmount = payment.Amount, // TODO: resolve VAT,
                                Currency = currentCart.Currency.CurrencyCode,
                                MerchantReference = orderNumber,
                                BillingAddress = new DinteroAddress
                                {
                                    FirstName = payment.BillingAddress.FirstName,
                                    LastName = payment.BillingAddress.LastName,
                                    AddressLine = $"{payment.BillingAddress.Line1} {payment.BillingAddress.Line2}",
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

                        var response = await client.PostAsJsonAsync(url, request);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            sessionData = await response.Content.ReadAsAsync<DinteroCreateSessionResponse>();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

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
            DinteroCaptureResponse result = null;

            if (Configuration.IsValid())
            {
                var url = DinteroAPIUrlHelper.GetTransactionCaptureUrl(payment.TransactionID);

                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        var client = GetHttpClient("Basic", $"{Configuration.ClientId}:{Configuration.ClientSecretId}");

                        var request = new DinteroCaptureRequest
                        {
                            Amount = payment.Amount,
                            CaptureReference = purchaseOrder.OrderNumber,
                            Items = new List<DinteroOrderLine>() // TODO: add order items
                        };

                        var response = client.PostAsJsonAsync(url, request).Result;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            result = response.Content.ReadAsAsync<DinteroCaptureResponse>().Result;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                }
            }

            return result;
        }

        /// <summary>
        /// Create an instance of HttpClient
        /// </summary>
        /// <param name="tokenType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static HttpClient GetHttpClient(string tokenType, string token)
        {
            var client = new HttpClient();

            if (tokenType == "Basic")
            {
                var byteArray = Encoding.ASCII.GetBytes(token);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            } else if (tokenType == "Bearer")
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private List<DinteroOrderLine> ConvertOrderLineItems(ICart currentCart)
        {
            var items = new List<DinteroOrderLine>();

            foreach (var item in currentCart.GetAllLineItems()
                .Select((value, i) => new { Index = i, Value = value }))
            {
                items.Add(new DinteroOrderLine
                {
                    Id = item.Value.LineItemId.ToString(),
                    Groups = new List<DinteroOrderLineGroup>(),
                    LineId = item.Index.ToString(),
                    Description = item.Value.DisplayName,
                    Quantity = item.Value.Quantity,
                    Amount = item.Value.GetExtendedPrice(currentCart.Currency).Amount, // TODO: fill property
                    VatAmount = 50, // TODO: fill property
                    Vat = 20 // TODO: fill property
                });
            }

            return items;
        }
    }
}