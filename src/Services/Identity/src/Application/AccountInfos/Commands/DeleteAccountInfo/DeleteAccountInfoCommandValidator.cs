namespace Identity.Application.AccountInfos.Commands.DeleteAccountInfo
{
    public class DeleteAccountInfoCommandValidator : AbstractValidator<DeleteAccountInfoCommand>
    {
        public DeleteAccountInfoCommandValidator()
        {
            //RuleFor(v => v.Id)
            //    .m(200).WithMessage("商品名称不能超过200个字符");
        }
    }
}
