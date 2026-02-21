using System;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Text;

namespace Amplify.Domain.Entities.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; set; }

        /// <summary>
        /// User's stated starting capital. Editable at any time.
        /// Used to calculate available cash, position sizing, and portfolio metrics.
        /// </summary>
        public decimal StartingCapital { get; set; } = 100_000m;

        /// <summary>
        /// Percentage of cash available allocated to AI auto-trading (0-100).
        /// 0 = simulation only (default), any value > 0 = AI can create real positions.
        /// </summary>
        public decimal AiTradingBudgetPercent { get; set; } = 0m;
    }

}