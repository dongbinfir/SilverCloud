
namespace User.Application.UserProfiles.Dtos
{
    public class UserProfileBriefDto : BaseEntity<int>, IMapFrom<UserProfile>
    {
        public string Email { get; set; } = null!;

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
