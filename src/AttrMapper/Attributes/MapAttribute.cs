using System;
using System.Linq;

namespace AttrMapper.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapAttribute : Attribute
    {
        public string[] PropertyNames { get; }
        public Type ConverterType { get; }

        // Map with converter only
        public MapAttribute(Type converterType)
        {
            ConverterType = converterType ?? throw new ArgumentNullException(nameof(converterType));
        }

        // Map to different property name
        public MapAttribute(string propertyName)
        {
            PropertyNames = ToPropertyNames(propertyName);
        }

        // Map to different property name with converter
        public MapAttribute(string propertyName, Type converterType)
        {
            PropertyNames = ToPropertyNames(propertyName);
            ConverterType = converterType;
        }

        private string[] ToPropertyNames(string propertyName)
        {
            return propertyName?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray() ?? Array.Empty<string>();
        }
    }
}