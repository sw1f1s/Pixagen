#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

exec dotnet run --project "$ROOT_DIR/Pixagen/Pixagen.csproj" -- \
  --fullscreen \
  --window-size=1920x1080 \
  --cell-pixel-size=4 \
  --fps=60 \
  "$@"
