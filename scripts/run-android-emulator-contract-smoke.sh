#!/usr/bin/env bash
set -euo pipefail

configuration="Release"
no_build=false
logcat_output="artifacts/android-logcat.txt"
adb_wait_timeout_seconds="${ADB_WAIT_TIMEOUT_SECONDS:-180}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)
      configuration="$2"
      shift 2
      ;;
    --no-build)
      no_build=true
      shift
      ;;
    --logcat-output)
      logcat_output="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

mkdir -p "$(dirname "$logcat_output")"

if command -v adb >/dev/null 2>&1; then
  deadline=$((SECONDS + adb_wait_timeout_seconds))
  while true; do
    state="$(adb get-state 2>/dev/null || true)"
    if [[ "$state" == "device" ]]; then
      break
    fi

    if (( SECONDS >= deadline )); then
      echo "Timed out waiting for adb device after ${adb_wait_timeout_seconds}s." >&2
      exit 1
    fi

    sleep 2
  done

  adb shell getprop ro.build.version.release || true
else
  echo "adb is not available; continuing with contract smoke only."
fi

cmd=(./scripts/run-mobile-browser-sample-smoke.sh --configuration "$configuration" --platform android)
if [[ "$no_build" == "true" ]]; then
  cmd+=(--no-build)
fi
"${cmd[@]}"

if command -v adb >/dev/null 2>&1; then
  adb logcat -d > "$logcat_output" || true
fi
