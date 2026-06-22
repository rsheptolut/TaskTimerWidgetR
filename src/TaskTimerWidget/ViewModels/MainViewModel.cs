using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using TaskTimerWidget.Helpers;
using TaskTimerWidget.Models;
using TaskTimerWidget.Services;
using Serilog;

namespace TaskTimerWidget.ViewModels
{
    /// <summary>
    /// Main ViewModel managing day navigation and task operations.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ITaskService _taskService;
        private DispatcherTimer? _timerUpdate;
        private TaskViewModel? _activeTask;
        private TaskViewModel? _selectedTask;
        private bool _isLoading;
        private string? _errorMessage;
        private string _selectedDayKey = string.Empty;
        private string _knownTodayKey = string.Empty;
        private string? _minDayKey;
        private bool _isTodaySelected;
        private string _selectedDayDisplay = string.Empty;
        private bool _canNavigatePrevious;
        private bool _canNavigateNext;
        private string _totalDayTimeDisplay = string.Empty;

        private ICommand? _addTaskCommand;
        private ICommand? _selectTaskCommand;

        public ObservableCollection<TaskViewModel> Tasks { get; }

        public TaskViewModel? ActiveTask
        {
            get => _activeTask;
            private set => SetProperty(ref _activeTask, value);
        }

        public TaskViewModel? SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string SelectedDayKey
        {
            get => _selectedDayKey;
            private set => SetProperty(ref _selectedDayKey, value);
        }

        public bool IsTodaySelected
        {
            get => _isTodaySelected;
            private set => SetProperty(ref _isTodaySelected, value);
        }

        public string SelectedDayDisplay
        {
            get => _selectedDayDisplay;
            private set => SetProperty(ref _selectedDayDisplay, value);
        }

        public bool CanNavigatePrevious
        {
            get => _canNavigatePrevious;
            private set => SetProperty(ref _canNavigatePrevious, value);
        }

        public bool CanNavigateNext
        {
            get => _canNavigateNext;
            private set => SetProperty(ref _canNavigateNext, value);
        }

        public string TotalDayTimeDisplay
        {
            get => _totalDayTimeDisplay;
            private set => SetProperty(ref _totalDayTimeDisplay, value);
        }

        public ICommand AddTaskCommand =>
            _addTaskCommand ??= new RelayCommand<string>(async taskName =>
            {
                if (!string.IsNullOrWhiteSpace(taskName))
                {
                    await AddTaskAsync(taskName);
                }
            });

        public ICommand SelectTaskCommand =>
            _selectTaskCommand ??= new RelayCommand<TaskViewModel>(async taskVm =>
            {
                if (taskVm != null)
                {
                    await SelectTaskAsync(taskVm);
                }
            });

        public MainViewModel(ITaskService taskService)
        {
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
            Tasks = new ObservableCollection<TaskViewModel>();
            InitializeTimer();
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                _knownTodayKey = _taskService.GetTodayDayKey();
                await LoadDayAsync(_knownTodayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load tasks";
                Log.Error(ex, "Error initializing MainViewModel");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task NavigatePreviousDayAsync()
        {
            if (!CanNavigatePrevious)
            {
                return;
            }

            var targetDate = WorkdayClock.ParseDayKey(SelectedDayKey).AddDays(-1);
            await NavigateToDayAsync(targetDate);
        }

        public async Task NavigateNextDayAsync()
        {
            if (!CanNavigateNext)
            {
                return;
            }

            var targetDate = WorkdayClock.ParseDayKey(SelectedDayKey).AddDays(1);
            await NavigateToDayAsync(targetDate);
        }

        public Task GoToTodayAsync()
        {
            return NavigateToDayAsync(WorkdayClock.ParseDayKey(_taskService.GetTodayDayKey()));
        }

        public async Task ReloadCurrentDayAsync()
        {
            await LoadDayAsync(SelectedDayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
        }

        public async Task RenameTaskAsync(TaskViewModel taskVm, string newName)
        {
            if (taskVm == null || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            await _taskService.RenameTaskAsync(taskVm.Id, newName.Trim());
            await LoadDayAsync(SelectedDayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
        }

        public async Task<int> GetFutureUndoneDaysAsync(TaskViewModel taskVm)
        {
            return await _taskService.CountFutureUndoneDaysAsync(SelectedDayKey, taskVm.Id);
        }

        public async Task MarkTaskDoneAsync(TaskViewModel taskVm, bool markFutureToo)
        {
            if (taskVm == null)
            {
                return;
            }

            await _taskService.SetTaskDoneAsync(SelectedDayKey, taskVm.Id, true);
            if (markFutureToo)
            {
                await _taskService.MarkFutureDaysDoneAsync(SelectedDayKey, taskVm.Id, deleteLowTimeEntries: true);
            }

            await LoadDayAsync(SelectedDayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
        }

        public async Task AddTaskToTodayAsync(TaskViewModel taskVm)
        {
            if (taskVm == null)
            {
                return;
            }

            var todayKey = _taskService.GetTodayDayKey();
            await _taskService.AddOrReactivateTaskOnDayAsync(taskVm.Id, todayKey);

            if (IsTodaySelected)
            {
                await LoadDayAsync(SelectedDayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
            }
            else
            {
                await RefreshNavigationAsync();
            }
        }

        public Task<TaskStatistics?> GetTaskStatisticsAsync(TaskViewModel taskVm)
        {
            if (taskVm == null)
            {
                return Task.FromResult<TaskStatistics?>(null);
            }

            return _taskService.GetTaskStatisticsAsync(taskVm.Id);
        }

        public async Task DeleteTaskForCurrentDayAsync(TaskViewModel taskVm)
        {
            if (taskVm == null)
            {
                return;
            }

            if (ActiveTask?.Id == taskVm.Id)
            {
                ActiveTask = null;
            }

            await _taskService.DeleteTaskForDayAsync(SelectedDayKey, taskVm.Id);
            await LoadDayAsync(SelectedDayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
        }

        public async Task DeleteTaskForeverAsync(TaskViewModel taskVm)
        {
            if (taskVm == null)
            {
                return;
            }

            if (ActiveTask?.Id == taskVm.Id)
            {
                ActiveTask = null;
            }

            await _taskService.DeleteTaskForeverAsync(taskVm.Id);
            await LoadDayAsync(SelectedDayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
        }

        public async Task UpdateTaskOrdersAsync()
        {
            try
            {
                var orderedIds = Tasks.Select(task => task.Id).ToList();
                await _taskService.ReorderTasksAsync(SelectedDayKey, orderedIds);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating task orders");
            }
        }

        public void SetTaskElapsedTime(TaskViewModel taskVm, long totalSeconds)
        {
            taskVm.ElapsedSeconds = Math.Max(0, totalSeconds);
            _ = PersistTaskAsync(taskVm);
            UpdateTaskPercentages();
        }

        private async Task NavigateToDayAsync(DateTime targetDate)
        {
            var todayDate = WorkdayClock.ParseDayKey(_taskService.GetTodayDayKey());
            if (targetDate > todayDate)
            {
                targetDate = todayDate;
            }

            if (_minDayKey != null)
            {
                var minDate = WorkdayClock.ParseDayKey(_minDayKey);
                if (targetDate < minDate)
                {
                    targetDate = minDate;
                }
            }

            var dayKey = targetDate.ToString("yyyy-MM-dd");
            await LoadDayAsync(dayKey, ensureDayExists: true, stopRunningBeforeLoad: true);
        }

        private async Task LoadDayAsync(string dayKey, bool ensureDayExists, bool stopRunningBeforeLoad)
        {
            if (string.IsNullOrWhiteSpace(dayKey))
            {
                return;
            }

            if (stopRunningBeforeLoad && !string.IsNullOrWhiteSpace(SelectedDayKey))
            {
                await StopRunningOnCurrentDayAsync();
            }

            var dayTasks = await _taskService.GetTasksForDayAsync(dayKey, ensureDayExists);

            Tasks.Clear();
            foreach (var task in dayTasks)
            {
                Tasks.Add(new TaskViewModel(task));
            }

            ActiveTask = null;
            SelectedTask = null;
            SelectedDayKey = dayKey;

            UpdateTaskPercentages();
            await RefreshNavigationAsync();
            Log.Information("Loaded {TaskCount} tasks for day {DayKey}", Tasks.Count, dayKey);
        }

        private async Task RefreshNavigationAsync()
        {
            var dayKeys = await _taskService.GetDayKeysAsync();
            _minDayKey = dayKeys.Count == 0 ? null : dayKeys[0];

            var todayKey = _taskService.GetTodayDayKey();
            _knownTodayKey = todayKey;
            IsTodaySelected = SelectedDayKey == todayKey;

            var displayDate = WorkdayClock.FormatDisplay(SelectedDayKey);
            SelectedDayDisplay = IsTodaySelected ? $"Today · {displayDate}" : displayDate;

            if (_minDayKey == null)
            {
                CanNavigatePrevious = false;
            }
            else
            {
                CanNavigatePrevious = WorkdayClock.ParseDayKey(SelectedDayKey) > WorkdayClock.ParseDayKey(_minDayKey);
            }

            CanNavigateNext = WorkdayClock.ParseDayKey(SelectedDayKey) < WorkdayClock.ParseDayKey(todayKey);
        }

        private async Task AddTaskAsync(string taskName)
        {
            if (!IsTodaySelected)
            {
                ErrorMessage = "Tasks can only be created on today.";
                return;
            }

            try
            {
                var newTask = await _taskService.CreateTaskAsync(SelectedDayKey, taskName);
                Tasks.Add(new TaskViewModel(newTask));
                UpdateTaskPercentages();
                ErrorMessage = null;
                Log.Information("Task added: {TaskName}", newTask.Name);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to add task";
                Log.Error(ex, "Error adding task");
            }
        }

        private async Task SelectTaskAsync(TaskViewModel taskVm)
        {
            if (!IsTodaySelected)
            {
                return;
            }

            try
            {
                if (taskVm.IsDone)
                {
                    return;
                }

                if (ActiveTask == taskVm)
                {
                    taskVm.IsRunning = !taskVm.IsRunning;
                    taskVm.IsActive = taskVm.IsRunning;
                    await PersistTaskAsync(taskVm);
                    return;
                }

                if (ActiveTask != null)
                {
                    ActiveTask.IsRunning = false;
                    ActiveTask.IsActive = false;
                    await PersistTaskAsync(ActiveTask);
                }

                SelectedTask = taskVm;
                ActiveTask = taskVm;
                ActiveTask.IsActive = true;
                ActiveTask.IsRunning = true;
                await PersistTaskAsync(ActiveTask);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to select task";
                Log.Error(ex, "Error selecting task");
            }
        }

        private void InitializeTimer()
        {
            _timerUpdate = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timerUpdate.Tick += async (_, _) => await OnTimerTickAsync();
            _timerUpdate.Start();
        }

        private async Task OnTimerTickAsync()
        {
            await HandleWorkdayRolloverAsync();

            if (!IsTodaySelected)
            {
                return;
            }

            if (ActiveTask?.IsRunning == true)
            {
                ActiveTask.UpdateElapsedDisplay(ActiveTask.ElapsedSeconds + 1);
                await PersistTaskAsync(ActiveTask);
            }

            UpdateTaskPercentages();
        }

        private async Task HandleWorkdayRolloverAsync()
        {
            var newTodayKey = _taskService.GetTodayDayKey();
            if (string.IsNullOrEmpty(_knownTodayKey))
            {
                _knownTodayKey = newTodayKey;
                return;
            }

            if (newTodayKey == _knownTodayKey)
            {
                return;
            }

            var previousToday = _knownTodayKey;
            _knownTodayKey = newTodayKey;

            if (SelectedDayKey == previousToday)
            {
                await StopRunningOnCurrentDayAsync();
                await LoadDayAsync(newTodayKey, ensureDayExists: true, stopRunningBeforeLoad: false);
            }
            else
            {
                await RefreshNavigationAsync();
            }
        }

        private async Task StopRunningOnCurrentDayAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedDayKey))
            {
                return;
            }

            await _taskService.StopRunningTasksAsync(SelectedDayKey);
            if (ActiveTask != null)
            {
                ActiveTask.IsRunning = false;
                ActiveTask.IsActive = false;
                ActiveTask = null;
            }
        }

        private async Task PersistTaskAsync(TaskViewModel taskVm)
        {
            await _taskService.UpdateTaskAsync(SelectedDayKey, taskVm.GetModel());
        }

        private void UpdateTaskPercentages()
        {
            var totalElapsedSeconds = Tasks.Sum(task => task.ElapsedSeconds);
            foreach (var task in Tasks)
            {
                task.SetTotalElapsedSeconds(totalElapsedSeconds);
            }

            TotalDayTimeDisplay = FormatTotalDuration(totalElapsedSeconds);
        }

        /// <summary>
        /// Formats a duration in the compact "Nh Nm" form used for the daily total
        /// (e.g. "6h 12m", "12m", "0m").
        /// </summary>
        private static string FormatTotalDuration(long totalSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;

            if (hours > 0)
            {
                return $"{hours}h {minutes}m";
            }
            return $"{minutes}m";
        }

        public void Dispose()
        {
            _timerUpdate?.Stop();
            _timerUpdate = null;
        }
    }
}
