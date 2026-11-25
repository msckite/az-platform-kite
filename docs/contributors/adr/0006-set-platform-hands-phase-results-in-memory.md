<!-- omit from toc -->
# 0006: Set-Platform hands phase results over in memory

- Status: Accepted
- Date: 2026-11-25

## Context

The platform is deployed in four phases, one cmdlet each: resource groups, security groups,
environment identities and GitHub environments. The later phases need what the earlier ones
created: the resource group ids for role scopes, and the client id of each identity for the
`${clientId}` secrets. Each cmdlet reads those back from Azure, because it can run on its own and
in its own GitHub Actions job.

Running the four cmdlets one after another from a computer has three problems:

- `-WhatIf` on a fresh subscription fails from phase 2 on, because the preview of phase 1 created
  nothing for the later phases to read.
- A resource group or identity that was just created may not be readable yet, so a back-to-back run
  can fail on propagation delays.
- A missing GitHub sign-in is only found in phase 3, after phases 1 and 2 have changed Azure.

See [issue #57](https://github.com/msckite/az-platform-kite/issues/57).

## Decision

Add `Set-Platform`, a cmdlet in the module that runs the four phases in order and hands each
phase's results to the next in memory.

- The body of each phase cmdlet lives in an internal runner under `src/Internal/Platform/Phases/`.
  The phase cmdlets and `Set-Platform` call the same runners, so a phase behaves the same either
  way.
- The phase cmdlets keep reading their inputs from Azure, and their parameters and output stay the
  same. They do not accept results piped from the previous phase; only `Set-Platform` chains them.
- `Set-Platform` loads every config file and checks the Azure context and the GitHub repository
  before the first phase.
- Under `-WhatIf`, a resource group that would be created gets the id it will have, and the
  `${clientId}` of an identity that would be created resolves to `(known after apply)`.
- The run stops after a phase that reports errors when later phases depend on it: phase 1 and
  phase 3. Errors in phase 2 do not stop it, because phases 3 and 4 do not need the security groups.
- The platform workflow templates keep running the four phase cmdlets as separate jobs.

## Consequences

- One command deploys or previews the whole platform, including a first deployment.
- Phase results gained the fields the next phase needs: `ResourceId` on resource groups and
  `ResourceGroupName` on identities.
- Runners report errors through `WritePhaseError`, which counts them so `Set-Platform` can tell
  which phase failed. New error paths in a runner must use it instead of `WriteError`.
- Two ways to run a phase means two paths to keep correct: a change to a phase's inputs must work
  both with the values read from Azure and with the values handed over by `Set-Platform`.
- A deployment that stops at a phase leaves the earlier phases applied. Every phase is idempotent,
  so the fix is to correct the errors and run `Set-Platform` again.
- The workflow still runs phases 2 and 3 side by side with a deployment gate per job; switching it
  to `Set-Platform` would lose both, so that stays a separate decision.
