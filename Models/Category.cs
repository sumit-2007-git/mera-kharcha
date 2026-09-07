using System;
using System.Collections.Generic;

namespace MeraKharcha.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-tag";
        public string ColorHex { get; set; } = "#3B82F6";
        public bool IsDefault { get; set; } = false;

        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }

    public enum SplitType
    {
        Equal = 1,
        ExactAmount = 2,
        Percentage = 3,
        Shares = 4
    }
}
