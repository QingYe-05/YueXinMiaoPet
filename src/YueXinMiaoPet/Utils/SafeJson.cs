using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace YueXinMiaoPet.Utils
{
    public static class SafeJson
    {
        public static T Read<T>(string path, T fallback)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return fallback;
                }

                using (FileStream stream = File.OpenRead(path))
                {
                    DataContractJsonSerializer serializer = CreateSerializer(typeof(T));
                    object value = serializer.ReadObject(stream);
                    if (value is T)
                    {
                        return (T)value;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("读取 JSON 失败：" + path, ex);
            }

            return fallback;
        }

        public static bool Write<T>(string path, T value)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    FilePathHelper.EnsureDirectory(directory);
                }

                DataContractJsonSerializer serializer = CreateSerializer(typeof(T));
                using (MemoryStream stream = new MemoryStream())
                {
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
                    {
                        serializer.WriteObject(writer, value);
                    }

                    File.WriteAllText(path, Encoding.UTF8.GetString(stream.ToArray()), new UTF8Encoding(false));
                }

                return true;
            }
            catch (Exception ex)
            {
                Services.LogService.Error("写入 JSON 失败：" + path, ex);
                return false;
            }
        }

        private static DataContractJsonSerializer CreateSerializer(Type type)
        {
            return new DataContractJsonSerializer(type, new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("o"),
                UseSimpleDictionaryFormat = true
            });
        }
    }
}
