#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEBSITE_DIR="$ROOT_DIR/website"

npm --prefix "$WEBSITE_DIR" run docs:generate
npm --prefix "$WEBSITE_DIR" run build
