<!-- omit from toc -->
# Releasing

A release of Azure Platform Kite is a GitHub Release. Publishing it builds the module from the
release tag and publishes it to the PowerShell Gallery. There is no release-please and no
`CHANGELOG` file for Kite itself; the release notes on the GitHub Release are the changelog.

> [!WARNING]
> Publishing to the PowerShell Gallery cannot be undone. A version, once published, cannot be
> replaced; you can only publish a higher one. You can unlist a version on the PowerShell Gallery,
> which hides it from search and listings but keeps it installable by its exact version. Check the
> release carefully before you publish it.

<!-- omit from toc -->
## Table of Contents
- [How a release works](#how-a-release-works)
- [Choose the version](#choose-the-version)
- [Release steps](#release-steps)
- [Verify](#verify)
- [If the pipeline fails](#if-the-pipeline-fails)

## How a release works

1. Publishing a GitHub Release triggers [cd.yaml](../.github/workflows/cd.yaml).
2. `cd.yaml` calls [ci.yaml](../.github/workflows/ci.yaml) with the release tag. The `UpdateManifest`
   task in [build.ps1](../build.ps1) stamps `ModuleVersion` (and `Prerelease`, when the tag has a
   suffix) into the module manifest, so the version always comes from the tag, not from a committed
   file. A tag with a `-` suffix (for example `v1.1.0-prev1`) uses the prerelease manifest.
3. CI builds the module and runs the tests; the tested module is uploaded as an artifact.
4. The `publish` job downloads that artifact and runs `Publish-Module` with the `PSGALLERY_API_KEY`
   secret.

Consumers pin the module through `kiteVersion` in their `global-config.jsonc`, and their templates
and schemas are downloaded from the matching release tag, so a tag must always contain the matching
templates and schemas.

## Choose the version

Kite uses [semantic versioning](https://semver.org/) with a `v` prefix on the tag:

| Change since the last release | Bump |
| --- | --- |
| Breaking change to a cmdlet, parameter, config schema or template contract | Major (`v2.0.0`) |
| New cmdlet, parameter, template or feature | Minor (`v1.2.0`) |
| Bug fix, documentation or maintenance only | Patch (`v1.1.1`) |

Use a prerelease tag such as `v1.2.0-prev1` to test a release in the sandbox repository before you
cut the stable version. Mark the GitHub Release as a prerelease.

## Release steps

1. Make sure `main` is green and contains everything for the release.
2. List what changed: `git log <previous-tag>..main --oneline`.
3. Write the release notes, grouped by theme (for example Security, Features, Fixes) and linking the
   pull requests. End with the **Full Changelog** compare link.
4. Create the release, which creates the tag on `main`:

   ```bash
   gh release create vX.Y.Z --target main --title vX.Y.Z --latest --notes-file release-notes.md
   ```

   Add `--prerelease` instead of `--latest` for a prerelease.
5. Watch the run: `gh run list --workflow cd.yaml --limit 1`, then `gh run watch <run-id>`.

## Verify

- The `Publish to PowerShell Gallery (CD)` run is green.
- The version appears on the module page on the PowerShell Gallery (it can take a few minutes).
- Optionally, install it in a clean session or the sandbox repository:
  `Install-Module MSCKite.Azure.Platform -RequiredVersion X.Y.Z` (add `-AllowPrerelease` for a
  prerelease).

## If the pipeline fails

- **Failure before the publish job** (build or tests): nothing is published. Fix the problem in a
  pull request, delete the release and its tag (`gh release delete vX.Y.Z --cleanup-tag`), and
  create it again from the new `main`.
- **Failure in the publish job**: check the logs first. If the version did reach the gallery, do
  not try to reuse it; release the next patch version instead.
