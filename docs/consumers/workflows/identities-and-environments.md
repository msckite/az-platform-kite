<!-- omit from toc -->
# Identities and environments

Each of your environments (`dev`, `stg` and `prd`) has two kinds of access: one for deploying your
application and one for deploying your infrastructure. This page explains why they are separate and
how that shows up in your repository.

<!-- omit from toc -->
## Table of Contents

- [Two pipelines per environment](#two-pipelines-per-environment)
- [Why they are separate](#why-they-are-separate)
- [Names you will see](#names-you-will-see)
- [Adding or changing a pipeline](#adding-or-changing-a-pipeline)

## Two pipelines per environment

In `config/platform/environments.jsonc`, every environment lists its `pipelines`. A pipeline is one
identity in Azure together with the GitHub environment that uses it. The sample has two per
environment:

| Pipeline | Used by | What it may do in Azure |
| --- | --- | --- |
| `infra` | The `infra-*.yml` workflows | Create, update and delete resources through a deployment stack. |
| `workload` | The `workload-*.yml` workflows | Deploy to existing resources (`Contributor`), but not change the infrastructure itself. |

The workflows sign in with the GitHub environment of their pipeline, for example `dev-infra` or
`dev-workload`.

## Why they are separate

The split makes the rule "developers deploy code, platform engineers deploy infrastructure" something
Azure enforces, not just something the team agrees on. A pull request that only changes application
code can never change your infrastructure, because the identity it signs in with is not allowed to.

Two workflow files would not give you that. Two separate identities, each tied to its own GitHub
environment, do.

## Names you will see

The suffix (`-infra`, `-workload`) is only part of the GitHub environment name and the sign-in
rule that goes with it. Everywhere else the environment keeps its short code: resource group ids,
Azure resource names and the `ENV_CODE` variable that the infra workflows read are still just `dev`,
`stg` and `prd`.

The platform workflows use two more environments, `platform` and `platform-plan`, that are not part
of your application environments. See the [platform flow](./platform-flow.md).

## Adding or changing a pipeline

You can have one pipeline per environment, or add another entry when a different kind of access is
needed. Each pipeline must have its own purpose, identity name, federated credential name and
GitHub environment name. Kite refuses to load the file when two of them share one, because their
secrets would overwrite each other.

If you run `Set-PlatformEnvironmentIdentity` and `Set-PlatformGitHubEnvironment` again after a
change, every pipeline in the file is created or updated.
