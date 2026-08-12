using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    /// <summary>
    /// 全国三级行政区目录。JSON 只在构造时读取一次，之后所有联动和搜索都使用内存索引。
    /// </summary>
    public class CityCatalogService
    {
        private readonly List<AdministrativeDivision> _provinces;
        private readonly Dictionary<string, AdministrativeDivision> _byCode;
        private readonly List<AdministrativeRegionPath> _countyPaths;

        public CityCatalogService()
        {
            _byCode = new Dictionary<string, AdministrativeDivision>(StringComparer.OrdinalIgnoreCase);
            _countyPaths = new List<AdministrativeRegionPath>();
            _provinces = LoadDivisions();
            BuildIndexes();
            LogStatistics();
        }

        public IList<AdministrativeDivision> GetProvinces()
        {
            return _provinces;
        }

        public IList<AdministrativeDivision> GetCities(string provinceCode)
        {
            AdministrativeDivision province = FindByCode(provinceCode);
            return province == null ? new List<AdministrativeDivision>() : province.Children;
        }

        public IList<AdministrativeDivision> GetCounties(string cityCode)
        {
            AdministrativeDivision city = FindByCode(cityCode);
            return city == null ? new List<AdministrativeDivision>() : city.Children;
        }

        public AdministrativeDivision FindByCode(string code)
        {
            AdministrativeDivision value;
            return !string.IsNullOrWhiteSpace(code) && _byCode.TryGetValue(code, out value) ? value : null;
        }

        public AdministrativeRegionPath FindPath(string provinceCode, string cityCode, string countyCode)
        {
            return _countyPaths.FirstOrDefault(path =>
                (string.IsNullOrWhiteSpace(provinceCode) || string.Equals(path.Province.Code, provinceCode, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(cityCode) || string.Equals(path.City.Code, cityCode, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(countyCode) || string.Equals(path.County.Code, countyCode, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// 迁移旧版省市名称。县区不自动猜测，避免同名地区定位错误。
        /// </summary>
        public AdministrativeRegionPath FindLegacyCity(string provinceName, string cityName)
        {
            foreach (AdministrativeDivision province in _provinces)
            {
                if (!string.IsNullOrWhiteSpace(provinceName) && !string.Equals(province.Name, provinceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AdministrativeDivision city = province.Children.FirstOrDefault(item =>
                    string.Equals(item.Name, cityName, StringComparison.OrdinalIgnoreCase));
                if (city != null)
                {
                    return new AdministrativeRegionPath { Province = province, City = city, County = null };
                }
            }

            return null;
        }

        public bool MigrateConfig(AppConfig config)
        {
            if (config == null) return false;
            if (FindByCode(config.ProvinceCode) != null && FindByCode(config.CityCode) != null) return false;

            AdministrativeRegionPath legacy = FindLegacyCity(config.ProvinceName ?? config.Province, config.CityName ?? config.City);
            if (legacy == null)
            {
                LogService.Warn("旧天气地区无法映射，请重新选择天气地区。Province=" + config.Province + "，City=" + config.City);
                return false;
            }

            config.ProvinceCode = legacy.Province.Code;
            config.ProvinceName = legacy.Province.Name;
            config.CityCode = legacy.City.Code;
            config.CityName = legacy.City.Name;
            config.Province = legacy.Province.Name;
            config.City = legacy.City.Name;
            config.LegacyCity = legacy.City.Name;
            config.CountyCode = string.Empty;
            config.CountyName = string.Empty;
            // 旧版天气缓存没有行政区代码，无法证明属于迁移后的地区，必须清空以避免跨城误用。
            config.LastWeatherCache = WeatherInfo.Unknown();
            config.LastWeather = config.LastWeatherCache;
            LogService.Info("旧天气地区已迁移：" + legacy.Province.Name + " / " + legacy.City.Name + "；请选择县/区以提高天气精度。");
            return true;
        }

        public IList<AdministrativeRegionPath> Search(string keyword, int limit)
        {
            string value = (keyword ?? string.Empty).Trim();
            if (value.Length == 0) return new List<AdministrativeRegionPath>();
            return _countyPaths
                .Where(path => path.Province.Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.City.Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.County.Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.FullPath.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(Math.Max(1, limit))
                .ToList();
        }

        private List<AdministrativeDivision> LoadDivisions()
        {
            try
            {
                string assemblyDirectory = Path.GetDirectoryName(typeof(CityCatalogService).Assembly.Location);
                string path = Path.Combine(assemblyDirectory ?? FilePathHelper.AppBaseDir, "Data", "china_administrative_divisions.json");
                List<AdministrativeDivision> data = SafeJson.Read<List<AdministrativeDivision>>(path, null);
                if (data != null && data.Count > 0) return data;
                LogService.Warn("全国行政区数据为空：" + path);
            }
            catch (Exception ex)
            {
                LogService.Error("读取全国行政区数据失败。", ex);
            }

            return new List<AdministrativeDivision>();
        }

        private void BuildIndexes()
        {
            foreach (AdministrativeDivision province in _provinces)
            {
                AddToIndex(province);
                foreach (AdministrativeDivision city in province.Children ?? new List<AdministrativeDivision>())
                {
                    AddToIndex(city);
                    foreach (AdministrativeDivision county in city.Children ?? new List<AdministrativeDivision>())
                    {
                        AddToIndex(county);
                        _countyPaths.Add(new AdministrativeRegionPath { Province = province, City = city, County = county });
                    }
                }
            }
        }

        private void AddToIndex(AdministrativeDivision division)
        {
            if (division != null && !string.IsNullOrWhiteSpace(division.Code) && !_byCode.ContainsKey(division.Code))
            {
                _byCode.Add(division.Code, division);
            }
        }

        private void LogStatistics()
        {
            int cityCount = _provinces.Sum(item => item.Children == null ? 0 : item.Children.Count);
            LogService.Info("AdministrativeDataProvinceCount=" + _provinces.Count +
                "，AdministrativeDataCityCount=" + cityCount +
                "，AdministrativeDataCountyCount=" + _countyPaths.Count +
                "，AdministrativeDataValidationResult=" + (_provinces.Count > 0 && _countyPaths.Count > 0 ? "PASS" : "FAIL"));
        }
    }
}
