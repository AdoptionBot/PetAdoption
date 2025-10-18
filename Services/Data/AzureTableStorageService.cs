using Azure;
using Azure.Data.Tables;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for interacting with multiple Azure Table Storage tables
    /// </summary>
    public class AzureTableStorageService : IAzureTableStorageService
    {
        private readonly KeyVaultSecretService? _keyVaultService;
        private string? _connectionString;
        private readonly Dictionary<string, TableClient> _tableClients;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Constructor that uses KeyVaultSecretService to retrieve connection string
        /// </summary>
        public AzureTableStorageService(KeyVaultSecretService keyVaultService)
        {
            _keyVaultService = keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));
            _tableClients = new Dictionary<string, TableClient>();
        }

        /// <summary>
        /// Constructor for testing that takes a direct connection string
        /// </summary>
        public AzureTableStorageService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _tableClients = new Dictionary<string, TableClient>();
        }

        /// <summary>
        /// Ensures the connection string is initialized
        /// </summary>
        private async Task EnsureConnectionStringAsync()
        {
            if (_connectionString != null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_connectionString == null && _keyVaultService != null)
                {
                    _connectionString = await _keyVaultService.GetStorageConnectionStringAsync();
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Gets or creates a TableClient for the specified table name
        /// </summary>
        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
            }

            await EnsureConnectionStringAsync();

            if (!_tableClients.TryGetValue(tableName, out var tableClient))
            {
                // Create a table service client
                var serviceClient = new TableServiceClient(_connectionString);

                // Get a table client for the specified table and create it if it doesn't exist
                tableClient = serviceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();

                _tableClients[tableName] = tableClient;
            }

            return tableClient;
        }

        /// <summary>
        /// Adds or updates an entity in the specified table
        /// </summary>
        public async Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.UpsertEntityAsync(entity);
        }

        /// <summary>
        /// Adds a new entity to the specified table
        /// </summary>
        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.AddEntityAsync(entity);
        }

        /// <summary>
        /// Updates an existing entity in the specified table
        /// </summary>
        public async Task UpdateEntityAsync<T>(string tableName, T entity, ETag etag) where T : class, ITableEntity
        {
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.UpdateEntityAsync(entity, etag);
        }

        /// <summary>
        /// Deletes an entity from the specified table
        /// </summary>
        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        /// <summary>
        /// Retrieves a single entity from the specified table
        /// </summary>
        public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableClient = await GetTableClientAsync(tableName);

            try
            {
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        /// <summary>
        /// Queries entities from the specified table with optional filtering
        /// </summary>
        public async Task<IEnumerable<T>> QueryEntitiesAsync<T>(string tableName, string? filter = null) where T : class, ITableEntity, new()
        {
            var tableClient = await GetTableClientAsync(tableName);
            var entities = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>(filter))
            {
                entities.Add(entity);
            }

            return entities;
        }

        /// <summary>
        /// Queries entities by partition key
        /// </summary>
        public async Task<IEnumerable<T>> QueryByPartitionKeyAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
        {
            var filter = $"PartitionKey eq '{partitionKey}'";
            return await QueryEntitiesAsync<T>(tableName, filter);
        }

        /// <summary>
        /// Gets all table names in the storage account
        /// </summary>
        public async Task<IEnumerable<string>> GetTablesAsync()
        {
            await EnsureConnectionStringAsync();
            var serviceClient = new TableServiceClient(_connectionString);
            var tables = new List<string>();

            await foreach (var table in serviceClient.QueryAsync())
            {
                tables.Add(table.Name);
            }

            return tables;
        }

        /// <summary>
        /// Creates a new table
        /// </summary>
        public async Task CreateTableAsync(string tableName)
        {
            await EnsureConnectionStringAsync();
            var serviceClient = new TableServiceClient(_connectionString);
            await serviceClient.CreateTableIfNotExistsAsync(tableName);
        }

        /// <summary>
        /// Deletes a table
        /// </summary>
        public async Task DeleteTableAsync(string tableName)
        {
            await EnsureConnectionStringAsync();
            var serviceClient = new TableServiceClient(_connectionString);
            await serviceClient.DeleteTableAsync(tableName);
        }
    }
}