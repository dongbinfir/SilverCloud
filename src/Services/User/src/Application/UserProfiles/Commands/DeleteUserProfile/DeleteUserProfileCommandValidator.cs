namespace User.Application.UserProfiles.Commands.DeleteUserProfile
{
    public class DeleteUserProfileCommandValidator : AbstractValidator<DeleteUserProfileCommand>
    {
        public DeleteUserProfileCommandValidator()
        {
            //RuleFor(v => v.Id)
            //    .m(200).WithMessage("商品名称不能超过200个字符");
        }
    }
}
