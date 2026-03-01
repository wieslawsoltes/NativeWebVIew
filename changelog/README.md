# Changelog Fragments

Release notes are generated from markdown fragments stored in `changelog/fragments`.

## Format

Each non-empty line in a fragment must match:

`[Category] Message`

Allowed categories:

- `Added`
- `Changed`
- `Fixed`
- `Security`
- `Docs`
- `CI`
- `Packaging`
- `Breaking`

Example:

```text
[Added] Introduced platform capability registry for backend discovery.
[Fixed] Prevented controller state race during dispose while initialization is in-flight.
[Docs] Added platform support matrix and package layout docs.
```

## Local commands

Validate fragments:

```bash
./scripts/validate-changelog-fragments.sh
```

Generate release notes preview:

```bash
./scripts/build-release-notes.sh --version 0.1.0 --output artifacts/release-notes.md
```
