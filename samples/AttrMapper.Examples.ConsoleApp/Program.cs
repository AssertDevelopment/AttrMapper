using System;
using System.Collections.Generic;
using AttrMapper.Attributes;
using AttrMapper.Interfaces;

namespace AttrMapper.Examples.ConsoleApp
{
    // Example source model (Normally in API project and not in Shared or client)
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
        public string PasswordHash { get; set; }
    }

    // Custom Converters
    public class NameConverter : IPropertyConverter<(string FirstName, string LastName), string>
    {
        public string ConvertTo((string FirstName, string LastName) source)
        {
            return $"{source.FirstName} {source.LastName}";
        }

        public (string FirstName, string LastName) ConvertFrom(string fullName)
        {
            var parts = fullName?.Split(' ', 2) ?? new string[0];
            return (
                FirstName: parts.Length > 0 ? parts[0] : "",
                LastName: parts.Length > 1 ? parts[1] : ""
            );
        }
    }

    public class AgeConverter : IPropertyConverter<DateTime, int>
    {
        public int ConvertTo(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        public DateTime ConvertFrom(int age)
        {
            return DateTime.Today.AddYears(-age);
        }
    }

    // One-way converter (ConvertFrom can throw or return default)
    public class EmailMaskConverter : IPropertyConverter<string, string>
    {
        public string ConvertTo(string email)
        {
            if (string.IsNullOrEmpty(email)) return email;
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            
            var localPart = parts[0];
            var maskedLocal = localPart.Length > 2 
                ? $"{localPart[0]}***{localPart[localPart.Length - 1]}"
                : "***";
            
            return $"{maskedLocal}@{parts[1]}";
        }

        public string ConvertFrom(string maskedEmail)
        {
            throw new NotSupportedException("Cannot unmask email address");
        }
    }

    public class SalaryFormatter : IPropertyConverter<decimal, string>
    {
        public string ConvertTo(decimal salary)
        {
            return $"${salary:N2}";
        }
        
        public decimal ConvertFrom(string formattedSalary)
        {
            return decimal.Parse(formattedSalary.Replace("$", ""));
        }
    }

    // Destination DTO with attributes
    public class UserDto
    {
        public int Id { get; set; }
        
        [Map("FirstName, LastName", typeof(NameConverter))]
        public string FullName { get; set; }
        
        [Map("Email")]
        public string EmailAddress { get; set; }
        
        [Map("BirthDate", typeof(AgeConverter))]
        public int Age { get; set; }
        
        [Map("Salary", typeof(SalaryFormatter))]
        public string FormattedSalary { get; set; }
        
        public bool IsActive { get; set; }
        
        [MapIgnore]
        public string InternalNotes { get; set; }
    }

    // Simple DTO for basic mapping example
    public class SimpleUserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class CreateUserDto
    {
        [Map("FirstName")]
        public string GivenName { get; set; }
        
        [Map("LastName")]
        public string FamilyName { get; set; }
        
        public string Email { get; set; }
        
        [Map("BirthDate")]
        public DateTime DateOfBirth { get; set; }
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        
        [Map("FirstName, LastName", typeof(NameConverter))]
        public string DisplayName { get; set; }
        
        [Map("Email", typeof(EmailMaskConverter))]
        public string MaskedEmail { get; set; }
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== AttrMapper Console Examples ===\n");

            // Create sample data
            var user1 = new User
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                BirthDate = new DateTime(1990, 5, 15),
                Email = "john.doe@example.com",
                IsActive = true,
                Salary = 75000.50m,
                PasswordHash = "secret123"
            };

            var user2 = new User
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                BirthDate = new DateTime(1985, 8, 22),
                Email = "jane.smith@example.com",
                IsActive = false,
                Salary = 92000.75m,
                PasswordHash = "secret12322"
            };

            // Example 1: Basic mapping (property names match)
            Console.WriteLine("=== Example 1: Basic Mapping ===");
            var simpleDto = AttrMapper.Map<User, SimpleUserDto>(user1);
            
            Console.WriteLine($"ID: {simpleDto.Id}");
            Console.WriteLine($"First Name: {simpleDto.FirstName}");
            Console.WriteLine($"Last Name: {simpleDto.LastName}");
            Console.WriteLine($"Email: {simpleDto.Email}");
            Console.WriteLine($"Is Active: {simpleDto.IsActive}");
            Console.WriteLine();

            // Example 2: Advanced mapping with attributes and converters
            Console.WriteLine("=== Example 2: Advanced Mapping with Attributes ===");
            var advancedDto = AttrMapper.Map<User, UserDto>(user1);
            
            Console.WriteLine($"ID: {advancedDto.Id}");
            Console.WriteLine($"Full Name: {advancedDto.FullName}");                    // Uses FullNameConverter
            Console.WriteLine($"Age: {advancedDto.Age}");                               // Uses AgeConverter
            Console.WriteLine($"Email Address: {advancedDto.EmailAddress}");            // Maps from "Email"
            Console.WriteLine($"Formatted Salary: {advancedDto.FormattedSalary}");      // Uses SalaryFormatter
            Console.WriteLine($"Is Active: {advancedDto.IsActive}");
            Console.WriteLine($"Internal Notes: {advancedDto.InternalNotes ?? "null"}"); // Should be null (MapIgnore)
            Console.WriteLine();

            // Example 3: Collection mapping
            Console.WriteLine("=== Example 3: Collection Mapping ===");
            var users = new List<User> { user1, user2 };
            var userDtos = AttrMapper.Map<User, UserDto>(users);

            Console.WriteLine($"Mapped {userDtos.Count} users:");
            foreach (var dto in userDtos)
            {
                Console.WriteLine($"  - {dto.FullName} (Age: {dto.Age}, Salary: {dto.FormattedSalary})");
            }
            Console.WriteLine();
            
            // Example 4: DTO → Model (Reverse direction with same attributes!)
            Console.WriteLine("=== Example 4: DTO → Model (Bidirectional) ===");
            advancedDto.FullName = "Jane Smith";
            advancedDto.Age = 25;
            
            var userFromDto = AttrMapper.Map<UserDto, User>(advancedDto);
            Console.WriteLine($"First Name: {userFromDto.FirstName}");
            Console.WriteLine($"Last Name: {userFromDto.LastName}");
            Console.WriteLine($"Birth Date: {userFromDto.BirthDate:yyyy-MM-dd}");
            Console.WriteLine();

            // Example 5: CreateDto → Model (Source-driven by DTO attributes)
            Console.WriteLine("=== Example 5: CreateDto → Model ===");
            var createDto = new CreateUserDto
            {
                GivenName = "Alice",
                FamilyName = "Johnson",
                Email = "alice.johnson@example.com",
                DateOfBirth = new DateTime(1988, 3, 10)
            };
            
            var newUser = AttrMapper.Map<CreateUserDto, User>(createDto);
            Console.WriteLine($"Created: {newUser.FirstName} {newUser.LastName}");
            Console.WriteLine($"Email: {newUser.Email}");
            Console.WriteLine();

            // Example 6: Model → Summary (with masking)
            Console.WriteLine("=== Example 6: Model → Summary (with Email Masking) ===");
            var summary = AttrMapper.Map<User, UserSummaryDto>(user2);
            Console.WriteLine($"Name: {summary.DisplayName}");
            Console.WriteLine($"Masked Email: {summary.MaskedEmail}");
            Console.WriteLine();

            // Example 7: Simple property matching (no attributes)
            Console.WriteLine("=== Example 7: Convention-Based Mapping ===");
            var simplceDto = AttrMapper.Map<User, dynamic>(user1);
            Console.WriteLine();

            // Example 8: Error handling
            Console.WriteLine("=== Example 8: Error Handling ===");
            try
            {
                // This should work fine
                var validMapping = AttrMapper.Map<User, UserDto>(user1);
                Console.WriteLine("✅ Valid mapping successful");

                // Uncomment the next lines to test error handling
                // This would throw an error if you tried to map from a different type
                // var invalidMapping = AttributeMapper.Map<SimpleUserDto, UserDto>(simpleDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Mapping error: {ex.Message}");
            }
            Console.WriteLine();

            // Example 9: Performance with caching
            AttrMapper.ClearCache();
            
            Console.WriteLine("=== Example 9: Performance Test (Caching) ===");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // First mapping (creates and caches the mapper)
            for (int i = 0; i < 1000; i++)
            {
                var dto = AttrMapper.Map<User, UserDto>(user1);
            }
            
            stopwatch.Stop();
            Console.WriteLine($"1000 mappings took: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine("(First run includes reflection setup + caching)");
            
            // Second run (uses cached mapper)
            stopwatch.Restart();
            for (int i = 0; i < 1000; i++)
            {
                var dto = AttrMapper.Map<User, UserDto>(user1);
            }
            stopwatch.Stop();
            Console.WriteLine($"Second 1000 mappings took: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine("(Uses cached mapper - should be faster)");
            Console.WriteLine();

            // Example 10: Null handling
            Console.WriteLine("=== Example 10: Null Handling ===");
            User nullUser = null;
            var nullResult = AttrMapper.Map<User, UserDto>(nullUser);
            Console.WriteLine($"Mapping null user result: {nullResult?.ToString() ?? "null"}");
            Console.WriteLine();

            Console.WriteLine("=== All Examples Complete! ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}