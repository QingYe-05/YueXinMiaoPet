using System.Collections.Generic;

namespace YueXinMiaoPet.Models
{
    public class DebugSnapshot
    {
        public PetState State { get; set; }
        public string CurrentGifFile { get; set; }
        public string CurrentGifCategory { get; set; }
        public List<GifPickCandidate> TopCandidates { get; set; }

        public DebugSnapshot()
        {
            State = new PetState();
            CurrentGifFile = string.Empty;
            CurrentGifCategory = string.Empty;
            TopCandidates = new List<GifPickCandidate>();
        }
    }
}
