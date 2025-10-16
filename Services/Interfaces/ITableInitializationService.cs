using PetAdoption.Services.Data;

namespace PetAdoption.Services.Interfaces
{
    /// <summary>
    /// Interface for table initialization service
    /// </summary>
    public interface ITableInitializationService
    {
        /// <summary>
        /// Initializes all tables defined in the Data.TableStorage namespace
        /// </summary>
        /// <param name="forceRecreate">If true, deletes and recreates all tables</param>
        Task InitializeTablesAsync(bool forceRecreate = false);

        /// <summary>
        /// Gets the status of all tables
        /// </summary>
        Task<Dictionary<string, TableStatus>> GetTableStatusAsync();
    }
}