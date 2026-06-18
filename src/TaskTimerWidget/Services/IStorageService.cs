using TaskTimerWidget.Models;

namespace TaskTimerWidget.Services
{
    /// <summary>
    /// Service for persisting and loading tasks from storage.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Loads all day snapshots from storage.
        /// </summary>
        System.Threading.Tasks.Task<TaskStoreData> LoadStoreAsync();

        /// <summary>
        /// Saves the full day snapshot store.
        /// </summary>
        System.Threading.Tasks.Task SaveStoreAsync(TaskStoreData storeData);
    }
}
