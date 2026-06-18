using Newtonsoft.Json;

namespace TaskTimerWidget.Models
{
    /// <summary>
    /// Per-day state for a task. References the task identity by <see cref="TaskId"/>;
    /// the name/createdAt live in the top-level <c>tasks</c> collection.
    /// </summary>
    public sealed class DayTaskEntry
    {
        [JsonProperty("taskId")]
        public Guid TaskId { get; set; }

        [JsonProperty("elapsedSeconds")]
        public long ElapsedSeconds { get; set; }

        [JsonProperty("isRunning")]
        public bool IsRunning { get; set; }

        [JsonProperty("isDone")]
        public bool IsDone { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("modifiedAt")]
        public DateTime ModifiedAt { get; set; }

        public DayTaskEntry Clone()
        {
            return new DayTaskEntry
            {
                TaskId = TaskId,
                ElapsedSeconds = ElapsedSeconds,
                IsRunning = IsRunning,
                IsDone = IsDone,
                Order = Order,
                ModifiedAt = ModifiedAt
            };
        }

        /// <summary>
        /// Creates a fresh entry for a new day: same task, time/running/done reset.
        /// </summary>
        public DayTaskEntry CloneForNewDay()
        {
            return new DayTaskEntry
            {
                TaskId = TaskId,
                ElapsedSeconds = 0,
                IsRunning = false,
                IsDone = false,
                Order = Order,
                ModifiedAt = DateTime.UtcNow
            };
        }
    }
}
