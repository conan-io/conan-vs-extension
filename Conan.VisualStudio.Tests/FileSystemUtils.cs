using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Conan.VisualStudio.Tests
{
    internal static class FileSystemUtils
    {
        public static string CreateTempDirectory()
        {
            var path = Path.GetTempFileName();
            File.Delete(path);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string CreateTempFile(string directory, string name)
        {
            var path = Path.Combine(directory, name);
            File.Create(path).Close();
            return path;
        }

        // Create a User object and serialize it to a JSON stream.  
        public static string WriteFromObject<T>(T obj)
        {
            //Create a stream to serialize the object to.  
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.  
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            ser.WriteObject(ms, obj);
            byte[] json = ms.ToArray();
            ms.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
        }

        // Deserialize a JSON stream to a User object.  
        public static T ReadToObject<T>(string json) where T : class, new()
        {
            T deserialized = new T();
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(deserialized.GetType());
            deserialized = ser.ReadObject(ms) as T;
            ms.Close();
            return deserialized;
        }

        public static string ReadTestFile(string filePath)
        {
            return File.ReadAllText(Path.Combine(@"..\..\..\Conan.VisualStudio.Tests", filePath));
        }
    }
}
