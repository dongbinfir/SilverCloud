
namespace User.Application.UserProfiles.Commands.CreateUserProfile
{
    public class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
    {
        public CreateUserProfileCommandValidator()
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("商品名称不能为空")
                .MaximumLength(200).WithMessage("商品名称不能超过200个字符");
        }
    }
}
