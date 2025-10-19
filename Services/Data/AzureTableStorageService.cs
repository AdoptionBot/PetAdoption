using Azure;
using Azure.Data.Tables;
using PetAdoption.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for interacting with Azure Table Storage
    /// Uses a shared TableServiceClient for optimal performance
    /// </summary>
    public class AzureTableStorageService : IAzureTableStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ConcurrentDictionary<string, TableClient> _tableClients;
        private readonly ILogger<AzureTableStorageService>? _logger;

        /// <summary>
        /// Constructor with direct connection string (recommended)
        /// </summary>
        public AzureTableStorageService(string connectionString, ILogger<AzureTableStorageService>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            }

            _logger = logger;
            _tableClients = new ConcurrentDictionary<string, TableClient>();

            try
            {
                _logger?.LogInformation("Creating TableServiceClient");
                _tableServiceClient = new TableServiceClient(connectionString);
                _logger?.LogInformation("TableServiceClient created successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Failed to create TableServiceClient");
                throw new InvalidOperationException($"Failed to initialize Azure Table Storage service: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets or creates a TableClient for the specified table
        /// </summary>
        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            }

            // Use concurrent dictionary for thread-safe caching
            return _tableClients.GetOrAdd(tableName, name =>
            {
                _logger?.LogDebug("Creating TableClient for table: {TableName}", name);
                var client = _tableServiceClient.GetTableClient(name);
                
                // Create table asynchronously (fire and forget for first access)
                _ = client.CreateIfNotExistsAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger?.LogWarning(t.Exception, "Failed to ensure table exists: {TableName}", name);
                    }
                }, TaskScheduler.Default);
                
                return client;
            });
        }

        /// <inheritdoc/>
        public async Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(entity);
            
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.UpsertEntityAsync(entity);
            
            _logger?.LogDebug("Upserted entity in {Table}: {PartitionKey}/{RowKey}", 
                tableName, entity.PartitionKey, entity.RowKey);
        }

        /// <inheritdoc/>
        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(entity);
            
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.AddEntityAsync(entity);
            
            _logger?.LogDebug("Added entity to {Table}: {PartitionKey}/{RowKey}", 
                tableName, entity.PartitionKey, entity.RowKey);
        }

        /// <inheritdoc/>
        public async Task UpdateEntityAsync<T>(string tableName, T entity, ETag etag) where T : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(entity);
            
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.UpdateEntityAsync(entity, etag);
            
            _logger?.LogDebug("Updated entity in {Table}: {PartitionKey}/{RowKey}", 
                tableName, entity.PartitionKey, entity.RowKey);
        }

        /// <inheritdoc/>
        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException("Partition key cannot be null or empty", nameof(partitionKey));
            }
            
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentException("Row key cannot be null or empty", nameof(rowKey));
            }

            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            
            _logger?.LogDebug("Deleted entity from {Table}: {PartitionKey}/{RowKey}", 
                tableName, partitionKey, rowKey);
        }

        /// <inheritdoc/>
        public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException("Partition key cannot be null or empty", nameof(partitionKey));
            }
            
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentException("Row key cannot be null or empty", nameof(rowKey));
            }

            var tableClient = await GetTableClientAsync(tableName);

            try
            {
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger?.LogDebug("Entity not found in {Table}: {PartitionKey}/{RowKey}", 
                    tableName, partitionKey, rowKey);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryEntitiesAsync<T>(string tableName, string? filter = null) where T : class, ITableEntity, new()
        {
            var tableClient = await GetTableClientAsync(tableName);
            var entities = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>(filter))
            {
                entities.Add(entity);
            }

            _logger?.LogDebug("Queried {Count} entities from {Table}", entities.Count, tableName);
            return entities;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryByPartitionKeyAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException("Partition key cannot be null or empty", nameof(partitionKey));
            }

            var filter = $"PartitionKey eq '{partitionKey}'";
            return await QueryEntitiesAsync<T>(tableName, filter);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetTablesAsync()
        {
            var tables = new List<string>();

            await foreach (var table in _tableServiceClient.QueryAsync())
            {
                tables.Add(table.Name);
            }

            return tables;
        }

        /// <inheritdoc/>
        public async Task CreateTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            }

            await _tableServiceClient.CreateTableIfNotExistsAsync(tableName);
            _logger?.LogInformation("Ensured table exists: {TableName}", tableName);
        }

        /// <inheritdoc/>
        public async Task DeleteTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            }

            await _tableServiceClient.DeleteTableAsync(tableName);
            _tableClients.TryRemove(tableName, out _);
            
            _logger?.LogWarning("Deleted table: {TableName}", tableName);
        }
    }
}