# Phase 9 Device Runner Validation Checklist

## Scope

- Move extended mobile contract checks from host-only smoke to device-runner-backed execution.
- Add reusable scripts for browser Playwright and device contract validation.
- Ensure extended-validation workflow publishes diagnostic artifacts from device jobs.

## Implemented

- Added reusable validation scripts:
  - `scripts/run-browser-playwright-smoke.sh`
  - `scripts/run-android-emulator-contract-smoke.sh`
  - `scripts/run-ios-simulator-contract-smoke.sh`
- Updated `.github/workflows/extended-validation.yml`:
  - Browser job now uses script wrapper for deterministic setup and execution.
  - iOS job now boots a simulator and runs iOS contract smoke.
  - Android job now runs contract smoke inside an emulator (`reactivecircus/android-emulator-runner`).
  - Added artifact upload for iOS simulator logs and Android logcat output.
- Updated planning and docs:
  - `plan/native-webview-native-control-implementation-plan.md` (Phase 9 section)
  - `docs/ci-and-release.md`
  - `README.md`

## Validation

- `bash -n scripts/run-browser-playwright-smoke.sh scripts/run-android-emulator-contract-smoke.sh scripts/run-ios-simulator-contract-smoke.sh`
- `./scripts/run-desktop-sample-smoke.sh --configuration Debug`
- `./scripts/run-mobile-browser-sample-smoke.sh --configuration Debug --platform all`
- `./scripts/run-ios-simulator-contract-smoke.sh --configuration Debug --skip-simulator-boot`
- `./scripts/run-android-emulator-contract-smoke.sh --configuration Debug`
- `./scripts/run-browser-playwright-smoke.sh --python python3 --venv .venv-docs`
- `dotnet build NativeWebView.sln -c Debug`
- `dotnet test NativeWebView.sln -c Debug --no-build`
