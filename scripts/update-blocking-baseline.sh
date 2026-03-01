#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
cd "$repo_root"

configuration="Release"
platform="all"
report_output="artifacts/diagnostics/baseline-refresh-platform-diagnostics.json"
markdown_output="artifacts/diagnostics/baseline-refresh-platform-diagnostics.md"
baseline_output="ci/baselines/blocking-issues-baseline.txt"
no_build=false
warnings_as_errors=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)
      configuration="$2"
      shift 2
      ;;
    --platform)
      platform="$2"
      shift 2
      ;;
    --output)
      baseline_output="$2"
      shift 2
      ;;
    --report-output)
      report_output="$2"
      shift 2
      ;;
    --markdown-output)
      markdown_output="$2"
      shift 2
      ;;
    --no-build)
      no_build=true
      shift
      ;;
    --warnings-as-errors)
      warnings_as_errors=true
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

mkdir -p "$(dirname "$baseline_output")"

cmd=("$repo_root/scripts/run-platform-diagnostics-report.sh"
  --configuration "$configuration"
  --platform "$platform"
  --output "$report_output"
  --markdown-output "$markdown_output"
  --blocking-baseline-output "$baseline_output"
  --allow-not-ready
  --allow-regression)

if [[ "$no_build" == "true" ]]; then
  cmd+=(--no-build)
fi

if [[ "$warnings_as_errors" == "true" ]]; then
  cmd+=(--warnings-as-errors)
fi

"${cmd[@]}"

if [[ ! -f "$baseline_output" ]]; then
  echo "Blocking diagnostics baseline file was not created: $baseline_output" >&2
  exit 1
fi

if ! grep -q "# Format: <Platform>|<IssueCode>" "$baseline_output"; then
  echo "Blocking diagnostics baseline file is missing expected header: $baseline_output" >&2
  exit 1
fi

echo "Blocking diagnostics baseline refreshed at $baseline_output"
