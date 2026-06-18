using TaskTimerWidget.Models;

namespace TaskTimerWidget.Services
{
    /// <summary>
    /// Service for managing tasks and their lifecycle.
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Initializes service state from persisted storage.
        /// </summary>
        System.Threading.Tasks.Task InitializeAsync();

        /// <summary>
        /// Gets the current workday key using the 4 AM day boundary.
        /// </summary>
        string GetTodayDayKey();

        /// <summary>
        /// Gets all existing day keys in storage.
        /// </summary>
        System.Threading.Tasks.Task<IReadOnlyList<string>> GetDayKeysAsync();

        /// <summary>
        /// Gets tasks for a day. If ensureDayExists is true and day is missing,
        /// the day is created by copying from today's tasks and resetting time.
        /// </summary>
        System.Threading.Tasks.Task<IReadOnlyList<TaskItem>> GetTasksForDayAsync(string dayKey, bool ensureDayExists);

        /// <summary>
        /// Creates a new task on the target day.
        /// </summary>
        System.Threading.Tasks.Task<TaskItem> CreateTaskAsync(string dayKey, string name);

        /// <summary>
        /// Updates an existing task on a specific day.
        /// </summary>
        System.Threading.Tasks.Task UpdateTaskAsync(string dayKey, TaskItem task);

        /// <summary>
        /// Renames a task identity across all days.
        /// </summary>
        System.Threading.Tasks.Task RenameTaskAsync(Guid taskId, string newName);

        /// <summary>
        /// Stops all running tasks on a day.
        /// </summary>
        System.Threading.Tasks.Task StopRunningTasksAsync(string dayKey);

        /// <summary>
        /// Adds or reactivates a task on a target day.
        /// If it already exists, it is marked as not done and time is kept.
        /// If missing, it is added with zero elapsed time.
        /// </summary>
        System.Threading.Tasks.Task AddOrReactivateTaskOnDayAsync(Guid taskId, string targetDayKey);

        /// <summary>
        /// Marks a task done/not done on a day.
        /// </summary>
        System.Threading.Tasks.Task SetTaskDoneAsync(string dayKey, Guid taskId, bool isDone);

        /// <summary>
        /// Counts future days where the task exists and is not done.
        /// </summary>
        System.Threading.Tasks.Task<int> CountFutureUndoneDaysAsync(string dayKey, Guid taskId);

        /// <summary>
        /// Marks future days done for a task and optionally deletes trivial entries.
        /// </summary>
        System.Threading.Tasks.Task MarkFutureDaysDoneAsync(string dayKey, Guid taskId, bool deleteLowTimeEntries);

        /// <summary>
        /// Deletes task from the selected day only.
        /// </summary>
        System.Threading.Tasks.Task DeleteTaskForDayAsync(string dayKey, Guid taskId);

        /// <summary>
        /// Deletes task from all days permanently.
        /// </summary>
        System.Threading.Tasks.Task DeleteTaskForeverAsync(Guid taskId);

        /// <summary>
        /// Reorders tasks on a day according to the provided task IDs.
        /// </summary>
        System.Threading.Tasks.Task ReorderTasksAsync(string dayKey, IReadOnlyList<Guid> orderedTaskIds);

        /// <summary>
        /// Gets aggregated stats for a task identity.
        /// </summary>
        System.Threading.Tasks.Task<TaskStatistics?> GetTaskStatisticsAsync(Guid taskId);
    }
}
