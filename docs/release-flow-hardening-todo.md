# Release flow hardening: outstanding to-dos

Template-level changes (concurrency, job timeouts, removal of unnecessary `secrets: inherit`) are
already applied to `templates/github/workflows/release-flow/*.yml` and the two shared reusable
workflows. The items below cannot be fixed by editing a template, because they depend on repo
settings or on project-specific data. Track and apply these separately.

## 1. Protect the `main` branch

This is a GitHub repository setting, not a workflow file. For each consuming project repository
(and for `az-platform-kite-sandbox`, the project this repo currently manages):

- [ ] Require a pull request before merging to `main`
- [ ] Require status checks to pass before merging (`validate`, `plan-dev`, `plan-stg`)
- [ ] Block direct pushes to `main`
- [ ] Restrict who can merge to `main`

No cmdlet in this module currently sets branch protection; apply it manually in
**Settings > Branches**, or scripted with `gh api repos/{owner}/{repo}/branches/main/protection`.

## 2. Require approval on the `stg` environment

`protectionRules.requiredReviewers` in `platform-config.jsonc`, applied by
`Set-PlatformGitHubEnvironment`, is what actually creates the GitHub environment approval gate.

`config/platform-config.jsonc` now defines `dev`, `stg` and `prd` environment blocks, so the
structural gap is closed. Every environment's `protectionRules.requiredReviewers` is still an empty
array, though, so none of them actually gate on approval yet.

- [ ] Set `protectionRules.requiredReviewers` for `stg` to the real reviewers/teams for this project
- [ ] Decide whether `prd` needs its own (likely stricter) reviewer set
- [ ] Consider a non-zero `waitTimerMinutes` for `stg`/`prd` in addition to reviewers
- [ ] Run `Set-PlatformGitHubEnvironment` to apply the updated protection rules

This is data specific to each consuming project (real reviewer usernames/teams), so it cannot be
filled in generically in the shared template.

## 3. Confirm least-privilege secrets and permissions for your project

The template-level `secrets: inherit` lines were removed because the reusable workflows only need
environment-scoped secrets (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`), which
GitHub resolves automatically from the job's `environment:` without the caller passing them.

- [ ] Verify a `plan-dev`/`plan-stg` run still authenticates correctly after this change (no more
      `secrets: inherit` on the calling jobs)
- [ ] Confirm no other repo or org secrets are relied on implicitly by project-specific automation
      you add inside `project-validate.yml` or `project-provision.yml`

On the cross-environment `Contributor` grants (`dev` identity on `stg`, `stg` identity on `prd`,
commented "to run preflight -WhatIf checks"): per Microsoft's own docs on
[template deployment what-if](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-what-if#prerequisites),
"the what-if operation has the same permission requirements" as a real deployment, i.e. write access
on the resources plus full `Microsoft.Resources/deployments/*` (or `deploymentStacks/*` for a
deployment stack). Deployment stacks use the caller's own RBAC, not an elevated deployment identity,
so `Contributor` on the target resource group is genuinely required here, not excess. This is not a
downgrade candidate.

- [ ] If tighter scoping is wanted, replace the built-in `Contributor` grant with a custom role
      limited to the specific resource-provider write actions your Bicep templates actually use
      (plus `Microsoft.Resources/deployments/*` / `deploymentStacks/*`), and keep it in sync as
      templates evolve

## 4. Decide the direct-main-deployment policy

`project-cd.yml` still deploys on every push to `main`. That is an intentional part of the current
design (see `templates/github/workflows/README.md`), but it only stays safe once items 1 and 2
above are in place.

- [ ] Confirm main-push auto-deploy to `dev` is the accepted policy for this project
- [ ] If not, change the trigger to require an explicit release or manual promotion instead
