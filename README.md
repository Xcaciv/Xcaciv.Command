# Xcaciv.Command

Sexy and modular command framework capable of executing a line of text as a command with options without leaving command assembly in memory.

```csharp
    var commandManager = new Xc.Command.Manager();
    manager.LoadCommands(new Crawler());
    _ = manager.Run("Say Hello to my little friend");
```