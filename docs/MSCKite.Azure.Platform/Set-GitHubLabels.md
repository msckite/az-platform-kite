---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/15/2026
PlatyPS schema version: 2024-05-01
title: Set-GitHubLabels
---

# Set-GitHubLabels

## SYNOPSIS

Creates, updates, and removes GitHub repository labels to match a labels definition file.

## SYNTAX

### __AllParameterSets

```
Set-GitHubLabels [-LabelFilePath] <string> [[-Owner] <string>] [[-Repository] <string>]
 [-KeepExistingLabels] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reconciles the labels on a GitHub repository with a jsonc definitions file, matching labels by
their `name`.
Labels present in the file but missing on the repository are created, labels that
exist on both but have a different `color` or `description` are updated, and labels that exist on
the repository but aren't listed in the file are removed unless `-KeepExistingLabels` is
specified.

The labels file must be a JSON object with a `templateVersion` in `Major.Minor.Patch` SemVer
format and a `labels` array property. JSONC comments and trailing commas are allowed. Each label object
requires a `name` and a `color` (a 6-digit hex value, with or without a leading `#`, e.g.
`#d73a4a`), and may include an optional `description`.

`Owner` and `Repository` fall back to the values stored via `Set-GitHubDefault` when not
specified.
This cmdlet supports `-WhatIf`/`-Confirm` and shells out to the GitHub CLI (`gh`), so
you must be signed in (`gh auth login`) with permission to manage labels on the target
repository.

## EXAMPLES

### Example 1 - Sync labels using the stored default owner/repository

```powershell
Set-GitHubLabels -LabelFilePath ./templates/github-labels.jsonc
```

### Example 2 - Preview changes without applying them

```powershell
Set-GitHubLabels -LabelFilePath ./templates/github-labels.jsonc -Owner "myorg" -Repository "myrepo" -WhatIf
```

### Example 3 - Sync labels without removing extras

```powershell
Set-GitHubLabels -LabelFilePath ./templates/github-labels.jsonc -Owner "myorg" -Repository "myrepo" -KeepExistingLabels
```

Creates missing labels and updates changed ones, but leaves any repository labels that aren't
listed in the file untouched instead of removing them.

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

### -KeepExistingLabels

Preserves repository labels that aren't present in the input file instead of removing them.

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

### -LabelFilePath

Path to a versioned jsonc file containing a `templateVersion` and the desired `labels` array.
Each label requires a `name` and a `color` (6-digit hex,
with or without a leading `#`), and may include a `description`.
Path to a jsonc file containing the desired labels: either a top-level array of label objects, or
an object with a `labels` array property.
Each label requires a `name` and a `color` (6-digit hex,
with or without a leading `#`), and may include a `description`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Path
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Owner

The name of the GitHub owner (user or organization) of the repository.
Falls back to the value
stored via `Set-GitHubDefault` when not specified.

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

### -Repository

The name of the GitHub repository whose labels should be synced.
Falls back to the value stored
via `Set-GitHubDefault` when not specified.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
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

You can pipe the label file path, owner, or repository name to this cmdlet by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.GitHubLabelSyncResult

The Owner, Repository, and the names of labels that were Added, Updated, Removed, and Unchanged.

## NOTES

This cmdlet requires the GitHub CLI (`gh`) to be installed and signed in
(`gh auth login`) with permission to manage labels on the target repository.

## RELATED LINKS

- [Set-GitHubDefault](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-GitHubDefault.md)

