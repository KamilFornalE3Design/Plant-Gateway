using System;

namespace PlantGateway.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class HeaderKeyAttribute : Attribute
    {
        public string Key { get; }
        public HeaderKeyAttribute(string key) => Key = key;
    }
}
