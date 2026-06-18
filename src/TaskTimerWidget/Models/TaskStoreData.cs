using Newtonsoft.Json;

namespace TaskTimerWidget.Models
{
    /// <summary>
    /// Root storage model. Normalized: task identities live in <see cref="Tasks"/> (keyed by
    /// task id), and each day in <see cref="Days"/> holds lightweight per-day entries that
    /// reference tasks by id.
    /// </summary>
    public sealed class TaskStoreData
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 3;

        /// <summary>
        /// Task identities keyed by task id (Guid "D" format).
        /// </summary>
        [JsonProperty("tasks")]
        public Dictionary<string, TaskDefinition> Tasks { get; set; } = new();

        /// <summary>
        /// Per-day task state, keyed by workday key (yyyy-MM-dd).
        /// </summary>
        [JsonProperty("days")]
        public Dictionary<string, List<DayTaskEntry>> Days { get; set; } = new();
    }
}
