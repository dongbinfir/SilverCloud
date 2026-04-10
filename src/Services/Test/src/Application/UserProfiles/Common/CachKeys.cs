namespace User.Application.UserProfiles.Common;

public static class CachKeys
{
    public static string UserProfileCacheKey(int id)
    {
        return $"UserProfile:{id}";
    }
}
