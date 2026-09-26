# Contributing

Thanks for your interest in Personal CRM. This project is in **pre-alpha**;
APIs and data shapes will change. Expect churn.

## Ways to contribute

| Type | Where |
|------|-------|
| **Bug reports** | [GitHub Issues](../../issues) — use the *Bug report* template |
| **Feature requests** | [GitHub Issues](../../issues) — use the *Feature request* template |
| **RFCs / design proposals** | [GitHub Discussions → RFCs](../../discussions/categories/rfcs) |
| **Pull requests** | See below |
| **Translations** | Coming after v0.3 (i18n scaffolding) |
| **Documentation fixes** | PRs welcome, no template needed |

## Development setup

Requires:
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (only for Playwright E2E)
- [Docker](https://docs.docker.com/get-docker/) (for integration tests + local Postgres)

```bash
git clone https://github.com/<org>/personal-crm
cd personal-crm

# Restore + build
dotnet restore
dotnet build

# Run unit tests
dotnet test tests/PersonalCrm.Tests.Unit

# Run the app (SQLite at ./data/personal-crm.db)
dotnet run --project src/PersonalCrm.App
```

Open <http://localhost:5000> (or whatever Kestrel picks).

## Pull request workflow

1. **Open an issue first** for non-trivial changes. We discuss before code.
2. Fork the repo, create a feature branch from `main`:
   `feat/<short-slug>` or `fix/<short-slug>`.
3. Make your changes. Keep them focused; one PR = one concern.
4. Add or update tests. PRs without tests will be asked to add them.
5. Run the full local quality gate:
   ```bash
   dotnet format
   dotnet build -c Release
   dotnet test
   ```
6. Sign your commit (`git commit -s`). The DCO check is required.
7. Open the PR. Fill in the template. Link the issue it addresses.
8. Be patient. A maintainer will review within ~1 week. Reviewers may
   request changes — that's normal, not personal.

## Coding standards

- **Language**: English for all code, comments, commit messages, and docs.
- **C# style**: follow `.editorconfig`; `dotnet format` must produce no diff.
- **Namespacing**: file-scoped namespaces, no `this.` qualifier.
- **Nullable**: `<Nullable>enable</Nullable>` is on; no `#nullable disable`.
- **Async**: suffix `Async`, never `.Result`/`.Wait()`.
- **Errors**: throw typed exceptions; don't return nulls for "not found"
  unless the contract explicitly says so.
- **Public APIs**: XML doc comments on all public types and members.

## Architecture decisions

Significant changes go through an **ADR** in `docs/adr/`. Use
`docs/adr/0000-template.md` (copy → number → fill in). Don't argue in PR
comments what should have been an ADR.

## Security issues

**Do not file public issues.** Follow [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions will be licensed under
the [Apache License 2.0](LICENSE). A signed-off-by line (`git commit -s`)
certifies the [Developer Certificate of Origin 1.1](https://developercertificate.org/).
