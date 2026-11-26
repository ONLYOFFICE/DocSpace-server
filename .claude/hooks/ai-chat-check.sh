#!/usr/bin/env bash
# PostToolUse hook (Edit|Write): lints and format-checks the TypeScript file of
# common/ASC.AI.Chat that was just written. Exit 2 hands the findings back to the agent;
# any other file, or a checkout without node_modules, passes silently.
set -u

# The hook input is read with node, which the check itself needs anyway: `python3` on Windows
# can be the Microsoft Store stub, which fails without a word and let every file pass.
command -v node >/dev/null 2>&1 || exit 0
file=$(node -e 'let s="";process.stdin.on("data",(d)=>(s+=d)).on("end",()=>{try{process.stdout.write(String(JSON.parse(s).tool_input?.file_path??""))}catch{}})' 2>/dev/null) || exit 0
# Windows hands the path over with backslashes (C:\Users\...\common\ASC.AI.Chat\app\x.ts): the
# patterns below are written with slashes.
file=${file//\\//}
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
