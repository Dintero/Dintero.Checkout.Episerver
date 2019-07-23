using Dintero.Checkout.Episerver.Helpers;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using System;
using System.Linq;

namespace Dintero.Checkout.Episerver
{
    [ServiceConfiguration(typeof(IPaymentOption))]
    public class DinteroPaymentOption : IPaymentOption
    {
        private readonly PaymentMethodDto.PaymentMethodRow _paymentMethod;

        public Guid PaymentMethodId { get; }
        public string SystemKeyword { get; }
        public string Name { get; }
        public string Description { get; }

        public DinteroPaymentOption()
        {
            _paymentMethod = DinteroConfiguration.GetDinteroPaymentMethod()?.PaymentMethod?.FirstOrDefault();

            if (_paymentMethod == null)
            {
                return;
            }

            PaymentMethodId = _paymentMethod.PaymentMethodId;
            SystemKeyword = _paymentMethod.SystemKeyword;
            Name = _paymentMethod.Name;
            Description = _paymentMethod.Description;
        }

        public IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var payment = new OtherPayment
            {
                PaymentMethodId = _paymentMethod.PaymentMethodId,
                PaymentMethodName = _paymentMethod.Name,
                Amount = amount,
                Status = PaymentStatus.Pending.ToString(),
                TransactionType = TransactionType.Authorization.ToString()
            };

            return payment;
        }

        public bool ValidateData()
        {
            return true;
        }
    }
}