using System;

namespace YueXinMiaoPet.Models
{
    public class PetState
    {
        public string WeatherTag { get; set; }
        public string TimeTag { get; set; }
        public string MoodTag { get; set; }
        public string ActionTag { get; set; }
        public bool IsWorkingTime { get; set; }
        public bool IsAfterWork { get; set; }
        public DateTime LastInteractionTime { get; set; }
        public double Temperature { get; set; }
        public int WeatherCode { get; set; }
        public DateTime LastMoodChangedAt { get; set; }
        public bool IsMoodLocked { get; set; }
        public bool IsWeatherReactionActive { get; set; }

        public PetState()
        {
            WeatherTag = "unknown";
            TimeTag = "idle";
            MoodTag = "neutral";
            ActionTag = "idle";
            LastInteractionTime = DateTime.Now;
            Temperature = 0;
            WeatherCode = -1;
            LastMoodChangedAt = DateTime.MinValue;
            IsMoodLocked = false;
            IsWeatherReactionActive = false;
        }

        public PetState Clone()
        {
            return (PetState)MemberwiseClone();
        }
    }
}
