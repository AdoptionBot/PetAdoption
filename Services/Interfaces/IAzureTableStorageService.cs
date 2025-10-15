using Azure;
using Azure.Data.Tables;

namespace Services.Interfaces
{
    /// <summary>
    /// Interface for Azure Table Storage service
    /// </summary>
    public interface IAzureTableStorageService
    {
        Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity;
        Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity;
        Task UpdateEntityAsync<T>(string tableName, T entity, ETag etag) where T : class, ITableEntity;
        Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);
        Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity;
        Task<IEnumerable<T>> QueryEntitiesAsync<T>(string tableName, string? filter = null) where T : class, ITableEntity, new();
        Task<IEnumerable<T>> QueryByPartitionKeyAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new();
        Task<IEnumerable<string>> GetTablesAsync();
        Task CreateTableAsync(string tableName);
        Task DeleteTableAsync(string tableName);
    }
}
