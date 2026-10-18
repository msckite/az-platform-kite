# Documentation

The documentation is split by who reads it. The [README](../README.md) in the repository root is the
starting point for everyone.

## Using Kite

For people who install the module and run it in their own repository.

- [Cmdlet help](./MSCKite.Azure.Platform/MSCKite.Azure.Platform.md): every cmdlet with its
  parameters and examples.
- [Upgrade path](./consumers/upgrade-path/README.md): what to change in your repository when you
  move to a newer release.
- [Workflow templates](../templates/github/workflows/README.md): the starter workflows you copy and
  own.

## Contributing to Kite

For people who change the module, the templates or the documentation. Start with
[CONTRIBUTING.md](../CONTRIBUTING.md).

- [Architecture decision records](./contributors/adr/README.md): the decisions that shape Kite and
  why they were made.
- [Help authoring](./contributors/help-authoring.md): how cmdlet help is written and generated.
- [Line endings](./contributors/line-endings.md): why every file uses LF and how that is enforced.
- [Workflow templates](./contributors/workflow-templates.md): how the workflow templates are laid out and
  installed.
- [Releasing](./contributors/releasing.md): how a GitHub Release publishes the module.
- [Release flow hardening](./contributors/release-flow-hardening-todo.md): outstanding to-dos that
  depend on repository settings.

Coding agents start with [AGENTS.md](../AGENTS.md).
