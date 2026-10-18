<!-- omit from toc -->
# Manual runs and concurrency

You can start every workflow by hand, but a manual run can never deploy something the automatic
trigger would not. This page explains what is allowed and what happens when runs overlap.

<!-- omit from toc -->
## Table of Contents

- [Starting a workflow by hand](#starting-a-workflow-by-hand)
- [When runs overlap](#when-runs-overlap)

## Starting a workflow by hand

- **CD workflows** (`*-cd.yml`) only deploy when you run them from `main`. From any other branch,
  every deploy job is skipped, so a feature branch can never reach `stg` or `prd`.
- **Release workflows** (`*-release.yml`, Release Flow only) ask for the `tag` of an existing
  release, for example `infra/v1.2.0`. Use this to deploy a release to `prd` again, for instance
  after a failed run. The run goes through the same check as a published release (see
  [what `stg` has to prove](./release-flow.md#what-stg-has-to-prove)) and deploys exactly the
  verified tag.

## When runs overlap

| Workflow | When a newer run starts |
| --- | --- |
| CI | The older run is cancelled. A newer commit makes the older preview obsolete. |
| CD, release and platform CD | The older run keeps going and the newer one waits. |

Deployments are never cancelled halfway, because a deployment stack or a role assignment change that
stops in the middle can leave Azure partly updated.

GitHub keeps at most one waiting run per workflow. If a third run arrives, it replaces the waiting
one. That is safe, because the newest commit on `main` includes the older ones.
