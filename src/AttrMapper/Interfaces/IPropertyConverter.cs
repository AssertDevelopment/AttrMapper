namespace AttrMapper.Interfaces
{
    public interface IPropertyConverter<TSource, TDestination>
    {
        TDestination ConvertTo(TSource source);
        TSource ConvertFrom(TDestination destination);
    }
}