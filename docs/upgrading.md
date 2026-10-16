<!-- omit from toc -->
# Upgrading

This page lists what a consuming repository must change when it moves to a newer Kite release. Each
release also has release notes on its GitHub Release.

<!-- omit from toc -->
## Table of Contents

- [From 1.1.x to 2.0.0](#from-11x-to-200)
  - [1. Split `platform-config.jsonc` into three files](#1-split-platform-configjsonc-into-three-files)
  - [2. Move each environment's identities into `pipelines`](#2-move-each-environments-identities-into-pipelines)
  - [3. Raise `templateVersion` to 2.0.0](#3-raise-templateversion-to-200)
  - [4. Update calls to the phase cmdlets](#4-update-calls-to-the-phase-cmdlets)
  - [5. Update your platform workflows](#5-update-your-platform-workflows)
  - [6. Optional: move repeated settings into `defaults`](#6-optional-move-repeated-settings-into-defaults)
- [From 1.0.x to 1.1.0](#from-10x-to-110)  - [1. Raise `templateVersion` to 1.1.0](#1-raise-templateversion-to-110)
  - [2. Pin `kiteVersion`](#2-pin-kiteversion)
  - [3. Know that `Get-PlatformTemplate` now defaults to the release tag](#3-know-that-get-platformtemplate-now-defaults-to-the-release-tag)
  - [4. Review `deploymentBranchPolicy` before you opt in](#4-review-deploymentbranchpolicy-before-you-opt-in)

## From 1.1.x to 2.0.0

The configuration contract and the phase cmdlet parameters changed. Plan one pull request that makes
all of the changes below together, and raise `kiteVersion` in it. ADR
[0005](./adr/0005-split-platform-config-by-phase.md) explains the decision.

### 1. Split `platform-config.jsonc` into three files

`config/platform-config.jsonc` is replaced by three files in `config/platform/`, one per top-level
array. Move each array into its file. The first two need no other change; reshape `environments` as described in step 2:

| Move this array | Into this file |
| --- | --- |
| `resourceGroups` | `config/platform/resource-groups.jsonc` |
| `securityGroups` | `config/platform/security-groups.jsonc` |
| `environments` | `config/platform/environments.jsonc` |

Every file keeps `$schema` and `templateVersion`. Download the 2.0.0 templates with
`Get-PlatformTemplate` and copy the three files from `templates/platform`, then paste your own
arrays over the sample ones. The `${placeholder}` tokens and the `resourceGroupId` references keep
their meaning, and a `resourceGroupId` may still point at an id in `resource-groups.jsonc` from the
other two files. Delete `config/platform-config.jsonc` afterwards; 2.0.0 does not read it.

### 2. Move each environment's identities into `pipelines`

Inside `environments.jsonc`, the four properties `userAssignedIdentity`, `githubEnvironment`,
`workloadUserAssignedIdentity` and `workloadGithubEnvironment` are replaced by one `pipelines`
array. Each entry has a `purpose` and keeps the official names for its identity and GitHub
environment, so the two that belong together sit in one object:

```jsonc
// 1.1.x
"userAssignedIdentity": { "name": "id-...-infra", ... },
"githubEnvironment": { "name": "${environmentCode}-infra", ... },
"workloadUserAssignedIdentity": { "name": "id-...-workload", ... },
"workloadGithubEnvironment": { "name": "${environmentCode}-workload", ... }

// 2.0.0
"pipelines": [
  {
    "purpose": "infra",
    "userAssignedIdentity": { "name": "id-...-infra", ... },
    "githubEnvironment": { "name": "${environmentCode}-infra", ... }
  },
  {
    "purpose": "workload",
    "userAssignedIdentity": { "name": "id-...-workload", ... },
    "githubEnvironment": { "name": "${environmentCode}-workload", ... }
  }
]
```

- Use `infra` for the old `userAssignedIdentity`/`githubEnvironment` pair and `workload` for the old
  `workload…` pair. An environment that had no workload pair gets a single entry.
- `purpose` must be unique within the environment. It is the `Purpose` value the phase cmdlets
  report, so a script that filters on it keeps working for `infra` and `workload`. The bootstrap
  `platform` and `platform-plan` environments use `provision` and `plan`; they reported `infra`
  before.
- The names of the identities, federated credentials and GitHub environments must differ between
  the pipelines of one environment. Names do not change, so existing resources are found as before.
- Nothing falls back from one pipeline to another any more: every entry is provisioned.

### 3. Raise `templateVersion` to 2.0.0

The three new schemas require `"templateVersion": "2.0.0"`. `global-config.jsonc` keeps its own
template and schema pair, which this release does not change.

### 4. Update calls to the phase cmdlets

`-PlatformConfigPath` is gone from the four phase cmdlets. Each now takes one path per file it
reads, with defaults under `config/platform/`:

| Cmdlet | New parameters |
| --- | --- |
| `Set-PlatformResourceGroup` | `-ResourceGroupsConfigPath` |
| `Set-PlatformSecurityGroup` | `-ResourceGroupsConfigPath`, `-SecurityGroupsConfigPath` |
| `Set-PlatformEnvironmentIdentity` | `-ResourceGroupsConfigPath`, `-EnvironmentsConfigPath` |
| `Set-PlatformGitHubEnvironment` | `-ResourceGroupsConfigPath`, `-EnvironmentsConfigPath` |

A script that relied on the default path needs no change once the files are in `config/platform/`.
A script that passed `-PlatformConfigPath` must pass the new parameters instead.

### 5. Update your platform workflows

If you copied the platform workflows, compare them with the 2.0.0 templates:

- `platform-ci.yml` and `platform-cd.yml` trigger on `config/platform/**` instead of
  `config/platform-config.jsonc`.
- `platform-provision.yml` and `platform-validate.yml` replace the `platform-config-path` input with
  `platform-config-folder` (default `config/platform`).
- `platform-validate.yml` checks three template and schema pairs instead of one.

A caller that passed `platform-config-path` must pass `platform-config-folder` instead.

### 6. Optional: move repeated settings into `defaults`

`environments.jsonc` accepts a new top-level `defaults` object, so the settings every environment
shares are written once. The 2.0.0 template uses it. Your own file keeps working without it, so
this step is optional.

```jsonc
"defaults": {
  "federatedCredential": { "issuer": "...", "subjectType": "environment", "audiences": ["..."] },
  "githubEnvironment": {
    "protectionRules": { "requiredReviewers": [], "waitTimerMinutes": 0 },
    "secrets": [ { "name": "AZURE_CLIENT_ID", "value": "${clientId}" } ]
  }
}
```

- A value set on an environment always wins over the default.
- Credential settings and protection rules are filled in field by field.
- Secrets and variables are merged by name: defaults first, and an environment entry replaces the
  default with the same name.
- The defaults apply to the identity and GitHub environment of every pipeline, so do not put a
  secret or variable in the defaults that one of them, such as the `platform` bootstrap
  environment, must not receive.

The schema no longer requires `issuer`, `subjectType`, `audiences`, `secrets` or `variables` on an
environment, because a default may supply them. If neither the environment nor the defaults set a
credential value, the phase cmdlets fail when they load the file and name the missing property.
## From 1.0.x to 1.1.0

No cmdlet, parameter, or output type was removed or renamed, so scripts that call the cmdlets keep
working. The configuration contract changed, and two defaults changed.

### 1. Raise `templateVersion` to 1.1.0

The schemas of 1.1.0 require `"templateVersion": "1.1.0"` in `global-config.jsonc` and
`platform-config.jsonc`. A file that still says `1.0.0` fails schema validation, and
`Test-PlatformTemplate` reports it as incompatible.

Change the value in both files. Nothing else in them is required to change. Compare your copies with
the 1.1.0 templates (`Get-PlatformTemplate`) for new optional settings.

### 2. Pin `kiteVersion`

`global-config.jsonc` has a new optional `kiteVersion` (without the leading `v`, for example
`"1.1.0"`). The pipelines install exactly that module version, so a new Kite release reaches your
pipelines only when you change the value in a pull request.

An empty value keeps the old behavior: the pipelines install the latest release. That is not
recommended. `New-PlatformConfigStructure` fills in the value for new repositories.

### 3. Know that `Get-PlatformTemplate` now defaults to the release tag

Without `-Branch`, `Get-PlatformTemplate` used to download from `main`. It now downloads from the tag
of the module you run (for example `v1.1.0`), so templates and schemas match the module. It also
pins the `$schema` and `$id` URLs in the downloaded JSON files to that tag.

- `-Branch main` restores the previous behavior.
- `-Branch latest` downloads the newest stable release tag.
- A development build of the module has no release version, warns, and downloads from `main`.

### 4. Review `deploymentBranchPolicy` before you opt in

`platform-config.jsonc` can now set `protectionRules.deploymentBranchPolicy` on a GitHub environment
to restrict which branches and tags may deploy. Without it nothing changes.

When you set it, `Set-PlatformGitHubEnvironment` makes the environment's policies match your list
exactly and **deletes** policies that are on the environment but not in the list, including ones set
by hand. Run it with `-WhatIf` first to see which would be created and deleted.
