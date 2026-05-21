using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Protocolo.Framework.Configuration.Providers
{
    public class JsonConfigurationProvider : IConfigurationProvider, ICommitableProvider
    {
        private Dictionary<string, object> m_entries = new Dictionary<string, object>();

        public string Path { get; private set; }

        public JsonConfigurationProvider(string path)
        {
            if (string.IsNullOrEmpty(path)) 
                throw new ArgumentException("path");
                
            Path = path;
        }

        public bool TryGet(string key, out object value)
        {
            return m_entries.TryGetValue(key, out value);
        }

        public void Set(string key, object value)
        {
            m_entries[key] = value;
        }

        public void Load(bool canCreate = true)
        {
            if (!File.Exists(Path))
            {
                if (canCreate)
                    Commit();
                return;
            }

            using (var reader = new JsonTextReader(new StreamReader(File.OpenRead(Path))))
            {
                var serializer = new JsonSerializer();
                var entries = new Dictionary<string, object>();
                reader.Read(); // StartObject '{'
                reader.Read(); // first PropertyName or EndObject
                while (reader.TokenType == JsonToken.PropertyName)
                {
                    var name = (string)reader.Value;
                    reader.Read();
                    entries[name] = reader.TokenType == JsonToken.Integer
                        ? Convert.ToInt32(reader.Value)
                        : (object)serializer.Deserialize(reader);
                    reader.Read();
                }
                m_entries = entries;
            }
        }

        public void Commit()
        {
            try
            {
                using (var file = new FileStream(Path, FileMode.Create))
                    GenerateFile(file);
            }
            catch
            {
                if (File.Exists(Path))
                    File.Delete(Path);
                throw;
            }
        }

        internal void GenerateFile(Stream outputStream)
        {
            using (var sw = new StreamWriter(outputStream, Encoding.UTF8, 1024, leaveOpen: true))
            using (var writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
            {
                writer.WriteStartObject();
                foreach (var entry in m_entries)
                {
                    writer.WritePropertyName(entry.Key);
                    writer.WriteValue(entry.Value);
                }
                writer.WriteEndObject();
            }
        }
    }
}
