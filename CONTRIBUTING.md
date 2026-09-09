<!-- omit from toc -->
# Contributing

<!-- omit from toc -->
## Table of Contents
- [Formatting guidelines](#formatting-guidelines)
- [Documentation Standards](#documentation-standards)
- [Building and testing locally](#building-and-testing-locally)
- [Help authoring](#help-authoring)
- [Modules Structure](#modules-structure)
- [Terminology](#terminology)

## Formatting guidelines

To make life a bit easier, a `.vscode/settings.json` file is configured to enforce _some_ of the syntax and style guidelines automatically. These will apply when `Saving` a file for example `cs`, `ps1`, `json`, `yaml` or `md` files. For this to work you need to install the recommended `extensions` in vscode.

- Use `PascalCase` for _all_ public identifiers like module names, function names, properties, parameters, global variables and constants.
- Use `camelCase` for _all_ variables within functions (or modules) to distinguish private variables from parameters.
- Use `camelCase` for _all_ keys within Json files.
- Use `four` spaces per indentation level.
- Use [approved verbs](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands) for PowerShell Commands.
- Take notice of the [PowerShell development guidelines](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/strongly-encouraged-development-guidelines) when you write modules or cmdlets.
- Take notice of the [PowerShell-Docs](https://learn.microsoft.com/en-us/powershell/scripting/community/contributing/powershell-style-guide) style guide when you write documentation content.

## Documentation Standards

- Use GitHub-flavored Markdown with proper heading hierarchy.
- Leverage alert blocks (`> [!NOTE]`, `> [!IMPORTANT]`, `> [!WARNING]`) for significant content (see [Markdown Styling](#markdown-styling)).
- Include code examples with proper syntax highlighting (specify language in fenced code blocks).
- Keep line length reasonable for readability in various editors.
- Use relative links for cross-referencing files within the repository.
- Validate all external links periodically to prevent link rot.

### Markdown Styling

Use alerts to provide distinctive styling for significant content.

> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.

## Building and testing locally

Azure Platform Kite is a PowerShell **binary** module: once its `.dll` is imported into a session
(via `Import-Module`), PowerShell keeps that file locked for the lifetime of the session. If you
then try to build again in a session that already has the module loaded, you'll get file-in-use
errors such as:

```text
error MSB3027: Could not copy "...\MSCKite.Azure.Platform.dll" ... Beginning retry 1 in 1000ms.
error MSB3021: Unable to copy file "...\MSCKite.Azure.Platform.dll" ... The process cannot access the file because it is being used by another process.
```

`Remove-Module` only unregisters the module's PowerShell commands, it does **not** release the
underlying .NET assembly from the process. Once a session has loaded the DLL, that process keeps
a file lock on it for as long as the process stays alive (there's no supported way to unload a
single assembly).

The `.vscode/tasks.json` and `.vscode/launch.json` files are set up so the build always runs
outside of any session that has the module loaded, following
[Using Visual Studio Code to debug compiled cmdlets](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/vscode/using-vscode-for-debugging-compiled-cmdlets).

### Building

Don't run `dotnet build` directly in a terminal where you've also imported the module. Instead,
use the **build** task:

1. Open the **Command Palette** (`Ctrl+Shift+P`).
2. Run **Tasks: Run Build Task** (or press `Ctrl+Shift+B`).
3. Choose **build**.

### Debugging

1. Set a breakpoint in the cmdlet's source code.
2. Open the **Debug** pane (`Ctrl+Shift+D`) and select the **PowerShell cmdlets: pwsh**
   configuration.
3. Press `F5` (or click **Start Debugging**). This runs the **build** task first (via
   `preLaunchTask`), then launches `pwsh` with the module imported in the integrated terminal.
4. In the integrated terminal, invoke the cmdlet you want to debug. Execution stops at your
   breakpoint, where you can step through code and inspect variables.
5. When you're done, press `Shift+F5` (or click **Stop**) to end the debug session. This closes
   the `pwsh` process and releases its lock on the DLL, so the module can be rebuilt again.

> [!IMPORTANT]
> Never `Import-Module` the built DLL in a terminal you also build in, that session will hold a
> file lock on the DLL until it closes, and the next build there will fail even after
> `Remove-Module`. Always use the **Run Build Task** command or the debug launch configuration,
> which import the module in the isolated integrated terminal spawned by the debugger instead.

## Help authoring

`Get-Help` for this module is generated from Markdown source via PlatyPS, not from the C# XML
doc comments on cmdlet classes. See [docs/help-authoring.md](./docs/help-authoring.md) for the
`platyps.ps1` workflow used to create, update, validate, and publish that help content.

## Modules Structure

```text
src
│
├── Commands
│   │
│   ├── Azure
│   │   └── .gitkeep
│   │
│   ├── Common
│   │   └── .gitkeep
│   │
│   ├── DevOps
│   │    ├── ConnectAdoOrganization.cs
│   │    ├── DisconnectAdoOrganization.cs
│   │    ├── GetAdoDefault.cs
│   │    └── SetAdoDefault.cs
│   │
│   ├── GitHub
│   │   ├── GetGitHubDefault.cs
│   │   └── SetGitHubDefault.cs
│   │
│   └── Platform
│       ├── DisconnectPlatformContext.cs
│       └── GetPlatformContext.cs
│
├── Internal
│   │
│   ├── Azure
│   │   └── AzureContextHelper.cs
│   │
│   ├── Common
│   │   ├── ModulePaths.cs
│   │   └── PowerShellModuleLoader.cs
│   │
│   ├── DevOps
│   │   ├── AdoApiHelper.cs
│   │   ├── AdoAuthHelper.cs
│   │   ├── AdoConfigStore.cs
│   │   ├── AdoRestClient.cs
│   │   ├── AdoSessionState.cs
│   │   └── AdoSessionStore.cs
│   │
│   └── GitHub
│       ├── GitHubCliHelper.cs
│       └── GitHubConfigStore.cs
│
└── Models
    ├── AdoDefaults.cs
    ├── AzureContext.cs
    ├── DevOpsContext.cs
    ├── GitHubContext.cs
    ├── GitHubDefaults.cs
    └── PowerShellModule.cs
```

## Terminology

- `lowercase` - all lowercase, no word separation.
- `UPPERCASE` - all capitals, no word separation.
- `PascalCase` - capitalize the first letter of each word.
- `camelCase` - capitalize the first letter of each word _except_ the first.
- `kebab-case` - all lowercase, with dash (`-`) word separation.
- `snake_case` - all lowercase, with underscore (`_`) word separation.
