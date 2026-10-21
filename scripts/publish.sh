#!/usr/bin/env sh
# Publishes the self-contained binaries of ttw for the supported platforms.
set -eu

for rid in win-x64 linux-x64 osx-arm64; do
    dotnet publish src/Ttw.Cli/Ttw.Cli.csproj -c Release -r "$rid" --self-contained true \
      -o "dist/$rid"
done
