
namespace User.Application.UserProfiles.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            //RuleFor(v => v.Email)
            //    .NotEmpty().WithMessage("商品名称不能为空")
            //    .MaximumLength(200).WithMessage("商品名称不能超过200个字符");
        }
    }
}
