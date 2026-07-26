#!/usr/bin/env bash
#
# Regenerates the C# API client for the backend from openapi.json.
#
#   ./openapi_generator.sh                       generate from the checked-in openapi.json
#   ./openapi_generator.sh --fetch               re-export the spec from a locally running
#                                                backend first, then generate
#   ./openapi_generator.sh --fetch http://host:port
#
# Note: this deliberately does NOT use `dotnet dotnet-openapi add file`.
# That command only writes an <OpenApiReference> into a .csproj and relies on
# MSBuild running NSwag at build time. Unity regenerates every .csproj in this
# repo from the Assets folder and compiles with its own pipeline, so the
# reference is wiped and the codegen target never runs. It also fails outright
# here because the repo root contains 15 .csproj files.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

SPEC="$ROOT/openapi.json"
GENERATOR="$ROOT/Tools/openapi_to_unity.py"
OUT="$ROOT/Assets/Database communication/Generated/ApiModels.cs"
NAMESPACE="FishyGame.Api"
DEFAULT_BACKEND="http://localhost:8000"

FETCH=0
BACKEND="$DEFAULT_BACKEND"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --fetch)
      FETCH=1
      if [[ "${2-}" == http* ]]; then
        BACKEND="$2"
        shift
      fi
      ;;
    -h|--help)
      sed -n '2,10p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *)
      echo "error: unknown argument: $1 (try --help)" >&2
      exit 1
      ;;
  esac
  shift
done

if ! command -v python3 >/dev/null 2>&1; then
  echo "error: python3 not found. Install it with: xcode-select --install" >&2
  exit 1
fi

if [[ ! -f "$GENERATOR" ]]; then
  echo "error: missing generator: $GENERATOR" >&2
  exit 1
fi

if [[ $FETCH -eq 1 ]]; then
  URL="${BACKEND%/}/api-docs/openapi.json"
  echo "Fetching spec from $URL"
  TMP="$(mktemp)"
  trap 'rm -f "$TMP"' EXIT
  if ! curl -fsS --max-time 15 "$URL" -o "$TMP"; then
    echo "error: could not reach $URL" >&2
    echo "       Is the backend running? (cargo run, then retry)" >&2
    exit 1
  fi
  # Pretty-print, which also validates that we got JSON and not an error page.
  if ! python3 -m json.tool "$TMP" "$SPEC"; then
    echo "error: $URL did not return valid JSON" >&2
    exit 1
  fi
  echo "Updated $SPEC"
fi

if [[ ! -f "$SPEC" ]]; then
  echo "error: missing spec: $SPEC (run with --fetch to export it)" >&2
  exit 1
fi

# Loud warning when generating from a spec older than the backend that produced
# it -- the failure mode this guards against is silently regenerating the
# previous contract after changing the Rust annotations.
if [[ $FETCH -eq 0 ]]; then
  echo "Using checked-in $SPEC (pass --fetch to re-export from a running backend)"
fi

python3 "$GENERATOR" \
  --input "$SPEC" \
  --output "$OUT" \
  --namespace "$NAMESPACE"

echo "Done. Switch to the Unity Editor to let it recompile."
