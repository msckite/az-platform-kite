---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/23/2026
PlatyPS schema version: 2024-05-01
title: New-RandomPassword
---

# New-RandomPassword

## SYNOPSIS

Generates cryptographically secure random passwords with configurable complexity, length, and output formats.

## SYNTAX

### __AllParameterSets

```
New-RandomPassword [-Length <short>] [-Uppercase] [-Lowercase] [-Numbers] [-SpecialCharacters]
 [-SpecialCharacterSet <string>] [-AsSecureString] [-ExcludeCharacters <string>]
 [-NoAmbiguousCharacters] [-Count <int>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Generates random passwords using a cryptographically secure random number generator. By default,
passwords are 16 characters long and include uppercase letters, lowercase letters, numbers, and
special characters. When multiple character sets are enabled, each generated password contains at
least one character from every enabled set.

Use `-ExcludeCharacters` or `-NoAmbiguousCharacters` for human-readable passwords, and use
`-AsSecureString` when a password must be passed directly to cmdlets that accept secure strings.

## EXAMPLES

### Example 1 - Generate a default password

```powershell
New-RandomPassword
```

Generates one 16-character password containing uppercase, lowercase, numeric, and special
characters.

### Example 2 - Generate a longer password

```powershell
New-RandomPassword -Length 32
```

Generates one 32-character password.

### Example 3 - Exclude ambiguous characters

```powershell
New-RandomPassword -NoAmbiguousCharacters
```

Generates a password that excludes visually similar characters such as `0`, `O`, `o`, `1`, `l`,
`L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.

### Example 4 - Use a custom special character set

```powershell
New-RandomPassword -Length 20 -SpecialCharacterSet '!@#$%'
```

Generates a 20-character password and uses only the supplied special characters for the special
character category.

### Example 5 - Generate multiple passwords

```powershell
New-RandomPassword -Count 10
```

Generates 10 unique password objects.

### Example 6 - Return a SecureString

```powershell
$Password = New-RandomPassword -AsSecureString
```

Returns the generated password as a read-only `SecureString`.

## PARAMETERS

### -AsSecureString

Returns each generated password as a read-only `SecureString` instead of a password object.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Count

Specifies how many unique passwords to generate.

```yaml
Type: System.Int32
DefaultValue: 1
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

### -ExcludeCharacters

Specifies characters to remove from every enabled character set before generating passwords.

```yaml
Type: System.String
DefaultValue: None
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

### -Length

Specifies the password length. The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.
Specifies the password length.
The minimum length is 8 and the maximum length is 256.

```yaml
Type: System.Int16
DefaultValue: 16
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

### -Lowercase

Includes lowercase letters from `a` through `z`. This character set is enabled by default. Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.
Includes lowercase letters from `a` through `z`.
This character set is enabled by default.
Use
`-Lowercase:$false` to disable it.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: True
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

### -NoAmbiguousCharacters

Excludes predefined visually similar characters from every enabled character set. The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.
Excludes predefined visually similar characters from every enabled character set.
The excluded
characters are `0`, `O`, `o`, `1`, `l`, `L`, `i`, `I`, `5`, `S`, `s`, `8`, `B`, and `b`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Numbers

Includes numeric characters from `0` through `9`. This character set is enabled by default. Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.
Includes numeric characters from `0` through `9`.
This character set is enabled by default.
Use
`-Numbers:$false` to disable it.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: True
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

### -SpecialCharacters

Includes special characters. This character set is enabled by default. Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.
Includes special characters.
This character set is enabled by default.
Use
`-SpecialCharacters:$false` to disable it.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: True
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

### -SpecialCharacterSet

Specifies the characters used by the special character category.

```yaml
Type: System.String
DefaultValue: '!@#$%^&*()-_=+[]{}:,.?'
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

### -Uppercase

Includes uppercase letters from `A` through `Z`. This character set is enabled by default. Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.
Includes uppercase letters from `A` through `Z`.
This character set is enabled by default.
Use
`-Uppercase:$false` to disable it.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: True
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### MSCKite.Azure.Platform.Models.RandomPasswordResult

The default output object containing the generated password, password length, and complexity label.

### System.Security.SecureString

When `-AsSecureString` is specified, the cmdlet returns each generated password as a read-only
secure string.

## NOTES

`New-RandomPassword` uses `System.Security.Cryptography.RandomNumberGenerator` and rejection
sampling to avoid modulo bias when selecting characters.

## RELATED LINKS

- [New-RandomUniqueId](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/New-RandomUniqueId.md)
