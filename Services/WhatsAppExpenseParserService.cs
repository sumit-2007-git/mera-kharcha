using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MeraKharcha.Services
{
    public class ParsedExpenseDto
    {
        public bool Success { get; set; } = true;
        public decimal Amount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PayerName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "General / Other";
        public string RawMessage { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    public interface IWhatsAppExpenseParserService
    {
        ParsedExpenseDto ParseExpenseMessage(string message, List<string> availableMemberNames);
        string GenerateDebtReminderText(string debtorName, string creditorName, decimal amount, string? creditorUpi, string groupName, string appUrl);
    }

    public class WhatsAppExpenseParserService : IWhatsAppExpenseParserService
    {
        public ParsedExpenseDto ParseExpenseMessage(string message, List<string> availableMemberNames)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ParsedExpenseDto { Success = false, Explanation = "Empty message received." };
            }

            var trimmed = message.Trim();
            var result = new ParsedExpenseDto { RawMessage = trimmed };

            // 1. Extract Amount (numbers like 1500, 1500.50, ₹1500, rs 1500)
            var amountMatch = Regex.Match(trimmed, @"(?:₹|rs\.?|inr)?\s*(\d+(?:\.\d{1,2})?)", RegexOptions.IgnoreCase);
            if (amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value, out var parsedAmount))
            {
                result.Amount = parsedAmount;
            }
            else
            {
                return new ParsedExpenseDto { Success = false, Explanation = "Could not detect a valid amount (e.g. 1500 or ₹500)." };
            }

            // 2. Identify Payer from availableMemberNames or keywords
            string? matchedPayer = null;
            foreach (var memberName in availableMemberNames)
            {
                var firstName = memberName.Split(' ')[0];
                if (Regex.IsMatch(trimmed, $@"\b{Regex.Escape(firstName)}\b", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(trimmed, $@"\b{Regex.Escape(memberName)}\b", RegexOptions.IgnoreCase))
                {
                    matchedPayer = memberName;
                    break;
                }
            }
            result.PayerName = matchedPayer ?? (availableMemberNames.Count > 0 ? availableMemberNames[0] : "You");

            // 3. Extract description/title
            // Remove amount and common filler words: paid, spent, for, on, by, rs, inr, etc.
            var cleanText = Regex.Replace(trimmed, @"(?:₹|rs\.?|inr)?\s*\d+(?:\.\d{1,2})?", "", RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(matchedPayer))
            {
                cleanText = Regex.Replace(cleanText, $@"\b{Regex.Escape(matchedPayer.Split(' ')[0])}\b", "", RegexOptions.IgnoreCase);
            }
            cleanText = Regex.Replace(cleanText, @"\b(paid|spent|gave|for|on|by|at|in|kharcha|rupaye|bill)\b", "", RegexOptions.IgnoreCase);
            cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim(' ', '-', ':', ',');

            result.Title = string.IsNullOrWhiteSpace(cleanText) ? "Shared Group Expense" : cleanText;
            // Capitalize title
            if (result.Title.Length > 1)
            {
                result.Title = char.ToUpper(result.Title[0]) + result.Title.Substring(1);
            }

            // 4. Categorization heuristics
            var lower = trimmed.ToLowerInvariant();
            if (lower.Contains("dinner") || lower.Contains("lunch") || lower.Contains("food") || lower.Contains("pizza") || lower.Contains("cafe") || lower.Contains("swiggy") || lower.Contains("zomato") || lower.Contains("shack") || lower.Contains("burger"))
            {
                result.CategoryName = "Food & Dining";
            }
            else if (lower.Contains("cab") || lower.Contains("uber") || lower.Contains("ola") || lower.Contains("petrol") || lower.Contains("diesel") || lower.Contains("scooter") || lower.Contains("flight") || lower.Contains("train") || lower.Contains("fuel") || lower.Contains("travel"))
            {
                result.CategoryName = "Travel & Transport";
            }
            else if (lower.Contains("grocery") || lower.Contains("dmart") || lower.Contains("milk") || lower.Contains("blinkit") || lower.Contains("zepto") || lower.Contains("ration") || lower.Contains("sabzi"))
            {
                result.CategoryName = "Groceries & Supplies";
            }
            else if (lower.Contains("rent") || lower.Contains("villa") || lower.Contains("hotel") || lower.Contains("room") || lower.Contains("resort") || lower.Contains("airbnb") || lower.Contains("stay"))
            {
                result.CategoryName = "Rent & Stay";
            }
            else if (lower.Contains("wifi") || lower.Contains("electricity") || lower.Contains("maid") || lower.Contains("bill") || lower.Contains("cylinder"))
            {
                result.CategoryName = "Utilities & Bills";
            }
            else if (lower.Contains("movie") || lower.Contains("scuba") || lower.Contains("game") || lower.Contains("party") || lower.Contains("club") || lower.Contains("drinks") || lower.Contains("beer"))
            {
                result.CategoryName = "Entertainment & Movies";
            }

            result.Explanation = $"Extracted: {result.PayerName} paid ₹{result.Amount:N2} for '{result.Title}' under {result.CategoryName}.";
            return result;
        }

        public string GenerateDebtReminderText(string debtorName, string creditorName, decimal amount, string? creditorUpi, string groupName, string appUrl)
        {
            var upiNote = !string.IsNullOrWhiteSpace(creditorUpi) 
                ? $"📲 *Pay via UPI:* `{creditorUpi}`\n" 
                : "";

            return $"👋 *Namaste {debtorName}!*\n\n" +
                   $"MeraKharcha par *{groupName}* ka hisaab settle karna hai.\n" +
                   $"Aapka total *₹{amount:N2}* pending hai jo *{creditorName}* ko dena hai.\n\n" +
                   $"{upiNote}" +
                   $"🔗 *View details & settle on MeraKharcha:*\n{appUrl}\n\n" +
                   $"_A friendly automated reminder sent via MeraKharcha_ ✨";
        }
    }
}
