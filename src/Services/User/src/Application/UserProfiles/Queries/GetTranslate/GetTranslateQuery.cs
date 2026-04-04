using Microsoft.Extensions.DependencyInjection;
using User.Domain.ExternalInterfaces.TranslationInterface;
using User.Domain.ExternalInterfaces.TranslationInterface.Dtos;

namespace User.Application.UserProfiles.Queries.GetTranslate
{
    public record GetTranslateQuery : IRequest<string>
    {
        public string Text { get; init; } = string.Empty;
        public string From { get; init; } = "auto";
        public string To { get; init; } = "zh";
    }

    public class GetTranslateQueryHandler : IRequestHandler<GetTranslateQuery, string>
    {
        private readonly IServiceProvider _serviceProvider;

        public GetTranslateQueryHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<string> Handle(GetTranslateQuery request, CancellationToken cancellationToken)
        {
            var service = _serviceProvider.GetRequiredKeyedService<ITranslationService>(TranslationSource.Baidu);

            return await service.TranslateAsync(
                new TranslationRequestDto
                {
                    Text = request.Text,
                    From = request.From,
                    To = request.To
                });
        }
    }
}
