---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/22/2026
PlatyPS schema version: 2024-05-01
title: New-PlatformWorkflow
---

# New-PlatformWorkflow

## SYNOPSIS

Copies the GitHub Actions workflow templates for the configured branch strategy into the
repository.

## SYNTAX

### __AllParameterSets

```
New-PlatformWorkflow [[-InputFolder] <string>] [[-OutputFolder] <string>] [-BranchStrategy <string>]
 [-GlobalConfigPath <string>] [-Force] [-WhatIf] [-Confirm]
```

## ALIASES

## DESCRIPTION

Reads `github/workflows/manifest.jsonc` from a downloaded templates folder and copies the shared
workflow templates, plus the ones belonging to the selected branch strategy, to their destination
in the repository. The manifest maps every template to a fixed path under `.github`, because
reusable workflows referenced as `./.github/workflows/<name>.yml` must sit directly in
`.github/workflows`.

The branch strategy is read from `sourceControl.branchStrategy` in global-config.jsonc unless
`-BranchStrategy` is specified. Files that already exist at the destination are reported and left
untouched unless `-Force` is specified, so local edits are never lost by accident.

Run `Get-PlatformTemplate` first to download the templates. This cmdlet only copies files, so it
needs no Azure or GitHub sign-in.

## EXAMPLES

### Example 1 - Install the workflows for the configured branch strategy

```powershell
Get-PlatformTemplate -IncludedFolders 'templates' -OutputFolder ./.downloads
New-PlatformWorkflow
```

Copies the shared templates and the ones for the branch strategy declared in
`config/global-config.jsonc` into `.github` of the current repository.

### Example 2 - Install a specific strategy and overwrite existing files

```powershell
New-PlatformWorkflow -InputFolder ./.downloads/templates -OutputFolder . -BranchStrategy release -Force
```

### Example 3 - Preview which files would be copied

```powershell
New-PlatformWorkflow -BranchStrategy github -WhatIf
```

## PARAMETERS

### -BranchStrategy

The branch strategy whose workflow templates are installed, matching a strategy declared in the
manifest (`github` or `release`).
Defaults to `sourceControl.branchStrategy` in global-config.jsonc.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Force

Overwrites workflow files that already exist at the destination instead of skipping them.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -GlobalConfigPath

Path to the global-config.jsonc file the branch strategy is read from when `-BranchStrategy` is
omitted.
Defaults to `config/global-config.jsonc`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -InputFolder

Path to the downloaded templates folder containing `github/workflows/manifest.jsonc`.
Defaults to
`.downloads/templates`, the location `Get-PlatformTemplate` writes to.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -OutputFolder

Path to the repository root the `.github` folder is created in.
Defaults to the current folder.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the template folder, repository root, branch strategy, or global config path by
property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.PlatformWorkflowResult

The resolved branch strategy and its environments, the manifest used, the files copied, the files
skipped because they already existed, and whether the installation succeeded.

## NOTES

## RELATED LINKS

- [Get-PlatformTemplate](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Get-PlatformTemplate.md)
- [New-PlatformConfigStructure](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/New-PlatformConfigStructure.md)

