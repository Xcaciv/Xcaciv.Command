# Xcaciv.Loader 2.1.1 Migration Summary

## Overview

The codebase has been successfully migrated to use **Xcaciv.Loader 2.1.1**, which introduces instance-based security configuration for assembly loading. This migration enhances plugin security through stricter default policies and per-assembly context isolation.

## Changes Made

### 1. **Crawler.cs** - Enhanced Plugin Discovery Security

**What Changed:**
- Added instance-based `AssemblySecurityPolicy` configuration
- Default policy changed to `AssemblySecurityPolicy.Strict` (from `Default`)
- Enhanced trace logging with Xcaciv.Loader 2.1.1 context tags
- Improved exception handling with detailed security violation messages

**Key Features:**
```csharp
// Configure security policy at crawler level
var crawler = new Crawler();
crawler.SetSecurityPolicy(AssemblySecurityPolicy.Strict); // Default, most secure

// Or use legacy mode for older plugins
crawler.SetSecurityPolicy(AssemblySecurityPolicy.Default); // Less restrictive
```

**Security Benefits:**
- **Per-plugin path isolation**: Each plugin is sandboxed to its own directory
- **Directory traversal prevention**: Plugins cannot access parent or sibling directories
- **Explicit security configuration**: Security policy must be consciously set for permissive mode
- **Detailed security violation logging**: Clear trace messages when security violations occur

### 2. **CommandFactory.cs** - Secure Command Instantiation

**What Changed:**
- Leverages `AssemblySecurityConfiguration` for instance-level security control
- Enhanced error messages with Xcaciv.Loader 2.1.1 context
- Per-command assembly context with isolated security policies
- Improved exception handling with security policy details

**Key Features:**
```csharp
// Configure security at factory level
var factory = new CommandFactory();
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    SecurityPolicy = AssemblySecurityPolicy.Strict,
    EnforceBasePathRestriction = true,
    AllowReflectionEmit = false // Disable dynamic code generation
});
```

**Security Benefits:**
- **Isolated assembly contexts**: Each command loads in its own security context
- **Base path restrictions**: Commands cannot load assemblies outside their directory
- **Reflection emit control**: Dynamic code generation can be disabled for added security
- **Clear security violation messages**: Detailed error information for troubleshooting

### 3. **AssemblySecurityConfiguration.cs** - Existing Configuration Enhanced

**What's Available:**
- `SecurityPolicy`: `Strict` (recommended) or `Default` (legacy)
- `EnforceBasePathRestriction`: Boolean, default `true`
- `AllowReflectionEmit`: Boolean, default `false`
- `AllowedDependencyPrefixes`: String array for dependency namespace filtering
- `Validate()`: Ensures configuration consistency

## How to Use Enhanced Security Features

### Default Usage (Strict Security - Recommended)

No code changes needed - strict security is now the default:

```csharp
var controller = new CommandController();
controller.AddPackageDirectory("./plugins");
controller.LoadCommands(); // Uses AssemblySecurityPolicy.Strict by default
```

**What this provides:**
- ? Each plugin sandboxed to its own directory
- ? Directory traversal attacks prevented
- ? Reflection emit disabled
- ? Base path restrictions enforced

### Legacy Plugin Compatibility

For older plugins that require less restrictive security:

```csharp
var crawler = new Crawler();
crawler.SetSecurityPolicy(AssemblySecurityPolicy.Default);

var factory = new CommandFactory();
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    SecurityPolicy = AssemblySecurityPolicy.Default,
    EnforceBasePathRestriction = false, // Allow broader path access
    AllowReflectionEmit = true // Allow dynamic code generation
});

var controller = new CommandController(
    new CommandRegistry(),
    new CommandExecutor(new CommandRegistry(), factory, null),
    new CommandLoader(crawler, new VerifiedSourceDirectories()),
    factory
);

controller.AddPackageDirectory("./legacy-plugins");
controller.LoadCommands();
```

**Warning:** Only use permissive mode for trusted plugins from verified sources.

### Advanced Configuration - Namespace Filtering

Restrict plugin dependencies to specific namespaces:

```csharp
var factory = new CommandFactory();
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    SecurityPolicy = AssemblySecurityPolicy.Strict,
    AllowedDependencyPrefixes = new[] { "MyApp.", "Common.", "Xcaciv." }
});
```

This allows plugins to load only assemblies from whitelisted namespaces.

## Migration Impact

### ? Backward Compatibility Maintained

- All 155 existing tests pass without modification
- Default behavior remains functional
- Existing code works with stricter defaults
- No breaking API changes

### ?? Security Improvements

1. **Strict by Default**: New security policy provides defense-in-depth
2. **Instance-Based Configuration**: Each assembly context can have different policies
3. **Path Isolation**: Directory traversal attacks prevented by default
4. **Clear Error Messages**: Security violations provide actionable diagnostic information

### ?? Test Results

```
Test summary: total: 155, failed: 0, succeeded: 155, skipped: 0
Build succeeded ?
```

## Troubleshooting

### Issue: Plugin Fails to Load with SecurityException

**Symptom:**
```
[Xcaciv.Loader 2.1.1] Security violation loading package [MyPlugin] from [C:\plugins\MyPlugin\bin\MyPlugin.dll]:
SecurityPolicy=Strict, BasePathRestriction=C:\plugins\MyPlugin\bin
```

**Solution:**
1. **Option A (Recommended)**: Fix plugin to work within security boundaries
2. **Option B**: Configure permissive policy for that specific plugin directory

```csharp
crawler.SetSecurityPolicy(AssemblySecurityPolicy.Default);
```

### Issue: Plugin Requires Reflection Emit

**Symptom:**
Plugin fails when trying to generate dynamic code

**Solution:**
Allow reflection emit for legacy plugins:

```csharp
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    AllowReflectionEmit = true
});
```

### Issue: Plugin Dependencies Not Loading

**Symptom:**
Plugin loads but its dependencies fail to resolve

**Solution:**
Ensure all dependencies are in the plugin's `bin` directory, or configure dependency prefixes:

```csharp
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    AllowedDependencyPrefixes = new[] { "PluginDependency." }
});
```

## Best Practices

### 1. Use Strict Policy for Production

```csharp
// Always use strict policy for production deployments
var crawler = new Crawler();
// No SetSecurityPolicy call needed - Strict is default
```

### 2. Verify Plugin Directory Structure

Ensure plugins follow the expected structure:
```
/plugins/
  ?? MyPlugin/
  ?  ?? bin/
  ?     ?? MyPlugin.dll
  ?     ?? (dependencies)
  ?? AnotherPlugin/
     ?? bin/
        ?? AnotherPlugin.dll
```

### 3. Monitor Security Violations

Enable trace output to catch security violations during development:

```csharp
System.Diagnostics.Trace.Listeners.Add(
    new System.Diagnostics.TextWriterTraceListener(Console.Out));
```

### 4. Test Plugins in Isolation

Test each plugin independently with strict security to ensure compatibility:

```csharp
[Fact]
public void Plugin_LoadsSuccessfully_WithStrictSecurity()
{
    var crawler = new Crawler();
    // Uses Strict by default
    var packages = crawler.LoadPackageDescriptions("./plugins/MyPlugin", "bin");
    
    Assert.NotEmpty(packages);
}
```

## Documentation Updates

The following documentation has been updated to reflect Xcaciv.Loader 2.1.1:

- ? `Crawler.cs` - XML doc comments updated
- ? `CommandFactory.cs` - XML doc comments updated
- ? `AssemblySecurityConfiguration.cs` - Existing documentation applies
- ? This migration summary created

## Next Steps

1. **Review plugin inventory**: Identify any legacy plugins that may need configuration adjustments
2. **Test in staging**: Deploy with strict security to staging environment
3. **Monitor logs**: Watch for security violation trace messages
4. **Update plugin guidelines**: Inform plugin developers of new security requirements
5. **Document exceptions**: If any plugins require permissive mode, document the justification

## References

- [Xcaciv.Loader 2.1.1 Release Notes](https://github.com/Xcaciv/Xcaciv.Loader)
- [Security Policy Documentation](../SECURITY.md)
- [Plugin Development Guide](./learn/getting-started-plugins.md)
- [Migration Guide v2.0](./migration_guide_v2.0.md)

---

**Migration Completed:** ? All tests passing  
**Security Posture:** ? Enhanced with strict defaults  
**Backward Compatibility:** ? Maintained  
**Production Ready:** ? Yes
