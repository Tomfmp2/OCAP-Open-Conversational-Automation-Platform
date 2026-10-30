#!/usr/bin/env bash
# Compatibilidad: levanta OCAP reconstruyendo las imágenes. Equivale a `ocap start --build`
# (con --no-build, a `ocap start`, que reconstruye solo si el código cambió).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

case "${1:-}" in
  --no-build) exec "$ROOT/ocap" start ;;
  -h|--help) echo "Uso: ./scripts/ocap-up.sh [--no-build]   (mejor: ./ocap install y luego ocap)"; exit 0 ;;
  *) exec "$ROOT/ocap" start --build ;;
esac
