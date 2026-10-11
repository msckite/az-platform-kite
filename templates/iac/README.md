# Infrastructure as Code (IaC)

## Platform Engineering Guidelines

> [!IMPORTANT]
> This directory contains infrastructure as code managed by the Platform Engineering Team.
>
> **Before making changes:**
> - Infrastructure modifications are privileged to the Platform Engineering Team
> - Follow the contribution guidelines outlined in [CONTRIBUTING.md](../CONTRIBUTING.md)
> - Submit infrastructure change requests via GitHub issue
> - Ensure all validations pass before deployment
>
> **Need to request infrastructure changes?**
> - Refer to [SUPPORT.md](../SUPPORT.md) in the root folder for guidance
> - File a GitHub issue for _bugs_, _feature requests_, or _questions_
>
> All infrastructure changes must go through the proper review and approval process to maintain security, compliance, and stability of the platform.

## Overview

This directory contains the Infrastructure as Code (IaC) definitions for deploying and managing Azure resources for this workload.

## Bicep modules

The Bicep in this directory is a quick-start reference. Write your own Bicep for your workload and
build it on [Azure Verified Modules](https://aka.ms/avm) (AVM) from the public Bicep registry, as the
sample does:

```bicep
module storageAccount 'br/public:avm/res/storage/storage-account:0.33.0' = { ... }
```

- Prefer AVM resource modules (`avm/res/...`) over hand-written resources.
- Pin every module to an exact version. Never use a floating reference.
- Update versions deliberately: bump the version in a pull request, read the module's changelog, and
  let the validation and what-if run before merging.
- Keep your own reusable modules in the `modules` directory of the deployment.
