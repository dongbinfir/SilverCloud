namespace User.Application.Common.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<UserRefreshToken?> GetByTokenAsync(string token);
        Task<List<UserRefreshToken>> GetActiveTokensByUserIdAsync(int userId);
        Task AddAsync(UserRefreshToken refreshToken);
        Task UpdateAsync(UserRefreshToken refreshToken);
        Task DeleteAsync(string token);
        Task DeleteAllByUserIdAsync(int userId);
    }
}
