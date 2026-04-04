using User.Domain.ExternalInterfaces.TranslationInterface.Dtos;

namespace User.Domain.ExternalInterfaces.TranslationInterface
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(TranslationRequestDto request);
    }
}
