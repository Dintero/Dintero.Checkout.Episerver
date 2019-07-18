using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dintero.Checkout.Episerver.Helpers
{
    public class DinteroConfiguration
    {
        private PaymentMethodDto _paymentMethodDto;
        private IDictionary<string, string> _settings;

        public Guid PaymentMethodId { get; protected set; }

        public string AccountId { get; protected set; }
        public string ClientId { get; protected set; }
        public string ClientSecretId { get; protected set; }
        public string ProfileId { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DinteroConfiguration"/>.
        /// </summary>
        public DinteroConfiguration() : this(null) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DinteroConfiguration"/> with specific settings.
        /// </summary>
        /// <param name="settings">The specific settings.</param>
        public DinteroConfiguration(IDictionary<string, string> settings)
        {
            Initialize(settings);
        }

        public bool IsValid()
        {
            return !(string.IsNullOrWhiteSpace(AccountId) || string.IsNullOrWhiteSpace(ClientId) ||
                     string.IsNullOrWhiteSpace(ClientSecretId) || string.IsNullOrWhiteSpace(ProfileId));
        }

        /// <summary>
        /// Gets the PaymentMethodDto's parameter (setting in CommerceManager of Dintero) by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter row.</returns>
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(PaymentMethodDto paymentMethodDto,
            string parameterName)
        {
            var rowArray =
                (PaymentMethodDto.PaymentMethodParameterRow[]) paymentMethodDto.PaymentMethodParameter.Select(
                    $"Parameter = '{parameterName}'");
            return rowArray.Length > 0 ? rowArray[0] : null;
        }

        /// <summary>
        /// Gets the PaymentMethodDto of Dintero.
        /// </summary>
        /// <returns>The Dintero payment method.</returns>
        public static PaymentMethodDto GetDinteroPaymentMethod()
        {
            return PaymentManager.GetPaymentMethodBySystemName(DinteroConstants.DinteroSystemName,
                SiteContext.Current.LanguageName);
        }

        protected void Initialize(IDictionary<string, string> settings)
        {
            _paymentMethodDto = GetDinteroPaymentMethod();
            PaymentMethodId = GetPaymentMethodId();

            _settings = settings ?? GetSettings();
            GetParametersValues();
        }

        private IDictionary<string, string> GetSettings()
        {
            return _paymentMethodDto.PaymentMethod.FirstOrDefault()?.GetPaymentMethodParameterRows()
                ?.ToDictionary(row => row.Parameter, row => row.Value);
        }

        private void GetParametersValues()
        {
            if (_settings != null)
            {
                AccountId = GetParameterValue(DinteroConstants.AccountIdParameter);
                ClientId = GetParameterValue(DinteroConstants.ClientIdParameter);
                ClientSecretId = GetParameterValue(DinteroConstants.ClientSecretIdParameter);
                ProfileId = GetParameterValue(DinteroConstants.ProfileIdParameter);
            }
        }

        private string GetParameterValue(string parameterName)
        {
            return _settings.TryGetValue(parameterName, out var parameterValue) ? parameterValue : string.Empty;
        }

        private Guid GetPaymentMethodId()
        {
            return _paymentMethodDto.PaymentMethod.Rows[0] is PaymentMethodDto.PaymentMethodRow dinteroPaymentMethodRow
                ? dinteroPaymentMethodRow.PaymentMethodId
                : Guid.Empty;
        }
    }
}