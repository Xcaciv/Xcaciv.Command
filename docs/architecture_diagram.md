# Xcaciv.Command Architecture Diagram

## Class Structure UML

```mermaid
graph TB
    subgraph "CommandController Layer"
        CC["CommandController"]
        CC -->|implements| ICC["ICommandController"]
    end

    subgraph "Command Registry & Discovery"
        CR["CommandRegistry"]
        CR -->|implements| ICR["ICommandRegistry"]
        CL["CommandLoader"]
        CL -->|implements| ICL["ICommandLoader"]
    end

    subgraph "Execution Pipeline"
        PE["PipelineExecutor"]
        PE -->|implements| IPE["IPipelineExecutor"]
        CE["CommandExecutor"]
        CE -->|implements| ICE["ICommandExecutor"]
        CF["CommandFactory"]
        CF -->|implements| ICF["ICommandFactory"]
    end

    subgraph "Command Interfaces"
        ICD["ICommandDelegate"]
        ICDE["ICommandDescription"]
        ICDE -->|contains| SCDE["SubCommands"]
    end

    subgraph "Context & IO"
        IIO["IIoContext"]
        IEC["IEnvironmentContext"]
        EC["EnvironmentContext"]
        EC -->|implements| IEC
    end

    subgraph "Support Services"
        IAL["IAuditLogger"]
        IOE["IOutputEncoder"]
        NAL["NoOpAuditLogger"]
        NAL -->|implements| IAL
        NOE["NoOpEncoder"]
        NOE -->|implements| IOE
        PC["PipelineConfiguration"]
    end

    subgraph "File Loading"
        ICrawler["ICrawler"]
        IVS["IVerifiedSourceDirectories"]
    end

    CC -->|uses| CR
    CC -->|uses| CL
    CC -->|uses| PE
    CC -->|uses| CE
    CC -->|uses| CF
    
    CE -->|uses| CR
    CE -->|uses| CF
    
    CF -->|creates| ICD
    
    CR -->|stores| ICDE
    CL -->|discovers| ICDE
    CL -->|uses| ICrawler
    CL -->|uses| IVS
    
    PE -->|executes via| ICE
    
    CC -->|uses| IIO
    CC -->|uses| IEC
    CC -->|uses| IAL
    CC -->|uses| IOE
    CC -->|uses| PC
    
    IIO -->|manages| ICD
    CE -->|logs to| IAL
    IIO -->|encodes with| IOE
```

## Detailed Class Relationships

```mermaid
classDiagram
    class CommandController {
        -ICommandRegistry _commandRegistry
        -ICommandLoader _commandLoader
        -IPipelineExecutor _pipelineExecutor
        -ICommandExecutor _commandExecutor
        -ICommandFactory _commandFactory
        -IAuditLogger _auditLogger
        -IOutputEncoder _outputEncoder
        -string _helpCommand
        +RegisterBuiltInCommands()
        +AddPackageDirectory(string)
        +LoadCommands(string)
        +AddCommand(string, ICommandDelegate, bool)
        +AddCommand(string, Type, bool)
        +AddCommand(ICommandDescription)
        +Run(string, IIoContext, IEnvironmentContext)*
        +Run(string, IIoContext, IControllerEnvironmentContext, CancellationToken)*
    }

    class CommandRegistry {
        -Dictionary~string, ICommandDescription~ _commands
        +AddCommand(ICommandDescription)
        +AddCommand(string, Type, bool)
        +AddCommand(string, ICommandDelegate, bool)
        +TryGetCommand(string, ICommandDescription)*
        +GetAllCommands() IEnumerable~ICommandDescription~
    }

    class CommandLoader {
        -ICrawler _crawler
        -IVerifiedSourceDirectories _verifiedDirectories
        +AddPackageDirectory(string)
        +SetRestrictedDirectory(string)
        +LoadCommands(string, Action~ICommandDescription~)
    }

    class PipelineExecutor {
        -PipelineConfiguration Configuration
        +ExecuteAsync(string, IIoContext, IEnvironmentContext, Func)*
    }

    class CommandExecutor {
        -ICommandRegistry _registry
        -ICommandFactory _commandFactory
        -IAuditLogger _auditLogger
        +ExecuteAsync(string, IIoContext, IEnvironmentContext)*
        +GetHelpAsync(string, IIoContext, IEnvironmentContext)*
    }

    class CommandFactory {
        -IServiceProvider _serviceProvider
        +CreateCommand(ICommandDescription, IIoContext) ICommandDelegate
        +CreateCommand(string, string) ICommandDelegate
    }

    class ICommandRegistry {
        <<interface>>
        +AddCommand(ICommandDescription)*
        +AddCommand(string, Type, bool)*
        +AddCommand(string, ICommandDelegate, bool)*
        +TryGetCommand(string, ICommandDescription)*
        +GetAllCommands() IEnumerable~ICommandDescription~*
    }

    class ICommandLoader {
        <<interface>>
        +Directories IReadOnlyList~string~*
        +AddPackageDirectory(string)*
        +SetRestrictedDirectory(string)*
        +LoadCommands(string, Action~ICommandDescription~)*
    }

    class IPipelineExecutor {
        <<interface>>
        +Configuration PipelineConfiguration*
        +ExecuteAsync(string, IIoContext, IEnvironmentContext, Func)*
    }

    class ICommandExecutor {
        <<interface>>
        +HelpCommand string*
        +AuditLogger IAuditLogger*
        +ExecuteAsync(string, IIoContext, IEnvironmentContext)*
        +GetHelpAsync(string, IIoContext, IEnvironmentContext)*
    }

    class ICommandFactory {
        <<interface>>
        +CreateCommand(ICommandDescription, IIoContext) ICommandDelegate*
        +CreateCommand(string, string) ICommandDelegate*
    }

    class ICommandDelegate {
        <<interface>>
        +HandleExecution(IIoContext, IEnvironmentContext) IAsyncEnumerable~CommandResult~*
        +HandlePipedChunk(CommandResult) IAsyncEnumerable~CommandResult~*
        +DisposeAsync() ValueTask*
    }

    class ICommandDescription {
        <<interface>>
        +BaseCommand string*
        +SubCommands Dictionary~string, ICommandDescription~*
        +FullTypeName string*
        +ModifiesEnvironment bool*
        +PackageDescription PackageDescription*
    }

    class IIoContext {
        <<interface>>
        +HasPipedInput bool*
        +Parameters string[]*
        +SetParameters(string[]) Task*
        +WriteOutput(string) Task*
        +AddTraceMessage(string, string) Task*
    }

    class IEnvironmentContext {
        <<interface>>
        +GetVariable(string) string*
        +SetVariable(string, string) Task*
    }

    class EnvironmentContext {
        -Dictionary~string, string~ _variables
        +GetVariable(string) string
        +SetVariable(string, string) Task
    }

    class IAuditLogger {
        <<interface>>
        +LogCommandExecution(string, string[], string) Task*
        +LogEnvironmentChange(string, string, string) Task*
    }

    class IOutputEncoder {
        <<interface>>
        +Encode(string) string*
    }

    class NoOpAuditLogger {
        +LogCommandExecution(string, string[], string) Task
        +LogEnvironmentChange(string, string, string) Task
    }

    class NoOpEncoder {
        +Encode(string) string
    }

    class PipelineConfiguration {
        +MaxBufferSize int
        +Timeout TimeSpan
    }

    CommandController --> ICommandRegistry
    CommandController --> ICommandLoader
    CommandController --> IPipelineExecutor
    CommandController --> ICommandExecutor
    CommandController --> ICommandFactory
    CommandController --> IIoContext
    CommandController --> IEnvironmentContext
    CommandController --> IAuditLogger
    CommandController --> IOutputEncoder

    CommandRegistry ..|> ICommandRegistry
    CommandLoader ..|> ICommandLoader
    PipelineExecutor ..|> IPipelineExecutor
    CommandExecutor ..|> ICommandExecutor
    CommandFactory ..|> ICommandFactory

    CommandExecutor --> ICommandRegistry
    CommandExecutor --> ICommandFactory
    CommandExecutor --> IAuditLogger

    CommandFactory --> ICommandDelegate

    EnvironmentContext ..|> IEnvironmentContext

    NoOpAuditLogger ..|> IAuditLogger
    NoOpEncoder ..|> IOutputEncoder

    PipelineConfiguration --> CommandController
```

## Component Interaction Flow

```mermaid
sequenceDiagram
    participant User as User Code
    participant CC as CommandController
    participant CR as CommandRegistry
    participant PE as PipelineExecutor
    participant CE as CommandExecutor
    participant CF as CommandFactory
    participant CD as ICommandDelegate

    User->>CC: Run(commandLine, ioContext, env)
    CC->>CC: Parse command line & args
    alt Has Pipeline
        CC->>PE: ExecuteAsync(commandLine, ...)
        PE->>PE: Split on '|' and create stages
    else Single Command
        CC->>CC: Get command name and args
    end
    
    CC->>CR: TryGetCommand(commandName)
    CR-->>CC: ICommandDescription
    
    CC->>CF: CreateCommand(description, ioContext)
    CF-->>CC: ICommandDelegate instance
    
    CC->>CE: ExecuteAsync(commandKey, ioContext, env)
    CE->>CD: HandleExecution(ioContext, env)
    CD-->>CE: IAsyncEnumerable<CommandResult>
    
    CE->>ioContext: WriteOutput(result)
    
    opt If command modifies environment
        CE->>env: SetVariable(...)
        CE->>IAuditLogger: LogEnvironmentChange(...)
    end
```

## Key Design Patterns

### 1. **Dependency Injection**

- All major components accept interfaces through constructor injection
- Supports testing with mock implementations
- CommandController provides default implementations when not supplied

### 2. **Strategy Pattern**

- `ICommandLoader` - Different strategies for discovering commands
- `IPipelineExecutor` - Pipeline execution strategy
- `ICommandFactory` - Command instantiation strategy

### 3. **Factory Pattern**

- `CommandFactory` - Creates command instances
- Supports both direct instantiation and dependency injection resolution

### 4. **Registry Pattern**

- `CommandRegistry` - Centralized command lookup and registration

### 5. **Observer/Audit Pattern**

- `IAuditLogger` - Tracks command execution and environment changes
- `NoOpAuditLogger` - Default no-operation implementation

### 6. **Pipeline Pattern**

- Commands connected via channels for streaming data
- `IIoContext` manages bounded channels for backpressure control

## Component Responsibilities

| Component | Responsibility |
|-----------|-----------------|
| **CommandController** | Orchestration, routing, entry point |
| **CommandRegistry** | Command storage and lookup |
| **CommandLoader** | Plugin discovery from file system |
| **CommandExecutor** | Command instantiation and execution lifecycle |
| **CommandFactory** | Instance creation with DI support |
| **PipelineExecutor** | Multi-stage pipeline coordination |
| **EnvironmentContext** | Environment variable isolation |
| **IAuditLogger** | Security audit trail logging |

## Security Boundaries

1. **Plugin Loading** - Verified directories only via `IVerifiedSourceDirectories`
2. **Environment Mutation** - Only commands with `ModifiesEnvironment=true` can modify
3. **Assembly Loading** - Dynamic loading through `AssemblyContext` with path restrictions
4. **Input Validation** - Framework validates parameters before command execution
5. **Audit Logging** - All security-relevant operations logged via `IAuditLogger`
