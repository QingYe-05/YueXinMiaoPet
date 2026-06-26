using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public class CityCatalogService
    {
        private readonly List<CityInfo> _cities;

        public CityCatalogService()
        {
            _cities = LoadCities();
        }

        public IList<CityInfo> Cities
        {
            get { return _cities; }
        }

        public IList<string> GetProvinces()
        {
            return _cities
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Province))
                .Select(c => c.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToList();
        }

        public IList<CityInfo> GetCities(string province)
        {
            return _cities
                .Where(c => c != null && string.Equals(c.Province, province, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.City)
                .ToList();
        }

        public CityInfo Find(string province, string city)
        {
            CityInfo match = _cities.FirstOrDefault(c =>
                string.Equals(c.Province, province, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                return match;
            }

            return _cities.FirstOrDefault(c => string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));
        }

        private List<CityInfo> LoadCities()
        {
            try
            {
                string path = Path.Combine(FilePathHelper.AppBaseDir, "Data", "china_cities.json");
                List<CityInfo> cities = SafeJson.Read<List<CityInfo>>(path, null);
                if (cities != null && cities.Count > 0)
                {
                    return cities;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("读取城市列表失败，使用内置兜底城市。", ex);
            }

            return new List<CityInfo>
            {
                new CityInfo { Province = "上海市", City = "上海市", Latitude = 31.2304, Longitude = 121.4737 },
                new CityInfo { Province = "北京市", City = "北京市", Latitude = 39.9042, Longitude = 116.4074 },
                new CityInfo { Province = "河南省", City = "郑州市", Latitude = 34.7466, Longitude = 113.6254 },
                new CityInfo { Province = "广东省", City = "广州市", Latitude = 23.1291, Longitude = 113.2644 },
                new CityInfo { Province = "广东省", City = "深圳市", Latitude = 22.5431, Longitude = 114.0579 }
            };
        }
    }
}
