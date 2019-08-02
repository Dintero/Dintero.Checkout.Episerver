using System;
using System.Linq;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;

namespace Dintero.Checkout.Episerver.Helpers
{
    public static class OrderHelper
    {
        public static ICart GetCartByDinteroSessionId(string dinteroSessionId)
        {
            if (!string.IsNullOrEmpty(dinteroSessionId))
            {
                try
                {
                    var sqlMetaWhereClause = $@"META.{DinteroConstants.DinteroSessionMetaField} = '{dinteroSessionId}'";
                    var orderSearchOptions = new OrderSearchOptions();
                    orderSearchOptions.Classes.Add("ShoppingCart");
                    orderSearchOptions.CacheResults = false;
                    orderSearchOptions.RecordsToRetrieve = 1;

                    var results = Cart.Search(
                        new OrderSearch
                        {
                            SearchParameters = new OrderSearchParameters {SqlMetaWhereClause = sqlMetaWhereClause},
                            SearchOptions = orderSearchOptions
                        }, out var number);


                    if (number > 0)
                    {
                        return results.FirstOrDefault();
                    }

                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            return null;
        }

        public static PurchaseOrder GetOrderByTrackingNumber(string orderId)
        {
            var orderSearchParameters = new OrderSearchParameters {SqlMetaWhereClause = $@"META.TrackingNumber = '{orderId}'"};

            var orderSearchOptions = new OrderSearchOptions {Namespace = "Mediachase.Commerce.Orders"};
            orderSearchOptions.Classes.Add("PurchaseOrder");
            orderSearchOptions.Classes.Add("Shipment");
            orderSearchOptions.CacheResults = false;
            orderSearchOptions.RecordsToRetrieve = 1;

            var purchaseOrders = OrderContext.Current.FindPurchaseOrders(orderSearchParameters, orderSearchOptions).ToList();

            if (purchaseOrders.Count > 0)
            {
                // order was found
                return purchaseOrders.FirstOrDefault();
            }
            else if (int.TryParse(orderId, out var orderIdNumeric))
            {
                // order was not found; try to get by id
                return OrderContext.Current.GetPurchaseOrderById(orderIdNumeric);
            }

            return null;
        }
    }
}