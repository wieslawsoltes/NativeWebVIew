# Phase 8 Extended Validation Automation Checklist

## Scope

- Add scheduled/manual extended validation workflows beyond primary PR/release gates.
- Add browser-facing smoke checks using Playwright.
- Standardize sample smoke execution scripts to make CI assertions deterministic.

## Implemented

- Added reusable smoke scripts:
  - `scripts/run-desktop-sample-smoke.sh`
  - `scripts/run-mobile-browser-sample-smoke.sh`
- Updated primary workflows to use reusable smoke scripts:
  - `.github/workflows/ci.yml`
  - `.github/workflows/release.yml`
- Added Playwright smoke test harness:
  - `tests/NativeWebView.Playwright/package.json`
  - `tests/NativeWebView.Playwright/package-lock.json`
  - `tests/NativeWebView.Playwright/playwright.config.mjs`
  - `tests/NativeWebView.Playwright/specs/docs-smoke.spec.mjs`
  - Pinned Playwright test dependency to a fixed, vulnerability-free version.
- Added ignore rules for generated validation artifacts:
  - `.venv-docs/`
  - `site/`
  - `tests/NativeWebView.Playwright/playwright-report/`
  - `tests/NativeWebView.Playwright/test-results/`
- Added extended validation workflow:
  - `.github/workflows/extended-validation.yml`
  - Includes:
    - Browser docs smoke via MkDocs + Playwright on Ubuntu.
    - iOS contract smoke on macOS runner.
    - Android contract smoke on Ubuntu runner.
- Updated planning and docs:
  - `plan/native-webview-native-control-implementation-plan.md` (Phase 8 section)
  - `docs/ci-and-release.md`
  - `README.md`

## Validation

- `bash -n scripts/run-desktop-sample-smoke.sh scripts/run-mobile-browser-sample-smoke.sh scripts/validate-changelog-fragments.sh scripts/build-release-notes.sh`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --platform all`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
- `dotnet build NativeWebView.sln -c Release`
- `dotnet test NativeWebView.sln -c Release --no-build`
- `dotnet pack NativeWebView.sln -c Release --no-build -o artifacts/packages`
- `python -m pip install -r docs/requirements.txt` (venv)
- `mkdocs build --strict`
- `npm ci --prefix tests/NativeWebView.Playwright`
- `npm audit --prefix tests/NativeWebView.Playwright --json`
- `npx --prefix tests/NativeWebView.Playwright playwright install chromium`
- `npm --prefix tests/NativeWebView.Playwright test`
