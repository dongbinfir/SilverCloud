using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Authorizations.Dtos
{
    public class LogoutRequestDto
    {
        [Required(ErrorMessage = "RefreshToken 不能为空")]
        public string RefreshToken { get; set; } = null!;
    }
}
