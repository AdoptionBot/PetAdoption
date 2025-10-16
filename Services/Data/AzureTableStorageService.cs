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
        private readonly string _connectionString;
        private readonly Dictionary<string, TableClient> _tableClients;

        /// <summary>
        /// Constructor that uses KeyVaultSecretService to retrieve connection string
        /// </summary>
        public AzureTableStorageService(KeyVaultSecretService keyVaultService)
        {
            if (keyVaultService == null)
            {
                throw new ArgumentNullException(nameof(keyVaultService));
            }

            // Retrieve the connection string from Key Vault using the service
            _connectionString = keyVaultService.GetTableStorageConnectionStringAsync()
                .GetAwaiter()
                .GetResult();

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
        /// Gets or creates a TableClient for the specified table name
        /// </summary>
        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
            }

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
        /// Adds an entity to the specified table
        /// </summary>
        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            var tableClient = await GetTableClientAsync(tableName);
            await tableClient.AddEntityAsync(entity);
        }

        /// <summary>
        /// Updates an entity in the specified table
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
        /// Gets an entity by partition key and row key
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
                // Entity not found
                return null;
            }
        }

        /// <summary>
        /// Queries entities from the specified table using a filter expression
        /// </summary>
        public async Task<IEnumerable<T>> QueryEntitiesAsync<T>(string tableName, string? filter = null) where T : class, ITableEntity, new()
        {
            var tableClient = await GetTableClientAsync(tableName);

            var results = new List<T>();

            AsyncPageable<T> queryResults;
            if (string.IsNullOrEmpty(filter))
            {
                queryResults = tableClient.QueryAsync<T>();
            }
            else
            {
                queryResults = tableClient.QueryAsync<T>(filter: filter);
            }

            await foreach (var entity in queryResults)
            {
                results.Add(entity);
            }

            return results;
        }

        /// <summary>
        /// Queries entities from a table by partition key
        /// </summary>
        public async Task<IEnumerable<T>> QueryByPartitionKeyAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
        {
            string filter = $"PartitionKey eq '{partitionKey}'";
            return await QueryEntitiesAsync<T>(tableName, filter);
        }

        /// <summary>
        /// Gets a list of all tables in the storage account
        /// </summary>
        public async Task<IEnumerable<string>> GetTablesAsync()
        {
            var serviceClient = new TableServiceClient(_connectionString);
            var tables = new List<string>();

            await foreach (var table in serviceClient.QueryAsync())
            {
                tables.Add(table.Name);
            }

            return tables;
        }

        /// <summary>
        /// Creates a new table if it doesn't exist
        /// </summary>
        public async Task CreateTableAsync(string tableName)
        {
            var serviceClient = new TableServiceClient(_connectionString);
            await serviceClient.CreateTableIfNotExistsAsync(tableName);
        }

        /// <summary>
        /// Deletes a table if it exists
        /// </summary>
        public async Task DeleteTableAsync(string tableName)
        {
            var serviceClient = new TableServiceClient(_connectionString);
            await serviceClient.DeleteTableAsync(tableName);
        }
    }
}