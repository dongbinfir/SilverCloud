namespace Identity.Application.AccountInfos.Commons;

public static class AccountInfoCacheKeys
{
    public static string AccountInfoCacheKey(int id)
    {
        return $"AccountInfo:{id}";
    }
}
