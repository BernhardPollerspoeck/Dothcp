# GitHub Copilot Coding Guidelines for Dothcp Project

This file outlines the coding guidelines and standards that GitHub Copilot should follow when contributing code to the Dothcp project. These guidelines are based on identified code style mistakes and preferred practices.

## General Principles

1. **Follow .NET 9.0 Standards**: Always target .NET 9.0 and use the latest framework features.

## Class Structure and Design

1. **No Static Classes**: Avoid static classes. Instead, create services with backing interfaces.
   - ❌ `public static class NetworkUtilities {...}`
   - ✅ `public interface INetworkUtilityService {...}` and `public class NetworkUtilityService : INetworkUtilityService {...}`

2. **One Class Per File**: Each class, interface, or enum should be in its own file.
   - ❌ Multiple classes/enums in one file
   - ✅ Single class/enum per file with matching filename

3. **Constructor Injection**: Use constructor injection rather than service locator pattern.
   - ❌ Injecting `IServiceProvider` and resolving services at runtime
   - ✅ Injecting specific required services directly in constructor

## Dependency Injection

1. **Direct Interface Injection**: Inject specific interfaces rather than generic service providers.
   - ❌ `public class MyClass(IServiceProvider serviceProvider)`
   - ✅ `public class MyClass(IMyService myService)`

2. **Register Services Properly**: All services should be registered in the DI container.
   - ✅ `builder.Services.AddTransient<IMyService, MyServiceImplementation>();`

## Assumptions and Configuration

1. **No Hardcoded Network Values**: Don't assume network configurations; use settings.
   - ❌ Hardcoded IP ranges, subnet masks, etc.
   - ✅ Load network configuration from settings

2. **No Framework or Package Downgrades**: Never downgrade framework versions or NuGet packages.
   - ❌ Changing from net9.0 to net8.0 or downgrading package versions
   - ✅ Maintain the specified versions

## Implementation Guidelines

1. **Always Use Production-Ready Code**: Never include code marked as non-production.
   - ❌ Comments like "This is a non-production method"
   - ✅ Properly implemented, tested, and robust solutions

2. **Property Injection vs Fields**: When using primary constructors, prefer direct parameter usage over assigning to fields/properties.
   - ❌ Redundant field/property assignments with primary constructors
   - ✅ Direct usage of constructor-injected parameters

3. **Method Naming**: Use clear, descriptive method names that indicate the action being performed.

4. **Proper Error Handling**: Include appropriate error handling and input validation.

5. **Documentation**: Add XML documentation for public APIs and complex logic.

## Testing

1. **Comprehensive Unit Testing**: Ensure all functionality has appropriate test coverage.
2. **Test With Various Inputs**: Test edge cases and normal operation cases.
3. **Isolated Testing**: Ensure tests are isolated and don't depend on external state.

## Resource Cleanup

1. **Proper Disposal**: Ensure disposable resources are properly cleaned up.
2. **Async/Await Best Practices**: Always follow async/await best practices.

Following these guidelines will ensure that GitHub Copilot contributions maintain the project's quality standards and architectural approach. This document should be updated as new guidelines are established or existing ones are refined.