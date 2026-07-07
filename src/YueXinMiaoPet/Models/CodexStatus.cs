using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class CodexStatus
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "task")]
        public string Task { get; set; }

        [DataMember(Name = "progress")]
        public int Progress { get; set; }

        [DataMember(Name = "updatedAt")]
        public string UpdatedAtText { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        public DateTime UpdatedAt
        {
            get
            {
                DateTime value;
                if (DateTime.TryParse(UpdatedAtText, null, DateTimeStyles.RoundtripKind, out value))
                {
                    return value;
                }

                return DateTime.MinValue;
            }
            set
            {
                UpdatedAtText = value.ToString("o");
            }
        }

        public CodexStatus()
        {
            Enabled = true;
            Status = "idle";
            Title = "Codex 空闲中";
            Message = "没有正在执行的任务";
            Task = string.Empty;
            Progress = 0;
            UpdatedAt = DateTime.Now;
            Source = "manual";
        }

        public CodexStatus Clone()
        {
            return new CodexStatus
            {
                Enabled = Enabled,
                Status = Status,
                Title = Title,
                Message = Message,
                Task = Task,
                Progress = Progress,
                UpdatedAtText = UpdatedAtText,
                Source = Source
            };
        }
    }
}
