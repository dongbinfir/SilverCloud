namespace Identity.Application.AccountInfos.Commands.UpdateAccountInfo
{
    public class UpdateAccountInfoCommandValidator : AbstractValidator<UpdateAccountInfoCommand>
    {
        public UpdateAccountInfoCommandValidator()
        {
            //RuleFor(v => v.Email)
            //    .NotEmpty().WithMessage("商品名称不能为空")
            //    .MaximumLength(200).WithMessage("商品名称不能超过200个字符");
        }
    }
}
