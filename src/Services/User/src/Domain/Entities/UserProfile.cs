using User.Domain.Common;
using User.Domain.ValueObjects;

namespace User.Domain.Entities
{
    public class UserProfile : BaseAuditableEntity<int>
    {
        public string Name { get; set; } = null!;

        public Email? Email { get; set; }

        public string? PhoneNum { get; set; }

        public string Password { get; set; } = null!;
    }
}
