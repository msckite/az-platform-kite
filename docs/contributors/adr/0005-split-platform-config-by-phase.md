<!-- omit from toc -->
# 0005: Split platform-config.jsonc into per-phase files with defaults and pipelines

- Status: Accepted
- Date: 2026-10-16

## Context

`templates/platform-config.jsonc` declares all four provisioning phases in one file of about 560
lines: `resourceGroups` (phase 1), `securityGroups` (phase 2) and `environments` (phases 3 and 4).
The `environments` array is about 440 lines of it, because every environment repeats its identity,
federated credential and GitHub environment blocks, including a second set for the workload
identity.

Every phase cmdlet already takes its own `-PlatformConfigPath`, and the loaders
(`LoadResourceGroups`, `LoadSecurityGroups`, `LoadEnvironments`) each read one top-level property,
so the phases are already independent readers of one shared file. Phases 3 and 4 both read
`environments`, so the natural split has three files, not four.

## Decision

Replace the single file with a `config/platform/` folder of three files, one per top-level array:

| File | Phase | Consumed by |
| --- | --- | --- |
| `config/platform/resource-groups.jsonc` | 1 | `Set-PlatformResourceGroup` (and as a lookup by phases 2 to 4) |
| `config/platform/security-groups.jsonc` | 2 | `Set-PlatformSecurityGroup` |
| `config/platform/environments.jsonc` | 3 and 4 | `Set-PlatformEnvironmentIdentity`, `Set-PlatformGitHubEnvironment` |

- Each file keeps `$schema` and `templateVersion` and gets its own schema in `schemas/`, versioned
  like the existing pairs. The `${placeholder}` tokens and the cross-file references by
  `resourceGroupId` keep their current meaning.
- Each cmdlet's `-PlatformConfigPath` is replaced by one path parameter per file it reads, with
  defaults under `config/platform/`.
- This is a breaking change to the configuration contract, so the templates and schemas move to
  `2.0.0` and `docs/consumers/upgrade-path/v1.1.x-to-v2.0.0.md` is the migration guide.
- The loader does not read the legacy single file. A consumer splits it by moving each top-level
  array into its file; no semantic changes are needed.
- `environments.jsonc` gets an optional top-level `defaults` object so the repeated blocks are
  written once. It is part of the same v2.0.0 release, so a consumer upgrades once:
  - `defaults.federatedCredential` supplies `issuer`, `subjectType` and `audiences`; a credential
    then only needs its `name`.
  - `defaults.githubEnvironment` supplies `protectionRules`, `secrets` and `variables` for every
    pipeline `githubEnvironment`.
  - A value set on an environment always wins. Credential settings and protection rules are filled
    in field by field; secrets and variables are merged by name, defaults first, and an
    environment entry replaces the default with the same name.
  - The defaults are folded into each environment while the file is loaded, so the cmdlets and the
    result of a run are the same as with every block spelled out.
  - Environment variables such as `ENV_CODE` stay per environment: the bootstrap environments
    must not receive them, and a default cannot be switched off for a single environment.
- Each environment declares its identities and GitHub environments as a `pipelines` array. It
  replaces `userAssignedIdentity`, `githubEnvironment`, `workloadUserAssignedIdentity` and
  `workloadGithubEnvironment`, which no longer exist, so the workload pair is not a special case
  next to the official names:
  - An entry has a `purpose` (for example `infra` or `workload`), a `userAssignedIdentity` and a
    `githubEnvironment`. The identity and the GitHub environment that receives its `clientId` are
    one object, so they cannot be paired up wrongly and there is no "both or neither" rule.
  - `purpose` is unique within the environment and is what the phase cmdlets report as `Purpose`.
    The bootstrap environments use `provision` and `plan`.
  - Within one environment the identity names, federated credential names and GitHub environment
    names must differ between pipelines, because equal names would overwrite each other.
  - A pipeline is always processed; there is no fallback from one pipeline to another.

## Consequences


- Each file maps to one phase, so a reader and a reviewer see only what a cmdlet acts on, and
  `CODEOWNERS` can give security groups and RBAC a different reviewer than environments.
- Diffs and pull request reviews get smaller and more focused.
- The starter `environments.jsonc` shrinks from about 440 to about 320 lines. The remaining size is
  the per-environment names, role assignments and variables, which differ on purpose.
- The schema can no longer require `issuer`, `subjectType`, `audiences`, `secrets` or `variables`
  on an environment, because a default may supply them. A missing value is reported by the
  cmdlets when the file is loaded, not by schema validation.- References between files (`resourceGroupId`) can no longer be seen in one view, so validation
  must report a dangling id with the file name.
- More moving parts: three schemas, three entries in the `Test-PlatformTemplate` pairs of
  `platform-validate.yml`, the `manifest.jsonc` destinations, the `paths:` triggers of
  `platform-ci.yml` and `platform-cd.yml`, the help, the tests and the documentation all change.
- A third kind of access, for example a release or data pipeline, is one more `pipelines` entry
  instead of another special property pair.
- The `Purpose` the cmdlets report for the `platform` and `platform-plan` environments changes from
  `infra` to `provision` and `plan`.
- Consumers on `1.x` keep working until they raise `kiteVersion`, since templates and schemas come
  from the release tag (see [0001](./0001-templates-are-starter-samples.md)).
