using Dintero.Checkout.Episerver.Helpers;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;

namespace Dintero.Checkout.Episerver.Business
{
    [InitializableModule]
    public class ModuleInitializer : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var mdContext = CatalogContext.MetaDataContext;

            var wrapOrder = CreateMetaField(mdContext, OrderConfiguration.Instance.MetaClasses.ShoppingCartClass.Name,
                DinteroConstants.DinteroSessionMetaField, MetaDataType.ShortString, 32, true, false);
            JoinField(mdContext, wrapOrder, OrderConfiguration.Instance.MetaClasses.ShoppingCartClass.Name);
            JoinField(mdContext, wrapOrder, "OrderFormEx");
        }

        private static MetaField CreateMetaField(MetaDataContext mdContext, string metaDataNamespace, string name,
            MetaDataType type, int length, bool allowNulls, bool cultureSpecific)
        {
            var f = MetaField.Load(mdContext, name) ?? MetaField.Create(mdContext, metaDataNamespace, name, name,
                        string.Empty, type, length, allowNulls, cultureSpecific, false, false);
            return f;
        }

        private void JoinField(MetaDataContext mdContext, MetaField field, string metaClassName)
        {
            var cls = MetaClass.Load(mdContext, metaClassName);

            if (MetaFieldIsNotConnected(field, cls))
            {
                cls.AddField(field);
            }
        }

        private static bool MetaFieldIsNotConnected(MetaField field, MetaClass cls)
        {
            return cls != null && !cls.MetaFields.Contains(field);
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}