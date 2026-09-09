---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Set-GitHubDefault
---

# Set-GitHubDefault

## SYNOPSIS

Sets the default GitHub owner and repository used by other GitHub commands.

## SYNTAX

### __AllParameterSets

```
Set-GitHubDefault [[-Owner] <string>] [[-Repository] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Persists the Owner and Repository as defaults for later GitHub commands. Pass `$null` (or omit
a parameter) to clear that value.

This is **not** the same as the GitHub CLI's own default repository (`gh repo set-default`).
`Set-GitHubDefault` writes a single, global, per-user default to `github-config.jsonc`,
independent of the current directory; this is intentional, so it keeps working outside of any
git repo, such as in Azure DevOps or GitHub Actions pipeline steps. `gh repo set-default` instead
sets a per-clone default in that repository's `.git/config`, used by `gh` commands like `gh pr`
and `gh issue`. If you want `gh` itself to default to a specific repository, run
`gh repo set-default` directly.

## EXAMPLES

### Example 1 - Set the default owner and repository

```powershell
Set-GitHubDefault -Owner "myorg" -Repository "myrepo"
```

## PARAMETERS

### -Owner

The name of the GitHub owner (user or organization) to use as the default. Pass `$null` to clear
it.
The name of the GitHub owner (user or organization) to use as the default.
Pass `$null` to clear
it.

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

### -Repository

The name of the GitHub repository to use as the default. Pass `$null` to clear it.
The name of the GitHub repository to use as the default.
Pass `$null` to clear it.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the owner or repository name to this cmdlet by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.GitHubDefaults

The Owner, Repository, and RepositoryUri that were persisted.

## NOTES

This cmdlet does not call or configure the GitHub CLI. See the DESCRIPTION section for how this
default relates to `gh repo set-default`.

## RELATED LINKS
