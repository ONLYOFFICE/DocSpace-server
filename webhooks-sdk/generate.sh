#!/usr/bin/env bash
#
# Regenerate the DocSpace webhook payload models for every target language.
#
#   ./generate.sh              # all languages
#   ./generate.sh python go    # just these
#
# Run from anywhere; paths resolve against this script's location. On Windows
# use Git Bash -- the generator itself is OS-neutral.
#
# This deliberately calls openapi-generator-cli directly rather than going
# through common/Tools/ASC.Api.Documentation. See README.md, "Why not the
# generate-sdk tool".

set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
spec="$here/../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml"

# Default generator properties: models only, no tests, but WITH the per-model
# Markdown reference. The API SDKs in sdk/ carry the same docs/*.md -- nothing
# in that pipeline enables them, they are simply openapi-generator's default,
# and models-only output keeps them as long as modelDocs is not switched off.
default_props="models,modelTests=false,modelDocs=true"

# dirname|generator|global-properties|additional-properties|template-dir
#
# typescript needs supportingFiles: its models import helpers from ../runtime
# (mapValues, parseDateTime, ...), which models-only does not emit, so the
# output would not compile. Add the same for any other language once its
# runtime is written and the same gap shows up.
#
# php needs supportingFiles for ObjectSerializer, which every model calls into
# and which is far too large to stand in for by hand. Nothing in the sample
# touches the HTTP classes, so guzzle is never loaded.
#
# java sets hideGenerationTimestamp: it is the only generator here that stamps
# the wall-clock time into every model, so without it all nine files show up as
# modified after each run with no semantic change.
#
# csharp pins library=httpclient. The 7.x default (generichost) wraps every
# property in Option<T> and needs its converters registered by hand, both of
# which live in the un-generated Client namespace; httpclient emits plain POCOs
# that need only the two small shims in src/Compat.cs.
targets=(
  "csharp|csharp|models,modelTests=false,modelDocs=true,supportingFiles=README.md|packageName=DocSpace.Webhooks.SDK,library=httpclient,targetFramework=net8.0|templates/csharp"
  "go|go||packageName=docspace_webhooks_sdk"
  "java|java||modelPackage=com.onlyoffice.docspace.webhooks.sdk.model,invokerPackage=com.onlyoffice.docspace.webhooks.sdk,groupId=com.onlyoffice,artifactId=docspace-webhooks-sdk,hideGenerationTimestamp=true"
  "kotlin|kotlin||packageName=onlyoffice.docspace.webhooks.sdk"
  "php|php|models,supportingFiles,modelTests=false,modelDocs=true|invokerPackage=OnlyOffice\DocSpace\Webhooks\Sdk,packageName=onlyoffice/docspace-webhooks-sdk"
  "python|python||packageName=docspace_webhooks_sdk"
  "ruby|ruby||gemName=docspace-webhooks-sdk,moduleName=DocspaceWebhooksSdk"
  "swift|swift6||projectName=DocSpaceWebhooksSDK"
  "typescript|typescript-fetch|models,supportingFiles,modelTests=false,modelDocs=true"
)

[ -f "$spec" ] || { echo "spec not found: $spec" >&2; exit 1; }
command -v openapi-generator-cli >/dev/null 2>&1 || {
  echo "openapi-generator-cli not found. Install with:" >&2
  echo "  npm i -g @openapitools/openapi-generator-cli" >&2
  exit 1
}

wanted=("$@")
cd "$here"   # so the pinned openapitools.json here is the one that applies

fail=0
for t in "${targets[@]}"; do
  dir="$(echo "$t" | cut -d'|' -f1)"
  gen="$(echo "$t" | cut -d'|' -f2)"
  props="$(echo "$t" | cut -d'|' -f3)"
  extra="$(echo "$t" | cut -d'|' -f4)"
  tmpl="$(echo "$t" | cut -d'|' -f5)"
  [ -n "$props" ] || props="$default_props"

  if [ ${#wanted[@]} -gt 0 ]; then
    match=0
    for w in "${wanted[@]}"; do [ "$w" = "$dir" ] && match=1; done
    [ $match -eq 1 ] || continue
  fi

  # Only ever wipe generated/ -- hand-written runtime lives beside it in src/.
  # Clear the contents rather than the directory: on Windows an editor or a
  # previous generator run can hold the directory handle open, and removing it
  # then fails with "Device or resource busy".
  mkdir -p "$here/$dir/generated"
  # Files first, then the directories they lived in. Windows can still hold a
  # directory handle open for a moment after its contents go, and a failure
  # here would otherwise abort the whole script under `set -e` -- leaving the
  # tree half-deleted and the generator never run.
  find "$here/$dir/generated" -mindepth 1 -type f -delete 2>/dev/null || true
  find "$here/$dir/generated" -mindepth 1 -depth -type d -exec rmdir {} + 2>/dev/null || true

  echo "=== $dir ($gen)"

  # Plain `cond && assign` would abort the script under `set -e` whenever the
  # condition is false, i.e. for every target without extra properties.
  extra_args=""
  if [ -n "$extra" ]; then
    extra_args="--additional-properties=$extra"
  fi

  # A template directory overrides only the templates it actually contains; the
  # generator falls back to its built-ins for everything else. That is what lets
  # one README.mustache replace the stock one without having to vendor the model
  # and doc templates too.
  tmpl_args=""
  if [ -n "$tmpl" ]; then
    tmpl_args="-t $here/$tmpl"
  fi

  if openapi-generator-cli generate \
       -i "$spec" \
       -g "$gen" \
       -o "$here/$dir/generated" \
       --global-property "$props" \
       $extra_args \
       $tmpl_args \
       > "$here/$dir/generated/.generate.log" 2>&1
  then
    echo "    models: $(find "$here/$dir/generated" -type f ! -name '.generate.log' | wc -l) files"
  else
    echo "    FAILED -- see $dir/generated/.generate.log" >&2
    fail=1
    continue
  fi

  # The trigger -> payload dispatch table is derived from the same contract,
  # never hand-maintained. Languages gain a case here as their runtime lands.
  case "$dir" in
    java)
      # Java packages are directories: the JSON stand-in has to live inside the
      # generated source tree, so it is copied in. See java/shim/.
      cp -r "$here/java/shim/." "$here/$dir/generated/src/main/java/"
      python "$here/tools/gen-trigger-map.py" --lang java --spec "$spec" --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out "$here/$dir/generated/src/main/java/com/onlyoffice/docspace/webhooks/sdk/JSON.java"
      echo "    shim+JSON: generated/src/main/java/com/onlyoffice/docspace/webhooks/sdk/"
      ;;
    go)
      # Go requires every file of a package to sit in one directory, so the
      # utils.go stand-ins have to be copied into generated/ rather than kept
      # beside it. See go/shim/.
      cp "$here/go/shim/"*.go "$here/$dir/generated/"
      echo "    shim: generated/zz_shim.go"
      ;;
    csharp)
      python "$here/tools/gen-trigger-map.py" --lang csharp --spec "$spec"              --package DocSpace.Webhooks.SDK --model-package DocSpace.Webhooks.SDK.Model --out "$here/$dir/generated/src/DocSpace.Webhooks.SDK/Triggers.cs"
      echo "    triggers: generated/src/DocSpace.Webhooks.SDK/Triggers.cs"
      ;;
    typescript)
      python "$here/tools/gen-trigger-map.py" --lang typescript --spec "$spec" \
             --out "$here/$dir/generated/triggers.ts"
      echo "    triggers: generated/triggers.ts"
      ;;
  esac
done

exit $fail
