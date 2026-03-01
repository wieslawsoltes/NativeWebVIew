#!/usr/bin/env bash
set -euo pipefail

python_bin="python3"
venv_dir=".venv-docs"
install_browsers=true

while [[ $# -gt 0 ]]; do
  case "$1" in
    --python)
      python_bin="$2"
      shift 2
      ;;
    --venv)
      venv_dir="$2"
      shift 2
      ;;
    --no-browser-install)
      install_browsers=false
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

"$python_bin" -m venv "$venv_dir"
# shellcheck disable=SC1090
. "$venv_dir/bin/activate"

python -m pip install --upgrade pip
pip install -r docs/requirements.txt
mkdocs build --strict

npm ci --prefix tests/NativeWebView.Playwright
if [[ "$install_browsers" == "true" ]]; then
  if [[ "$(uname -s)" == "Linux" ]]; then
    npx --prefix tests/NativeWebView.Playwright playwright install --with-deps chromium
  else
    npx --prefix tests/NativeWebView.Playwright playwright install chromium
  fi
fi
npm --prefix tests/NativeWebView.Playwright test
