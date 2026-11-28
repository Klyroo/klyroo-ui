#!/usr/bin/env bash
set -euo pipefail

framework=${1:?Specify the target framework to verify}
project=Klyroo.UI.Maui.csproj
scratch=$(mktemp -d)
trap 'rm -rf "$scratch"' EXIT

# Use a fresh package version so NuGet cannot satisfy the reference from an earlier pack.
version_suffix="verify.${GITHUB_RUN_ID:-0}.${GITHUB_RUN_ATTEMPT:-0}.$RANDOM"
version=$(dotnet msbuild "$project" -nologo -getProperty:PackageVersion "-p:VersionSuffix=$version_suffix")
dotnet pack "$project" -c Release --nologo "-p:TargetFrameworks=$framework" \
  "-p:VersionSuffix=$version_suffix" -o "$scratch/feed"

cp global.json "$scratch/"
cat > "$scratch/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$framework</TargetFramework>
    <UseMaui>true</UseMaui>
    <RestoreAdditionalProjectSources>\$(MSBuildProjectDirectory)/feed</RestoreAdditionalProjectSources>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Klyroo.UI.Maui" Version="[$version]" />
  </ItemGroup>
</Project>
EOF
cat > "$scratch/Consumer.cs" <<'EOF'
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

public static class Consumer
{
    public static View CreateView() => new KlyButton { Text = "Save" };
}
EOF

cd "$scratch"
dotnet build Consumer.csproj -c Release --nologo
