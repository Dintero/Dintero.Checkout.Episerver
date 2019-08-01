using System;
using System.Linq;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;

namespace Dintero.Checkout.Episerver.Helpers
{
    public static class OrderHelper
    {
        public static ICart GetOrderByDinteroSessionId(string dinteroSessionId)
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
    }
}