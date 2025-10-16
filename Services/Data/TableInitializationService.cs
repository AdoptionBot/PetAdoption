using Azure;
using Azure.Data.Tables;
using PetAdoption.Data.TableStorage.SchemaUtilities;
using Microsoft.Extensions.Logging;
using PetAdoption.Services.Interfaces;

namespace PetAdoption.Services.Data
{
    /// <summary>
    /// Service for initializing and managing Azure Table Storage tables
    /// </summary>
    public class TableInitializationService(
        IAzureTableStorageService storageService,
        ILogger<TableInitializationService> logger) : ITableInitializationService
    {
        private readonly IAzureTableStorageService _storageService = storageService;
        private readonly ILogger<TableInitializationService> _logger = logger;

        /// <summary>
        /// Initializes all tables defined in the Data.TableStorage namespace
        /// </summary>
        public async Task InitializeTablesAsync(bool forceRecreate = false)
        {
            try
            {
                _logger.LogInformation("Starting table initialization process...");

                // Get all entity types from the Data assembly
                var entityTypes = TableSchemaManager.GetTableEntityTypes();
                _logger.LogInformation("Found {EntityCount} table entity types to process", entityTypes.Count);

                // Get existing tables from Azure
                var existingTables = (await _storageService.GetTablesAsync()).ToHashSet();

                foreach (var (tableName, entityType) in entityTypes)
                {
                    _logger.LogInformation("Processing table: {TableName}", tableName);

                    bool tableExists = existingTables.Contains(tableName);

                    if (tableExists)
                    {
                        _logger.LogInformation("Table {TableName} exists in Azure", tableName);

                        if (forceRecreate)
                        {
                            _logger.LogWarning("Force recreate enabled. Deleting table {TableName}...", tableName);
                            await DeleteAndRecreateTableAsync(tableName, entityType);
                        }
                        else
                        {
                            // Validate schema only if table has data
                            bool schemaValid = await ValidateTableSchemaAsync(tableName, entityType);

                            if (!schemaValid)
                            {
                                _logger.LogWarning("Schema mismatch detected for table {TableName}. Recreating...", tableName);
                                await DeleteAndRecreateTableAsync(tableName, entityType);
                            }
                            else
                            {
                                _logger.LogInformation("Table {TableName} schema is valid (or empty)", tableName);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Table {TableName} does not exist. Creating...", tableName);
                        await _storageService.CreateTableAsync(tableName);
                        _logger.LogInformation("Table {TableName} created successfully", tableName);
                    }
                }

                _logger.LogInformation("Table initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during table initialization");
                throw;
            }
        }

        /// <summary>
        /// Validates that a table's schema matches the entity type definition.
        /// Note: Azure Table Storage is schema-less. This validation only checks existing entities.
        /// Empty tables are considered valid since they have no schema until data is inserted.
        /// </summary>
        private async Task<bool> ValidateTableSchemaAsync(string tableName, Type entityType)
        {
            try
            {
                _logger.LogInformation("Validating schema for table {tableName}...", tableName);

                // Get expected properties from entity type definition
                var expectedProperties = TableSchemaManager.GetPropertyNames(entityType);
                _logger.LogDebug("Expected properties for table {tableName}: {expectedProperties}", tableName, string.Join(", ", expectedProperties));

                // Try to get entities from the table to sample the schema
                var entities = await _storageService.QueryEntitiesAsync<TableEntity>(tableName);
                var entityList = entities.Take(10).ToList(); // Sample first 10 entities

                if (!entityList.Any())
                {
                    // Empty table - cannot validate schema, but that's okay
                    // Azure Table Storage is schema-less, properties appear when data is inserted
                    _logger.LogInformation("Table {tableName} is empty. Schema will be defined when entities are inserted.", tableName);
                    return true;
                }

                _logger.LogInformation("Sampling {entityCount} entities from table {tableName} for schema validation", entityList.Count, tableName);

                // Check if ANY entity in the sample has missing properties
                bool allEntitiesValid = true;
                var allActualProperties = new HashSet<string>();

                foreach (var entity in entityList)
                {
                    var actualProperties = entity.Keys.ToHashSet();
                    allActualProperties.UnionWith(actualProperties);

                    // Check if this entity is missing any expected properties
                    var missingProperties = expectedProperties.Except(actualProperties).ToList();

                    if (missingProperties.Any())
                    {
                        _logger.LogWarning("Entity with PartitionKey='{PartitionKey}', RowKey='{RowKey}' is missing properties:", entity.PartitionKey, entity.RowKey);
                        _logger.LogWarning("  Missing: {MissingProperties}", string.Join(", ", missingProperties));
                        allEntitiesValid = false;
                    }
                }

                // Check for extra properties across all sampled entities
                var extraProperties = allActualProperties.Except(expectedProperties).ToList();

                if (extraProperties.Any())
                {
                    _logger.LogWarning("Found extra properties in table {tableName} not defined in entity class:", tableName);
                    _logger.LogWarning("  Extra properties: {ExtraProperties}", string.Join(", ", extraProperties));
                    _logger.LogInformation("This may indicate old schema or data from a previous version.");
                    // Extra properties alone don't invalidate the schema, but log them
                }

                if (!allEntitiesValid)
                {
                    _logger.LogWarning("Schema validation failed for table {tableName} - entities have incomplete properties", tableName);
                    return false;
                }

                _logger.LogInformation("Schema validation passed for table {tableName}", tableName);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Table {tableName} not found during validation", tableName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating schema for table {tableName}", tableName);
                // In case of error, assume schema is valid to avoid unnecessary recreation
                return true;
            }
        }

        /// <summary>
        /// Deletes and recreates a table
        /// </summary>
        private async Task DeleteAndRecreateTableAsync(string tableName, Type entityType)
        {
            try
            {
                _logger.LogInformation("Deleting table {tableName}...", tableName);
                await _storageService.DeleteTableAsync(tableName);

                // Wait a bit for Azure to process the deletion
                _logger.LogInformation("Waiting for table deletion to complete...");
                await Task.Delay(TimeSpan.FromSeconds(10)); // Increased wait time

                _logger.LogInformation("Creating table {tableName}...", tableName);
                await _storageService.CreateTableAsync(tableName);

                _logger.LogInformation("Table {tableName} recreated successfully", tableName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error recreating table {tableName}", tableName);
                throw;
            }
        }

        /// <summary>
        /// Gets initialization status for all tables
        /// </summary>
        public async Task<Dictionary<string, TableStatus>> GetTableStatusAsync()
        {
            var result = new Dictionary<string, TableStatus>();
            var entityTypes = TableSchemaManager.GetTableEntityTypes();
            var existingTables = (await _storageService.GetTablesAsync()).ToHashSet();

            foreach (var (tableName, entityType) in entityTypes)
            {
                var status = new TableStatus
                {
                    TableName = tableName,
                    EntityType = entityType.Name,
                    Exists = existingTables.Contains(tableName),
                    Schema = TableSchemaManager.GetSchemaDescription(entityType)
                };

                if (status.Exists)
                {
                    try
                    {
                        var entities = await _storageService.QueryEntitiesAsync<TableEntity>(tableName);
                        var entityList = entities.ToList();
                        status.EntityCount = entityList.Count;

                        if (entityList.Count != 0)
                        {
                            status.SchemaValid = await ValidateTableSchemaAsync(tableName, entityType);
                        }
                        else
                        {
                            status.SchemaValid = true; // Empty tables are valid
                        }
                    }
                    catch
                    {
                        status.EntityCount = -1;
                        status.SchemaValid = false;
                    }
                }

                result[tableName] = status;
            }

            return result;
        }
    }

    /// <summary>
    /// Represents the status of a table
    /// </summary>
    public class TableStatus
    {
        public string TableName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public bool SchemaValid { get; set; }
        public int EntityCount { get; set; }
        public string Schema { get; set; } = string.Empty;
    }
}