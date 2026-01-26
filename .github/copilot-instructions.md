# Copilot Instructions

The github repo is xcaciv/Xcaciv.Loader and the primary branch that I work off of is main.

Never stub or mock functionality unless explicitly asked to do so.  
Your terminal is PowerShell, only use PowerShell syntax.  
NEVER delete or remove content from task files. I will only update/mark completion status while preserving all existing text for future phases. Thank you for the correction.

## Formatting and Style

- Do not use emojis in code, documentation, or responses. Use only standard markdown formatting.
- Extended ASCII characters are acceptable where appropriate.
- Use standard markdown headers, lists, tables, and code blocks for formatting.

## Naming Conventions

- Use `String` for object usage and only lowercase `string` for scalar value indications; stop lowercasing `String` when used as an object.
- Never use single character variable names, always use descriptive names.
- Never use the term "Helper" or "Utils" in class names. Use descriptive, intent-revealing names instead.
- Examples:
  - BAD: `PathHelper`, `StringHelper`, `ValidationUtils`
  - GOOD: `PathValidator`, `StringFormatter`, `InputValidator`
- Before extracting functionality to a separate class, consider if it belongs closer to where it is used.
- Follow the Single Responsibility Principle, but avoid over-emphasis of DRY to the detriment of maintainability.
- Prioritize analyzability: code should be easy to understand and trace, even if it means some controlled duplication.

This document includes guiding principles for .NET development, based on the **Framework for Integrating Application Security into Software Engineering (FIASSE)** and the **Securable Software Engineering Model (SSEM)**. The goal is to influence code generation on a fundamental level to produce software that is inherently "securable", adaptable and resilient to evolving threats.

The primary directive is to **"Resiliently add computing value."** This means generating code that is not only functional but also possesses intrinsic qualities that allow it to be maintained and secured over its lifecycle.

---

# Exception handling

Only the UI layer is allowed to catch general exceptions and log them. All other layers should only catch specific exceptions that they can handle or recover from or add context to.  
eg. `catch (FileNotFoundException ex) { throw new ConfigurationException("Config file missing", ex); }`

Never use try/catch for flow control or to return a default value. Only use try/catch when an exception can be recovered from gracefully, when context needs to be added to an exception, when an exception needs to be logged. If you need to catch an exception and rethrow it, always add context. In the rare occasion that you cannot, use `throw;` instead of `throw ex;` to preserve the original stack trace.

# Security

Avoid string concatenation for commands or queries. Use parameterized queries or safe APIs.

## The Securable Principle

Security is not a static state but a dynamic process. When generating code, prioritize qualities that allow the software to be evolved, corrected, and adapted to new threats efficiently and without introducing new vulnerabilities.

## Concurrency and Thread Safety

Prefer concurrent collections over explicit locking mechanisms:
- Use `ConcurrentDictionary<TKey, TValue>`, `ConcurrentBag<T>`, `ConcurrentQueue<T>` instead of lock statements or ReaderWriterLockSlim
- Concurrent collections provide lock-free reads and fine-grained locking for writes, improving performance and reducing deadlock risk
- Only use explicit locks when coordinating complex multi-step operations that concurrent collections cannot handle atomically
- Document why explicit locking is needed if you must use it

## Software Engineering principles to follow for security:

#### The Derived Integrity Principle

The Derived Integrity Principle asserts that any value critical to a system’s integrity—whether related to business logic or internal state—must be calculated or derived within the trusted server-side context, never accepted directly from the client.

- Server as the Source of Truth: The server must own and compute authoritative values (e.g., prices, permissions, object states).
- Client Intent ≠ Client Authority: Clients express intent (e.g., “I want to buy item X”) but must not dictate facts (e.g., “It costs $0.01”).

#### Canonical Input Handling

Prefer the most narrow data types possible for input parameters. For example, use enums or booleans instead of strings where applicable.
- Canonicalization/Normalization: Ensures that input data conforms to expected formats, types, lengths, and ranges before processing. Prevents unexpected or malicious data from entering the system.
- Sanitization: Cleans input data to remove or neutralize potentially harmful content. Prevents malicious data from being executed or interpreted as code, protecting against injection attacks.
- Validation: Checks that input data meets specific criteria before processing. Ensures that only valid and expected data is processed, reducing the risk of injection attacks or unexpected behavior. Always prefer allowing explicit values instead of rejecting unexpected values.

## The Securable Software Engineering Model (SSEM)

Focus on the following three core attributes of securable software when generating or modifying code.

### Maintainability

**Goal:** Create code that can be easily understood, modified, and tested, as this is the foundation for long-term security.

-   **Analyzability:** Generate code that is simple, readable, and has low cyclomatic complexity. Use clear and consistent naming conventions. The easier the code is to understand, the easier it is to spot and fix security flaws.
-   **Modifiability:** Produce modular, loosely coupled, and highly cohesive code. This ensures that changes in one part of the system do not have unintended security consequences elsewhere.
-   **Testability:** Write code that is inherently testable. Use pure functions where possible and apply dependency injection. Code with high test coverage is more likely to be secure.

### Trustworthiness

**Goal:** Create code that behaves as expected, protects sensitive data, and ensures that actions are legitimate and traceable.

-   **Confidentiality:** Protect data from unauthorized disclosure. Never hardcode secrets (credentials, API keys). Use placeholders for secrets and rely on secure configuration management or vault systems. Employ strong, modern encryption for data in transit and at rest.
-   **Authenticity & Accountability:** Ensure users and services are who they claim to be and that their actions are traceable. Implement strong authentication and authorization checks for all sensitive operations. Generate detailed audit logs for security-relevant events (e.g., login attempts, access control decisions, data modification).
-   **Integrity:** Protect data and system resources from unauthorized modification.

### Reliability

**Goal:** Create code that operates correctly, resists failure, and can recover from attacks.

-   **Integrity (as part of Reliability):** All inputs are potential vectors for attack. Implement rigorous input validation at the boundaries of your application. Use parameterized queries and safe APIs to prevent injection attacks. Prefer safe serialization formats.
-   **Resilience:** Implement robust and graceful error handling. Fail safely without leaking sensitive information in error messages. Code should be resilient to unexpected inputs and states.
-   **Availability:** Ensure the system remains operational. Be mindful of resource consumption and potential denial-of-service vectors (e.g., unbounded operations, resource leaks).

## Securable strategies

### Transparency

Transparency is the principle of designing a system so that its internal state and behavior are observable and understandable to authorized parties. Transparency means that exceptions are not silently swallowed, all errors are logged appropriately, and system behavior can be audited. Logging should be structured and include context to facilitate monitoring and forensic analysis.

### Resilient Coding Practices

- Strong typing to ensure data is usable in the intended way.
- Filtering and validating all input at trust boundaries. Input validation ensures that data conforms to expected formats, types, lengths, and ranges before processing.
- Properly escaping and encoding all output destined for other systems or interpreters (exiting trust boundaries). This prevents injection attacks by ensuring that data is treated as data, not executable code.
- Sandboxing the use of null values to input checks and database communication. Use exceptions to handle exceptional cases.
- Implement comprehensive and strategic error handling to manage unexpected conditions gracefully, rather than allowing the application to crash or behave unpredictably.
- Using immutable data structures for threaded programming to prevent insecure modification and ensure thread safety and prevent race conditions.
- Canonical Input Handling: Canonicalization/Normalization, Sanitization Validation

---

# Project Structure 

-   **Modularity:** Organize code into well-defined, independent modules or components. This improves maintainability and limits the change surface of security vulnerabilities.
-   **Layering:** Adhere to architectural layering (e.g., presentation, business logic, data access). This enforces separation of concerns and helps in applying security controls at appropriate layers.
-   **Configuration Management:** Externalize configuration settings, especially security-sensitive ones. Use environment variables or Windows Credential Manager stores instead of hardcoding values.
-   **Dependency Management:** Regularly audit and update third-party libraries and frameworks to mitigate known vulnerabilities. Use tools to scan for vulnerable dependencies.

## Big picture
- **What this repo is:** an extensible .NET command framework that discovers and runs command plugins (class library DLLs) and also ships a set of built-in commands.
- **Primary components:**
  - `src/Xcaciv.Command/CommandController.cs` — central orchestrator: loads commands, routes command lines, handles piping and help.
  - `src/Xcaciv.Command.Core/` — base command helper code such as `AbstractCommand.cs` and `CommandParameters.cs` (parameter parsing and help generation).
  - `src/Xcaciv.Command.Interface/` — public interfaces and Attributes (e.g. `ICommandDelegate`, `ICommandController`, and attribute types under `Attributes/`).
  - `src/Xcaciv.Command.FileLoader` — plugin discovery (`ICrawler` / `Crawler`) and verified package directory handling.
  - `src/Xcaciv.Command/Commands/` — built-in commands (e.g. `SayCommand`, `SetCommand`, `EnvCommand`, `RegifCommand`).
  - /docs                   # Documentation files including readme, design docs, etc.
  - /tmp                    # Test command and temporary files
  - README.md               # Project description, installation/usage instructions

## Key design & important behaviors
- Commands are loaded either as compiled-in types (via `EnableDefaultCommands()` or `AddCommand(Type)`) or discovered from plugin folders (`AddPackageDirectory()` + `LoadCommands()`).
- Command registration relies on attributes: `CommandRegisterAttribute` (required) and `CommandRootAttribute` (for multi-level sub-commands). See `src/Xcaciv.Command.Interface/Attributes/` for definitions.
- Parameter binding uses attributes on the command class: ordered, named, flag, and suffix parameters are defined via attributes and processed by `CommandParameters` in `Xcaciv.Command.Core`.
- Pipelining: the controller splits on `|` (PIPELINE_CHAR in `CommandController`) and connects `IIoContext` pipes; commands should support piped input via `AbstractCommand.HandlePipedChunk` or `IAsyncEnumerable` semantics.
- Runtime loading: when a type is not already loaded, `CommandController` uses `AssemblyContext(packagePath)` to load plugin assemblies and create instances dynamically.

## Files to consult for behavioral examples
- Controller & orchestration: `src/Xcaciv.Command/CommandController.cs` (routing, pipeline, help, instance creation)
- Command base and parameter rules: `src/Xcaciv.Command.Core/AbstractCommand.cs` and `src/Xcaciv.Command.Core/CommandParameters.cs`
- Attributes and parameter definitions: `src/Xcaciv.Command.Interface/Attributes/` (e.g. `CommandRegisterAttribute.cs`, `CommandParameterOrderedAttribute.cs`, `CommandFlagAttribute.cs`)
- Built-in commands: `src/Xcaciv.Command/Commands/` for canonical implementations of how to implement `ICommandDelegate` and produce help, piping, and parameter processing.

## Project-specific conventions an AI should follow
- Command classes must implement `ICommandDelegate` (typically by inheriting `AbstractCommand`) and must be decorated with `CommandRegisterAttribute`. If the command is a root that contains sub-commands, also use `CommandRootAttribute`.
- Use attribute-driven parameter definitions (ordered, named, flag, suffix). The controller expects ordered parameters to precede named parameters.
- Help is triggered by `--HELP` (case-insensitive) or by calling controller `GetHelp`. `AbstractCommand.BuildHelpString` generates canonical help output; follow its format for consistency.
- Only certain commands may modify the environment. The controller tracks `ModifiesEnvironment` on `ICommandDescription`; when a command sets `ModifiesEnvironment` to true, the environment will be updated after command execution.
- When adding or modifying commands, ensure `CommandDescription.CreatePackageDescription` logic (see `CommandParameters.CreatePackageDescription`) stays compatible with the attribute semantics.


## Build and Test Workflow (Visual Studio, No PowerShell)

When working in Visual Studio, always use the IDE’s native build and test pipeline instead of PowerShell or CLI commands.  
Use GitHub Copilot Chat commands to trigger builds, generate tests, and run them through Test Explorer.

### Core Commands

- `@test #solution`  
  Analyze the entire solution, generate missing tests, build the project, and run all tests.

- `@test #<ClassName>`  
  Generate and run tests for a specific class.  
  Example: `@test #BankAccount`

- `@test #changes`  
  Generate tests only for recently modified code.

### Expected Behavior

Copilot should:
1. Inspect the code and project structure.
2. Create or update a test project (MSTest, NUnit, or xUnit).
3. Trigger a **Visual Studio build** using MSBuild (not PowerShell).
4. Run tests through **Test Explorer**.
5. Iterate on failing tests by refining mocks, edge cases, and assumptions.

### Guidelines for Copilot

- Prefer Visual Studio’s built‑in build/test pipeline.  
- Do **not** generate PowerShell scripts for build or test tasks.  
- Do **not** call `dotnet build` or `dotnet test` unless explicitly requested.  
- Use the `@test` command family for all test‑generation workflows.  
- When generating tests, follow clean, idiomatic C# patterns and avoid brittle mocks.

## Example usage patterns for agents
  - Example usage to exercise built-ins from code:var controller = new Xc.Command.CommandController();
controller.EnableDefaultCommands();
await controller.Run("Say Hello to my little friend", ioContext, env);- Load external plugin packages at runtime (from `CommandController`):
  - call `AddPackageDirectory("<plugin-base-path>")` then `LoadCommands()` and then `Run(...)` as needed.

## Testing and debugging tips for agents
- Unit tests live in `src/*Tests/` projects. Use `@test` for targeted test runs (e.g. `@test src/Xcaciv.Command.FileLoaderTests`).
- When a command fails during Run, `CommandController` writes trace messages and returns a brief error output chunk; inspect trace with `IIoContext.AddTraceMessage` call sites when debugging.
- For assembly-loading issues, check `AssemblyContext` use sites in `CommandController.GetCommandInstance(string, string)` and ensure plugin DLLs are in the expected `bin/<pluginName>/` layout.

## Example patterns the agent can safely apply/modify
- Adding a new built-in command: create a class under `src/Xcaciv.Command/Commands/`, inherit `AbstractCommand`, decorate with `CommandRegisterAttribute`, implement `HandleExecution` and `HandlePipedChunk`.
- Adding plugin discovery support: adjust `AddPackageDirectory` usage or `IVerfiedSourceDirectories` configuration; do not change `AssemblyContext` behavior unless you also update `GetCommandInstance` paths.

## Safety & style constraints for automated edits
- Preserve attribute names and parameter order semantics. Changes that break `CommandParameters.Process*` behavior will cause downstream runtime errors.
- Keep help text formatting consistent by using `AbstractCommand.BuildHelpString` patterns.
- Avoid changing the pipeline separator (`|`) or `IIoContext` contract without updating `CommandController` pipeline logic.

## Where to ask for human guidance
- If a change affects dynamic assembly loading, command registration, or parameter parsing, request a human review.

## FIASSE / SSEM guidance (apply before code generation)
- High-level SSEM pillars to obey: **Maintainability**, **Trustworthiness**, **Reliability**. Use these as decision criteria when generating changes.
  - Maintainability: prefer small, well-named methods (split `CommandController` logic if you must), add unit tests for `CommandParameters` when changing parsing rules, and prefer explicit attribute usage over implicit string parsing.
  - Trustworthiness: treat plugin assembly loading, environment mutation (`ModifiesEnvironment`), and run-time type activation (via `AssemblyContext`) as trust boundaries — require human review for widening these boundaries.
  - Reliability: commands and pipeline code must fail gracefully — always use trace messages (`IIoContext.AddTraceMessage`) and avoid bubbling raw exceptions to callers.

## .NET / OWASP-derived actionable rules (practical)
- Input validation: use allowlists for structured command parameters. For free-form text, normalize and context-encode on output. Validate pipe input before using it for integrity-critical values (respect `UsePipe` on parameter attributes).
- Serialization: avoid enabling automatic type resolution. When using JSON, prefer System.Text.Json with strongly-typed DTOs; if using Newtonsoft.Json, do NOT enable `TypeNameHandling` for untrusted payloads.
- Cryptography & RNG: use .NET primitives from `System.Security.Cryptography`.
  - Use `RandomNumberGenerator` for CSPRNG and `AesGcm` / `AesCng` for authenticated encryption. Do not write custom crypto.
- Secrets & keys: never hardcode secrets. Use platform vaults (Azure Key Vault, HashiCorp Vault) or `ProtectedData` for local developer scenarios. Do not commit `.secret` or key material to git.
- Logging & telemetry: emit structured logs and avoid logging secrets (session tokens, raw environment snapshots). If session correlation is needed, log a salted hash of the session id rather than the id itself.
- Dependency & runtime loading: pin versions and vet transitive dependencies. For runtime plugin assembly loading, preserve `basePathRestriction` semantics and do not widen to `"*"` without explicit review.
- Error messages & help: return generic error messages to the user but record detailed trace information via `IIoContext.AddTraceMessage` for diagnostics.



