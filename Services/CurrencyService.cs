using System;
using System.Collections.Generic;
using System.Linq;

namespace MeraKharcha.Services
{
    public class CurrencyInfoDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal RateToInr { get; set; }
        public string Flag { get; set; } = string.Empty;
    }

    public interface ICurrencyService
    {
        List<CurrencyInfoDto> GetSupportedCurrencies();
        decimal GetRateToInr(string currencyCode);
        decimal ConvertToInr(decimal foreignAmount, string currencyCode);
    }

    public class CurrencyService : ICurrencyService
    {
        private static readonly List<CurrencyInfoDto> Currencies = new()
        {
            new() { Code = "INR", Name = "Indian Rupee", Symbol = "₹", RateToInr = 1.0m, Flag = "🇮🇳" },
            new() { Code = "USD", Name = "US Dollar", Symbol = "$", RateToInr = 84.65m, Flag = "🇺🇸" },
            new() { Code = "EUR", Name = "Euro", Symbol = "€", RateToInr = 91.40m, Flag = "🇪🇺" },
            new() { Code = "AED", Name = "UAE Dirham", Symbol = "د.إ", RateToInr = 23.05m, Flag = "🇦🇪" },
            new() { Code = "THB", Name = "Thai Baht", Symbol = "฿", RateToInr = 2.48m, Flag = "🇹🇭" },
            new() { Code = "GBP", Name = "British Pound", Symbol = "£", RateToInr = 108.25m, Flag = "🇬🇧" },
            new() { Code = "SGD", Name = "Singapore Dollar", Symbol = "S$", RateToInr = 63.70m, Flag = "🇸🇬" },
            new() { Code = "JPY", Name = "Japanese Yen", Symbol = "¥", RateToInr = 0.57m, Flag = "🇯🇵" }
        };

        public List<CurrencyInfoDto> GetSupportedCurrencies() => Currencies;

        public decimal GetRateToInr(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode)) return 1.0m;
            var curr = Currencies.FirstOrDefault(c => c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
            return curr?.RateToInr ?? 1.0m;
        }

        public decimal ConvertToInr(decimal foreignAmount, string currencyCode)
        {
            var rate = GetRateToInr(currencyCode);
            return Math.Round(foreignAmount * rate, 2);
        }
    }
}
