using TaskTimerWidget.Models;

namespace TaskTimerWidget.ViewModels
{
    /// <summary>
    /// ViewModel for individual task representation and management.
    /// </summary>
    public class TaskViewModel : ViewModelBase
    {
        private TaskItem _model;
        private bool _isActive;
        private long _totalElapsedSeconds;

        public object? Tag { get; set; }

        public Guid Id => _model.Id;

        public string Name
        {
            get => _model.Name;
            set
            {
                if (_model.Name != value)
                {
                    _model.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public long ElapsedSeconds
        {
            get => _model.ElapsedSeconds;
            set
            {
                if (_model.ElapsedSeconds != value)
                {
                    _model.ElapsedSeconds = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedTime));
                }
            }
        }

        public bool IsRunning
        {
            get => _model.IsRunning;
            set
            {
                if (_model.IsRunning != value)
                {
                    _model.IsRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        public bool IsDone
        {
            get => _model.IsDone;
            set
            {
                if (_model.IsDone != value)
                {
                    _model.IsDone = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDoneIndicator));
                }
            }
        }

        public string IsDoneIndicator => IsDone ? "✓" : string.Empty;

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value, nameof(IsActive));
        }

        public string FormattedTime => _model.GetFormattedTime();

        public string TimePercentage
        {
            get
            {
                if (_totalElapsedSeconds == 0)
                    return "0%";

                var percentage = (ElapsedSeconds * 100) / _totalElapsedSeconds;
                return $"{percentage}%";
            }
        }

        public DateTime CreatedAt => _model.CreatedAt;

        public TaskViewModel(TaskItem model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _isActive = false;
        }

        /// <summary>
        /// Updates the elapsed time display without changing the model.
        /// Used for live timer updates.
        /// </summary>
        public void UpdateElapsedDisplay(long seconds)
        {
            _model.ElapsedSeconds = seconds;
            OnPropertyChanged(nameof(ElapsedSeconds));
            OnPropertyChanged(nameof(FormattedTime));
        }

        /// <summary>
        /// Sets the total elapsed seconds for all tasks (for percentage calculation).
        /// </summary>
        public void SetTotalElapsedSeconds(long totalSeconds)
        {
            if (_totalElapsedSeconds != totalSeconds)
            {
                _totalElapsedSeconds = totalSeconds;
                OnPropertyChanged(nameof(TimePercentage));
            }
        }

        /// <summary>
        /// Gets the underlying task model.
        /// </summary>
        public TaskItem GetModel() => _model;
    }
}
