#!/usr/bin/env bash
# PostToolUse hook (Edit|Write): lints and format-checks the TypeScript file of
# common/ASC.AI.Chat that was just written. Exit 2 hands the findings back to the agent;
# any other file, or a checkout without node_modules, passes silently.
set -u

file=$(python3 -c 'import json,sys; print(json.load(sys.stdin).get("tool_input",{}).get("file_path",""))' 2>/dev/null) || exit 0
case "$file" in
  */common/ASC.AI.Chat/*.ts) ;;
  *) exit 0 ;;
esac

module="${file%%/common/ASC.AI.Chat/*}/common/ASC.AI.Chat"
[ -d "$module/node_modules/eslint" ] || exit 0
[ -f "$file" ] || exit 0
cd "$module" || exit 0

out=$(node node_modules/eslint/bin/eslint.js "$file" 2>&1)
lint=$?
fmt_out=$(node node_modules/prettier/bin/prettier.cjs --check "$file" 2>&1)
fmt=$?

if [ $lint -ne 0 ] || [ $fmt -ne 0 ]; then
  {
    [ $lint -ne 0 ] && echo "$out"
    [ $fmt -ne 0 ] && echo "$fmt_out" && echo "Format it: yarn prettier --write ${file#"$module"/}"
    echo "Rules: .claude/rules/ts-ai-style.md, ts-ai-failure-paths.md"
  } >&2
  exit 2
fi
exit 0
