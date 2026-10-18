<!-- omit from toc -->
# Pinning the Kite version

Your pipelines run the Kite module, and you decide which version. A new Kite release never changes
a running pipeline on its own: it only reaches your pipelines when you change the version in a pull
request.

<!-- omit from toc -->
## Table of Contents

- [Set the version](#set-the-version)
- [Upgrade to a new release](#upgrade-to-a-new-release)
- [Templates and schemas follow the pin](#templates-and-schemas-follow-the-pin)

## Set the version

`kiteVersion` in `config/global-config.jsonc` holds the Kite release your pipelines install, without
the leading `v`:

```jsonc
"kiteVersion": "1.1.0"
```

A prerelease such as `1.1.0-prev1` works too. `New-PlatformConfigStructure` fills it in with the
version of the module you run it with.

If `kiteVersion` is empty, the pipelines install the latest release and show a
`Platform Kite not pinned` warning. That is not recommended, because a new release could change a
pipeline without any pull request.

## Upgrade to a new release

1. Read the [upgrade path](../upgrade-path/README.md) for the release you move to and make the
   changes it lists.
2. Change `kiteVersion` in the same pull request.
3. Let `platform-ci` preview the platform configuration with the new module. Nothing is applied
   until you merge.

You can override the version for a single run with the `module-version` input of a workflow or of the
`setup-platform-kite` action. The pin in `global-config.jsonc` stays as it is.

## Templates and schemas follow the pin

`Get-PlatformTemplate` downloads from the release you run, not from `main`, and points the
`$schema` references in the downloaded files at that release. Your configuration is therefore
checked against the schemas of your pinned release, so a Kite change that is merged but not yet
released, or not yet adopted by you, never breaks it.

The validation workflow also compares your templates with the newest stable release. When that
release ships a newer template version, it reports a warning. Set its `fail-on-update-available`
input to turn the warning into a failure.
