<!-- omit from toc -->
# 0004: Consumers own their Bicep and use Azure Verified Modules

- Status: Accepted
- Date: 2026-10-11

## Context

Kite ships a sample workload in `templates/iac/res/sample`. An open idea was to offer versioned Bicep
building blocks (a module registry or Template Specs) together with a contract in which a workload
declares the minimum infrastructure version it needs. That would make Kite host and maintain
infrastructure content for every consumer, which conflicts with
[0001](./0001-templates-are-starter-samples.md).

## Decision

- The Bicep in `templates/` is a quick-start reference. Consumers write the Bicep for their own
  workloads.
- Kite hosts no Bicep registry and no Template Specs.
- Consumers are advised to build on [Azure Verified Modules](https://aka.ms/avm) (AVM) from the public
  Bicep registry (`br/public:avm/...`) and to pin every module to an exact version. The sample already
  does this.
- There is no infrastructure/workload version contract. Infrastructure and workload repositories stay
  loosely coupled.

## Consequences

- No Kite-owned module lifecycle to version, publish, or support.
- Module versions are updated by the consumer, deliberately, with a pull request they review.
- A consumer that needs a stricter coupling between infrastructure and workload builds it in their own
  workflows.
