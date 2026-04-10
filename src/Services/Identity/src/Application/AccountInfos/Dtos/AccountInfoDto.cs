namespace Identity.Application.AccountInfos.Dtos
{
    public class AccountInfoDto : IMapFrom<AccountInfo>
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNum { get; set; }

        private class Mapping : AccountInfo
        {
            public Mapping(Profile profile)
            {
                profile.CreateMap<AccountInfo, AccountInfoDto>()
                    .ForMember(d => d.Email, opt => opt.MapFrom(a => a.Email == null ? null : a.Email.Value))
                    ;
            }
        }
    }
}
