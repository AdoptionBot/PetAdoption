using Azure.Data.Tables;
using System.Reflection;

namespace Data.TableStorage.SchemaUtilities
{
    /// <summary>
    /// Manages table schemas and validation for Azure Table Storage entities
    /// </summary>
    public class TableSchemaManager
    {
        /// <summary>
        /// Gets all table entity types defined in the Data assembly
        /// </summary>
        public static Dictionary<string, Type> GetTableEntityTypes()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var entityTypes = assembly.GetTypes()
                .Where(t => t.Namespace == "Data.TableStorage"
                    && typeof(ITableEntity).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract)
                .ToDictionary(t => GetTableName(t), t => t);

            return entityTypes;
        }

        /// <summary>
        /// Gets the table name for an entity type
        /// </summary>
        public static string GetTableName(Type entityType)
        {
            return entityType.Name;
        }

        /// <summary>
        /// Gets the table name for an entity instance
        /// </summary>
        public static string GetTableName<T>() where T : ITableEntity
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// Gets all property names for a table entity type
        /// </summary>
        public static HashSet<string> GetPropertyNames(Type entityType)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Select(p => p.Name)
                .ToHashSet();

            return properties;
        }

        /// <summary>
        /// Gets a human-readable schema description
        /// </summary>
        public static string GetSchemaDescription(Type entityType)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Select(p => $"{p.Name} ({p.PropertyType.Name})");

            return string.Join(", ", properties);
        }
    }
}