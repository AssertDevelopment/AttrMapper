using System;

namespace AttrMapper.Helpers
{
    internal class MappingInfo
    {
        public MappingDirection Direction { get; set; }
        public Type SourceType { get; set; }
        public Type DestinationType { get; set; }
        public bool HasSourceMappingAttributes { get; set; }
        public bool HasDestinationMappingAttributes { get; set; }
    }
}