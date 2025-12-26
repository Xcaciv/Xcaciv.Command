# Xcaciv.Loader 2.1.1 Security Quick Reference

## Instance-Based Security Configuration

Xcaciv.Loader 2.1.1 provides instance-based security policies that can be configured per assembly context.

## Security Policies

| Policy | Behavior | Use Case |
|--------|----------|----------|
| `AssemblySecurityPolicy.Strict` | **Default**. Enforces path restrictions, disallows reflection emit, requires explicit allowlists | Production environments, untrusted plugins |
| `AssemblySecurityPolicy.Default` | Legacy permissive mode. Allows broader access | Trusted plugins, legacy compatibility |

## Common Configuration Patterns

### Pattern 1: Maximum Security (Recommended)

```csharp
// Uses strict security by default - no configuration needed
var controller = new CommandController();
controller.AddPackageDirectory("./plugins");
controller.LoadCommands();
```

**Provides:**
- ? Directory traversal prevention
- ? Path-based plugin sandboxing
- ? Reflection emit disabled
- ? Base path restriction enforced

---

### Pattern 2: Legacy Plugin Support

```csharp
var crawler = new Crawler();
crawler.SetSecurityPolicy(AssemblySecurityPolicy.Default);

var factory = new CommandFactory();
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    SecurityPolicy = AssemblySecurityPolicy.Default,
    EnforceBasePathRestriction = false,
    AllowReflectionEmit = true
});

// Create controller with configured components
var controller = new CommandController(
    new CommandRegistry(),
    new CommandExecutor(new CommandRegistry(), factory, null),
    new CommandLoader(crawler, new VerifiedSourceDirectories()),
    factory
);
```

---

### Pattern 3: Strict with Reflection Emit

For plugins that need dynamic code generation but still require path isolation:

```csharp
var factory = new CommandFactory();
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    SecurityPolicy = AssemblySecurityPolicy.Strict,
    EnforceBasePathRestriction = true,
    AllowReflectionEmit = true // Only enable if absolutely necessary
});
```

---

### Pattern 4: Namespace-Restricted Dependencies

Allow only specific dependency namespaces:

```csharp
var factory = new CommandFactory();
factory.SetSecurityConfiguration(new AssemblySecurityConfiguration
{
    SecurityPolicy = AssemblySecurityPolicy.Strict,
    AllowedDependencyPrefixes = new[] { "MyCompany.", "Xcaciv.", "System." }
});
```

---

## AssemblySecurityConfiguration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SecurityPolicy` | `AssemblySecurityPolicy` | `Strict` | Security policy to enforce |
| `EnforceBasePathRestriction` | `bool` | `true` | Prevent loading from parent directories |
| `AllowReflectionEmit` | `bool` | `false` | Allow dynamic code generation |
| `AllowedDependencyPrefixes` | `string[]` | `[]` | Namespace prefixes for allowed dependencies |

## Security Policy Decision Tree

```
Is this a trusted, signed plugin from your organization?
?? YES ? Can use AssemblySecurityPolicy.Default
?         (But Strict is still recommended)
?
?? NO ? Is this a third-party or external plugin?
    ?? YES ? MUST use AssemblySecurityPolicy.Strict
    ?         • Never use Default
    ?         • Never disable EnforceBasePathRestriction
    ?         • Only enable AllowReflectionEmit if absolutely required
    ?
    ?? UNSURE ? Use AssemblySecurityPolicy.Strict
                • Test plugin behavior
                • Review plugin source code
                • Document any required exceptions
```

## Diagnostic Trace Messages

Xcaciv.Loader 2.1.1 emits detailed trace messages for security events:

```csharp
// Enable trace output
System.Diagnostics.Trace.Listeners.Add(
    new System.Diagnostics.ConsoleTraceListener());

// Example trace messages you'll see:
// [Xcaciv.Loader 2.1.1] Crawler security policy set to: Strict
// [Xcaciv.Loader 2.1.1] Security violation loading package [BadPlugin] from [C:\plugins\BadPlugin\bin\BadPlugin.dll]:
//   SecurityPolicy=Strict, BasePathRestriction=C:\plugins\BadPlugin\bin. Details: Attempted to load from parent directory
```

## Testing Security Configuration

### Test 1: Verify Strict Policy is Enforced

```csharp
[Fact]
public void StrictPolicy_PreventsDirectoryTraversal()
{
    var crawler = new Crawler(); // Uses Strict by default
    
    // This should fail if plugin attempts directory traversal
    var result = crawler.LoadPackageDescriptions("./plugins", "bin");
    
    // Verify plugin loaded successfully or was skipped with trace message
    Assert.NotNull(result);
}
```

### Test 2: Verify Legacy Plugin Loads with Permissive Policy

```csharp
[Fact]
public void DefaultPolicy_AllowsLegacyPlugin()
{
    var crawler = new Crawler();
    crawler.SetSecurityPolicy(AssemblySecurityPolicy.Default);
    
    var result = crawler.LoadPackageDescriptions("./legacy-plugins", "bin");
    
    Assert.NotEmpty(result);
}
```

## Migration Checklist

- [ ] Review all plugin sources (trusted vs. untrusted)
- [ ] Test plugins with `AssemblySecurityPolicy.Strict` (default)
- [ ] Document any plugins requiring `AssemblySecurityPolicy.Default`
- [ ] Enable trace output in development environment
- [ ] Monitor logs for security violation messages
- [ ] Update plugin documentation with security requirements
- [ ] Create exception approval process for permissive policies
- [ ] Schedule regular plugin security audits

## Common Errors and Solutions

### Error: "Security violation loading package"

**Cause:** Plugin attempted to access files outside its sandbox

**Solutions:**
1. **Recommended:** Fix plugin to only access its own directory
2. **Legacy:** Configure `AssemblySecurityPolicy.Default` (document justification)

### Error: "Reflection emit not allowed"

**Cause:** Plugin uses dynamic code generation with strict policy

**Solutions:**
1. **Recommended:** Refactor plugin to avoid dynamic code
2. **Workaround:** Set `AllowReflectionEmit = true` (document justification)

### Error: "Assembly dependency not found"

**Cause:** Dependency not in plugin's `bin` directory or not whitelisted

**Solutions:**
1. **Recommended:** Copy dependencies to plugin's `bin` directory
2. **Alternative:** Add dependency namespace to `AllowedDependencyPrefixes`

## Security Recommendations

### DO ?

- ? Use `AssemblySecurityPolicy.Strict` for all production deployments
- ? Keep all plugin dependencies in the plugin's `bin` directory
- ? Enable trace output during development and testing
- ? Document any security policy exceptions
- ? Regularly audit plugin security configurations
- ? Test plugins in isolation before deployment

### DON'T ?

- ? Use `AssemblySecurityPolicy.Default` without documented justification
- ? Disable `EnforceBasePathRestriction` unless absolutely necessary
- ? Enable `AllowReflectionEmit` for untrusted plugins
- ? Deploy plugins without testing with strict security first
- ? Ignore security violation trace messages
- ? Load plugins from unverified sources

## Performance Notes

- ? **No performance impact**: Instance-based security has negligible overhead
- ? **No memory impact**: Each assembly context is disposed after use
- ? **Isolation benefits**: Per-plugin contexts prevent version conflicts

## Further Reading

- [Full Migration Summary](./loader-2.1.1-migration-summary.md)
- [Security Policy](../SECURITY.md)
- [Plugin Development Guide](./learn/getting-started-plugins.md)
- [Xcaciv.Loader Documentation](https://github.com/Xcaciv/Xcaciv.Loader)
