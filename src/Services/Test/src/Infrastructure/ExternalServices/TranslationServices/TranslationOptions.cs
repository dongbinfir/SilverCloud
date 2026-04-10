namespace User.Infrastructure.ExternalServices.TranslationServices
{
    public class TranslationOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }
}
