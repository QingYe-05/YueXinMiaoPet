using System.Collections.Generic;
using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class AdministrativeDivision
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "level")]
        public string Level { get; set; }

        [DataMember(Name = "children")]
        public List<AdministrativeDivision> Children { get; set; }

        public AdministrativeDivision()
        {
            Code = string.Empty;
            Name = string.Empty;
            Level = string.Empty;
            Children = new List<AdministrativeDivision>();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class AdministrativeRegionPath
    {
        public AdministrativeDivision Province { get; set; }
        public AdministrativeDivision City { get; set; }
        public AdministrativeDivision County { get; set; }

        public string FullPath
        {
            get
            {
                return (Province == null ? string.Empty : Province.Name) + " / " +
                    (City == null ? string.Empty : City.Name) + " / " +
                    (County == null ? string.Empty : County.Name);
            }
        }

        public override string ToString()
        {
            return FullPath;
        }
    }

    [DataContract]
    public class WeatherLocationCacheEntry
    {
        [DataMember(Name = "administrativeCode")]
        public string AdministrativeCode { get; set; }

        [DataMember(Name = "latitude")]
        public double Latitude { get; set; }

        [DataMember(Name = "longitude")]
        public double Longitude { get; set; }

        [DataMember(Name = "weatherLocationId")]
        public string WeatherLocationId { get; set; }

        [DataMember(Name = "updatedAtUtc")]
        public string UpdatedAtUtc { get; set; }
    }
}
