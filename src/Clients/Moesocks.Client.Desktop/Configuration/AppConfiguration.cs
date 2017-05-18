using Moesocks.Client.Services.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client.Configuration
{
    class AppConfiguration
    {
        private readonly string _fileName;
        private readonly JObject _allConfig;
        private readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        public UpdateConfiguration Update { get; }

        public AppConfiguration(string fileName)
        {
            _fileName = fileName;

            using (var reader = new JsonTextReader(File.OpenText(fileName)) { CloseInput = true })
                _allConfig = JObject.Load(reader, new JsonLoadSettings
                {
                    CommentHandling = CommentHandling.Load,
                    LineInfoHandling = LineInfoHandling.Load
                });

            Update = LoadConfiguration<UpdateConfiguration>();
        }

        private T LoadConfiguration<T>() where T : ConfigurationBase, new()
        {
            var sectionName = typeof(T).GetCustomAttribute<ConfigurationSectionNameAttribute>().SectionName;
            T config;
            if (_allConfig.TryGetValue(sectionName, out var value))
                config = value.ToObject<T>(_serializer);
            else
                config = new T();
            config.Saving += Config_Saving;
            config.AutoSave = true;
            return config;
        }

        private void Config_Saving(object sender, EventArgs e)
        {
            var sectionName = sender.GetType().GetCustomAttribute<ConfigurationSectionNameAttribute>().SectionName;
            var newValue = JToken.FromObject(sender, _serializer);
            if (_allConfig.TryGetValue(sectionName, out var oldValue))
                oldValue.Replace(newValue);
            else
                _allConfig.Add(sectionName, newValue);

            using (var writer = new JsonTextWriter(new StreamWriter(_fileName))
            {
                CloseOutput = true,
                Formatting = Formatting.Indented
            })
                _allConfig.WriteTo(writer);
        }
    }
}
