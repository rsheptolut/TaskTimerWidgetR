using Newtonsoft.Json;

namespace TaskTimerWidget.Models
{
    /// <summary>
    /// Stable identity/metadata for a task, shared across all days it appears in.
    /// Stored once in the top-level <c>tasks</c> collection and referenced by id from each day.
    /// </summary>
    public sealed class TaskDefinition
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("modifiedAt")]
        public DateTime ModifiedAt { get; set; }

        public TaskDefinition Clone()
        {
            return new TaskDefinition
            {
                Id = Id,
                Name = Name,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt
            };
        }
    }
}
