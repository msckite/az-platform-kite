---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/15/2026
PlatyPS schema version: 2024-05-01
title: Get-PlatformTemplate
---

# Get-PlatformTemplate

## SYNOPSIS

Downloads one or more folders (with their subfolders and files) from the Azure Platform Kite repository into a local folder.

## SYNTAX

### __AllParameterSets

```
Get-PlatformTemplate [[-IncludedFolders] <string[]>] [[-OutputFolder] <string>]
 [-RepositoryUrl <string>] [-Branch <string>] [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Shallow-clones the `az-platform-kite` repository (or `-RepositoryUrl`/`-Branch` override) to a
temporary folder, copies the folder(s) listed in `-IncludedFolders` into `-OutputFolder`
(preserving their relative path and subfolders/files), then deletes the temporary clone.

By default, `-IncludedFolders` is `templates` and `-OutputFolder` is `.downloads`, a
dedicated folder so rerunning the cmdlet never overwrites configuration files already in use
elsewhere in the workspace. Use `-Force` to overwrite files that already exist at the
destination.

## EXAMPLES

### Example 1 - Download the default templates folder

```powershell
Get-PlatformTemplate
```

Copies the repository's `templates` folder into `.downloads\templates`.

### Example 2 - Download specific folders to a custom location

```powershell
Get-PlatformTemplate -IncludedFolders 'templates', 'templates/github' -OutputFolder ./config-src -Force
```

### Example 3 - Download from a specific branch and feed New-PlatformConfigStructure

```powershell
Get-PlatformTemplate -Branch release -OutputFolder ./config-src
New-PlatformConfigStructure -InputFolder ./config-src/templates -OutputFolder ./config
```

## PARAMETERS

### -Branch

The repository branch to clone. Defaults to `main`.
The repository branch to clone.
Defaults to `main`.

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
  ValueFromPipelineByPropertyName: false
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

Overwrites files that already exist at the destination instead of failing.

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

### -IncludedFolders

Relative folder path(s) inside the repository to copy, including their subfolders and files.
Defaults to `templates`.

```yaml
Type: System.String[]
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

Local folder to copy the downloaded folder(s) into. Defaults to `.downloads`, a dedicated folder
separate from any in-use configuration so reruns never overwrite it.
Local folder to copy the downloaded folder(s) into.
Defaults to `.downloads`, a dedicated folder
separate from any in-use configuration so reruns never overwrite it.

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

### -RepositoryUrl

The git URL to clone. Defaults to the `az-platform-kite` repository.
The git URL to clone.
Defaults to the `az-platform-kite` repository.

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
  ValueFromPipelineByPropertyName: false
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

### System.String[]

You can pipe folder paths to `-IncludedFolders` by property name.

### System.String

You can pipe a destination path to `-OutputFolder` by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.RepoTemplateDownloadResult

The repository URL/branch, the resolved output folder, the copied folder and file paths, and
whether the download succeeded.

## NOTES

Requires the `git` CLI to be installed and available on `PATH`.

## RELATED LINKS

- [New-PlatformConfigStructure](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/New-PlatformConfigStructure.md)
- [Test-PlatformTemplate](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Test-PlatformTemplate.md)

