using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Listik
{
    [DataContract]
    public class AppSettings
    {
        [DataMember] public string HotKey1 { get; set; } = "Q";
        [DataMember] public string HotKey2 { get; set; } = "Y";
        [DataMember] public bool HookGrass { get; set; }
        [DataMember] public bool KeepTrunks { get; set; }
        [DataMember] public bool AutoDisable { get; set; }
        [DataMember] public string DeviceId { get; set; } = System.Guid.NewGuid().ToString("N");
        [DataMember] public string ActivationCode { get; set; } = string.Empty;
    }

    public static class AppSettingsStore
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Listik");
        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

        public static AppSettings LoadOrCreate()
        {
            if (!File.Exists(SettingsPath))
            {
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }

            try
            {
                using (var stream = File.OpenRead(SettingsPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    var settings = (AppSettings)serializer.ReadObject(stream) ?? new AppSettings();
                    if (string.IsNullOrWhiteSpace(settings.DeviceId))
                    {
                        settings.DeviceId = System.Guid.NewGuid().ToString("N");
                        Save(settings);
                    }
                    return settings;
                }
            }
            catch (SerializationException)
            {
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(SettingsDirectory);
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                serializer.WriteObject(stream, settings);
                File.WriteAllText(SettingsPath, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }
    }
}
