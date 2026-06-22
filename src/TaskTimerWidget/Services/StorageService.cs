using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaskTimerWidget.Models;
using Serilog;

namespace TaskTimerWidget.Services
{
    /// <summary>
    /// Implementation of storage service using local JSON files.
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly string _storageDirectory;
        private readonly string _tasksFilePath;
        private readonly object _lockObject = new();

        public StorageService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Initialize storage directory (fork uses its own folder to avoid clashing with
            // the original Task Timer Widget's data).
            _storageDirectory = Path.Combine(localAppData, "TaskTimerWidgetR", "Data");
            _tasksFilePath = Path.Combine(_storageDirectory, "tasks.json");

            // Ensure directory exists
            EnsureStorageDirectoryExists();

            // One-time import of existing data from the original app's folder, so users who
            // started on the upstream app keep their history on first run of the fork.
            MigrateLegacyDataIfNeeded(localAppData);
        }

        /// <summary>
        /// Copies the original app's tasks.json into the fork's data folder on first run,
        /// only when the fork has no data yet. The original file is left untouched.
        /// </summary>
        private void MigrateLegacyDataIfNeeded(string localAppData)
        {
            try
            {
                if (File.Exists(_tasksFilePath))
                {
                    return;
                }

                var legacyPath = Path.Combine(localAppData, "TaskTimerWidget", "Data", "tasks.json");
                if (File.Exists(legacyPath))
                {
                    File.Copy(legacyPath, _tasksFilePath, overwrite: false);
                    Log.Information("Imported existing data from original app: {LegacyPath}", legacyPath);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not import legacy data from original app folder");
            }
        }

        public System.Threading.Tasks.Task<TaskStoreData> LoadStoreAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    if (!File.Exists(_tasksFilePath))
                    {
                        Log.Information("No existing tasks file found, returning empty store");
                        return Task.FromResult(new TaskStoreData());
                    }

                    var json = File.ReadAllText(_tasksFilePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return Task.FromResult(new TaskStoreData());
                    }

                    var token = JToken.Parse(json);

                    // Schema v1 (legacy): a plain array of TaskItem. Treat as today's tasks.
                    if (token.Type == JTokenType.Array)
                    {
                        var legacyTasks = token.ToObject<List<TaskItem>>() ?? new List<TaskItem>();
                        var todayKey = Helpers.WorkdayClock.GetDayKey(DateTime.Now);
                        var embedded = new Dictionary<string, List<TaskItem>> { [todayKey] = legacyTasks };
                        var migrated = Normalize(embedded);
                        Log.Information("Migrated legacy v1 list ({TaskCount} tasks) into normalized store on {DayKey}", legacyTasks.Count, todayKey);
                        return Task.FromResult(migrated);
                    }

                    // Schema v3 (current normalized): has a top-level "tasks" map.
                    if (token.Type == JTokenType.Object && token["tasks"] != null)
                    {
                        var storeData = token.ToObject<TaskStoreData>() ?? new TaskStoreData();
                        storeData.Tasks ??= new Dictionary<string, TaskDefinition>();
                        storeData.Days ??= new Dictionary<string, List<DayTaskEntry>>();
                        Log.Information("Loaded normalized store: {TaskCount} tasks across {DayCount} days from {Path}", storeData.Tasks.Count, storeData.Days.Count, _tasksFilePath);
                        return Task.FromResult(storeData);
                    }

                    // Schema v2 (denormalized): days holding full TaskItem snapshots. Normalize.
                    var daysToken = token["days"];
                    var embeddedDays = daysToken?.ToObject<Dictionary<string, List<TaskItem>>>()
                                       ?? new Dictionary<string, List<TaskItem>>();
                    var normalized = Normalize(embeddedDays);
                    Log.Information("Migrated v2 store ({DayCount} days) into normalized schema with {TaskCount} task identities", embeddedDays.Count, normalized.Tasks.Count);
                    return Task.FromResult(normalized);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading store from storage");
                return Task.FromResult(new TaskStoreData());
            }
        }

        /// <summary>
        /// Converts a denormalized day->TaskItem-list map into the normalized store, extracting
        /// distinct task identities (by id) into the top-level tasks collection and replacing each
        /// day's tasks with lightweight entries that reference them by id. All days are preserved.
        /// </summary>
        private static TaskStoreData Normalize(Dictionary<string, List<TaskItem>> embeddedDays)
        {
            var store = new TaskStoreData { SchemaVersion = 3 };

            foreach (var (dayKey, tasks) in embeddedDays)
            {
                var entries = new List<DayTaskEntry>();
                foreach (var task in tasks ?? new List<TaskItem>())
                {
                    var id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id;
                    var key = id.ToString();

                    if (!store.Tasks.TryGetValue(key, out var def))
                    {
                        def = new TaskDefinition
                        {
                            Id = id,
                            Name = task.Name,
                            CreatedAt = task.CreatedAt == default ? DateTime.UtcNow : task.CreatedAt,
                            ModifiedAt = task.ModifiedAt == default ? DateTime.UtcNow : task.ModifiedAt
                        };
                        store.Tasks[key] = def;
                    }
                    else
                    {
                        // Keep earliest creation and the most recently modified name.
                        if (task.CreatedAt != default && task.CreatedAt < def.CreatedAt)
                        {
                            def.CreatedAt = task.CreatedAt;
                        }
                        if (task.ModifiedAt >= def.ModifiedAt && !string.IsNullOrWhiteSpace(task.Name))
                        {
                            def.Name = task.Name;
                            def.ModifiedAt = task.ModifiedAt;
                        }
                    }

                    entries.Add(new DayTaskEntry
                    {
                        TaskId = id,
                        ElapsedSeconds = task.ElapsedSeconds,
                        IsRunning = task.IsRunning,
                        IsDone = task.IsDone,
                        Order = task.Order,
                        ModifiedAt = task.ModifiedAt == default ? DateTime.UtcNow : task.ModifiedAt
                    });
                }

                store.Days[dayKey] = entries;
            }

            return store;
        }

        public System.Threading.Tasks.Task SaveStoreAsync(TaskStoreData storeData)
        {
            if (storeData == null)
            {
                throw new ArgumentNullException(nameof(storeData));
            }

            try
            {
                lock (_lockObject)
                {
                    var json = JsonConvert.SerializeObject(storeData, Formatting.Indented);
                    File.WriteAllText(_tasksFilePath, json);
                    Log.Debug("Saved day store with {DayCount} days to {Path}", storeData.Days.Count, _tasksFilePath);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving day store to storage");
                throw;
            }
        }

        /// <summary>
        /// Ensures the storage directory exists.
        /// </summary>
        private void EnsureStorageDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_storageDirectory))
                {
                    Directory.CreateDirectory(_storageDirectory);
                    Log.Information($"Created storage directory: {_storageDirectory}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error creating storage directory: {_storageDirectory}");
                throw;
            }
        }
    }
}
