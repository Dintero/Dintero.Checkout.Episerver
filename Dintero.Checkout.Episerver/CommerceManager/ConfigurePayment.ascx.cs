using System;
using System.Data;
using System.Web.UI.WebControls;
using Dintero.Checkout.Episerver.Helpers;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;

namespace Dintero.Checkout.Episerver.CommerceManager
{
    public partial class ConfigurePayment : OrderBaseUserControl, IGatewayControl
    {
        // Fields
        private PaymentMethodDto _paymentMethodDto;
        private string _validationGroup;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurePayment"/> class.
        /// </summary>
        public ConfigurePayment()
        {
            this._validationGroup = string.Empty;
            this._paymentMethodDto = null;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.BindData();
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        public void BindData()
        {
            if ((this._paymentMethodDto != null) && (this._paymentMethodDto.PaymentMethodParameter != null))
            {
                SetTextBoxValue(DinteroConstants.AccountIdParameter, AccountId);
                SetTextBoxValue(DinteroConstants.ClientIdParameter, ClientId);
                SetTextBoxValue(DinteroConstants.ClientSecretIdParameter, ClientSecretId);
                SetTextBoxValue(DinteroConstants.ProfileIdParameter, ProfileId);
            }
            else
            {
                this.Visible = false;
            }
        }

        private void SetTextBoxValue(string keyname, TextBox ctrl)
        {
            PaymentMethodDto.PaymentMethodParameterRow parameterByName;
            parameterByName = this.GetParameterByName(keyname);
            if (parameterByName != null)
            {
                ctrl.Text = parameterByName.Value;
            }
        }

        private void SetCheckBoxValue(string keyname, CheckBox ctrl)
        {
            PaymentMethodDto.PaymentMethodParameterRow parameterByName;
            parameterByName = this.GetParameterByName(keyname);
            ctrl.Checked = false;
            if (parameterByName != null)
            {
                bool value = false;
                if (bool.TryParse(parameterByName.Value, out value))
                {
                    ctrl.Checked = value;
                }
            }
        }


        #region IGatewayControl Members

        /// <summary>
        /// Loads the object.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void LoadObject(object dto)
        {
            this._paymentMethodDto = dto as PaymentMethodDto;
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void SaveChanges(object dto)
        {
            if (this.Visible)
            {
                this._paymentMethodDto = dto as PaymentMethodDto;
                if ((this._paymentMethodDto != null) && (this._paymentMethodDto.PaymentMethodParameter != null))
                {
                    Guid paymentMethodId = Guid.Empty;
                    if (this._paymentMethodDto.PaymentMethod.Count > 0)
                    {
                        paymentMethodId = this._paymentMethodDto.PaymentMethod[0].PaymentMethodId;
                    }

                    SaveParameter(DinteroConstants.AccountIdParameter, AccountId, paymentMethodId);
                    SaveParameter(DinteroConstants.ClientIdParameter, ClientId, paymentMethodId);
                    SaveParameter(DinteroConstants.ClientSecretIdParameter, ClientSecretId, paymentMethodId);
                    SaveParameter(DinteroConstants.ProfileIdParameter, ProfileId, paymentMethodId);
                }
            }

        }

        private void SaveParameter(string key, TextBox ctrl, Guid paymentMethodId)
        {
            PaymentMethodDto.PaymentMethodParameterRow parameterByName;
            parameterByName = this.GetParameterByName(key);
            if (parameterByName != null)
            {
                parameterByName.Value = ctrl.Text;
            }
            else
            {
                this.CreateParameter(this._paymentMethodDto, key, ctrl.Text, paymentMethodId);
            }
        }

        private void SaveBoolParameter(string key, CheckBox ctrl, Guid paymentMethodId)
        {
            PaymentMethodDto.PaymentMethodParameterRow parameterByName;
            parameterByName = GetParameterByName(key);
            if (parameterByName != null)
            {
                parameterByName.Value = ctrl.Checked.ToString();
            }
            else
            {
                CreateParameter(_paymentMethodDto, key, ctrl.Checked.ToString(), paymentMethodId);
            }
        }

        private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
        {
            PaymentMethodDto.PaymentMethodParameterRow[] rowArray =
                (PaymentMethodDto.PaymentMethodParameterRow[]) this._paymentMethodDto.PaymentMethodParameter.Select(
                    string.Format("Parameter = '{0}'", name));
            if ((rowArray != null) && (rowArray.Length > 0))
            {
                return rowArray[0];
            }
            return null;
        }

        private void CreateParameter(PaymentMethodDto dto, string name, string value, Guid paymentMethodId)
        {
            PaymentMethodDto.PaymentMethodParameterRow row = dto.PaymentMethodParameter.NewPaymentMethodParameterRow();
            row.PaymentMethodId = paymentMethodId;
            row.Parameter = name;
            row.Value = value;
            if (row.RowState == DataRowState.Detached)
            {
                dto.PaymentMethodParameter.Rows.Add(row);
            }
        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>The validation group.</value>
        public string ValidationGroup
        {
            get
            {
                return _validationGroup;
            }
            set
            {
                _validationGroup = value;
            }
        }

        #endregion
    }
}