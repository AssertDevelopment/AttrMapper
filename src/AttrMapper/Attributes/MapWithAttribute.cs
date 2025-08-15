using System;

namespace AttrMapper.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapWithAttribute : Attribute
    {
        public Type ConverterType { get; }

        public MapWithAttribute(Type converterType)
        {
            ConverterType = converterType ?? throw new ArgumentNullException(nameof(converterType));
        }
    }
}