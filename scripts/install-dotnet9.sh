#!/usr/bin/env bash
set -euo pipefail

# Installs the latest .NET 9 SDK using Microsoft's official installer.
# If DOTNET_INSTALL_DIR is set, the SDK will be installed there; otherwise it defaults to $HOME/.dotnet.
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"

curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "$INSTALL_DIR"

cat <<EOM

.NET 9 SDK installed to $INSTALL_DIR
Add the following to your shell profile to use it:
  export DOTNET_ROOT="$INSTALL_DIR"
  export PATH="\$DOTNET_ROOT:\$PATH"
EOM
