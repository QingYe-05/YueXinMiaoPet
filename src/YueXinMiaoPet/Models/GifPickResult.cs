using System.Collections.Generic;

namespace YueXinMiaoPet.Models
{
    public class GifPickCandidate
    {
        public GifAsset Asset { get; set; }
        public int Score { get; set; }
        public string Reason { get; set; }
        public string CategoryName { get; set; }

        public GifPickCandidate()
        {
            Reason = string.Empty;
            CategoryName = string.Empty;
        }
    }

    public class GifPickResult
    {
        public GifAsset Selected { get; set; }
        public List<GifPickCandidate> TopCandidates { get; set; }

        public GifPickResult()
        {
            TopCandidates = new List<GifPickCandidate>();
        }
    }
}
