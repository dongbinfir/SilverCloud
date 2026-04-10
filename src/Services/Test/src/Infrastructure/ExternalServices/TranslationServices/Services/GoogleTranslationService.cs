using System.Security.Cryptography;
using System.Text;
using User.Domain.ExternalInterfaces.TranslationInterface;
using User.Domain.ExternalInterfaces.TranslationInterface.Dtos;

namespace User.Infrastructure.ExternalServices.TranslationServices.Services
{
    public class GoogleTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;

        public GoogleTranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> TranslateAsync(TranslationRequestDto request)
        {
            var salt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var sign = Md5Lower($"20260402002585762{request.Text}{salt}9YPqLnftvpcvDnSEAdJA");

            var url =
                "https://fanyi-api.baidu.com/api/trans/vip/translate" +
                "?q=" + Uri.EscapeDataString(request.Text) +
                "&from=" + Uri.EscapeDataString(request.From) +
                "&to=" + Uri.EscapeDataString(request.To) +
                "&appid=" + Uri.EscapeDataString("20260402002585762") +
                "&salt=" + Uri.EscapeDataString(salt) +
                "&sign=" + Uri.EscapeDataString(sign);

            using var response = await _httpClient.GetAsync(url);

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
