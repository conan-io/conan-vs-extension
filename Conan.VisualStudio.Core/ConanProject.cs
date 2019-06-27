using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System;

namespace Conan.VisualStudio.Core
{
    public class ConanProject
    {
        public string Path { get; set; }
        public string ConfigFile { get; set; }
        public List<ConanConfiguration> Configurations { get; } = new List<ConanConfiguration>();

        public string getProfile(ConanConfiguration configuration)
        {
            if (!File.Exists(ConfigFile)) return null;
            try
            {
                JObject jObject = JObject.Parse(File.ReadAllText(ConfigFile));
                var configs = jObject["configurations"].ToObject<Dictionary<string, string>>();
                configs.TryGetValue(configuration.VSName, out string conanProfile);
                return conanProfile;
            }
            catch (Exception)
            {
                // TODO: Check the error and log to user
                return null;
            }
        }
    }
}
