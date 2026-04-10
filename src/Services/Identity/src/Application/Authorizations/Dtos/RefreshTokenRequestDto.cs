using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Authorizations.Dtos
{
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "AccessToken 不能为空")]
        public string AccessToken { get; set; } = null!;

        [Required(ErrorMessage = "RefreshToken 不能为空")]
        public string RefreshToken { get; set; } = null!;
    }
}
