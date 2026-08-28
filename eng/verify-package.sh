#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "Usage: $0 <package-version> <artifacts-directory>" >&2
  exit 64
fi

package_version="$1"
artifact_directory="$(cd "$2" && pwd)"
package="$artifact_directory/KitchenPC.Imp.$package_version.nupkg"

if [[ ! -f "$package" ]]; then
  echo "Package not found: $package" >&2
  exit 1
fi

nuspec="$(unzip -p "$package" '*.nuspec')"
if grep -q '<dependency ' <<< "$nuspec"; then
  echo "Imp should not carry ordinary NuGet package dependencies:" >&2
  echo "$nuspec" >&2
  exit 1
fi

temporary_directory="$(mktemp -d)"
trap 'rm -rf "$temporary_directory"' EXIT

for framework in net8.0 net10.0; do
  project_directory="$temporary_directory/$framework"
  dotnet new web --output "$project_directory" --no-restore
  dotnet add "$project_directory" package KitchenPC.Imp --version "$package_version" --no-restore
  dotnet restore "$project_directory" -p:TargetFramework="$framework" --source "$artifact_directory" --source https://api.nuget.org/v3/index.json
  dotnet build "$project_directory" --configuration Release --no-restore -p:TargetFramework="$framework"
done
