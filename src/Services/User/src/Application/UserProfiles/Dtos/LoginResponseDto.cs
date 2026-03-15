using System;
using System.Collections.Generic;
using System.Text;

namespace User.Application.UserProfiles.Dtos
{
    public class LoginResponseDto
    {
        public UserProfileBriefDto User { get; set; } = null!;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
