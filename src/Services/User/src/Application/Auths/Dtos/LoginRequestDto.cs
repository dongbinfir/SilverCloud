using System.ComponentModel.DataAnnotations;

namespace User.Application.Auths.Dtos
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "用户名或邮箱不能为空")]
        public string Identity { get; set; } = null!;

        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = null!;
    }
}
