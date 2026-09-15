---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/15/2026
PlatyPS schema version: 2024-05-01
title: New-PlatformConfigStructure
---

# New-PlatformConfigStructure

## SYNOPSIS

Scaffolds the platform configuration folder, creating a `global-config.jsonc` file either from a default template or copied from an existing input folder.

## SYNTAX

### __AllParameterSets

```
New-PlatformConfigStructure [[-OutputFolder] <string>] [-InputFolder <string>] [-Force] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates `-OutputFolder` (default `config`) if it doesn't exist and writes a `global-config.jsonc`
file into it. Without `-InputFolder`, a default template is written. With `-InputFolder`, the
cmdlet copies `global-config.jsonc` from that folder instead, for example the `templates` folder
downloaded by `Get-PlatformTemplate`.

Fails if the output folder already contains a platform configuration structure, unless `-Force`
is specified to overwrite it.

## EXAMPLES

### Example 1 - Scaffold config using the default template

```powershell
New-PlatformConfigStructure
```

### Example 2 - Scaffold config from a downloaded templates folder

```powershell
Get-PlatformTemplate -OutputFolder ./config-src
New-PlatformConfigStructure -InputFolder ./config-src/templates -OutputFolder ./config
```

### Example 3 - Overwrite an existing configuration folder

```powershell
New-PlatformConfigStructure -OutputFolder ./config -Force
```

## PARAMETERS

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

Overwrites an already-initialized configuration folder instead of failing.

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

### -InputFolder

Folder containing a `global-config.jsonc` file to copy into `-OutputFolder`. When omitted, a
default `global-config.jsonc` template is generated instead.
Folder containing a `global-config.jsonc` file to copy into `-OutputFolder`.
When omitted, a
default `global-config.jsonc` template is generated instead.

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

### -OutputFolder

Destination folder for the platform configuration structure. Defaults to `config`.
Destination folder for the platform configuration structure.
Defaults to `config`.

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

You can pipe a destination path to `-OutputFolder` by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.PlatformConfigFolderResult

The resolved configuration root path, any folders created, the `global-config.jsonc` path, and
whether scaffolding succeeded.

## NOTES

## RELATED LINKS

- [Get-PlatformTemplate](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Get-PlatformTemplate.md)
- [Test-PlatformTemplate](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Test-PlatformTemplate.md)

