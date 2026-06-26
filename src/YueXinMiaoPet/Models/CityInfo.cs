using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class CityInfo
    {
        [DataMember(Name = "province")]
        public string Province { get; set; }

        [DataMember(Name = "city")]
        public string City { get; set; }

        [DataMember(Name = "latitude")]
        public double Latitude { get; set; }

        [DataMember(Name = "longitude")]
        public double Longitude { get; set; }

        public override string ToString()
        {
            return City;
        }
    }
}
