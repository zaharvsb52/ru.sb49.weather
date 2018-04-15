using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Sb49.Weather.Droid.Model
{
    //http://www.newtonsoft.com/json/help/html/CustomContractResolver.htm
    public class LocationAddressDynamicContractResolver : DefaultContractResolver
    {
        public LocationAddressDynamicContractResolver(string[] excludeProperties)
        {
            ExcludeProperties = excludeProperties;
        }

        public string[] ExcludeProperties { get; }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            if (ExcludeProperties == null || ExcludeProperties.Length == 0)
                return properties;

            properties = properties.Where(p => !ExcludeProperties.Contains(p.PropertyName)).ToList();
            return properties;
        }
    }
}