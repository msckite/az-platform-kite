---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/15/2026
PlatyPS schema version: 2024-05-01
title: New-RandomUniqueId
---

# New-RandomUniqueId

## SYNOPSIS

Generates a simple random alphanumeric identifier for non-security use cases.

## SYNTAX

### __AllParameterSets

```
New-RandomUniqueId [-Length <short>]
```

## ALIASES

## DESCRIPTION

Generates a random string from uppercase letters, lowercase letters, and numbers. The identifier is
intended for test data, resource suffixes, demo environments, and temporary object names. It does
not contain special characters and is not suitable for passwords, secrets, or security tokens.

## EXAMPLES

### Example 1 - Generate an identifier with the default length

```powershell
New-RandomUniqueId
```

Generates an 8-character alphanumeric string.

### Example 2 - Generate a shorter resource suffix

```powershell
$ProjectSuffix = New-RandomUniqueId -Length 6
$StorageAccountName = "stg$ProjectSuffix"
$ResourceGroupName = "rg-$ProjectSuffix"
```

Generates a 6-character suffix and uses it in temporary Azure resource names.

### Example 3 - Supply lengths through the pipeline

```powershell
@(
    [pscustomobject]@{ Length = 4 }
    [pscustomobject]@{ Length = 12 }
) | New-RandomUniqueId
```

Generates one identifier for each input object by binding its `Length` property.

## PARAMETERS

### -Length

Specifies the identifier length. The minimum length is 4, the maximum length is 12, and the default
length is 8.

```yaml
Type: System.Int16
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int16

You can pipe an object with a `Length` property to this cmdlet.

## OUTPUTS

### System.String

The cmdlet returns the generated identifier as a plain string.

## NOTES

This cmdlet uses a pseudo-random number generator and does not provide cryptographic security or
guaranteed global uniqueness.

## RELATED LINKS

