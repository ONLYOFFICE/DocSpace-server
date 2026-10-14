#!/usr/bin/env bash
#
# Regenerate the DocSpace webhook payload models for every target language.
#
#   ./generate.sh              # all languages
#   ./generate.sh python go    # just these
#   VERIFY=0 ./generate.sh     # skip the build-and-run check
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

# dirname|generator|global-properties|additional-properties|template-dir|extra-cli-args
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
#
# go renames the hasAvatar field: the generator also emits a HasXxx() presence
# method for every optional field, and a field called HasAvatar collides with
# the method of that name, so the package does not compile. Only the Go field
# name changes; the JSON key stays hasAvatar.
#
# java turns off disallowAdditionalPropertiesIfNotPresent. By default every
# model's validateJsonElement throws on a property the contract does not list,
# so a field added on the server breaks every receiver built before it.
#
# kotlin and php cannot render the EntryId oneOf (integer | string): both emit a
# property-less class and the id is lost. Kotlin maps the schema onto the
# hand-written onlyoffice.docspace.webhooks.sdk.EntryId (kotlin/src/), PHP onto
# `mixed`, which ObjectSerializer passes through as the int or string it is.
#
# ruby needs supportingFiles for api_model_base.rb (every model inherits it),
# version.rb, the gem entry point and the gemspec; templates/ruby overrides the
# last two so that loading the gem does not pull in an HTTP client.
#
# swift needs supportingFiles for JSONValue.swift, the type of the untyped
# envelope payload.
#
# Packaging metadata. go emits go.mod/go.sum into generated/, which is the
# module root: --git-user-id/--git-repo-id make the module path the one the
# README tells users to `go get` (this generator ignores them as additional
# properties). python emits pyproject.toml, py.typed and the package
# __init__.py; templates/python overrides the first and the last, because the
# stock ones describe and import the HTTP client a models-only package does not
# have. php's composer.json comes from templates/php for the same reason, and
# because the stock one has no name unless gitUserId/gitRepoId are set. The
# other languages keep their package files outside generated/, edited by hand.
targets=(
  "csharp|csharp|models,modelTests=false,modelDocs=true,supportingFiles=README.md:RECEIVING.md|packageName=DocSpace.Webhooks.SDK,library=httpclient,targetFramework=net8.0|templates/csharp|"
  "go|go|models,modelTests=false,modelDocs=true,supportingFiles=README.md:go.mod:go.sum|packageName=docspace_webhooks_sdk|templates/go|--name-mappings hasAvatar=HasAvatarFlag --git-user-id ONLYOFFICE --git-repo-id docspace-webhooks-sdk-go"
  "java|java|models,modelTests=false,modelDocs=true,supportingFiles=README.md|modelPackage=com.onlyoffice.docspace.webhooks.sdk.model,invokerPackage=com.onlyoffice.docspace.webhooks.sdk,groupId=com.onlyoffice,artifactId=docspace-webhooks-sdk,hideGenerationTimestamp=true,disallowAdditionalPropertiesIfNotPresent=false|templates/java|"
  "kotlin|kotlin|models,modelTests=false,modelDocs=true,supportingFiles=README.md|packageName=onlyoffice.docspace.webhooks.sdk|templates/kotlin|--schema-mappings EntryId=onlyoffice.docspace.webhooks.sdk.EntryId"
  "php|php|models,supportingFiles,modelTests=false,modelDocs=true|invokerPackage=OnlyOffice\DocSpace\Webhooks\Sdk,packageName=onlyoffice/docspace-webhooks-sdk,artifactVersion=1.0.0|templates/php|--schema-mappings EntryId=mixed --type-mappings EntryId=mixed"
  "python|python|models,modelTests=false,modelDocs=true,supportingFiles=README.md:pyproject.toml:__init__.py:py.typed|packageName=docspace_webhooks_sdk|templates/python|"
  "ruby|ruby|models,modelTests=false,modelDocs=true,supportingFiles=README.md:api_model_base.rb:version.rb:docspace-webhooks-sdk.rb:docspace-webhooks-sdk.gemspec|gemName=docspace-webhooks-sdk,moduleName=DocspaceWebhooksSdk|templates/ruby|"
  "swift|swift6|models,modelTests=false,modelDocs=true,supportingFiles=README.md:JSONValue.swift|projectName=DocSpaceWebhooksSDK|templates/swift|"
  "typescript|typescript-fetch|models,supportingFiles,modelTests=false,modelDocs=true||templates/typescript|"
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
generated=()
for t in "${targets[@]}"; do
  dir="$(echo "$t" | cut -d'|' -f1)"
  gen="$(echo "$t" | cut -d'|' -f2)"
  props="$(echo "$t" | cut -d'|' -f3)"
  extra="$(echo "$t" | cut -d'|' -f4)"
  tmpl="$(echo "$t" | cut -d'|' -f5)"
  cli="$(echo "$t" | cut -d'|' -f6)"
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

  # Custom output files can only be declared in a config file -- there is no
  # command-line equivalent -- so one is picked up by convention when present.
  # Anything it names must ALSO appear in the supportingFiles allowlist above,
  # colon-separated, or the generator silently skips it.
  cfg_args=""
  if [ -n "$tmpl" ] && [ -f "$here/$tmpl/files.yaml" ]; then
    cfg_args="-c $here/$tmpl/files.yaml"
  fi

  if openapi-generator-cli generate \
       -i "$spec" \
       -g "$gen" \
       -o "$here/$dir/generated" \
       --global-property "$props" \
       $extra_args \
       $tmpl_args \
       $cfg_args \
       $cli \
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
    kotlin)
      # The EntryId schema is mapped onto the hand-written class in kotlin/src/;
      # its doc page is hand-written too, since the model docs link to it.
      cp "$here/kotlin/docs/"*.md "$here/$dir/generated/docs/"
      echo "    docs: generated/docs/onlyoffice.docspace.webhooks.sdk.EntryId.md"
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
      python "$here/tools/gen-trigger-map.py" --lang markdown --spec "$spec" --doc-path docs/ --out "$here/$dir/generated/TRIGGERS.md"
      echo "    triggers: generated/src/DocSpace.Webhooks.SDK/Triggers.cs + generated/TRIGGERS.md"
      ;;
    typescript)
      python "$here/tools/gen-trigger-map.py" --lang typescript --spec "$spec" \
             --out "$here/$dir/generated/triggers.ts"
      echo "    triggers: generated/triggers.ts"
      ;;
  esac

  generated+=("$dir")
done

# ------------------------------------------------------------------ verify
#
# Generating is not the same as working: every SDK here once generated cleanly
# and still failed to compile or to parse a real delivery. So each language that
# was just generated is built and its sample is run over every signed fixture in
# samples/ - the sample verifies the signature and parses the payload with the
# generated models, which is exactly what a receiver does. A missing toolchain
# skips its language and is reported, never silently passed.

[ "${VERIFY:-1}" = "0" ] && exit $fail

# Native tools on Windows (java, php, python, dotnet) cannot open /c/... paths.
native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

need() {
  for tool in "$@"; do
    command -v "$tool" >/dev/null 2>&1 || { echo "    SKIPPED -- $tool not found"; return 1; }
  done
}

# run_sample <dir> <fixture.json> <fixture.sig>; the secret is samples/SECRET
run_sample() {
  local body sig secret
  body="$(native "$2")"; sig="$(cat "$3")"; secret="$(cat "$here/samples/SECRET")"
  case "$1" in
    csharp)     (cd "$here/csharp/samples/WebhookLogger" && dotnet run --no-build -- --file "$body" --secret "$secret" --signature "$sig") ;;
    go)         (cd "$here/go/samples" && go run . "$body" "$secret" "$sig") ;;
    java)       (cd "$here/java" && mvn -q exec:java -Dexec.args="$body $secret $sig") ;;
    kotlin)     (cd "$here/kotlin" && mvn -q exec:java -Dexec.args="$body $secret $sig") ;;
    php)        (cd "$here/php/samples" && php log_webhook.php "$body" "$secret" "$sig") ;;
    python)     (cd "$here/python/samples" && python log_webhook.py "$body" "$secret" "$sig") ;;
    ruby)       (cd "$here/ruby/samples" && ruby log_webhook.rb "$body" "$secret" "$sig") ;;
    swift)      (cd "$here/swift" && swift run WebhookLogger "$body" "$secret" "$sig") ;;
    typescript) (cd "$here/typescript" && node dist/samples/log-webhook.js "$body" "$secret" "$sig") ;;
  esac
}

build() {
  case "$1" in
    csharp)     need dotnet && (cd "$here/csharp/samples/WebhookLogger" && dotnet build -nologo -v q) ;;
    go)         need go && (cd "$here/go/generated" && go vet ./...) && (cd "$here/go/samples" && go vet ./...) ;;
    java)       need mvn && (cd "$here/java" && mvn -q compile) ;;
    kotlin)     need mvn && (cd "$here/kotlin" && mvn -q compile) ;;
    php)        need php && find "$here/php/generated/lib" -name '*.php' -print0 | xargs -0 -n1 php -l > /dev/null ;;
    python)     need python && python -m compileall -q "$(native "$here/python/generated")" ;;
    ruby)       need ruby ;;
    swift)      need swift && (cd "$here/swift" && swift build) ;;
    typescript) need npm && (cd "$here/typescript" && { [ -d node_modules ] || npm ci --silent || npm install --silent; } && npm run -s build) ;;
  esac
}

unverified=()
for dir in "${generated[@]}"; do
  echo "=== verify $dir"
  log="$here/$dir/generated/.verify.log"
  : > "$log"
  if ! build "$dir" >> "$log" 2>&1; then
    if grep -q '^    SKIPPED' "$log"; then
      cat "$log"
      unverified+=("$dir")
    else
      echo "    BUILD FAILED -- see $dir/generated/.verify.log" >&2
      fail=1
    fi
    continue
  fi

  for fixture in "$here"/samples/*.json; do
    name="$(basename "$fixture" .json)"
    if run_sample "$dir" "$fixture" "${fixture%.json}.sig" >> "$log" 2>&1; then
      echo "    $name: ok"
    else
      echo "    $name: FAILED -- see $dir/generated/.verify.log" >&2
      fail=1
    fi
  done
done

if [ ${#unverified[@]} -gt 0 ]; then
  echo
  echo "NOT VERIFIED (toolchain missing): ${unverified[*]}" >&2
fi

exit $fail
