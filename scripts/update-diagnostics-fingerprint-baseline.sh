#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
cd "$repo_root"

configuration="Release"
output="ci/baselines/diagnostics-fingerprint-baseline.txt"
blocking_baseline="ci/baselines/blocking-issues-baseline.txt"
no_build=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)
      configuration="$2"
      shift 2
      ;;
    --output)
      output="$2"
      shift 2
      ;;
    --blocking-baseline)
      blocking_baseline="$2"
      shift 2
      ;;
    --no-build)
      no_build=true
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [[ ! -f "$blocking_baseline" ]]; then
  echo "Blocking diagnostics baseline file was not found: $blocking_baseline" >&2
  exit 1
fi

temp_dir="$(mktemp -d)"
trap 'rm -rf "$temp_dir"' EXIT

cmd=(
  "$repo_root/scripts/validate-diagnostics-exit-code-contract.sh"
  --configuration "$configuration"
  --output-dir "$temp_dir"
  --baseline "$blocking_baseline"
)

if [[ "$no_build" == "true" ]]; then
  cmd+=(--no-build)
fi

"${cmd[@]}"

generated="$temp_dir/fingerprint-current.txt"
if [[ ! -f "$generated" ]]; then
  echo "Fingerprint baseline generation failed: $generated" >&2
  exit 1
fi

mkdir -p "$(dirname "$output")"
cp "$generated" "$output"

echo "Diagnostics fingerprint baseline updated at: $output"
