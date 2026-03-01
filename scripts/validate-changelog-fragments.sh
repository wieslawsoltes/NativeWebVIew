#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fragments_dir="${1:-${root_dir}/changelog/fragments}"

if [[ ! -d "$fragments_dir" ]]; then
  echo "Fragments directory not found: $fragments_dir" >&2
  exit 1
fi

shopt -s nullglob
files=("$fragments_dir"/*.md)
shopt -u nullglob

if [[ ${#files[@]} -eq 0 ]]; then
  echo "No changelog fragments found in $fragments_dir"
  exit 0
fi

pattern='^\[(Added|Changed|Fixed|Security|Docs|CI|Packaging|Breaking)\][[:space:]]+.+$'
failures=0

for file in "${files[@]}"; do
  line_number=0
  valid_entries=0

  while IFS= read -r line || [[ -n "$line" ]]; do
    line_number=$((line_number + 1))

    if [[ -z "$line" ]]; then
      continue
    fi

    if [[ "$line" =~ ^# ]]; then
      continue
    fi

    if [[ "$line" =~ $pattern ]]; then
      valid_entries=$((valid_entries + 1))
      continue
    fi

    echo "Invalid changelog entry: ${file}:${line_number}: ${line}" >&2
    failures=$((failures + 1))
  done < "$file"

  if [[ $valid_entries -eq 0 ]]; then
    echo "Fragment contains no valid entries: $file" >&2
    failures=$((failures + 1))
  fi
done

if [[ $failures -gt 0 ]]; then
  echo "Changelog validation failed with $failures issue(s)." >&2
  exit 1
fi

echo "Changelog fragments validation passed (${#files[@]} file(s))."
