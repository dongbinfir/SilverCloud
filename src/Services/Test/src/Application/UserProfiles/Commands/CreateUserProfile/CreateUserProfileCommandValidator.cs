
namespace User.Application.UserProfiles.Commands.CreateUserProfile
{
    public class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
    {
        public CreateUserProfileCommandValidator()
        {
            RuleFor(v => v.Email)
                .MaximumLength(200).WithMessage("邮箱不能超过200个字符")
                .EmailAddress().When(v => !string.IsNullOrWhiteSpace(v.Email)).WithMessage("邮箱格式不正确");

            RuleFor(v => v.PhoneNum)
                .MaximumLength(20).WithMessage("电话号码不能超过20个字符")
                .Matches(@"^[0-9+\-\s()]*$").When(v => !string.IsNullOrWhiteSpace(v.PhoneNum)).WithMessage("电话号码格式不正确");

            RuleFor(v => v)
                .Must(v => !string.IsNullOrWhiteSpace(v.Email) || !string.IsNullOrWhiteSpace(v.PhoneNum))
                .WithMessage("邮箱和电话号码必须至少填写一个");
        }
    }
}
