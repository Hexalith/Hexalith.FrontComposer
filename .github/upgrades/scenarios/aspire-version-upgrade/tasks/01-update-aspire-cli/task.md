# 01-update-aspire-cli: Update Aspire CLI to 13.4.6

- Verify the currently installed Aspire CLI version.
- Update the Aspire CLI from 13.4.3 to 13.4.6.
- Confirm `aspire --version` reports 13.4.6 before continuing.
- Commit the task result after verification.

## Research findings

- Scenario target Aspire version is `13.4.6`.
- `aspire` resolves to `C:\Users\JeromePiquot\.aspire\bin\aspire.exe`.
- `dotnet tool list -g aspire.cli` did not show a global `aspire.cli` package entry, so the installed CLI is managed from the Aspire user-local CLI location rather than as a registered global .NET tool.
- `aspire update --help` confirms `aspire update --self` is the supported CLI self-update path.
- This task is atomic and does not require decomposition because it only updates/verifies external CLI tooling plus task artifacts.
- Per task-worker operating instructions, no source commit was created by this worker even though the scenario commit strategy says to commit after each task.

## Execution notes

- Ran `aspire update --self --channel stable --non-interactive -y`.
- Update completed successfully and reported `Updated to version: 13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`.
- Post-update `aspire --version` reports `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`.
- No project, package, or source files were modified; build validation is not required for this CLI/tooling-only task.
