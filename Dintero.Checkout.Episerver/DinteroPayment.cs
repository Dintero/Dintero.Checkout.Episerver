using System;
using System.Runtime.Serialization;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus.Configurator;

namespace Dintero.Checkout.Episerver
{
    [Serializable]
    public class DinteroPayment : Payment
    {
        public static MetaClass DinteroPaymentMetaClass
        {
            get
            {
                if (DinteroPayment._MetaClass == null)
                    DinteroPayment._MetaClass = MetaClass.Load(OrderContext.MetaDataContext, "DinteroPayment");
                return DinteroPayment._MetaClass;
            }
        }

        public DinteroPayment()
            : base(DinteroPayment.DinteroPaymentMetaClass)
        {

        }

        public DinteroPayment(MetaClass metaClass)
            : base(metaClass)
        {
            this.PaymentType = PaymentType.CreditCard;

        }

        public DinteroPayment(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.PaymentType = PaymentType.Other;
        }

        private static MetaClass _MetaClass;



        public string CardNumberMasked
        {
            get { return base.GetString("CardNumberMasked"); }
            set { this["CardNumberMasked"] = value; }
        }

        public string CartTypeName
        {
            get { return base.GetString("CardTypeName"); }
            set { this["CardTypeName"] = value; }
        }
    }
}