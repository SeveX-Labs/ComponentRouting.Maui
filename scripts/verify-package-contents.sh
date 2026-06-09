#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <package.nupkg>" >&2
  exit 2
fi

package_path="$1"

if [[ ! -f "$package_path" ]]; then
  echo "Package not found: $package_path" >&2
  exit 2
fi

if ! command -v unzip >/dev/null 2>&1; then
  echo "unzip is required to verify package contents." >&2
  exit 2
fi

package_entries="$(unzip -Z1 "$package_path")"
target_frameworks=(
  "net10.0-android"
  "net10.0-ios"
  "net10.0-maccatalyst"
)
assemblies=(
  "ComponentRouting.Core.dll"
  "ComponentRouting.Maui.dll"
)

missing_count=0

for target_framework in "${target_frameworks[@]}"; do
  for assembly in "${assemblies[@]}"; do
    if ! grep -Eq "^lib/${target_framework}[^/]*/${assembly}$" <<<"$package_entries"; then
      echo "Missing lib/${target_framework}*/${assembly}" >&2
      missing_count=$((missing_count + 1))
    fi
  done
done

if [[ "$missing_count" -gt 0 ]]; then
  echo "Package verification failed: $missing_count required file(s) missing." >&2
  exit 1
fi

echo "Package verification passed: $package_path"
