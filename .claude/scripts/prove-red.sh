#!/usr/bin/env bash
# Prove that a test guards a change: take the product part of the change away, run the test and
# expect it to fail, then put the change back and expect it to pass.
#
# A guard test that stays green without the change it is meant to protect protects nothing, and
# reading the test is not enough to tell: reviews kept finding tests that passed with the fix
# reverted. This is the check `.claude/rules/tests.md` ("A test has to be able to fail") and
# `.claude/rules/fixing-findings.md` section 3 ask for.
#
# Usage (from the repo root, server/):
#   .claude/scripts/prove-red.sh [--commit SHA | --patch FILE] [--exclude PATH]... [--build PROJECT]...
#                                --test PROJECT [-- ARGS...]
#
#   --test PROJECT   the test project (.csproj) to build and run
#   -- ARGS...       Microsoft.Testing.Platform arguments, e.g. --filter-class "*LicenseReaderTests"
#   --commit SHA     take away the product part of that commit (the re-check of a committed fix)
#   --patch FILE     take away only the change in FILE, a diff as `git diff` writes it whose reverse
#                    gives the code without the behaviour under test. Use it when the test calls what
#                    the change adds, so taking the whole change away leaves it unable to build (exit 3)
#   (neither)        take away every uncommitted product change of the working tree
#   --exclude PATH   a file or directory inside the repo to leave in place although it is product code,
#                    e.g. a local edit of an appsettings file that is not part of the change
#   --build PROJECT  one more project to build in both states, for code the test project does not
#                    reference
#
# The test project is built with its references. For an integration suite that includes the AppHost
# and every service it starts, so the Aspire stack runs the code of each state, never stale binaries.
# A running DocSpace locks those binaries (MSB3027): stop it with `aspire stop` first.
#
# "Product" is a changed file under common/, products/, web/, migrations/ or thirdparty/, or a
# Directory.Build.* / Directory.Packages.props at the repo root, outside the test projects (the known
# test folders and the folder of the --test project) and the submodules. The rest of the root
# (global.json, nuget.config, the solutions), .claude/ and sdk/ stay in place, so a local edit of the
# environment is not taken away with the change and the state without it is built by the same SDK.
# The script lists what it leaves in place. Test files are never touched, so the test of the change
# runs against the code without it. A --patch must hold product files only; it is refused otherwise.
# Staged deletions and renames of product files are refused too: git stash cannot take them away.
#
# The verdict comes from the exit code of the test run, checked against its summary; the CLI is
# switched to English (DOTNET_CLI_UI_LANGUAGE) so that the summary can be read and quoted, and
# TESTINGPLATFORM_EXITCODE_IGNORE is cleared, as it would turn a failed run into exit code 0.
#
# Exit codes: 0 red without the change and green with it; 1 still green without the change; 2 usage;
# 3 the test does not build without the change; 4 not green with the change, no test ran, or the test
# host failed instead of reporting a result; 5 the change cannot be taken away.
#
# Nothing is lost if the run is interrupted: an uncommitted change waits in `git stash list` as
# "prove-red", a committed one is still in HEAD, a patch is reapplied with `git apply FILE`. The script
# prints the command that puts the change back before it takes it away.

set -uo pipefail

# `dotnet` prints its summary and build errors in the language of the machine, so "total:" is not there to read.
export DOTNET_CLI_UI_LANGUAGE=en
# Set in a shell, it makes the test host exit with 0 for the codes it lists: a red run would read as green.
unset TESTINGPLATFORM_EXITCODE_IGNORE

usage() {
    sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'
    exit 2
}

commit=""
patch=""
test_project=""
extra_builds=()
excludes=()
test_args=()

while [ $# -gt 0 ]; do
    case "$1" in
        --commit) commit="${2:-}"; shift 2 || usage ;;
        --patch) patch="${2:-}"; shift 2 || usage ;;
        --test) test_project="${2:-}"; shift 2 || usage ;;
        --build) extra_builds+=("${2:-}"); shift 2 || usage ;;
        --exclude) excludes+=("${2:-}"); shift 2 || usage ;;
        --) shift; test_args=("$@"); break ;;
        -h|--help) usage ;;
        *) echo "Unknown argument: $1" >&2; usage ;;
    esac
done

[ -n "$test_project" ] || { echo "--test is required" >&2; usage; }
git rev-parse --git-dir >/dev/null 2>&1 && [ -z "$(git rev-parse --show-prefix)" ] || {
    echo "Run from the repo root (server/)." >&2; exit 2; }
[ -f "$test_project" ] || { echo "Test project not found: $test_project" >&2; exit 2; }
[ -n "$commit" ] && [ -n "$patch" ] && { echo "--commit and --patch exclude each other." >&2; exit 2; }
[ -z "$patch" ] || [ -f "$patch" ] || { echo "Patch not found: $patch" >&2; exit 2; }
# Written for the bash 3.2 that macOS ships: no mapfile, and an empty array is expanded as
# ${a[@]+"${a[@]}"}, because "${a[@]}" of an empty array fails under `set -u` before bash 4.4.
for p in ${extra_builds[@]+"${extra_builds[@]}"}; do
    [ -f "$p" ] || { echo "Project not found: $p" >&2; exit 2; }
done
for a in ${test_args[@]+"${test_args[@]}"}; do
    case "$a" in
        --ignore-exit-code*) echo "--ignore-exit-code hides the result this script reads; leave it out." >&2; exit 2 ;;
    esac
done

product_re='^(common|products|web|migrations|thirdparty)/|^Directory\.(Build\.(props|targets)|Packages\.props)$'
test_re='^(products/[^/]+/(Tests|ASC\.[A-Za-z.]+\.Tests)/|common/Tests/|web/ASC\.Web\.Api(\.Billing)?\.Tests/)'

# Submodule pointers (common/resources/DocStore lies inside common/) are checkouts, not product code.
submodules=()
if [ -f .gitmodules ]; then
    while IFS= read -r line; do
        [ -n "$line" ] && submodules+=("${line#* }")
    done < <(git config --file .gitmodules --get-regexp '\.path$' || true)
fi

repo_root=$(pwd -P)

# Prints PATH as git prints the paths it reports, relative to the repo root, whatever form it was
# passed in ("./x", "x\y", an absolute path). Fails for a path outside the repo and for the root
# itself, which would match every file.
repo_path() {
    local p=${1//\\//} dir
    if [ -d "$p" ]; then
        dir=$(CDPATH='' cd -- "$p" 2>/dev/null && pwd -P) || return 1
        p=$dir
    else
        dir=$(CDPATH='' cd -- "$(dirname -- "$p")" 2>/dev/null && pwd -P) || return 1
        p=$dir/$(basename -- "$p")
    fi
    case "$p" in
        "$repo_root"/?*) echo "${p#"$repo_root"/}" ;;
        *) return 1 ;;
    esac
}

# The folder of the --test project holds test code wherever it lies (thirdparty/*.Tests, a new suite).
test_dir=$(repo_path "$(dirname -- "${test_project//\\//}")") || {
    echo "--test has to name a project in a folder of the repo, not at its root or outside it: $test_project" >&2
    exit 2
}
test_dir="$test_dir/"

resolved=()
for e in ${excludes[@]+"${excludes[@]}"}; do
    r=$(repo_path "$e") || { echo "--exclude has to name a path in an existing folder of the repo, not the root: $e" >&2; exit 2; }
    resolved+=("$r")
done
excludes=(${resolved[@]+"${resolved[@]}"})

# Prints what a changed path is: test or tooling (.claude/), never taken away; environment (the
# root, submodules, sdk/, what --exclude names), left in place and listed; or product.
kind_of() {
    local f=$1 e
    if [[ "$f" =~ $test_re ]] || [ "${f#"$test_dir"}" != "$f" ]; then
        echo test
        return
    fi
    if [ "${f#.claude/}" != "$f" ]; then
        echo tooling
        return
    fi
    for e in ${submodules[@]+"${submodules[@]}"} ${excludes[@]+"${excludes[@]}"}; do
        case "$f" in "$e"|"$e"/*) echo environment; return ;; esac
    done
    if [[ "$f" =~ $product_re ]]; then echo product; else echo environment; fi
}

# Splits the paths on stdin: product files go to `files`, environment files to `left`, test and
# tooling files to `other`.
split_product() {
    files=()
    left=()
    other=()
    local f
    while IFS= read -r f; do
        [ -n "$f" ] || continue
        case "$(kind_of "$f")" in
            product) files+=("$f") ;;
            environment) left+=("$f") ;;
            *) other+=("$f") ;;
        esac
    done
    return 0
}

tmp=$(mktemp -d)
files=()
left=()
other=()
taken_away=0
restore_hint=""

# --- what to take away -----------------------------------------------------------------------------

if [ -n "$commit" ]; then
    git rev-parse --verify -q "$commit^{commit}" >/dev/null || { echo "Not a commit: $commit" >&2; exit 2; }
    if [ "$(git rev-list --parents -n 1 "$commit" | wc -w | tr -d ' ')" -ne 2 ]; then
        echo "$commit is a merge or a root commit; pass the commit of the fix itself." >&2
        exit 2
    fi
    split_product < <(git diff-tree --no-commit-id --name-only -r --no-renames "$commit")
    what="the product part of $(git rev-parse --short "$commit")"
    none="the commit changes no product file"
elif [ -n "$patch" ]; then
    cp -- "$patch" "$tmp/change.patch"
    # A patch is taken away as written, so it may hold product files only: reversing a test file
    # would test the old test, reversing this script would rewrite it while it runs.
    split_product < <(git apply --numstat "$tmp/change.patch" | cut -f3 | sort -u)
    what="the change in $patch"
    none="the patch changes no file: it is empty or not a diff"
    if [ $(( ${#left[@]} + ${#other[@]} )) -gt 0 ]; then
        echo "The patch must take away product code only; it also changes:" >&2
        printf '  %s\n' ${left[@]+"${left[@]}"} ${other[@]+"${other[@]}"} >&2
        exit 2
    fi
else
    split_product < <(git status --porcelain=v1 --untracked-files=all \
        | sed -E 's/^.{3}//; s/^.* -> //; s/^"(.*)"$/\1/' | sort -u)
    what="the uncommitted product changes"
    none="only test, tooling or environment files changed"
    # git stash cannot take away a staged deletion (its path matches nothing) nor either side of a
    # staged rename, and a failed push can still leave an entry behind.
    staged=()
    while IFS=$'\t' read -r status from to; do
        case "$status" in D*|R*) ;; *) continue ;; esac
        for p in "$from" ${to:+"$to"}; do
            [ "$(kind_of "$p")" = product ] && staged+=("$p")
        done
    done < <(git diff --cached --name-status -M)
    if [ ${#staged[@]} -gt 0 ]; then
        echo "Staged deletions or renames of product files cannot be taken away; unstage them first:" >&2
        echo "  git restore --staged -- ${staged[*]}" >&2
        exit 5
    fi
fi

if [ ${#files[@]} -eq 0 ]; then
    echo "No product change to take away: $none." >&2
    exit 2
fi

echo "Taking away $what (${#files[@]} file(s)):"
printf '  %s\n' "${files[@]}"
if [ ${#left[@]} -gt 0 ]; then
    echo "Left in place, changed but not product code (${#left[@]} file(s); --exclude adds more):"
    printf '  %s\n' "${left[@]}" | head -10
    [ ${#left[@]} -le 10 ] || echo "  ... and $(( ${#left[@]} - 10 )) more"
fi

# --- take away and put back ------------------------------------------------------------------------

put_back() {
    [ "$taken_away" = 1 ] || return 0
    if [ -n "$commit" ]; then
        local f
        for f in "${files[@]}"; do
            if git cat-file -e "HEAD:$f" 2>/dev/null; then
                git checkout -q HEAD -- "$f"
            else
                rm -f -- "$f"
            fi
        done
    elif [ -n "$patch" ]; then
        git apply "$tmp/change.patch" || {
            echo "Could not put the change back automatically: $restore_hint" >&2
            return 1
        }
    else
        git stash pop -q --index 2>/dev/null || git stash pop -q || {
            echo "Could not put the change back automatically. It is in the stash: $restore_hint" >&2
            return 1
        }
    fi
    taken_away=0
}

trap 'put_back; rm -rf "$tmp"' EXIT
trap 'echo; echo "Interrupted."; exit 130' INT TERM

if [ -n "$commit" ]; then
    if ! git diff --quiet HEAD -- "${files[@]}" || [ -n "$(git ls-files --others --exclude-standard -- "${files[@]}")" ]; then
        echo "The files of $commit have uncommitted edits; commit or stash them first." >&2
        exit 5
    fi
    git diff --binary "$commit^" "$commit" -- "${files[@]}" > "$tmp/change.patch"
    if ! git apply --check -R "$tmp/change.patch" 2>"$tmp/apply.log"; then
        echo "The product part of $commit no longer reverses cleanly on HEAD (later commits changed the same lines):" >&2
        head -5 "$tmp/apply.log" >&2
        exit 5
    fi
    restore_hint="git checkout HEAD -- ${files[*]}"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    git apply -R "$tmp/change.patch"
    taken_away=1
elif [ -n "$patch" ]; then
    if ! git apply --check -R "$tmp/change.patch" 2>"$tmp/apply.log"; then
        echo "The patch does not reverse cleanly on the working tree:" >&2
        head -5 "$tmp/apply.log" >&2
        exit 5
    fi
    restore_hint="git apply $patch"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    git apply -R "$tmp/change.patch"
    taken_away=1
else
    stash_name="prove-red $(date +%Y%m%d-%H%M%S)"
    if ! git stash push -q --include-untracked -m "$stash_name" -- "${files[@]}"; then
        if git stash list -n 1 | grep -qF "$stash_name"; then
            echo "git stash failed but recorded \"$stash_name\": compare the working tree with that entry before anything else." >&2
        else
            echo "git stash failed; nothing was taken away." >&2
        fi
        exit 5
    fi
    if ! git stash list -n 1 | grep -qF "$stash_name"; then
        echo "git stash recorded nothing; nothing was taken away." >&2
        exit 5
    fi
    restore_hint="git stash pop --index   (the entry named \"$stash_name\")"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    taken_away=1
fi

# --- build and run ---------------------------------------------------------------------------------

build() {
    local log="$tmp/build-$1.log" p
    : > "$log"
    for p in "$test_project" ${extra_builds[@]+"${extra_builds[@]}"}; do
        if ! dotnet build "$p" -nologo -v:q >> "$log" 2>&1; then
            grep -E ' error |MSB3027' "$log" | sed -E 's/ \[[^]]*\]$//' | sort -u | head -10 >&2
            grep -q 'MSB3027' "$log" && echo "Binaries are locked by a running DocSpace: stop it with 'aspire stop'." >&2
            return 1
        fi
    done
}

total=0
failed=0
outcome=""
run_tests() {
    local log="$tmp/test-$1.log" code
    dotnet test --project "$test_project" --no-build -- ${test_args[@]+"${test_args[@]}"} > "$log.raw" 2>&1
    code=$?
    # Colour codes out; written to a new file because BSD sed has no plain `-i`.
    sed -E "s/$(printf '\033')\[[0-9;]*[A-Za-z]//g" "$log.raw" > "$log"
    total=$(grep -oE '^ *total: *[0-9]+' "$log" | tail -1 | grep -oE '[0-9]+' || true)
    failed=$(grep -oE '^ *failed: *[0-9]+' "$log" | tail -1 | grep -oE '[0-9]+' || true)
    total=${total:-?}
    failed=${failed:-?}
    grep -E '^failed ' "$log" | sed -E 's/^failed //' | head -10 > "$tmp/failed-$1.txt"
    # Microsoft.Testing.Platform exit codes (https://aka.ms/testingplatform/exitcodes): 0 every test
    # passed, 2 some failed, 8 none ran. Anything else is the host failing, not a test result: a crash,
    # an aborted session, a filter it could not read.
    local why=""
    case "$code" in
        0) outcome=passed ;;
        2) outcome=failed ;;
        8) outcome=none ;;
        *) outcome=host; why="ended with exit code $code, which is not a test result" ;;
    esac
    # A 0 has to agree with the summary: a rewritten exit code would turn a red run green.
    if [ "$outcome" = passed ]; then
        if [ "$total" = 0 ]; then
            outcome=none
        elif [ "$failed" != "?" ] && [ "$failed" -gt 0 ]; then
            outcome=host
            why="ended with exit code 0 although its summary counts $failed failed"
        fi
    fi
    if [ "$outcome" = host ]; then
        echo "The test run $1 the change $why; its last lines:" >&2
        tail -15 "$log" | sed 's/^/  /' >&2
    fi
}

echo "Building without the change..."
if ! build red; then
    echo "Nothing builds without the change: the test uses what the change adds, or the code without it does not compile."
    echo "Take away only the behavioural part with --patch, or prove it another way and say so in the commit."
    put_back
    echo "Rebuilding with the change put back..."
    build green >/dev/null 2>&1 || echo "Warning: the build with the change put back failed too." >&2
    exit 3
fi

echo "Running without the change..."
run_tests without
red_outcome=$outcome
red_total=$total
red_failed=$failed

put_back || exit 5

echo "Building with the change..."
if ! build green; then
    echo "The test project does not build with the change." >&2
    exit 4
fi

echo "Running with the change..."
run_tests with

echo
if [ "$red_outcome" = host ] || [ "$outcome" = host ]; then
    echo "NO RESULT: the test host failed (see above); neither run says whether the test guards the change." >&2
    exit 4
fi

if [ "$red_outcome" = none ] || [ "$outcome" = none ]; then
    echo "NO TESTS RAN: the filter matched nothing (${test_args[*]:-no filter})." >&2
    exit 4
fi

if [ "$outcome" = failed ]; then
    echo "NOT GREEN WITH THE CHANGE: $failed of $total failed with the change in place:" >&2
    sed 's/^/  /' "$tmp/failed-with.txt" >&2
    exit 4
fi

if [ "$red_outcome" = passed ]; then
    echo "STILL GREEN: $red_total of $red_total passed without $what; the test does not depend on it."
    exit 1
fi

echo "Failed without the change:"
sed 's/^/  /' "$tmp/failed-without.txt"
echo
echo "RED as expected: $red_failed of $red_total failed without $what (${#files[@]} file(s)); green with it: $total of $total passed."
exit 0
