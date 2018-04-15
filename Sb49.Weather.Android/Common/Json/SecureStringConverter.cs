using System;
using System.Text;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Sb49.Security.Core;

namespace Sb49.Weather.Droid.Common.Json
{
    public sealed class SecureStringConverter : JsonConverter
    {
        private readonly IUnityContainer _container;
        private Type _type;

        public SecureStringConverter(IUnityContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var secure = (ISb49SecureString)value;
            if(secure.IsDisposed)
                throw new ObjectDisposedException(nameof(value));

            writer.WriteValue(JsonConvert.SerializeObject(
                new { sb49SecureString = Encoding.UTF8.GetBytes(secure.Decrypt()) }));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var definition = new { sb49SecureString = new byte[0] };
            var value = JsonConvert.DeserializeAnonymousType(reader.Value.ToString(), definition);
            return CreateObject(Encoding.UTF8.GetString(value.sb49SecureString));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ISb49SecureString) || objectType == GetObjectType();
        }

        private IDisposable CreateObject(string src)
        {
            return _container.Resolve<ISb49SecureString>(new DependencyOverride(typeof(string), src));
        }

        private Type GetObjectType()
        {
            if (_type == null)
            {
                using (var src = CreateObject(string.Empty))
                {
                    _type = src.GetType();
                }
            }

            return _type;
        }
    }
}