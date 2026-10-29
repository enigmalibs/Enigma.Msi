#!/usr/bin/env bash
#
# Publishes Enigma.Msi (the Enigma.Msi.Desktop app) for Linux x64. Runs, from wherever it is started:
#
#   dotnet publish src/Enigma.Msi.Desktop -c Release -r linux-x64 -p:PublishReadyToRun=true
#
# A Release build for linux-x64, compiled ReadyToRun so it starts faster, which runs on an installed
# .NET 10 runtime. The result lands in
#
#   src/Enigma.Msi.Desktop/bin/Release/net10.0/linux-x64/publish/
#
# and dotnet's last line names that folder. Install it for the current user with
# install.sh --from <that folder>, in this same directory. On Linux the app edits, validates and saves
# profiles; building the MSI itself needs Windows.

set -euo pipefail

# Resolved from this script rather than the current directory, so it runs from anywhere.
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
readonly script_dir

# exec: the script's exit code is dotnet's, so a failed publish fails the script.
exec dotnet publish "${script_dir}/../../src/Enigma.Msi.Desktop" -c Release -r linux-x64 -p:PublishReadyToRun=true
