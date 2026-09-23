---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/23/2026
PlatyPS schema version: 2024-05-01
title: Test-PlatformTemplate
---

# Test-PlatformTemplate

## SYNOPSIS

Reports template and schema version compatibility and whether a newer template is available.

## SYNTAX

### __AllParameterSets

```
Test-PlatformTemplate [-TemplatePath] <string> [-SchemaPath] <string> [-LatestTemplatePath <string>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Requires a `templateVersion` in the template and a `schemaVersion` in the schema. Both values
must use `Major.Minor.Patch` SemVer. A pair is compatible only when the versions match exactly.
An unversioned or malformed file is rejected. Provide `-LatestTemplatePath` to compare the
current template with a separately downloaded template; the command reports update availability
without changing either file.

## EXAMPLES

### Example 1 - Validate a template/schema pair

```powershell
Test-PlatformTemplate -TemplatePath ./config/global-config.jsonc -SchemaPath ./schemas/global-config.schema.json
```

### Example 2 - Check for a newer downloaded template

```powershell
Test-PlatformTemplate -TemplatePath ./config/global-config.jsonc -SchemaPath ./schemas/global-config.schema.json -LatestTemplatePath ./.downloads/templates/global-config.jsonc
```

## PARAMETERS

### -LatestTemplatePath

Path to a separately downloaded template to compare against `-TemplatePath` for update
availability. When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.
Path to a separately downloaded template to compare against `-TemplatePath` for update
availability.
When omitted, no update check is performed.

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

### -SchemaPath

Path to the JSON schema file containing the `schemaVersion` to validate against the template.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TemplatePath

Path to the template file containing the `templateVersion` to validate against the schema.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe a template path to `-TemplatePath` by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.PlatformTemplateVersionResult

The template and schema paths, their versions, compatibility status, and (when
`-LatestTemplatePath` is specified) whether a newer template is available.

## NOTES

Use matching template and schema releases. Increase major versions for breaking changes, and
increase minor or patch versions for compatible changes. All templates must use the versioned
object format.

## RELATED LINKS

- [Get-PlatformTemplate](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Get-PlatformTemplate.md)
- [New-PlatformConfigStructure](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/New-PlatformConfigStructure.md)
