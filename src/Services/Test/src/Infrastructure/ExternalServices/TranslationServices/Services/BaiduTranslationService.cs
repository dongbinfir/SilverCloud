using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using User.Domain.ExternalInterfaces.TranslationInterface;
using User.Domain.ExternalInterfaces.TranslationInterface.Dtos;

namespace User.Infrastructure.ExternalServices.TranslationServices.Services
{
    public class BaiduTranslationService: ITranslationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<TranslationOptions> _options;

        public BaiduTranslationService(IHttpClientFactory httpClientFactory, IOptionsMonitor<TranslationOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
        }

        public async Task<string> TranslateAsync(TranslationRequestDto request)
        {
            var _client = _httpClientFactory.CreateClient(nameof(BaiduTranslationService));

            var salt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var sign = Md5Lower($"{_options.CurrentValue.AppId}{request.Text}{salt}{_options.CurrentValue.SecretKey}");

            var url =
                $"{_options.CurrentValue.BaseUrl}/api/trans/vip/translate" +
                "?q=" + Uri.EscapeDataString(request.Text) +
                "&from=" + Uri.EscapeDataString(request.From) +
                "&to=" + Uri.EscapeDataString(request.To) +
                "&appid=" + Uri.EscapeDataString(_options.CurrentValue.AppId) +
                "&salt=" + Uri.EscapeDataString(salt) +
                "&sign=" + Uri.EscapeDataString(sign);

            using var response = await _client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private static string Md5Lower(string value)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
