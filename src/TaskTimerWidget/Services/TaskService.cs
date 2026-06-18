using TaskTimerWidget.Helpers;
using TaskTimerWidget.Models;
using Serilog;

namespace TaskTimerWidget.Services
{
    /// <summary>
    /// Day-based task service with a 4 AM workday boundary.
    /// Persists a normalized store (task identities + per-day entries referencing them by id),
    /// but exposes composed <see cref="TaskItem"/> objects to callers.
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly IStorageService _storageService;
        private readonly object _lockObject = new();
        private TaskStoreData _store = new();

        public TaskService(IStorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                var loadedStore = await _storageService.LoadStoreAsync();
                lock (_lockObject)
                {
                    _store = loadedStore ?? new TaskStoreData();
                    _store.Tasks ??= new Dictionary<string, TaskDefinition>();
                    _store.Days ??= new Dictionary<string, List<DayTaskEntry>>();
                }

                // Persist once after load to lock in migration to the normalized schema.
                await PersistAsync();
                Log.Information("TaskService initialized with {TaskCount} tasks across {DayCount} days", _store.Tasks.Count, _store.Days.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing TaskService");
            }
        }

        public string GetTodayDayKey() => WorkdayClock.GetDayKey(DateTime.Now);

        public System.Threading.Tasks.Task<IReadOnlyList<string>> GetDayKeysAsync()
        {
            lock (_lockObject)
            {
                var keys = _store.Days.Keys
                    .OrderBy(WorkdayClock.ParseDayKey)
                    .ToList();
                return System.Threading.Tasks.Task.FromResult<IReadOnlyList<string>>(keys);
            }
        }

        public async System.Threading.Tasks.Task<IReadOnlyList<TaskItem>> GetTasksForDayAsync(string dayKey, bool ensureDayExists)
        {
            if (string.IsNullOrWhiteSpace(dayKey))
            {
                throw new ArgumentException("Day key is required", nameof(dayKey));
            }

            TaskStoreData? snapshotToPersist = null;
            List<TaskItem> result;

            lock (_lockObject)
            {
                if (ensureDayExists)
                {
                    if (EnsureDayExistsInternal(dayKey))
                    {
                        snapshotToPersist = CreateSnapshotLocked();
                    }
                }

                if (!_store.Days.TryGetValue(dayKey, out var dayEntries))
                {
                    result = new List<TaskItem>();
                }
                else
                {
                    result = dayEntries
                        .OrderBy(e => e.IsDone)
                        .ThenBy(e => e.Order)
                        .ThenBy(e => GetCreatedAtLocked(e.TaskId))
                        .Select(ComposeLocked)
                        .ToList();
                }
            }

            if (snapshotToPersist != null)
            {
                await _storageService.SaveStoreAsync(snapshotToPersist);
            }

            return result;
        }

        public async System.Threading.Tasks.Task<TaskItem> CreateTaskAsync(string dayKey, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Task name cannot be empty", nameof(name));
            }

            TaskItem composed;
            TaskStoreData snapshot;

            lock (_lockObject)
            {
                EnsureDayExistsInternal(dayKey);
                var dayEntries = _store.Days[dayKey];
                var nextOrder = dayEntries.Count == 0 ? 0 : dayEntries.Max(e => e.Order) + 1;

                var now = DateTime.UtcNow;
                var def = new TaskDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = name.Trim(),
                    CreatedAt = now,
                    ModifiedAt = now
                };
                _store.Tasks[def.Id.ToString()] = def;

                var entry = new DayTaskEntry
                {
                    TaskId = def.Id,
                    ElapsedSeconds = 0,
                    IsRunning = false,
                    IsDone = false,
                    Order = nextOrder,
                    ModifiedAt = now
                };
                dayEntries.Add(entry);

                composed = ComposeLocked(entry);
                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
            Log.Information("Task created on {DayKey}: {TaskId} - {Name}", dayKey, composed.Id, composed.Name);
            return composed;
        }

        public async System.Threading.Tasks.Task UpdateTaskAsync(string dayKey, TaskItem task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            TaskStoreData snapshot;
            lock (_lockObject)
            {
                EnsureDayExistsInternal(dayKey);
                var dayEntries = _store.Days[dayKey];
                var entry = dayEntries.FirstOrDefault(e => e.TaskId == task.Id);
                if (entry == null)
                {
                    return;
                }

                entry.ElapsedSeconds = Math.Max(0, task.ElapsedSeconds);
                entry.IsRunning = task.IsRunning;
                entry.IsDone = task.IsDone;
                entry.Order = task.Order;
                entry.ModifiedAt = DateTime.UtcNow;

                // Name is identity-level; keep the central definition in sync.
                if (_store.Tasks.TryGetValue(task.Id.ToString(), out var def) &&
                    !string.IsNullOrWhiteSpace(task.Name) && def.Name != task.Name)
                {
                    def.Name = task.Name;
                    def.ModifiedAt = DateTime.UtcNow;
                }

                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
        }

        public async System.Threading.Tasks.Task RenameTaskAsync(Guid taskId, string newName)
        {
            if (taskId == Guid.Empty)
            {
                throw new ArgumentException("Task id is required", nameof(taskId));
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Task name cannot be empty", nameof(newName));
            }

            TaskStoreData snapshot;
            lock (_lockObject)
            {
                if (_store.Tasks.TryGetValue(taskId.ToString(), out var def))
                {
                    def.Name = newName.Trim();
                    def.ModifiedAt = DateTime.UtcNow;
                }
                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
        }

        public async System.Threading.Tasks.Task StopRunningTasksAsync(string dayKey)
        {
            TaskStoreData? snapshot = null;

            lock (_lockObject)
            {
                if (!_store.Days.TryGetValue(dayKey, out var dayEntries))
                {
                    return;
                }

                var changed = false;
                foreach (var entry in dayEntries.Where(e => e.IsRunning))
                {
                    entry.IsRunning = false;
                    entry.ModifiedAt = DateTime.UtcNow;
                    changed = true;
                }

                if (changed)
                {
                    snapshot = CreateSnapshotLocked();
                }
            }

            if (snapshot != null)
            {
                await _storageService.SaveStoreAsync(snapshot);
            }
        }

        public async System.Threading.Tasks.Task AddOrReactivateTaskOnDayAsync(Guid taskId, string targetDayKey)
        {
            if (taskId == Guid.Empty)
            {
                throw new ArgumentException("Task id is required", nameof(taskId));
            }

            TaskStoreData snapshot;

            lock (_lockObject)
            {
                if (!_store.Tasks.ContainsKey(taskId.ToString()))
                {
                    throw new InvalidOperationException("Could not find source task for Add to today.");
                }

                EnsureDayExistsInternal(targetDayKey);
                var targetEntries = _store.Days[targetDayKey];
                var existing = targetEntries.FirstOrDefault(e => e.TaskId == taskId);
                if (existing != null)
                {
                    existing.IsDone = false;
                    existing.IsRunning = false;
                    existing.ModifiedAt = DateTime.UtcNow;
                }
                else
                {
                    var nextOrder = targetEntries.Count == 0 ? 0 : targetEntries.Max(e => e.Order) + 1;
                    targetEntries.Add(new DayTaskEntry
                    {
                        TaskId = taskId,
                        ElapsedSeconds = 0,
                        IsRunning = false,
                        IsDone = false,
                        Order = nextOrder,
                        ModifiedAt = DateTime.UtcNow
                    });
                }

                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
        }

        public async System.Threading.Tasks.Task SetTaskDoneAsync(string dayKey, Guid taskId, bool isDone)
        {
            TaskStoreData? snapshot = null;

            lock (_lockObject)
            {
                if (!_store.Days.TryGetValue(dayKey, out var dayEntries))
                {
                    return;
                }

                var entry = dayEntries.FirstOrDefault(e => e.TaskId == taskId);
                if (entry == null)
                {
                    return;
                }

                entry.IsDone = isDone;
                entry.IsRunning = false;
                if (isDone)
                {
                    entry.Order = (dayEntries.Count == 0 ? 0 : dayEntries.Max(e => e.Order) + 1);
                }
                entry.ModifiedAt = DateTime.UtcNow;
                snapshot = CreateSnapshotLocked();
            }

            if (snapshot != null)
            {
                await _storageService.SaveStoreAsync(snapshot);
            }
        }

        public System.Threading.Tasks.Task<int> CountFutureUndoneDaysAsync(string dayKey, Guid taskId)
        {
            var selectedDate = WorkdayClock.ParseDayKey(dayKey);
            lock (_lockObject)
            {
                var count = _store.Days
                    .Where(kvp => WorkdayClock.ParseDayKey(kvp.Key) > selectedDate)
                    .Count(kvp => kvp.Value.Any(e => e.TaskId == taskId && !e.IsDone));
                return System.Threading.Tasks.Task.FromResult(count);
            }
        }

        public async System.Threading.Tasks.Task MarkFutureDaysDoneAsync(string dayKey, Guid taskId, bool deleteLowTimeEntries)
        {
            var selectedDate = WorkdayClock.ParseDayKey(dayKey);
            TaskStoreData snapshot;

            lock (_lockObject)
            {
                foreach (var kvp in _store.Days.Where(kvp => WorkdayClock.ParseDayKey(kvp.Key) > selectedDate))
                {
                    var dayEntries = kvp.Value;
                    var entry = dayEntries.FirstOrDefault(e => e.TaskId == taskId);
                    if (entry == null)
                    {
                        continue;
                    }

                    if (deleteLowTimeEntries && entry.ElapsedSeconds <= 60)
                    {
                        dayEntries.Remove(entry);
                        continue;
                    }

                    entry.IsDone = true;
                    entry.IsRunning = false;
                    entry.Order = (dayEntries.Count == 0 ? 0 : dayEntries.Max(e => e.Order) + 1);
                    entry.ModifiedAt = DateTime.UtcNow;
                }

                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
        }

        public async System.Threading.Tasks.Task DeleteTaskForDayAsync(string dayKey, Guid taskId)
        {
            TaskStoreData? snapshot = null;

            lock (_lockObject)
            {
                if (!_store.Days.TryGetValue(dayKey, out var dayEntries))
                {
                    return;
                }

                var removed = dayEntries.RemoveAll(e => e.TaskId == taskId) > 0;
                if (!removed)
                {
                    return;
                }

                PruneOrphanDefinitionLocked(taskId);
                snapshot = CreateSnapshotLocked();
            }

            if (snapshot != null)
            {
                await _storageService.SaveStoreAsync(snapshot);
            }
        }

        public async System.Threading.Tasks.Task DeleteTaskForeverAsync(Guid taskId)
        {
            TaskStoreData snapshot;

            lock (_lockObject)
            {
                foreach (var dayEntries in _store.Days.Values)
                {
                    dayEntries.RemoveAll(e => e.TaskId == taskId);
                }
                _store.Tasks.Remove(taskId.ToString());
                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
        }

        public async System.Threading.Tasks.Task ReorderTasksAsync(string dayKey, IReadOnlyList<Guid> orderedTaskIds)
        {
            if (orderedTaskIds == null)
            {
                throw new ArgumentNullException(nameof(orderedTaskIds));
            }

            TaskStoreData? snapshot = null;
            lock (_lockObject)
            {
                if (!_store.Days.TryGetValue(dayKey, out var dayEntries))
                {
                    return;
                }

                var orderLookup = orderedTaskIds
                    .Select((id, index) => new { id, index })
                    .ToDictionary(x => x.id, x => x.index);

                foreach (var entry in dayEntries)
                {
                    if (orderLookup.TryGetValue(entry.TaskId, out var index))
                    {
                        entry.Order = index;
                    }
                }

                snapshot = CreateSnapshotLocked();
            }

            if (snapshot != null)
            {
                await _storageService.SaveStoreAsync(snapshot);
            }
        }

        public System.Threading.Tasks.Task<TaskStatistics?> GetTaskStatisticsAsync(Guid taskId)
        {
            lock (_lockObject)
            {
                var matches = _store.Days
                    .SelectMany(kvp => kvp.Value.Where(e => e.TaskId == taskId).Select(e => new { DayKey = kvp.Key, Entry = e }))
                    .OrderBy(x => WorkdayClock.ParseDayKey(x.DayKey))
                    .ToList();

                if (matches.Count == 0)
                {
                    return System.Threading.Tasks.Task.FromResult<TaskStatistics?>(null);
                }

                var name = _store.Tasks.TryGetValue(taskId.ToString(), out var def)
                    ? def.Name
                    : string.Empty;

                var stats = new TaskStatistics
                {
                    TaskId = taskId,
                    Name = name,
                    TotalElapsedSeconds = matches.Sum(x => x.Entry.ElapsedSeconds),
                    TotalActiveDays = matches.Count(x => x.Entry.ElapsedSeconds > 60),
                    TotalUsedDays = matches.Count,
                    FirstSeenLocalDate = WorkdayClock.ParseDayKey(matches.First().DayKey),
                    LastSeenLocalDate = WorkdayClock.ParseDayKey(matches.Last().DayKey)
                };

                return System.Threading.Tasks.Task.FromResult<TaskStatistics?>(stats);
            }
        }

        private bool EnsureDayExistsInternal(string dayKey)
        {
            if (_store.Days.ContainsKey(dayKey))
            {
                return false;
            }

            // Copy from the most recent existing day strictly before the day being created,
            // so a new "today" inherits yesterday's (or the latest prior day's) tasks.
            var targetDate = WorkdayClock.ParseDayKey(dayKey);
            var sourceKey = _store.Days.Keys
                .Where(k => WorkdayClock.ParseDayKey(k) < targetDate)
                .OrderByDescending(WorkdayClock.ParseDayKey)
                .FirstOrDefault();

            var sourceEntries = sourceKey != null && _store.Days.TryGetValue(sourceKey, out var entries)
                ? entries
                : new List<DayTaskEntry>();

            _store.Days[dayKey] = sourceEntries
                .OrderBy(e => e.Order)
                .Select(e => e.CloneForNewDay())
                .ToList();

            Log.Information("Created day snapshot {DayKey} by copying {Count} tasks from {SourceKey}", dayKey, _store.Days[dayKey].Count, sourceKey ?? "(none)");
            return true;
        }

        /// <summary>
        /// Removes a task definition if no day still references it.
        /// </summary>
        private void PruneOrphanDefinitionLocked(Guid taskId)
        {
            var stillUsed = _store.Days.Values.Any(entries => entries.Any(e => e.TaskId == taskId));
            if (!stillUsed)
            {
                _store.Tasks.Remove(taskId.ToString());
            }
        }

        private DateTime GetCreatedAtLocked(Guid taskId)
        {
            return _store.Tasks.TryGetValue(taskId.ToString(), out var def)
                ? def.CreatedAt
                : DateTime.MaxValue;
        }

        private TaskItem ComposeLocked(DayTaskEntry entry)
        {
            _store.Tasks.TryGetValue(entry.TaskId.ToString(), out var def);
            return new TaskItem
            {
                Id = entry.TaskId,
                Name = def?.Name ?? string.Empty,
                ElapsedSeconds = entry.ElapsedSeconds,
                IsRunning = entry.IsRunning,
                IsDone = entry.IsDone,
                CreatedAt = def?.CreatedAt ?? entry.ModifiedAt,
                ModifiedAt = entry.ModifiedAt,
                Order = entry.Order
            };
        }

        private async System.Threading.Tasks.Task PersistAsync()
        {
            TaskStoreData snapshot;
            lock (_lockObject)
            {
                snapshot = CreateSnapshotLocked();
            }

            await _storageService.SaveStoreAsync(snapshot);
        }

        private TaskStoreData CreateSnapshotLocked()
        {
            return new TaskStoreData
            {
                SchemaVersion = _store.SchemaVersion,
                Tasks = _store.Tasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
                Days = _store.Days.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(e => e.Clone()).ToList())
            };
        }
    }
}
