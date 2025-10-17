using Azure;
using PetAdoption.Data.TableStorage;
using PetAdoption.Data.TableStorage.Enums;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    public class UserService : IUserService
    {
        private readonly IAzureTableStorageService _tableStorageService;
        private const string TableName = "Users";

        public UserService(IAzureTableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var users = await _tableStorageService.QueryEntitiesAsync<User>(
                TableName, 
                $"RowKey eq '{email}'");
            return users.FirstOrDefault();
        }

        public async Task<User?> GetUserByNameAsync(string userName)
        {
            var users = await _tableStorageService.QueryByPartitionKeyAsync<User>(TableName, userName);
            return users.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _tableStorageService.QueryEntitiesAsync<User>(TableName);
        }

        public async Task CreateUserAsync(User user)
        {
            await _tableStorageService.AddEntityAsync(TableName, user);
        }

        public async Task UpdateUserAsync(User user)
        {
            await _tableStorageService.UpsertEntityAsync(TableName, user);
        }

        public async Task DeleteUserAsync(string userName, string email)
        {
            await _tableStorageService.DeleteEntityAsync(TableName, userName, email);
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            return user != null;
        }

        public async Task<bool> IsUserAdminAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            return user?.Role == UserRole.Admin;
        }

        public async Task UpdateUserRoleAsync(string userName, string email, UserRole newRole)
        {
            var user = await _tableStorageService.GetEntityAsync<User>(TableName, userName, email);
            if (user != null)
            {
                user.Role = newRole;
                await _tableStorageService.UpdateEntityAsync(TableName, user, user.ETag);
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            var allUsers = await GetAllUsersAsync();
            return allUsers.Where(u => u.Role == role);
        }

        public async Task<bool> IsProfileCompleteAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            return user?.IsProfileCompleted ?? false;
        }
    }
}