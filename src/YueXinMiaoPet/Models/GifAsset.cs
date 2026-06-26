using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class GifAsset
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "file")]
        public string File { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "tags")]
        public List<string> Tags { get; set; }

        [DataMember(Name = "weight")]
        public int Weight { get; set; }

        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "categoryName")]
        public string CategoryName { get; set; }

        [DataMember(Name = "categoryTag")]
        public string CategoryTag { get; set; }

        [DataMember(Name = "categoryPath")]
        public string CategoryPath { get; set; }

        [DataMember(Name = "sourceMode")]
        public string SourceMode { get; set; }

        public GifAsset()
        {
            Id = string.Empty;
            File = string.Empty;
            Name = string.Empty;
            Tags = new List<string>();
            Weight = 1;
            Enabled = true;
            CategoryName = string.Empty;
            CategoryTag = string.Empty;
            CategoryPath = string.Empty;
            SourceMode = string.Empty;
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || Tags == null)
            {
                return false;
            }

            for (int i = 0; i < Tags.Count; i++)
            {
                if (string.Equals(Tags[i], tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return false;
            }

            string canonical = YueXinMiaoPet.Services.MoodCategoryService.GetCanonicalCategory(category);
            string selfCanonical = YueXinMiaoPet.Services.MoodCategoryService.GetCanonicalCategory(CategoryName);

            return string.Equals(CategoryName, category, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(CategoryTag, category, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(selfCanonical, canonical, StringComparison.OrdinalIgnoreCase);
        }
    }
}
