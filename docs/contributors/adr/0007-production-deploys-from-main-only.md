<!-- omit from toc -->
# 0007: Production deploys from main only

- Status: Accepted
- Date: 2026-11-28
- Supersedes: [0002](./0002-no-required-reviewers-on-private-repositories.md)

## Context

Anyone with write access can edit a workflow on a branch and ask for an environment, so an
environment that accepts every branch gives its identity to unreviewed code. Required reviewers
would stop that, but Kite assumes a GitHub Team or personal Pro plan, where they only work on public
repositories. Deployment branch policies also work on private repositories on those plans.

A what-if on an existing deployment stack needs more than read access: it stores a result, which
takes `Microsoft.Resources/deploymentStacksWhatIfResults/write`, and `Reader` does not include that.
A read-only preview identity would need a custom role, which Kite does not create.

## Decision

- The templates do not rely on required reviewers.
- `prd-infra` and `prd-workload` allow deployments from `main` only, like `platform`.
- Pull requests do not preview `prd`.
- Release workflows run from `main` (`workflow_dispatch`), not from a tag. A tag policy would let
  anyone who can push a matching tag reach `prd`.
- A release workflow checks the tag with the script on `main`, requires the tagged commit to be on
  `main`, and deploys that commit rather than the tag.

## Consequences

- GitHub Flow pull requests deploy to `dev` and show no preview of `prd`.
- A release published by hand starts no deployment; the release workflow is run from `main`.
- The protection depends on a branch protection rule or ruleset that requires pull requests for
  `main`.
- Release Flow pull requests still preview `stg` with the `stg` identities.
- The Release Flow `stg` check guards against mistakes, not misuse. Anyone with write access can set
  a commit status, including one that makes a workload release deploy a package built by a branch
  run.
- A consumer on GitHub Enterprise, or with public repositories, can add `requiredReviewers` to
  their own copy of the configuration.
