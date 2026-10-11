<!-- omit from toc -->
# 0003: The platform workflow is optional and stays in the configured repository

- Status: Accepted
- Date: 2026-10-11

## Context

The platform phases (`Set-PlatformResourceGroup`, `Set-PlatformSecurityGroup`,
`Set-PlatformEnvironmentIdentity`, `Set-PlatformGitHubEnvironment`) are run from a workstation for the
first time and are idempotent. Most platforms are provisioned once and changed only now and then.
The `platform` identity holds subscription-level `Contributor` and `User Access Administrator`, which
raised the idea of keeping the platform pipeline in a separate platform repository.

## Decision

- Installing the platform workflows (`New-PlatformWorkflow -WorkflowType platform`) is optional. A
  consumer can keep running the phases locally, or hand later platform changes to GitHub Actions.
- Kite does not offer a separate platform repository. The phases act on the single repository named by
  `sourceControl.owner` and `sourceControl.repository` in `global-config.jsonc`: phase 3 binds each
  federated credential to that repository and phase 4 creates the GitHub environments in it. The
  platform workflows therefore run in that repository.

## Consequences

- Consumers choose per project how the platform is maintained; the documentation describes both
  options.
- The privileged `platform` identity is protected by locking its environment to `main` and by the
  read-only `platform-plan` identity for pull request previews, not by repository separation.
- Supporting a separate platform repository would need more than one repository in the global
  configuration. That would be a new decision.
