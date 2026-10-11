<!-- omit from toc -->
# Architecture decision records

An architecture decision record (ADR) captures one decision that shapes Azure Platform Kite, why it
was made, and what it rules out. Read the relevant record before proposing a change in the same
area, and add a new one when you make a decision others would otherwise revisit.

- Name files `NNNN-short-title.md`, numbered in order.
- Keep each record short: context, decision, consequences.
- Do not edit an accepted record to change history. Add a new record that supersedes it and update
  the status of the old one.

| ADR | Title | Status |
| --- | --- | --- |
| [0001](./0001-templates-are-starter-samples.md) | Templates are starter samples | Accepted |
| [0002](./0002-no-required-reviewers-on-private-repositories.md) | No required reviewers on stg and prd by default | Accepted |
| [0003](./0003-platform-workflow-is-optional.md) | The platform workflow is optional and stays in the configured repository | Accepted |
| [0004](./0004-consumers-own-their-bicep-and-use-avm.md) | Consumers own their Bicep and use Azure Verified Modules | Accepted |
