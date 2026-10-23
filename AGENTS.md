# AGENTS.md

Guidance for coding agents working in this repository.

## Project

`ttw` is a .NET command line tool for TomTom sports watches. It talks to the
watch over USB and does two things:

- **update** downloads the GPS QuickFix (ephemeris) data and writes it to the
  watch;
- **activities** copies the recorded activities of a date range to files.

The layout is three projects under `src/` plus the test project:

- `src/Ttw.Core` — the domain: the TTBIN reader, the activity model and the
  export writers. No I/O.
- `src/Ttw.Services` — the watch over USB, the HTTP downloads and the two
  services that use them.
- `src/Ttw.Cli` — the command line surface, built with System.CommandLine.
- `tests/Ttw.Tests` — xUnit tests, including comparisons against the reference
  outputs of the Java implementation.

## Agent skills

### Issue tracker

Issues live in the GitHub issues of `msdevpt/ttw`; use the `gh` CLI. See
`docs/agents/issue-tracker.md`.

### Triage labels

The five canonical triage roles, with the default label strings. See
`docs/agents/triage-labels.md`.

### Domain docs

Single-context: a root `GLOSSARY.md` and `docs/adr/`. See
`docs/agents/domain.md`.

## Conventions

- Commit messages follow Conventional Commits, in English, in the style of the
  sibling Java project: an imperative subject and a body that explains why.
- The exported files are compared against the outputs of the Java
  implementation, so the formatting of the numbers and of the XML is part of
  the contract. Change it with care.
- Failures are written to standard error and the process exits with code 1.
  All other output goes to standard output.
