<!-- omit from toc -->
# 0002: No required reviewers on stg and prd by default

- Status: Accepted
- Date: 2026-10-11

## Context

An approval gate on the `stg` and `prd` environments would be configured with
`protectionRules.requiredReviewers` in `platform-config.jsonc` and applied by
`Set-PlatformGitHubEnvironment`. Kite assumes a GitHub Team or personal Pro plan. On those plans
required reviewers and wait timers only work on public repositories; a private repository needs
GitHub Enterprise.

## Decision

The templates do not rely on required reviewers.
Protection comes from `deploymentBranchPolicy` (deployments to an environment only from
allowed branches and tags), the `platform` environment being locked to `main`, and the read-only
`platform-plan` identity for pull request previews.

## Consequences

- A consumer on GitHub Enterprise, or with public repositories, can add `requiredReviewers` to
  their own copy of the configuration.
- Until then, pull request previews of `stg` and `prd` run with those environments' identities, so
  anyone who can open a pull request can run code with their rights. The options to close this gap
  are listed in [release-flow-hardening-todo.md](../release-flow-hardening-todo.md).
