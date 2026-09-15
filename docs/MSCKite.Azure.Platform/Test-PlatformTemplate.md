---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/15/2026
PlatyPS schema version: 2024-05-01
title: Test-PlatformTemplate
---

# Test-PlatformTemplate

## SYNOPSIS

Reports template and schema version compatibility and whether a newer template is available.

## SYNTAX

```powershell
Test-PlatformTemplate [-TemplatePath] <string> [-SchemaPath] <string> [-LatestTemplatePath <string>]
```

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

## NOTES

Use matching template and schema releases. Increase major versions for breaking changes, and
increase minor or patch versions for compatible changes. All templates must use the versioned
object format.
