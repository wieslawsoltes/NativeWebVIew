#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fragments_dir="${root_dir}/changelog/fragments"
output_path="${root_dir}/artifacts/release-notes.md"
version="unreleased"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --fragments)
      fragments_dir="$2"
      shift 2
      ;;
    --output)
      output_path="$2"
      shift 2
      ;;
    --version)
      version="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

mkdir -p "$(dirname "$output_path")"

if [[ ! -d "$fragments_dir" ]]; then
  {
    echo "# NativeWebView ${version}"
    echo
    echo "No changelog fragments directory found at: ${fragments_dir}"
  } > "$output_path"
  echo "Wrote release notes to $output_path"
  exit 0
fi

shopt -s nullglob
files=("$fragments_dir"/*.md)
shopt -u nullglob

if [[ ${#files[@]} -eq 0 ]]; then
  {
    echo "# NativeWebView ${version}"
    echo
    echo "No changelog fragments were found."
  } > "$output_path"
  echo "Wrote release notes to $output_path"
  exit 0
fi

categories=(Added Changed Fixed Security Docs CI Packaging Breaking)

tmp_file="$(mktemp)"
trap 'rm -f "$tmp_file"' EXIT

cat "${files[@]}" | awk '
  /^\[(Added|Changed|Fixed|Security|Docs|CI|Packaging|Breaking)\][[:space:]]+/ {
    line = $0
    category = line
    sub(/^\[/, "", category)
    sub(/\].*$/, "", category)

    text = line
    sub(/^\[[^]]+\][[:space:]]+/, "", text)

    if (text != "") {
      print category "|" text
    }
  }
' > "$tmp_file"

{
  echo "# NativeWebView ${version}"
  echo
  echo 'Generated from changelog fragments in `changelog/fragments`.'
  echo

  total_entries=0
  for category in "${categories[@]}"; do
    if grep -q "^${category}|" "$tmp_file"; then
      echo "## ${category}"
      grep "^${category}|" "$tmp_file" | sed "s/^${category}|/- /"
      echo
      count=$(grep -c "^${category}|" "$tmp_file")
      total_entries=$((total_entries + count))
    fi
  done

  if [[ $total_entries -eq 0 ]]; then
    echo "No valid changelog entries were found in fragments."
  fi
} > "$output_path"

echo "Wrote release notes to $output_path"
