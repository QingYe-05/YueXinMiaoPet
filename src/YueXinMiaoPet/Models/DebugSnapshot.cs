using System.Collections.Generic;

namespace YueXinMiaoPet.Models
{
    public class DebugSnapshot
    {
        public PetState State { get; set; }
        public string CurrentGifFile { get; set; }
        public string CurrentGifCategory { get; set; }
        public string CurrentPlaybackMode { get; set; }
        public string CurrentPlaylistSource { get; set; }
        public int CurrentPlaylistCount { get; set; }
        public int CurrentPlaylistIndex { get; set; }
        public string CurrentMoodCategory { get; set; }
        public int CurrentMoodCustomPlaylistCount { get; set; }
        public int GlobalCustomPlaylistCount { get; set; }
        public bool WeatherEnabled { get; set; }
        public bool WeatherAffectsGif { get; set; }
        public string WeatherBadgeText { get; set; }
        public bool CodexStatusEnabled { get; set; }
        public bool CodexStatusBubbleEnabled { get; set; }
        public bool CodexStatusAffectsGif { get; set; }
        public string CodexStatusFilePath { get; set; }
        public string CodexStatusTag { get; set; }
        public string CodexStatusBadgeText { get; set; }
        public List<GifPickCandidate> TopCandidates { get; set; }

        public DebugSnapshot()
        {
            State = new PetState();
            CurrentGifFile = string.Empty;
            CurrentGifCategory = string.Empty;
            CurrentPlaybackMode = "Sequential";
            CurrentPlaylistSource = string.Empty;
            CurrentMoodCategory = string.Empty;
            WeatherBadgeText = string.Empty;
            CodexStatusFilePath = string.Empty;
            CodexStatusTag = "unknown";
            CodexStatusBadgeText = string.Empty;
            TopCandidates = new List<GifPickCandidate>();
        }
    }
}
