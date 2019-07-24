using System;
using System.Globalization;

namespace Dintero.Checkout.Episerver.Helpers
{
    public static class CurrencyHelper
    {
        public static int CurrencyToInt(decimal amount, string currencyCode)
        {
            return CurrencyToInt(Convert.ToDouble(amount), currencyCode);
        }
        public static int CurrencyToInt(double amount, string currencyCode)
        {
            var culture = new CultureInfo(currencyCode);
            var precision = culture.NumberFormat.CurrencyDecimalDigits;
            return (int)Math.Round((amount * Math.Pow(10, precision)));
        }

        private static int GetPrecision(string currencyCode)
        {
            int precision;

            currencyCode = currencyCode?.ToUpperInvariant();

            switch (currencyCode)
            {
                case "BHD":
                case "IQD":
                case "JOD":
                case "KWD":
                case "LYD":
                case "OMR":
                case "TND":
                    precision = 3;
                    break;
                case "AED":
                case "AFN":
                case "ALL":
                case "AMD":
                case "ANG":
                case "AOA":
                case "ARS":
                case "AUD":
                case "AWG":
                case "AZN":
                case "BAM":
                case "BBD":
                case "BDT":
                case "BGN":
                case "BMD":
                case "BND":
                case "BOB":
                case "BRL":
                case "BSD":
                case "BTN":
                case "BWP":
                case "BZD":
                case "CAD":
                case "CDF":
                case "CHF":
                case "CNY":
                case "COP":
                case "CRC":
                case "CUC":
                case "CUP":
                case "CZK":
                case "DKK":
                case "DOP":
                case "DZD":
                case "EGP":
                case "ERN":
                case "ETB":
                case "EUR":
                case "FJD":
                case "FKP":
                case "GBP":
                case "GEL":
                case "GGP":
                case "GHS":
                case "GIP":
                case "GMD":
                case "GTQ":
                case "GYD":
                case "HKD":
                case "HNL":
                case "HRK":
                case "HTG":
                case "HUF":
                case "IDR":
                case "ILS":
                case "IMP":
                case "INR":
                case "IRR":
                case "JEP":
                case "JMD":
                case "KES":
                case "KGS":
                case "KHR":
                case "KPW":
                case "KYD":
                case "KZT":
                case "LAK":
                case "LBP":
                case "LKR":
                case "LRD":
                case "LSL":
                case "LTL":
                case "LVL":
                case "MAD":
                case "MDL":
                case "MKD":
                case "MMK":
                case "MNT":
                case "MOP":
                case "MUR":
                case "MVP":
                case "MVR":
                case "MWK":
                case "MXN":
                case "MYR":
                case "MZN":
                case "NAD":
                case "NGN":
                case "NIO":
                case "NOK":
                case "NPR":
                case "NZD":
                case "PAB":
                case "PEN":
                case "PGK":
                case "PHP":
                case "PKR":
                case "PLN":
                case "QAR":
                case "RON":
                case "RSD":
                case "RUB":
                case "SAR":
                case "SBD":
                case "SCR":
                case "SDG":
                case "SEK":
                case "SGD":
                case "SHP":
                case "SLL":
                case "SOS":
                case "SPL":
                case "SRD":
                case "STD":
                case "SVC":
                case "SYP":
                case "SZL":
                case "THB":
                case "TJS":
                case "TMT":
                case "TOP":
                case "TRY":
                case "TTD":
                case "TVD":
                case "TWD":
                case "TZS":
                case "UAH":
                case "USD":
                case "UYU":
                case "UZS":
                case "VEF":
                case "WST":
                case "XCD":
                case "XDR":
                case "YER":
                case "ZAR":
                case "ZMW":
                case "ZWD":
                    precision = 2;
                    break;
                case "MGA":
                case "MRO":
                    precision = 1;
                    break;
                case "BIF":
                case "BYR":
                case "CLP":
                case "CVE":
                case "DJF":
                case "GNF":
                case "ISK":
                case "JPY":
                case "KMF":
                case "KRW":
                case "PYG":
                case "RWF":
                case "UGX":
                case "VND":
                case "VUV":
                case "XAF":
                case "XOF":
                case "XPF":
                    precision = 0;
                    break;
                default:
                    throw new ArgumentException($"Unknown currency code {currencyCode}!");
            }

            return precision;
        }
    }
}