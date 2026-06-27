# Aspire Version Upgrade Progress

## Overview

Upgrade the root Aspire setup to target Aspire 13.4.6. The AppHost SDK version and target framework are already current, so this workflow aligns CLI tooling, consolidates AppHost project format, and validates the solution.

**Progress**: 2/4 tasks complete <progress value="50" max="100"></progress> 50%

## Tasks

- ✅ 01-update-aspire-cli: Update Aspire CLI to 13.4.6 ([Content](tasks/01-update-aspire-cli/task.md), [Progress](tasks/01-update-aspire-cli/progress-details.md))
- ✅ 02-consolidate-apphost-sdk: Consolidate AppHost SDK format and clean package references ([Content](tasks/02-consolidate-apphost-sdk/task.md), [Progress](tasks/02-consolidate-apphost-sdk/progress-details.md))
- 🔲 03-build-solution: Build the solution and fix introduced errors
- 🔲 04-aspire-validation: Run the Aspire validation gate
