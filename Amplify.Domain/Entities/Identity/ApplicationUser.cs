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
    }

}
