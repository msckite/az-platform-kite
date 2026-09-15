<!-- omit from toc -->
# Templates

## Template versioning

Every shipped JSON template has a required `templateVersion` and every matching JSON Schema has a `schemaVersion`. Both use `Major.Minor.Patch` SemVer and the versions must match exactly for a template/schema pair to be compatible. The schemas also require the matching template version, so standard JSON Schema validators reject an incompatible template.

Use `Test-PlatformTemplate` after downloading templates, supplying the corresponding local schema. Pass a newer downloaded template with `-LatestTemplatePath` to report whether an update is available without overwriting the configuration currently in use.

```powershell
Test-PlatformTemplate `
  -TemplatePath .\config\global-config.jsonc `
  -SchemaPath .\schemas\global-config.schema.json `
  -LatestTemplatePath .\.downloads\templates\global-config.jsonc
```

> [!NOTE]
> `-SchemaPath` always points at a local schema file, never the `$schema` URL embedded in the
template, so this check runs offline. It's primarily useful when maintaining templates and
schemas in this repo (keeping a pair's versions in sync as changes are made) and for anyone who
has downloaded both a template and its matching schema and wants to confirm compatibility, or
check for a newer template, before adopting it.

Increment the major version for breaking contract changes and increment minor or patch versions for compatible additions or corrections, updating the template and schema together. All template consumers require the versioned object format with `templateVersion`.
