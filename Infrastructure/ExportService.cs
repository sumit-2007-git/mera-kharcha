using System.Collections.Generic;
using System.Text;
using MeraKharcha.Models;

namespace MeraKharcha.Infrastructure
{
    public interface IExportService
    {
        byte[] GenerateExpensesCsv(IEnumerable<Expense> expenses);
        byte[] GenerateSettlementsCsv(IEnumerable<Settlement> settlements);
    }

    public class ExportService : IExportService
    {
        public byte[] GenerateExpensesCsv(IEnumerable<Expense> expenses)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Expense ID,Date,Title,Category,Amount (INR),Paid By,Split Type,Participants Count,Notes");

            foreach (var exp in expenses)
            {
                var title = EscapeCsv(exp.Title);
                var category = EscapeCsv(exp.Category?.Name ?? "Uncategorized");
                var paidBy = EscapeCsv(exp.PaidByUser?.FullName ?? "Unknown");
                var notes = EscapeCsv(exp.Notes ?? string.Empty);
                var date = exp.ExpenseDate.ToString("yyyy-MM-dd");

                sb.AppendLine($"{exp.Id},{date},{title},{category},{exp.Amount:F2},{paidBy},{exp.SplitType},{exp.Participants.Count},{notes}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] GenerateSettlementsCsv(IEnumerable<Settlement> settlements)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Settlement ID,Date,Payer,Receiver,Amount (INR),Status,Payment Method,Reference,Notes");

            foreach (var s in settlements)
            {
                var payer = EscapeCsv(s.PayerUser?.FullName ?? "Unknown");
                var receiver = EscapeCsv(s.ReceiverUser?.FullName ?? "Unknown");
                var notes = EscapeCsv(s.Notes ?? string.Empty);
                var method = EscapeCsv(s.PaymentMethod ?? "Cash");
                var refCode = EscapeCsv(s.TransactionReference ?? "-");
                var date = s.CreatedAt.ToString("yyyy-MM-dd");

                sb.AppendLine($"{s.Id},{date},{payer},{receiver},{s.Amount:F2},{s.Status},{method},{refCode},{notes}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return $"\"{field}\"";
        }
    }
}
