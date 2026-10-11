<!-- omit from toc -->
# Upgrading

This page lists what a consuming repository must change when it moves to a newer Kite release. Each
release also has release notes on its GitHub Release.

<!-- omit from toc -->
## Table of Contents

- [From 1.0.x to 1.1.0](#from-10x-to-110)
  - [1. Raise `templateVersion` to 1.1.0](#1-raise-templateversion-to-110)
  - [2. Pin `kiteVersion`](#2-pin-kiteversion)
  - [3. Know that `Get-PlatformTemplate` now defaults to the release tag](#3-know-that-get-platformtemplate-now-defaults-to-the-release-tag)
  - [4. Review `deploymentBranchPolicy` before you opt in](#4-review-deploymentbranchpolicy-before-you-opt-in)

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
