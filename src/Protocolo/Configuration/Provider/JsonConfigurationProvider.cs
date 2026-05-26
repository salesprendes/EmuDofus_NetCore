using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

            using (var stream = File.OpenRead(Path))
            {
                var entries = new Dictionary<string, object>();

                using (var document = JsonDocument.Parse(stream))
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        entries[property.Name] = ReadValue(property.Value);
                    }
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
            var options = new JsonWriterOptions { Indented = true };
            using (var writer = new Utf8JsonWriter(outputStream, options))
            {
                writer.WriteStartObject();
                foreach (var entry in m_entries)
                {
                    writer.WritePropertyName(entry.Key);
                    JsonSerializer.Serialize(writer, entry.Value, entry.Value?.GetType() ?? typeof(object));
                }
                writer.WriteEndObject();
            }
        }

        private static object ReadValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.Number:
                    if (value.TryGetInt32(out var intValue))
                        return intValue;
                    if (value.TryGetInt64(out var longValue))
                        return longValue;
                    return value.GetDouble();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return value.GetBoolean();
                case JsonValueKind.Null:
                    return null;
                default:
                    return JsonSerializer.Deserialize<object>(value.GetRawText());
            }
        }
    }
}
