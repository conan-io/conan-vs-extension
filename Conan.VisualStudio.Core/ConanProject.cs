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

        public string getProfile(ConanConfiguration configuration, Core.IErrorListService errorListService)
        {
            if (!File.Exists(ConfigFile)) return null;
            try
            {
                JObject jObject = JObject.Parse(File.ReadAllText(ConfigFile));
                var configs = jObject["configurations"].ToObject<Dictionary<string, string>>();
                configs.TryGetValue(configuration.VSName, out string conanProfile);
                if (conanProfile == null)
                {
                    errorListService.WriteWarning($"File for Conan configuration found at '{ConfigFile}'," +
                        $" but no profile declared for VS configuration '{configuration.VSName}'");
                }
                return conanProfile;
            }
            catch (Exception)
            {
                errorListService.WriteError($"Error parsing Conan configuration from file '{ConfigFile}'");
                return null;
            }
        }
    }
}
