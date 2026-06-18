namespace TaskTimerWidget.Models
{
    /// <summary>
    /// Aggregated task metrics across all stored days.
    /// </summary>
    public sealed class TaskStatistics
    {
        public Guid TaskId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long TotalElapsedSeconds { get; set; }
        public int TotalActiveDays { get; set; }
        public int TotalUsedDays { get; set; }
        public DateTime? FirstSeenLocalDate { get; set; }
        public DateTime? LastSeenLocalDate { get; set; }
    }
}
