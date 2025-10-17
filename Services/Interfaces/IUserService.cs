using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;

namespace PetAdoption.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByNameAsync(string userName);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(string userName, string email);
        Task<bool> UserExistsAsync(string email);
        Task<bool> IsUserAdminAsync(string email);
        Task UpdateUserRoleAsync(string userName, string email, UserRole newRole);
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<bool> IsProfileCompleteAsync(string email);
    }
}
