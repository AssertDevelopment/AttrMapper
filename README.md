# AttrMapper

🚀 Fast attribute-based object mapping for .NET - Map your models to DTOs with decorators, custom converters, and zero configuration

[![NuGet Version](https://img.shields.io/nuget/v/AttrMapper.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AttrMapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AttrMapper.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AttrMapper)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg?style=flat-square&logo=.net)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Build Status](https://img.shields.io/github/actions/workflow/status/AssertDevelopment/AttrMapper/ci.yml?branch=main&style=flat-square&logo=github)](https://github.com/AssertDevelopment/AttrMapper/actions)
[![GitHub Stars](https://img.shields.io/github/stars/AssertDevelopment/AttrMapper?style=flat-square&logo=github)](https://github.com/AssertDevelopment/AttrMapper/stargazers)
[![GitHub Issues](https://img.shields.io/github/issues/AssertDevelopment/AttrMapper?style=flat-square&logo=github)](https://github.com/AssertDevelopment/AttrMapper/issues)

## Why AttrMapper?

**Simple** - No complex configuration or setup. Just add attributes and map!  
**Fast** - Reflection caching ensures high performance after first mapping  
**Intuitive** - Decorators make mapping intent crystal clear  
**Flexible** - Custom converters for any transformation logic  
**Compatible** - Works with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Quick Start

## Installation

**Package Manager Console:**
```powershell
Install-Package AttrMapper
```

**NuGet CLI:**
```bash
nuget install AttrMapper
```

**.NET CLI:**
```bash
dotnet add package AttrMapper
```

**PackageReference (csproj):**
```xml
<PackageReference Include="AttrMapper" Version="1.0.0" />
```

## Basic Usage
### Simple object mapping
```csharp
// Source model
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

// Destination DTO with attributes
public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

// Basic mapping (property names must match)
var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
var dto = AttrMapper.Map<User, UserDto>(user);
```

### Collection Mapping
```csharp
var users = new List<User> { user1, user2, user3 };
var dtos = AttrMapper.Map<User, UserDto>(users);
```
## Features

### 🎯 Attribute-Based Mapping

Control exactly how properties map with intuitive attributes:

```csharp
public class ProductDto
{
    [MapFrom("ProductId")]
    public int Id { get; set; }
    
    [MapFrom("ProductName")]
    public string Name { get; set; }
    
    [MapFrom("Price", typeof(CurrencyFormatter))]
    public string FormattedPrice { get; set; }
    
    [MapIgnore]
    public string CacheKey { get; set; }
}
```

### 🔧 Custom Converters

Create powerful transformations with type-safe converters:

```csharp
public class FullNameConverter : IPropertyConverter<User, string>
{
    public string Convert(User source)
    {
        return $"{source.FirstName} {source.LastName}";
    }
}

public class AgeConverter : IPropertyConverter<DateTime, int>
{
    public int Convert(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

### ⚡ High Performance

Built-in reflection caching ensures fast mappings after the first run:

```csharp
// First mapping: Creates and caches mapper
var dto1 = AttributeMapper.Map<User, UserDto>(user);

// Subsequent mappings: Uses cached mapper (very fast!)
var dto2 = AttributeMapper.Map<User, UserDto>(anotherUser);
```

## Available Attributes

| Attribute | Description | Example |
|-----------|-------------|---------|
| `[MapFrom("PropertyName")]` | Map from different property name | `[MapFrom("EmailAddress")]` |
| `[MapFrom("PropertyName", typeof(Converter))]` | Map with conversion | `[MapFrom("BirthDate", typeof(AgeConverter))]` |
| `[MapWith(typeof(Converter))]` | Apply converter using same property name | `[MapWith(typeof(UpperCaseConverter))]` |
| `[MapIgnore]` | Skip this property during mapping | `[MapIgnore]` |
| `[MapFromType(typeof(SourceType))]` | Specify source type at class level | `[MapFromType(typeof(User))]` |

## Advanced Examples

### Complex Mapping Scenario

```csharp
[MapFromType(typeof(Order))]
public class OrderSummaryDto
{
    public int Id { get; set; }
    
    [MapFrom("Customer.FullName")]
    public string CustomerName { get; set; }
    
    [MapFrom("OrderDate", typeof(DateFormatter))]
    public string FormattedDate { get; set; }
    
    [MapFrom("OrderItems", typeof(ItemCountConverter))]
    public int TotalItems { get; set; }
    
    [MapWith(typeof(CurrencyConverter))]
    public string TotalAmount { get; set; }
    
    [MapIgnore]
    public string ProcessingNotes { get; set; }
}
```

### Collection Mapping

```csharp
var users = GetUsers();
var userDtos = AttributeMapper.Map<User, UserDto>(users);

// Works with any IEnumerable<T>
var activeUserDtos = AttributeMapper.Map<User, UserDto>(
    users.Where(u => u.IsActive)
);
```

### Error Handling

```csharp
try
{
    var dto = AttributeMapper.Map<User, UserDto>(user);
}
catch (MappingException ex)
{
    Console.WriteLine($"Mapping failed: {ex.Message}");
    // Handle specific mapping errors
}
```

## Performance

AttrMapper is designed for high performance:

- **Reflection caching** - Mappers are compiled once and cached
- **Minimal allocations** - Efficient object creation
- **Type safety** - Compile-time checking where possible

Benchmark results (1000 mappings):
- First run: ~50ms (includes reflection setup)
- Cached runs: ~5ms (uses compiled mapper)

## Comparison with AutoMapper

| Feature | AttrMapper | AutoMapper |
|---------|------------|------------|
| **Configuration** | Attribute-based (declarative) | Code-based (imperative) |
| **Setup** | Zero configuration | Requires profile setup |
| **Performance** | Fast with caching | Fast with configuration |
| **Type Safety** | Compile-time attributes | Runtime configuration |
| **Learning Curve** | Minimal | Moderate |
| **Flexibility** | High with converters | Very high |

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch
3. Add tests for your changes
4. Ensure all tests pass
5. Submit a pull request

## Building from Source

```bash
git clone https://github.com/AssertDevelopment/AttrMapper.git
cd AttrMapper
dotnet restore
dotnet build
dotnet test
```

## Examples

Check out the `/samples` directory for complete examples:

- **Console Application** - Basic usage examples
- **Web API** - Integration with ASP.NET Core
- **Performance Tests** - Benchmarking and optimization

## Roadmap

- [ ] Nested object mapping
- [ ] Collection element transformation
- [ ] Conditional mapping
- [ ] Source generators for compile-time mapping
- [ ] Advanced debugging tools

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [Documentation](https://github.com/AssertDevelopment/AttrMapper/wiki)
- 🐛 [Issues](https://github.com/AssertDevelopment/AttrMapper/issues)
- 💬 [Discussions](https://github.com/AssertDevelopment/AttrMapper/discussions)
- 📦 [NuGet Package](https://www.nuget.org/packages/AttrMapper)

---

<div align="center">

⭐ **Like AttrMapper?** Give us a star on GitHub to show your support!

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Support%20Development-FFDD00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/assertdev)

**Made with ❤️ by [AssertDevelopment](https://github.com/AssertDevelopment)**

</div>
