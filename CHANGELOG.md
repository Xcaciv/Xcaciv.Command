# Changelog

All notable changes to Xcaciv.Command will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2025-12-25

### Changed
- Bump all package versions to 2.1.0 across projects.
- Update README to reference Xcaciv.Loader 2.1.0 and current features.
- Align documentation with current security policies and .NET 8.

## [2.0.0] - 2025-12-19 - Comprehensive v2.0 Maintenance Release

### Overview

Major version release completing 8 phases of systematic improvements targeting maintainability, trustworthiness, and reliability. All 150 tests passing with full backward compatibility through deprecated methods.

**See [Migration Guide](docs/migration_guide_v2.0.md) for upgrade instructions.**

---

### 🔴 BREAKING CHANGES

#### API Renames (Backward compatible via deprecation)
- `EnableDefaultCommands()` → `RegisterBuiltInCommands()` [Deprecated in v2.0, removed in v3.0]
- `GetHelp()` → `GetHelpAsync()` [Deprecated in v2.0, removed in v3.0]

#### Security Defaults Changed
- **Default SecurityPolicy:** `AssemblySecurityPolicy.Default` → `AssemblySecurityPolicy.Strict`
- **Base Path Restriction:** Now enforced by default (prevents directory traversal)
- **Reflection Emit:** Disabled by default (prevents runtime code generation)
- **Impact:** Legacy plugins may fail to load - configure `AssemblySecurityConfiguration` explicitly

---

### Added - Phase 1: Auditing & Tracing

- **`AuditEvent` class** - Structured audit event with correlation IDs, timestamps, package origins
- **`AuditMaskingConfiguration` class** - Configure parameter masking patterns (e.g., passwords, tokens)
- **`StructuredAuditLogger` class** - JSON-structured audit logging with configurable masking
- **Correlation IDs** - Track command execution across pipeline stages
- **Package origin metadata** - Log which package each command came from
- **Parameter masking** - Redact sensitive values from audit logs

### Added - Phase 2: Configuration & DI Support

- **`CommandControllerOptions` class** - Configuration for controller initialization
- **`PipelineOptions` class** - Configuration for pipeline behavior
- **`CommandControllerFactory` class** - Factory pattern for creating controllers
  - `Create()` - Creates controller without DI
  - `Create(CommandControllerOptions)` - Creates controller with configuration
- **`Xcaciv.Command.DependencyInjection` package** (separate) - Optional Microsoft.Extensions.DependencyInjection adapter
  - `AddXcacivCommand()` extension method
  - Scoped/Singleton lifetime support
  - Configuration binding support

### Added - Phase 3: Cancellation Token Propagation

- **`CancellationToken` overloads:**
  - `ICommandController.Run(commandLine, output, env, CancellationToken)`
  - `IPipelineExecutor.ExecuteAsync(..., CancellationToken)`
  - `ICommandExecutor.ExecuteAsync(..., CancellationToken)`
- Cancellation propagates through: Controller → Pipeline → Executor → Commands
- Commands can now be gracefully cancelled during long operations

### Added - Phase 4: Registry & Factory Thread-Safety

- **`CommandRegistry.GetCommandSnapshot()`** - Thread-safe snapshot for concurrent iteration
- **`ICommandFactory.CreateCommandAsync()`** - Async factory method for command instantiation
- **Thread-safe registry:** Replaced explicit locks with `ConcurrentDictionary<string, ICommandDescription>`
- Lock-free reads, fine-grained writes

### Added - Phase 5: Centralized Help System

- **`IHelpService` interface** - Centralized help generation
- **`HelpService` class** - Generates help from command attributes and metadata
  - `BuildOneLineHelp()` - Single-line command summary
  - `BuildDetailedHelp()` - Full command documentation
  - `IsHelpRequest()` - Detects help flags (`--HELP`, `-?`, `/?`)
- **Assembly.LoadFrom() fallback** - Fixes plugin type loading for help generation
- Commands no longer need custom `Help()` implementations

### Added - Phase 6: Pipeline Hardening

- **`PipelineParser` class** - Formal grammar parser for command lines
  - Supports double quotes: `"arg with spaces"`
  - Supports single quotes: `'literal text'`
  - Supports escape sequences: `\"`, `\'`, `\\`, `\|`
  - Pipe delimiter `|` handling
- **`PipelineConfiguration` enhancements:**
  - `StageTimeoutSeconds` - Per-stage timeout (default: 0 = unlimited)
  - `MaxStageOutputBytes` - Output byte limit per stage (default: 0 = unlimited)
  - `MaxStageOutputItems` - Output item limit per stage (default: 0 = unlimited)
  - `Validate()` - Configuration validation method
- **Per-stage timeouts** - Individual pipeline stages can timeout independently

### Added - Phase 7: Security Alignment

- **`AssemblySecurityConfiguration` class** - Configurable plugin security policies
  - `SecurityPolicy` - Defaults to `Strict` (was `Default`)
  - `AllowReflectionEmit` - Defaults to `false` (disabled)
  - `EnforceBasePathRestriction` - Defaults to `true` (enabled)
  - `AllowedDependencyPrefixes` - Optional namespace allowlist
  - `Validate()` - Configuration validation
- **`CommandFactory.SetSecurityConfiguration()`** - Configure security policies
- **`Crawler.SetSecurityPolicy()`** - Configure plugin discovery security
- **Enhanced error messages** - Security violations include policy context

### Added - Phase 8: Breaking API Changes

- **`ICommandController.RegisterBuiltInCommands()`** - New primary method (replaces `EnableDefaultCommands`)
- **`ICommandController.GetHelpAsync()`** - New async method (replaces `GetHelp`)
- **`[Obsolete]` attributes** - Old methods marked for v3.0 removal
- **Migration guide** - Comprehensive upgrade documentation

---

### Changed

- **Security defaults:** Stricter plugin loading policies by default
- **Registry implementation:** `ConcurrentDictionary` instead of explicit locks
- **Help generation:** Centralized in `HelpService` instead of per-command
- **Pipeline parsing:** Formal grammar instead of simple `string.Split()`
- **Factory pattern:** `CommandControllerFactory` for non-DI scenarios

---

### Deprecated

- **`ICommandController.EnableDefaultCommands()`** - Use `RegisterBuiltInCommands()` [Removed in v3.0]
- **`CommandController.GetHelp()`** - Use `GetHelpAsync()` [Removed in v3.0]

---

### Fixed

- Plugin type loading in help generation (Assembly.LoadFrom fallback)
- Thread-safety issues in CommandRegistry (ConcurrentDictionary migration)
- Pipeline parsing with quoted arguments and escape sequences
- Security policy context in error messages

---

### Documentation

- **`migration_guide_v2.0.md`** - Comprehensive upgrade guide
- **`maintenance_v2.0_execution_plan.md`** - Detailed phase documentation
- Updated XML documentation for all new APIs
- Security policy guidance in SECURITY.md

---

### Testing

- **150/150 tests passing** (100% pass rate)
- All phases validated with full regression testing
- 4 expected deprecation warnings from deprecated API usage

---

### Technical Metrics

- **Build Status:** SUCCESS (0 errors)
- **Test Coverage:** 150/150 passing
- **Phases Complete:** 8/8 (100%)
- **Compilation Warnings:** 4 (expected deprecation warnings)
- **Dependencies:** Xcaciv.Loader 2.0.1, System.IO.Abstractions 21.0.2

---

## [Unreleased] - SSEM Improvement Initiative (Phases 1-8)

### Overview

This release represents a comprehensive improvement initiative targeting the **Securable Software Engineering Model (SSEM)** score, increasing overall quality from **7.4 (Adequate)** to **8.2+ (Good)**.

**SSEM Score Improvements by Pillar:**
- **Maintainability:** 7.5 ? 8.3 (+0.8 points)
- **Trustworthiness:** 7.1 ? 7.8 (+0.7 points)
- **Reliability:** 7.7 ? 8.5 (+0.8 points)
- **Overall SSEM:** 7.4 ? 8.2 (+0.8 points)

**Key Metrics:**
- **Tests Added:** 103 new tests during SSEM initiative
- **Test Success Rate:** 100% (148/148 passing across all test projects)
- **Code Coverage:** ~65% ? ~80% (estimated)
- **Lines of Code Refactored:** ~350+ LOC across major orchestration methods
- **Documentation:** 6000+ words added (SECURITY.md + XML docs)

### Added - Phase 7: Output Encoding Infrastructure
- **IOutputEncoder Interface** (`src/Xcaciv.Command.Interface/IOutputEncoder.cs`)
  - `Encode()` - Encodes output for safe consumption by target systems
- **NoOpEncoder** (`src/Xcaciv.Command.Interface/IOutputEncoder.cs`) - Default no-op encoder (backward compatible)
- **HtmlEncoder** (`src/Xcaciv.Command/Encoders/HtmlEncoder.cs`) - HTML-encodes output for web display
- **JsonEncoder** (`src/Xcaciv.Command/Encoders/JsonEncoder.cs`) - JSON-encodes output for API responses
- **CommandController.OutputEncoder Property** - Optional output encoder injection
- **IIoContext.SetOutputEncoder()** - Output encoder propagation to IO contexts
- **AbstractTextIo.OutputChunk()** - Applies output encoding when writing chunks
- 17 comprehensive output encoding tests
- **Namespace:** `Xcaciv.Command.Encoders` for concrete encoder implementations

### Added - Phase 4: Audit Logging Infrastructure
- **IAuditLogger Interface** (`src/Xcaciv.Command.Interface/IAuditLogger.cs`)
  - `LogCommandExecution()` - Logs command execution with parameters, timing, success/error status
  - `LogEnvironmentChange()` - Logs environment variable changes with old/new values
- **NoOpAuditLogger** (`src/Xcaciv.Command/NoOpAuditLogger.cs`) - Default no-op implementation
- **CommandController.AuditLogger Property** - Optional audit logger injection
- **EnvironmentContext.SetAuditLogger()** - Audit logger propagation to child contexts
- Audit logging integrated into `ExecuteCommandWithErrorHandling` with execution timing
- Environment change logging in `EnvironmentContext.SetValue`
- 8 comprehensive audit logging tests

### Added - Phase 6: Pipeline DoS Protection
- **PipelineConfiguration Class** (`src/Xcaciv.Command/PipelineConfiguration.cs`)
  - `MaxChannelQueueSize` (default: 10,000 items) - Prevents unbounded memory growth
  - `BackpressureMode` enum (DropOldest, DropNewest, Block) - Configurable overflow strategies
  - `ExecutionTimeoutSeconds` (default: 0 = no timeout) - Reserved for future use
- **Bounded Channels** - Replaced unbounded channels in pipeline implementation
- **CommandController.PipelineConfig Property** - Per-instance pipeline configuration
- `GetChannelFullMode()` helper method - Converts enum to BoundedChannelFullMode
- 10 comprehensive pipeline backpressure tests
- **Memory Protection:** Default configuration prevents DoS via memory exhaustion (~100MB max per pipeline)

### Added - Phase 5: Security Documentation
- **SECURITY.md** (6000+ words) - Comprehensive security policy
  - Plugin security model with trust boundaries
  - Threat analysis (directory traversal, tampering, malicious execution, audit tampering)
  - Plugin development guidelines (safe env var access, input/output handling)
  - Known vulnerabilities and mitigations
  - Vulnerability reporting procedures
  - Security checklist for plugin developers
- **README.md Security Section** - Expanded with audit logging examples and SECURITY.md link

### Added - Phase 2: Test Coverage Expansion
- **PipelineErrorTests.cs** - 7 tests for pipeline error handling and graceful failure
- **ParameterValidationBoundaryTests.cs** - 12 tests for parameter edge cases (empty, long, special chars, Unicode)
- **SecurityExceptionTests.cs** - 7 tests for security exception handling in plugin loading
- **EnvironmentContextEdgeCaseTests.cs** - 18 tests for environment variable management (case-insensitivity, collision, isolation)

### Added - Phase 3: Refactoring Tests
- **CommandControllerRefactoringTests.cs** - 11 behavioral regression tests for refactored methods

### Added - Phase 1: Parameter Bounds Tests
- **ParameterBoundsTests.cs** - 10 tests for parameter parsing edge cases (empty lists, null, missing parameters)
- 3 help command tests in CommandControllerTests.cs

### Added - Phase 8: XML Documentation
- Comprehensive XML documentation for all public APIs
- `<summary>`, `<param>`, `<returns>`, `<exception>`, `<remarks>` tags throughout
- Security considerations documented in remarks sections
- IntelliSense-ready documentation for improved developer experience

### Changed - Phase 3: Method Refactoring
- **CommandController.PipelineTheBitch** - Refactored from ~60 LOC to ~10 LOC (83% reduction)
  - Extracted `CreatePipelineStages()` (38 LOC) - Pipeline stage creation
  - Extracted `CollectPipelineOutput()` (5 LOC) - Output collection
  - Reduced cyclomatic complexity from 12+ to <6
- **CommandController.ExecuteCommand** - Refactored from ~80 LOC to ~39 LOC (51% reduction)
  - Extracted `InstantiateCommand()` (8 LOC) - Command instantiation
  - Extracted `ExecuteCommandWithErrorHandling()` (28 LOC) - Execution with error handling
  - Reduced cyclomatic complexity from 12+ to <8

### Fixed - Phase 1: Bounds Checking
- **Critical:** Added bounds checking to `ProcessOrderedParameters` and `ProcessSuffixParameters` in `CommandParameters.cs`
  - Prevents `IndexOutOfRangeException` when commands invoked with insufficient parameters
- **Critical:** Refactored help request handling in `CommandController.ExecuteCommand`
  - Help requests (`--HELP`) now checked before try-catch block (no longer caught as exceptions)
  - Cleaner error handling separation

### Security
- **Fixed:** Parameter bounds checking prevents IndexOutOfRangeException (Internal Bug)
- **Added:** Audit logging infrastructure for command execution and environment changes
- **Added:** Bounded pipeline channels prevent denial-of-service via memory exhaustion
- **Added:** Comprehensive security documentation (SECURITY.md)

### Performance
- **Improved:** Refactored orchestration methods reduce cyclomatic complexity by ~40%
- **Improved:** Bounded channels prevent unbounded memory growth in pipelines
- **No Regression:** All performance-critical paths maintained or improved

### Migration Notes

**Backward Compatible:** All changes are backward compatible. Existing code continues to work without modification.

**Breaking Changes:** None

**Optional Enhancements:**
1. **Audit Logging:** Implement custom `IAuditLogger` for production logging (Serilog, Application Insights, etc.)
2. **Pipeline Configuration:** Review default bounded channel settings (10,000 items) for your use case
3. **Output Encoding:** Use `HtmlEncoder` for web UIs, `JsonEncoder` for JSON APIs, or implement custom encoders
4. **Security:** Read SECURITY.md for plugin security guidelines

**Known Limitations:**
- Execution timeouts not yet implemented (Phase 6b deferred to future release)
- Requires breaking change to `ICommandDelegate.Main` signature (add `CancellationToken`)

### Test Summary

**Current Test Status:**
- **Xcaciv.Command.Tests:** 136/136 passing (100%)
  - Includes OutputEncodingTests (17 tests)
  - Includes all Phase 1-8 test additions
- **Xcaciv.Command.FileLoaderTests:** 12/12 passing (100%)
- **Grand Total:** 148/148 tests passing (100%)

**Tests Added by Phase:**
- Phase 1: Bounds Checking - 13 tests
- Phase 2: Test Expansion - 44 tests  
- Phase 3: Refactoring - 11 tests
- Phase 4: Audit Logging - 8 tests
- Phase 5: Security Docs - 0 tests
- Phase 6: DoS Protection - 10 tests
- Phase 7: Output Encoding - 17 tests
- Phase 8: Documentation - 0 tests
- **Total Added:** 103 new tests

### Usage Examples

#### Audit Logging Example
```csharp
public class ConsoleAuditLogger : IAuditLogger
{
    public void LogCommandExecution(string commandName, string[] parameters, 
        DateTime executedAt, TimeSpan duration, bool success, string? errorMessage = null)
    {
        Console.WriteLine($"[{executedAt:O}] {commandName} | {duration.TotalMilliseconds}ms | Success: {success}");
    }
    
    public void LogEnvironmentChange(string variableName, string? oldValue, 
        string? newValue, string changedBy, DateTime changedAt)
    {
        Console.WriteLine($"[{changedAt:O}] {variableName}: {oldValue ?? "(null)"} ? {newValue ?? "(null)"}");
    }
}

// Use with CommandController
var logger = new ConsoleAuditLogger();
var controller = new CommandController { AuditLogger = logger };
await controller.Run("Say Hello", ioContext, env);
```

#### Pipeline Configuration Example
```csharp
// For memory-constrained environments
controller.PipelineConfig = new PipelineConfiguration
{
    MaxChannelQueueSize = 1_000,
    BackpressureMode = PipelineBackpressureMode.Block
};

// For high-throughput scenarios
controller.PipelineConfig = new PipelineConfiguration
{
    MaxChannelQueueSize = 50_000,
    BackpressureMode = PipelineBackpressureMode.DropOldest
};
```

#### Output Encoding Example
```csharp
using Xcaciv.Command.Encoders;

// For web UI output (HTML encoding)
var htmlController = new CommandController { OutputEncoder = new HtmlEncoder() };
await htmlController.Run("Say <script>alert('XSS')</script>", ioContext, env);
// Output: &lt;script&gt;alert('XSS')&lt;/script&gt;

// For JSON API output
var jsonController = new CommandController { OutputEncoder = new JsonEncoder() };
await jsonController.Run("Say Hello \"World\"", ioContext, env);
// Output: Hello \"World\"

// Default (no encoding) - backward compatible
var defaultController = new CommandController(); // Uses NoOpEncoder by default
await defaultController.Run("Say Hello", ioContext, env);
// Output: Hello (unchanged)
```

---

## [1.5.18] - 2025-01-XX

### Changed - BREAKING
- **Migrated to Xcaciv.Loader 2.0.1** with instance-based security configuration
  - Replaced static `SetStrictDirectoryRestriction()` with per-instance `AssemblySecurityPolicy`
  - Changed from wildcard (`*`) path restrictions to directory-based security
  - Each `AssemblyContext` now has independent security configuration
  
### Added
- Comprehensive security exception handling for plugin loading
- Detailed trace logging for security violations
- `LoaderMigrationTests` test suite with 8 new security-focused tests
- Security documentation in README files

### Fixed
- CS8602 null reference warnings in `MemoryIoContext` and `TestTextIo` constructors
- Proper path-based security restrictions for plugin loading (replaced TODO comment)
- Enhanced error messages when security violations occur

### Security
- Plugin assemblies now restricted to their actual directory paths
- Security violations are explicitly caught and logged
- Follows SSEM principles: Maintainability, Trustworthiness, and Reliability
- No more wildcard access permissions

### Migration Notes
For users upgrading from versions using Xcaciv.Loader 1.x:
- No API changes required for basic usage
- Plugin security is automatically enhanced
- Security exceptions now propagate with clear error messages
- See [Xcaciv.Loader Migration Guide](https://github.com/Xcaciv/Xcaciv.Loader/blob/main/docs/MIGRATION-v1-to-v2.md)

## Previous Versions

### [1.5.x] - Previous
- Threaded pipeline support
- Sub-command structure
- Auto-generated help
- Built-in commands (SAY, SET, ENV, REGIF)
- Plugin system with Xcaciv.Loader 1.x
