namespace User.Application.UserProfiles.Dtos
{
    public class UserProfileBriefDto : IMapFrom<UserProfile>
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNum { get; set; }

        private class Mapping : UserProfile
        {
            public Mapping(Profile profile)
            {
                profile.CreateMap<UserProfile, UserProfileBriefDto>()
                    .ForMember(d => d.Email, opt => opt.MapFrom(a => a.Email == null ? null : a.Email.Value))
                    ;
            }
        }
    }
}
