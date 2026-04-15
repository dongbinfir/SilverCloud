namespace User.Application.UserProfiles.Dtos
{
    public class UserProfileDto : IMapFrom<UserProfile>
    {
        public int Id { get; set; }
        public int AccountInfoId { get; set; }


        private class Mapping : UserProfile
        {
            public Mapping(Profile profile)
            {
                profile.CreateMap<UserProfile, UserProfileDto>()
                    ;
            }
        }
    }
}
