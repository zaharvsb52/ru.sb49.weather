using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Common.Json
{
    public sealed class ShouldSerializeContractResolver : DefaultContractResolver
    {
        internal static IContractResolver Instance { get; } = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(WeatherForecast))
            {
                property.ShouldSerialize =
                    instance => !WeatherForecast.NonSerializedProperties.Contains(property.PropertyName);
            }

            return property;
        }
    }
}