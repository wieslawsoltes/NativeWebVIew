#!/usr/bin/env bash
set -euo pipefail

configuration="Release"
no_build=false
skip_simulator_boot=false
device_name="${IOS_SIMULATOR_DEVICE_NAME:-iPhone 15}"
device_type="${IOS_SIMULATOR_DEVICE_TYPE:-com.apple.CoreSimulator.SimDeviceType.iPhone-15}"
runtime_identifier="${IOS_SIMULATOR_RUNTIME_IDENTIFIER:-}"
python_bin="${PYTHON_BIN:-python3}"

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
    --skip-simulator-boot)
      skip_simulator_boot=true
      shift
      ;;
    --device-name)
      device_name="$2"
      shift 2
      ;;
    --device-type)
      device_type="$2"
      shift 2
      ;;
    --runtime)
      runtime_identifier="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

created_device=0
sim_udid=""

cleanup() {
  if [[ "$skip_simulator_boot" == "true" ]]; then
    return
  fi

  if [[ -n "$sim_udid" ]]; then
    xcrun simctl shutdown "$sim_udid" >/dev/null 2>&1 || true
    if [[ $created_device -eq 1 ]]; then
      xcrun simctl delete "$sim_udid" >/dev/null 2>&1 || true
    fi
  fi
}
trap cleanup EXIT

if [[ "$skip_simulator_boot" != "true" ]]; then
  if ! command -v xcrun >/dev/null 2>&1; then
    echo "xcrun is required for simulator boot." >&2
    exit 1
  fi

  if ! command -v "$python_bin" >/dev/null 2>&1; then
    echo "$python_bin is required for simulator runtime discovery." >&2
    exit 1
  fi

  if [[ -z "$runtime_identifier" ]]; then
    runtime_identifier="$(xcrun simctl list runtimes available -j | "$python_bin" -c 'import json,sys,re
runtimes=json.load(sys.stdin).get("runtimes", [])
ios=[r for r in runtimes if r.get("isAvailable") and "iOS" in r.get("name", "")]
def version_key(version):
    return tuple(int(part) for part in re.findall(r"\d+", version or ""))
ios.sort(key=lambda r: version_key(r.get("version", "")), reverse=True)
print(ios[0]["identifier"] if ios else "")')"
  fi

  if [[ -z "$runtime_identifier" ]]; then
    echo "No available iOS simulator runtime found." >&2
    exit 1
  fi

  sim_udid="$(xcrun simctl list devices available -j | "$python_bin" -c 'import json,sys
name=sys.argv[1]
runtime=sys.argv[2]
devices=json.load(sys.stdin).get("devices", {})
for entry in devices.get(runtime, []):
    if entry.get("isAvailable") and entry.get("name")==name:
        print(entry.get("udid", ""))
        raise SystemExit(0)
print("")' "$device_name" "$runtime_identifier")"

  if [[ -z "$sim_udid" ]]; then
    sim_udid="$(xcrun simctl create "$device_name" "$device_type" "$runtime_identifier")"
    created_device=1
  fi

  xcrun simctl boot "$sim_udid" >/dev/null 2>&1 || true
  xcrun simctl bootstatus "$sim_udid" -b
fi

cmd=(./scripts/run-mobile-browser-sample-smoke.sh --configuration "$configuration" --platform ios)
if [[ "$no_build" == "true" ]]; then
  cmd+=(--no-build)
fi
"${cmd[@]}"
