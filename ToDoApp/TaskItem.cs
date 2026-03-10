using System;

namespace TodoApp
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public string Category { get; set; }
        public string TicketNumber { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public int PercentComplete { get; set; }
        public string Priority { get; set; }
        public bool IsImportant { get; set; }
        public bool IsReminder { get; set; }
        public DateTime? ReminderDate { get; set; }
        public string StartDate { get; set; } // Stored as YYYY-MM-DD
        public string EndDate { get; set; } // Stored as YYYY-MM-DD
        public string LastModified { get; set; }
        public bool ReminderDismissed { get; set; }

        // UI Helpers
        public string StatusLabel => PercentComplete == 100 ? "Done" : (PercentComplete > 0 ? "Active" : "Stuck");
        public string LastModifiedDisplay => string.IsNullOrEmpty(LastModified) ? "Never" : DateTime.Parse(LastModified).ToString("g");
    }
}