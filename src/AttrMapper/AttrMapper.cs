using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AttrMapper.Attributes;
using AttrMapper.Exceptions;
using AttrMapper.Helpers;
using AttrMapper.Interfaces;

namespace AttrMapper
{
    public static class AttrMapper
    {
        private static readonly Dictionary<string, Func<object, object>> _cachedMappers 
            = new Dictionary<string, Func<object, object>>();

        public static TTarget Map<TSource, TTarget>(TSource source)
            where TTarget : new()
        {
            if (source == null) return default(TTarget);

            if (typeof(TTarget) == typeof(object) || typeof(TTarget).Name == "Object")
            {
                return (TTarget)MapToDynamic<TSource>(source);
            }

            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var cacheKey = $"{sourceType.FullName}→{targetType.FullName}";

            // Check cache first
            if (_cachedMappers.TryGetValue(cacheKey, out var cachedMapper))
            {
                return (TTarget)cachedMapper(source);
            }

            // Create mapper based on direction
            var mapper = CreateMapper<TSource, TTarget>();
        
            // Cache the mapper
            _cachedMappers[cacheKey] = src => mapper((TSource)src);

            return mapper(source);
        }

        public static List<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> sources)
            where TTarget : new()
        {
            return sources?.Select(Map<TSource, TTarget>).ToList() ?? new List<TTarget>();
        }
        
        private static object MapToDynamic<TSource>(TSource source)
        {
            var sourceType = typeof(TSource);
            var sourceProperties = GetPropertiesSafely(sourceType, BindingFlags.Public | BindingFlags.Instance);
    
            var expandoDict = new Dictionary<string, object>();
    
            foreach (var prop in sourceProperties.Values)
            {
                try
                {
                    var value = prop.GetValue(source);
                    if (value != null)
                    {
                        expandoDict[prop.Name] = value;
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle property access errors
                    Console.WriteLine($"Error accessing property {prop.Name}: {ex.Message}");
                }
            }
    
            // Convert to ExpandoObject for true dynamic behavior
            var expando = new System.Dynamic.ExpandoObject();
            var expandoCollection = (ICollection<KeyValuePair<string, object>>)expando;
    
            foreach (var kvp in expandoDict)
            {
                expandoCollection.Add(kvp);
            }
    
            return expando;
        }

        private static Func<TSource, TTarget> CreateMapper<TSource, TTarget>()
            where TTarget : new()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            // Get properties with duplicate name handling
            var sourceProperties = GetPropertiesSafely(sourceType, BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = GetPropertiesSafely(targetType, BindingFlags.Public | BindingFlags.Instance)
                .Where(kvp => kvp.Value.CanWrite)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);


            // Check which type has mapping attributes
            var sourceHasMapAttributes = sourceProperties.Values.Any(HasMappingAttributes);
            var targetHasMapAttributes = targetProperties.Values.Any(HasMappingAttributes);

            if (targetHasMapAttributes && sourceHasMapAttributes)
            {
                // Both have attributes
                Console.WriteLine("Both source and target have Map attributes. Using target-driven mapping.");
                return CreateTargetDrivenMapper<TSource, TTarget>(sourceProperties, targetProperties);
            }
            
            if (targetHasMapAttributes)
            {
                return CreateTargetDrivenMapper<TSource, TTarget>(sourceProperties, targetProperties);
            }
            
            if (sourceHasMapAttributes)
            {
                return CreateSourceDrivenMapper<TSource, TTarget>(sourceProperties, targetProperties);
            }
            
            return CreateConventionMapper<TSource, TTarget>(sourceProperties, targetProperties);
        }
    
        private static Dictionary<string, PropertyInfo> GetPropertiesSafely(Type type, BindingFlags bindingFlags)
        {
            var properties = type.GetProperties(bindingFlags);
            var result = new Dictionary<string, PropertyInfo>();
        
            foreach (var prop in properties)
            {
                if (!result.ContainsKey(prop.Name))
                {
                    result[prop.Name] = prop;
                }
                // Optionally log duplicate property names if needed
            }
        
            return result;
        }

        private static bool HasMappingAttributes(PropertyInfo property)
        {
            return property.GetCustomAttribute<MapAttribute>() != null ||
                   property.GetCustomAttribute<MapIgnoreAttribute>() != null;
        }

        private static Func<TSource, TTarget> CreateTargetDrivenMapper<TSource, TTarget>(
            Dictionary<string, PropertyInfo> sourceProperties,
            Dictionary<string, PropertyInfo> targetProperties)
            where TTarget : new()
        {
            return source =>
            {
                var target = new TTarget();

                foreach (var targetProp in targetProperties.Values)
                {
                    try
                    {
                        // Check if property should be ignored
                        if (targetProp.GetCustomAttribute<MapIgnoreAttribute>() != null)
                            continue;

                        var mapAttr = targetProp.GetCustomAttribute<MapAttribute>();
                        PropertyInfo sourceProp = null;
                        object sourceValue = null;

                        if (mapAttr != null && mapAttr.PropertyNames != null && mapAttr.PropertyNames.Length > 0)
                        {
                            // Multi-property mapping (e.g., FirstName + LastName → FullName)
                            var sourceValues = new List<object>();
                            var allPropertiesFound = true;

                            foreach (var propName in mapAttr.PropertyNames)
                            {
                                if (sourceProperties.TryGetValue(propName, out var prop))
                                {
                                    sourceValues.Add(prop.GetValue(source));
                                }
                                else
                                {
                                    allPropertiesFound = false;
                                    break;
                                }
                            }

                            if (allPropertiesFound)
                            {
                                // Create tuple or array based on converter expectation
                                sourceValue = CreateMultiPropertyValue(sourceValues, mapAttr.ConverterType);
                            }
                        }
                        else
                        {
                            // Map from same property name
                            sourceProperties.TryGetValue(targetProp.Name, out sourceProp);
                        }

                        // Get single property value if not multi-property
                        ProcessPropertyValue(source, sourceProp, sourceValue, mapAttr, targetProp, target);
                    }
                    catch (Exception ex)
                    {
                        throw new MappingException($"Error mapping property {targetProp.Name}: {ex.Message}", ex);
                    }
                }

                return target;
            };
        }

        private static Func<TSource, TTarget> CreateSourceDrivenMapper<TSource, TTarget>(
            Dictionary<string, PropertyInfo> sourceProperties,
            Dictionary<string, PropertyInfo> targetProperties)
            where TTarget : new()
        {
            return source =>
            {
                var target = new TTarget();

                foreach (var sourceProp in sourceProperties.Values)
                {
                    try
                    {
                        // Check if property should be ignored
                        if (sourceProp.GetCustomAttribute<MapIgnoreAttribute>() != null)
                            continue;

                        var mapAttr = sourceProp.GetCustomAttribute<MapAttribute>();
                        PropertyInfo targetProp = null;
                        object sourceValue = null;
                        
                        if (mapAttr != null && mapAttr.PropertyNames != null && mapAttr.PropertyNames.Length > 1)
                        {
                            // One-to-many mapping (e.g., FullName → FirstName + LastName)
                            // The source property contains the value, target properties are specified in PropertyNames
                            sourceValue = sourceProp.GetValue(source);
    
                            if (sourceValue != null && mapAttr.ConverterType != null)
                            {
                                // Use converter to split the single source value into multiple target values
                                var convertedResult = ApplyConverter(sourceValue, mapAttr.ConverterType, 
                                    sourceProp.PropertyType, null); // We don't know target type yet
        
                                // The converter should return a tuple or array that we can decompose
                                MapToMultipleTargetProperties(convertedResult, mapAttr.PropertyNames, 
                                    targetProperties, target);
                            }
                        }
                        else if (mapAttr != null && mapAttr.PropertyNames != null && mapAttr.PropertyNames.Length == 1)
                        {
                            // Map from defined property name
                            targetProperties.TryGetValue(mapAttr.PropertyNames[0], out targetProp);
                            sourceValue = sourceProp.GetValue(source);
                            
                            // Get single property value if not multi-property
                            ProcessPropertyValue(source, sourceProp, sourceValue, mapAttr, targetProp, target);
                        }
                        else
                        {
                            // Map from same property name
                            targetProperties.TryGetValue(sourceProp.Name, out targetProp);
                            sourceValue = sourceProp.GetValue(source);
                            
                            // Get single property value if not multi-property
                            ProcessPropertyValue(source, sourceProp, sourceValue, mapAttr, targetProp, target);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new MappingException($"Error mapping property {sourceProp.Name}: {ex.Message}", ex);
                    }
                }

                return target;
            };
        }
        
        private static void ProcessPropertyValue(object source, PropertyInfo sourceProp, object sourceValue, 
            MapAttribute mapAttr, PropertyInfo targetProp, object target)
        {
            if (sourceProp != null && sourceValue == null)
            {
                sourceValue = sourceProp.GetValue(source);
            }
    
            if (sourceValue != null && mapAttr?.ConverterType != null)
            {
                sourceValue = ApplyConverter(sourceValue, mapAttr.ConverterType, sourceValue.GetType(), targetProp.PropertyType);
            }
            else if (sourceValue != null)
            {
                sourceValue = ConvertValue(sourceValue, targetProp.PropertyType);
            }
    
            if (sourceValue != null)
            {
                targetProp.SetValue(target, sourceValue);
            }
        }

        private static void MapToMultipleTargetProperties(object convertedResult, string[] targetPropertyNames, 
            Dictionary<string, PropertyInfo> targetProperties, object target)
        {
            if (convertedResult == null) return;
    
            var resultType = convertedResult.GetType();
    
            // Handle ValueTuple results
            if (resultType.IsGenericType && IsValueTupleType(resultType.GetGenericTypeDefinition()))
            {
                var tupleValues = ExtractTupleValues(convertedResult);
                SetTargetProperties(tupleValues, targetPropertyNames, targetProperties, target);
            }
            // Handle array results
            else if (resultType.IsArray)
            {
                var arrayValues = ((Array)convertedResult).Cast<object>().ToArray();
                SetTargetProperties(arrayValues, targetPropertyNames, targetProperties, target);
            }
            // Handle IEnumerable results
            else if (convertedResult is System.Collections.IEnumerable enumerable && 
                     !(convertedResult is string))
            {
                var enumerableValues = enumerable.Cast<object>().ToArray();
                SetTargetProperties(enumerableValues, targetPropertyNames, targetProperties, target);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Converter returned {resultType.Name}, but expected tuple, array, or enumerable for multi-property mapping");
            }
        }

        private static void SetTargetProperties(object[] values, string[] targetPropertyNames, 
            Dictionary<string, PropertyInfo> targetProperties, object target)
        {
            if (values.Length != targetPropertyNames.Length)
            {
                throw new InvalidOperationException(
                    $"Converter returned {values.Length} values, but {targetPropertyNames.Length} target properties were specified");
            }
    
            for (int i = 0; i < targetPropertyNames.Length; i++)
            {
                var propertyName = targetPropertyNames[i];
                if (targetProperties.TryGetValue(propertyName, out var targetProp))
                {
                    var convertedValue = ConvertValue(values[i], targetProp.PropertyType);
                    targetProp.SetValue(target, convertedValue);
                }
                else
                {
                    throw new InvalidOperationException($"Target property '{propertyName}' not found");
                }
            }
        }

        private static Func<TSource, TTarget> CreateConventionMapper<TSource, TTarget>(
            Dictionary<string, PropertyInfo> sourceProperties,
            Dictionary<string, PropertyInfo> targetProperties)
            where TTarget : new()
        {
            return source =>
            {
                var target = new TTarget();

                foreach (var targetProp in targetProperties.Values)
                {
                    try
                    {
                        if (sourceProperties.TryGetValue(targetProp.Name, out var sourceProp))
                        {
                            var sourceValue = sourceProp.GetValue(source);
                            if (sourceValue != null)
                            {
                                sourceValue = ConvertValue(sourceValue, targetProp.PropertyType);
                                targetProp.SetValue(target, sourceValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new MappingException($"Error mapping property {targetProp.Name}: {ex.Message}", ex);
                    }
                }

                return target;
            };
        }

        private static object ApplyConverter(object value, Type converterType, Type actualSourceType
            , Type targetType)
        {
            var converter = Activator.CreateInstance(converterType);

            // Find the IConverter<,> interface that matches our types
            var converterInterface = converterType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPropertyConverter<,>));

            if (converterInterface == null)
            {
                throw new InvalidOperationException($"Converter {converterType.Name} must implement IConverter<T1, T2>");
            }

            var genericArgs = converterInterface.GetGenericArguments();
            var converterSourceType = genericArgs[0];
            var converterTargetType = genericArgs[1];
            
            // For multi-property scenarios, use the actual type of the value passed in
            var sourceTypeToCheck = actualSourceType ?? value?.GetType();
            
            // When targetType is null (one-to-many scenario), determine direction based on source type only
            if (targetType == null)
            {
                if (IsCompatibleType(sourceTypeToCheck, converterSourceType))
                {
                    // Forward direction: use ConvertTo()
                    var convertMethod = converterInterface.GetMethod("ConvertTo");
                    return convertMethod.Invoke(converter, new[] { value });
                }
                else if (IsCompatibleType(sourceTypeToCheck, converterTargetType))
                {
                    // Reverse direction: use ConvertFrom()
                    var convertBackMethod = converterInterface.GetMethod("ConvertFrom");
                    return convertBackMethod.Invoke(converter, new[] { value });
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Converter {converterType.Name} (IPropertyConverter<{converterSourceType.Name}, {converterTargetType.Name}>) " +
                        $"cannot convert from {sourceTypeToCheck?.Name ?? "null"}. Source type must match either TSource or TDestination.");
                }
            }

            // Determine which method to call based on our mapping direction
            if (IsCompatibleType(sourceTypeToCheck, converterSourceType) && IsCompatibleType(targetType, converterTargetType))
            {
                // Forward direction: source → target, use ConvertTo()
                var convertMethod = converterInterface.GetMethod("ConvertTo");
                return convertMethod.Invoke(converter, new[] { value });
            }
            else if (IsCompatibleType(sourceTypeToCheck, converterTargetType) && IsCompatibleType(targetType, converterSourceType))
            {
                // Reverse direction: source → target where source is T2 and target is T1, use ConvertFrom()
                var convertBackMethod = converterInterface.GetMethod("ConvertFrom");
                return convertBackMethod.Invoke(converter, new[] { value });
            }
            else
            {
                throw new InvalidOperationException(
                    $"Converter {converterType.Name} (IPropertyConverter<{converterSourceType.Name}, {converterTargetType.Name}>) " +
                    $"cannot convert from {sourceTypeToCheck?.Name ?? "null"} to {targetType.Name}");
            }
        }

        private static bool IsCompatibleType(Type actualType, Type expectedType)
        {
            if (actualType == null || expectedType == null) return false;
    
            return actualType == expectedType || 
                   expectedType.IsAssignableFrom(actualType) ||
                   // Handle tuple types specially
                   (actualType.IsGenericType && expectedType.IsGenericType &&
                    actualType.GetGenericTypeDefinition() == expectedType.GetGenericTypeDefinition());
        }


        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;

            var valueType = value.GetType();
        
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }

            // Same type, no conversion needed
            if (valueType == targetType || targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            // String conversions
            if (targetType == typeof(string))
            {
                return value.ToString();
            }

            // Enum conversions
            if (targetType.IsEnum)
            {
                if (valueType == typeof(string))
                {
                    return Enum.Parse(targetType, (string)value, true);
                }
                return Enum.ToObject(targetType, value);
            }

            // Use Convert.ChangeType for primitive types
            if (targetType.IsPrimitive || targetType == typeof(decimal) || targetType == typeof(DateTime) || targetType == typeof(Guid))
            {
                return Convert.ChangeType(value, targetType);
            }

            return value;
        }
        
        private static object CreateMultiPropertyValue(List<object> values, Type converterType)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (converterType == null) return values.Count == 1 ? values[0] : values.ToArray();

            // Find the IConverter interface to determine expected input type
            var converterInterface = converterType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPropertyConverter<,>));

            if (converterInterface == null)
            {
                throw new InvalidOperationException($"Converter {converterType.Name} must implement IConverter<TSource, TDestination>");
            }

            var inputType = converterInterface.GetGenericArguments()[0];

            // Handle different input types
            if (inputType.IsGenericType && inputType.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
            {
                // Create tuple for two values
                if (values.Count == 2)
                {
                    var tupleGenericArgs = inputType.GetGenericArguments();
                    var tupleType = typeof(ValueTuple<,>).MakeGenericType(tupleGenericArgs[0], tupleGenericArgs[1]);

                    return Activator.CreateInstance(tupleType, values[0], values[1]);
                }
            }
            else if (inputType.IsArray)
            {
                // Create array
                var elementType = inputType.GetElementType();
                var array = Array.CreateInstance(elementType, values.Count);
                for (int i = 0; i < values.Count; i++)
                {
                    array.SetValue(values[i], i);
                }
                return array;
            }

            // Fallback: return the values as-is (for custom handling)
            return values.Count == 1 ? values[0] : values.ToArray();
        }
        
        private static object[] ExtractTupleValues(object tuple)
        {
            var tupleType = tuple.GetType();
            var fields = tupleType.GetFields();
    
            // Handle regular tuples (Item1, Item2, etc.)
            if (fields.Any(f => f.Name.StartsWith("Item")))
            {
                return fields
                    .Where(f => f.Name.StartsWith("Item"))
                    .OrderBy(f => int.Parse(f.Name.Substring(4))) // Sort by Item number
                    .Select(f => f.GetValue(tuple))
                    .ToArray();
            }
    
            // Handle named tuples or other tuple-like structures
            return fields.Select(f => f.GetValue(tuple)).ToArray();
        }

        private static bool IsValueTupleType(Type genericTypeDefinition)
        {
            return genericTypeDefinition == typeof(ValueTuple<,>) ||
                   genericTypeDefinition == typeof(ValueTuple<,,>) ||
                   genericTypeDefinition == typeof(ValueTuple<,,,>) ||
                   genericTypeDefinition == typeof(ValueTuple<,,,,>) ||
                   genericTypeDefinition == typeof(ValueTuple<,,,,,>) ||
                   genericTypeDefinition == typeof(ValueTuple<,,,,,,>) ||
                   genericTypeDefinition == typeof(ValueTuple<,,,,,,,>);
        }

        public static void ClearCache()
        {
            _cachedMappers.Clear();
        }
    }
}