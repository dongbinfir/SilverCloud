namespace User.Domain.ExternalInterfaces.TranslationInterface.Dtos
{
    public class TranslationRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;  
    }
}
