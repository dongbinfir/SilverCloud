
using User.Domain.ValueObjects;

namespace User.Application.UserProfiles.Dtos
{
    public class UserProfileBriefDto : BaseAuditableEntity<int>, IMapFrom<UserProfile>
    {
        public string Name { get; set; } = null!;

        public Email? Email { get; set; }

        public string? PhoneNum { get; set; }

        public string Password { get; set; } = null!;

        //private class Mapping : UserProfile
        //{
        //    public Mapping(Profile profile)
        //    {
        //        profile.CreateMap<UserProfile, UserProfileBriefDto>();
        //    }
        //}
    }
}
