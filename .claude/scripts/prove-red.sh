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
#   .claude/scripts/prove-red.sh [--commit SHA | --patch FILE | --mutate MUTATION...] [--exclude PATH]...
#                                [--build PROJECT]... --test PROJECT [-- ARGS...]
#
#   --test PROJECT   the test project (.csproj) to build and run
#   -- ARGS...       Microsoft.Testing.Platform arguments, e.g. --filter-class "*LicenseReaderTests"
#   --commit SHA     take away the product part of that commit (the re-check of a committed fix)
#   --patch FILE     take away only the change in FILE, a diff as `git diff` writes it whose reverse
#                    gives the code without the behaviour under test. Use it when the test calls what
#                    the change adds, so taking the whole change away leaves it unable to build (exit 3)
#   --mutate FILE:OLD=>NEW
#   --mutate FILE --old OLD --new NEW
#                    instead of taking a change away, replace the text OLD by NEW in FILE (a path from the
#                    repo root; OLD must occur exactly once) and expect the test red with the mutation and
#                    green without it. FILE:OLD=>NEW ends FILE at the first ':' and is refused unless it
#                    holds exactly one '=>'; when OLD or NEW holds '=>' (a C# lambda, an expression body) or
#                    FILE holds ':', use the second form, which takes FILE, OLD and NEW whole. Repeatable,
#                    all mutations in one run. The mutations of one file are applied in the order given:
#                    their OLD texts must not overlap, and each OLD must still occur exactly once when the
#                    mutations before it have been applied. For a test that pins a value: one run per
#                    side, e.g. 'X.cs:MinimumThroughput = 5=>MinimumThroughput = 4', then `= 6`
#   (none of them)   take away every uncommitted product change of the working tree
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
# 3 the test does not build without the change (with the mutation, for --mutate); 4 not green with the
# change, no test ran, or the test host failed instead of reporting a result; 5 the change cannot be
# taken away or put back.
#
# An interrupted run (Ctrl+C, SIGTERM) puts the change back before it exits. Nothing is lost if it
# cannot: an uncommitted change waits in `git stash list` as "prove-red", a committed one is still in
# HEAD, a patch is reapplied with `git apply --whitespace=nowarn FILE`, and a mutated file is put back
# from a folder in ${TMPDIR:-/tmp}, unique to the run, that holds the patch of the mutations and the
# files as they were; the run deletes that folder when it ends with every file as it was and keeps it
# otherwise. The script prints the command that puts the change back before it takes it away.

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
# One entry per --mutate, in the order given: the file as passed, OLD and NEW.
mut_files=()
mut_olds=()
mut_news=()
test_project=""
extra_builds=()
excludes=()
test_args=()

# Adds a FILE:OLD=>NEW mutation. FILE ends at the first ':'. C# code holds '=>' itself (lambdas,
# expression bodies), so a spec with more than one '=>' has no sure split: it is refused.
split_mutation() {
    local m=$1 file rest arrows n
    file=${m%%:*}
    rest=${m#*:}
    arrows=${rest//=>/}
    n=$(( (${#rest} - ${#arrows}) / 2 ))
    if [ "$file" = "$m" ] || [ -z "$file" ] || [ "$n" -eq 0 ]; then
        echo "--mutate takes FILE:OLD=>NEW or FILE --old OLD --new NEW, got: $m" >&2
        exit 2
    fi
    if [ "$n" -gt 1 ]; then
        echo "--mutate FILE:OLD=>NEW needs exactly one '=>', this one holds $n: $m" >&2
        echo "When OLD or NEW holds '=>', pass the mutation as --mutate FILE --old OLD --new NEW." >&2
        exit 2
    fi
    mut_files+=("$file")
    mut_olds+=("${rest%%=>*}")
    mut_news+=("${rest#*=>}")
}

while [ $# -gt 0 ]; do
    case "$1" in
        --commit) commit="${2:-}"; shift 2 || usage ;;
        --patch) patch="${2:-}"; shift 2 || usage ;;
        --mutate)
            [ $# -ge 2 ] || usage
            if [ "${3:-}" = --old ]; then
                [ $# -ge 6 ] && [ "$5" = --new ] || {
                    echo "--mutate FILE --old OLD takes --new NEW right after OLD." >&2; exit 2; }
                mut_files+=("$2")
                mut_olds+=("$4")
                mut_news+=("$6")
                shift 6
            else
                split_mutation "$2"
                shift 2
            fi ;;
        --old|--new) echo "$1 belongs to a mutation: --mutate FILE --old OLD --new NEW, in this order." >&2; exit 2 ;;
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
[ ${#mut_files[@]} -eq 0 ] || [ -z "$commit$patch" ] || { echo "--mutate excludes --commit and --patch." >&2; exit 2; }
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

# Prints the repo path of the existing file PATH as git spells it when git knows that file under any
# letter case, else PATH itself. On a file system that ignores case, common/lib/x.cs names the file
# common/Lib/X.cs too: it has to count as that one file, and a patch written with the other spelling
# would rename it.
git_spelling() {
    local c scope
    for scope in --cached "--others --exclude-standard"; do
        while IFS= read -r -d '' c; do
            [ "$c" -ef "$1" ] && { echo "$c"; return 0; }
        done < <(git ls-files -z $scope -- ":(icase,literal)$1")
    done
    echo "$1"
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
# Until something is taken away, an exit only has to delete $tmp; the full EXIT trap comes with put_back.
trap 'rm -rf "$tmp"' EXIT
trap 'echo; echo "Interrupted."; exit 130' INT TERM

# --mutate writes the mutated text into the files and saves their bytes to put back (see put_back); the
# rest of the run is the one of the other modes, with the verdict worded for a mutation.
without_what=""
green_label="green with it"
# The folder that the restore hint of a mutation names; see the EXIT trap.
mutation_copy=""

# Sets count to the number of times the non-empty text $1 occurs in the text $2, without overlaps.
# Counted from what a global replace removes: a loop of ${s#*"$1"} takes time quadratic in the
# length of the text.
count_of() {
    local rest=${2//"$1"/}
    count=$(( (${#2} - ${#rest}) / ${#1} ))
}

if [ ${#mut_files[@]} -gt 0 ]; then
    # The mutations of a file are applied in turn to one mutated copy of it, and the file is diffed
    # once: a diff of the original per mutation gives each its own hunk, and a hunk whose context
    # lines hold another mutation does not apply after that one.
    mark=$'\001'
    m_paths=()  # the files, in the order they are first named
    m_origs=()  # the text of each as it is in the working tree
    m_texts=()  # the text of each with its mutations so far
    m_masks=()  # the text of each with the OLD of each of its mutations so far replaced by $mark
    described=()
    i=0
    while [ $i -lt ${#mut_files[@]} ]; do
        file=${mut_files[$i]}
        old=${mut_olds[$i]}
        new=${mut_news[$i]}
        i=$((i + 1))
        [ -n "$old" ] || { echo "--mutate: OLD is empty for $file." >&2; exit 2; }
        case "$old" in *"$mark"*)
            echo "--mutate: OLD holds the control character \\001, which this script uses as a marker." >&2; exit 2 ;;
        esac
        f=$(repo_path "$file") && [ -f "$f" ] || { echo "--mutate: no such file in the repo: $file" >&2; exit 2; }
        f=$(git_spelling "$f")
        k=0
        while [ $k -lt ${#m_paths[@]} ] && [ "${m_paths[$k]}" != "$f" ] && ! [ "${m_paths[$k]}" -ef "$f" ]; do
            k=$((k + 1))
        done
        if [ $k -eq ${#m_paths[@]} ]; then
            # The text is read from a copy, so the bytes put back are the bytes the mutation was made of.
            cp -- "$f" "$tmp/original.$k" || exit 2
            # Read byte for byte: $(cat) would drop the trailing newlines.
            content=$(cat -- "$tmp/original.$k"; printf x)
            content=${content%x}
            m_paths+=("$f")
            m_origs+=("$content")
            m_texts+=("$content")
            m_masks+=("$content")
        fi
        f=${m_paths[$k]}
        count_of "$old" "${m_origs[$k]}"
        if [ "$count" -ne 1 ]; then
            echo "--mutate: \"$old\" occurs $count times in $f; give text that occurs exactly once." >&2; exit 2
        fi
        # OLD holds no $mark, so an occurrence of it in the mask lies between marks, in text of the
        # original: it is the one occurrence there, which is gone if an earlier OLD overlapped it.
        count_of "$old" "${m_masks[$k]}"
        if [ "$count" -ne 1 ]; then
            echo "--mutate: \"$old\" overlaps the OLD text of an earlier mutation of $f; make them one mutation." >&2
            exit 2
        fi
        # That occurrence is still in the mutated text too, but the NEW of an earlier mutation, alone
        # or with the text next to it, can make a second one.
        count_of "$old" "${m_texts[$k]}"
        if [ "$count" -ne 1 ]; then
            echo "--mutate: \"$old\" occurs $count times in $f once the mutations before it are applied; give text that stays unique." >&2
            exit 2
        fi
        m_texts[$k]="${m_texts[$k]/"$old"/"$new"}"
        m_masks[$k]="${m_masks[$k]/"$old"/"$mark"}"
        described+=("$f: \"$old\" => \"$new\"")
    done
    # The patch from the mutated text back to the original, per file and whole: the restore uses it
    # for a file changed meanwhile, the restore hint for the user.
    : > "$tmp/mutation.patch"
    k=0
    while [ $k -lt ${#m_paths[@]} ]; do
        if [ "${m_texts[$k]}" = "${m_origs[$k]}" ]; then
            echo "--mutate: the mutations of ${m_paths[$k]} leave its text as it is." >&2; exit 2
        fi
        printf '%s' "${m_texts[$k]}" > "$tmp/mutated.$k"
        diff -u --label "a/${m_paths[$k]}" --label "b/${m_paths[$k]}" "$tmp/mutated.$k" "$tmp/original.$k" > "$tmp/mutation.$k.patch"
        cat -- "$tmp/mutation.$k.patch" >> "$tmp/mutation.patch"
        k=$((k + 1))
    done
    without_what="with the mutation ($(IFS=';'; echo "${described[*]}"))"
    green_label="green without it"
fi

files=()
left=()
other=()
taken_away=0
restore_hint=""
fingerprint_before=""
stash_name=""

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
elif [ ${#mut_files[@]} -gt 0 ]; then
    # Product files only, as for a patch: a mutated test would test another test.
    split_product < <(printf '%s\n' "${m_paths[@]}")
    what="the mutated text"
    none="no mutated file is product code"
    if [ $(( ${#left[@]} + ${#other[@]} )) -gt 0 ]; then
        echo "A mutation must change product code only; these files are not:" >&2
        printf '  %s\n' ${left[@]+"${left[@]}"} ${other[@]+"${other[@]}"} >&2
        exit 2
    fi
elif [ -n "$patch" ]; then
    cp -- "$patch" "$tmp/change.patch"
    # A patch is taken away as written, so it may hold product files only: reversing a test file
    # would test the old test, reversing this script would rewrite it while it runs.
    split_product < <(git apply --numstat --whitespace=nowarn "$tmp/change.patch" | cut -f3 | sort -u)
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

# taken_away: 0 nothing is taken away; 2 taken away; 1 the take-away has started and may have failed
# or been cut short by an interrupt, which put_back handles in every mode: it compares the files with
# what it saved before (mutation, patch), restores them from HEAD whatever their state (commit), or
# pops the newest stash entry only if this run made it (uncommitted changes).
# --whitespace=nowarn keeps an apply.whitespace=fix in the user's git config from editing the lines
# a patch adds. restore_failed: put_back has failed and said so; it does not try again.
restore_failed=0

# Prints a line per product file: its checksum, or "-" for a file that does not exist.
fingerprint() {
    local f
    for f in "${files[@]}"; do
        if [ -e "$f" ]; then cksum < "$f"; else echo -; fi
    done
}

# Says that the change could not be put back, why ($1), and the command that does it by hand.
cannot_put_back() {
    echo "Could not put the change back automatically${1:+ ($1)}: $restore_hint" >&2
    restore_failed=1
    return 1
}

put_back() {
    [ "$restore_failed" = 0 ] || return 1
    [ "$taken_away" != 0 ] || return 0
    if [ ${#mut_files[@]} -gt 0 ]; then
        # A file that holds the mutated text gets the saved bytes back, one that holds the original is
        # left alone, and one changed otherwise since is patched, if the patch still applies, which
        # keeps that change.
        local k=0 f bad=""
        while [ $k -lt ${#m_paths[@]} ]; do
            f=${m_paths[$k]}
            if cmp -s -- "$f" "$tmp/original.$k"; then
                :
            elif cmp -s -- "$f" "$tmp/mutated.$k"; then
                cp -- "$tmp/original.$k" "$f" || bad="$bad $f"
            else
                git apply --whitespace=nowarn "$tmp/mutation.$k.patch" || bad="$bad $f"
            fi
            k=$((k + 1))
        done
        [ -z "$bad" ] || { cannot_put_back "not put back:$bad"; return 1; }
    elif [ -n "$commit" ]; then
        # The files were as in HEAD before the take-away, so HEAD puts them back from any state of it.
        local f
        for f in "${files[@]}"; do
            if git cat-file -e "HEAD:$f" 2>/dev/null; then
                git checkout -q HEAD -- "$f" || { cannot_put_back; return 1; }
            else
                rm -f -- "$f"
            fi
        done
    elif [ -n "$patch" ]; then
        # Files whose checksums still match the ones from before the take-away are taken as untouched by it.
        if [ "$taken_away" = 2 ] || [ "$(fingerprint)" != "$fingerprint_before" ]; then
            git apply --whitespace=nowarn "$tmp/change.patch" || { cannot_put_back; return 1; }
        fi
    else
        # Only the entry of this run is popped: the worktrees of a repo share one stash.
        if git stash list -n 1 | grep -qF "$stash_name"; then
            git stash pop -q --index 2>/dev/null || git stash pop -q || { cannot_put_back "it is in the stash"; return 1; }
        elif [ "$taken_away" = 2 ]; then
            cannot_put_back "the newest stash entry is not this run's; the change is in the stash"
            return 1
        fi
    fi
    taken_away=0
}

# On exit, put back what is still taken away; a second Ctrl+C does not cut that short. The folder
# that the restore hint of a mutation names is deleted unless put_back failed, whose message names
# it; a run killed before this trap ran leaves it too.
on_exit() {
    trap '' INT TERM
    if put_back; then
        [ -z "$mutation_copy" ] || rm -rf -- "$mutation_copy"
    fi
    rm -rf "$tmp"
}
trap on_exit EXIT

if [ -n "$commit" ]; then
    if ! git diff --quiet HEAD -- "${files[@]}" || [ -n "$(git ls-files --others --exclude-standard -- "${files[@]}")" ]; then
        echo "The files of $commit have uncommitted edits; commit or stash them first." >&2
        exit 5
    fi
    git diff --binary "$commit^" "$commit" -- "${files[@]}" > "$tmp/change.patch"
    if ! git apply --check -R --whitespace=nowarn "$tmp/change.patch" 2>"$tmp/apply.log"; then
        echo "The product part of $commit no longer reverses cleanly on HEAD (later commits changed the same lines):" >&2
        head -5 "$tmp/apply.log" >&2
        exit 5
    fi
    restore_hint="git checkout HEAD --$(printf ' %q' "${files[@]}")"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    taken_away=1
    git apply -R --whitespace=nowarn "$tmp/change.patch" || {
        echo "git apply could not take away the product part of $commit; the files are put back from HEAD." >&2
        exit 5
    }
    taken_away=2
elif [ ${#mut_files[@]} -gt 0 ]; then
    k=0
    while [ $k -lt ${#m_paths[@]} ]; do
        cmp -s -- "${m_paths[$k]}" "$tmp/original.$k" || {
            echo "${m_paths[$k]} changed after the script read it; nothing was mutated." >&2; exit 5; }
        k=$((k + 1))
    done
    # The patch and the files as they are go to a folder outside $tmp, which the EXIT trap deletes:
    # the restore hint has to work after the script has ended. It is unique to this run.
    mutation_copy=$(mktemp -d "${TMPDIR:-/tmp}/prove-red-mutation.XXXXXX") \
        && cp -- "$tmp/mutation.patch" "$mutation_copy/mutation.patch" || {
        echo "Could not write the restore folder in ${TMPDIR:-/tmp}; nothing was mutated." >&2; exit 5; }
    k=0
    while [ $k -lt ${#m_paths[@]} ]; do
        mkdir -p -- "$mutation_copy/original/$(dirname -- "${m_paths[$k]}")" \
            && cp -- "$tmp/original.$k" "$mutation_copy/original/${m_paths[$k]}" || {
            echo "Could not write the restore folder $mutation_copy; nothing was mutated." >&2; exit 5; }
        k=$((k + 1))
    done
    restore_hint="git apply --whitespace=nowarn $(printf '%q' "$mutation_copy/mutation.patch")   (the files as they were: $(printf '%q' "$mutation_copy/original"))"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    taken_away=1
    k=0
    while [ $k -lt ${#m_paths[@]} ]; do
        cp -- "$tmp/mutated.$k" "${m_paths[$k]}" || { echo "Could not write the mutated text to ${m_paths[$k]}." >&2; exit 5; }
        k=$((k + 1))
    done
    taken_away=2
elif [ -n "$patch" ]; then
    if ! git apply --check -R --whitespace=nowarn "$tmp/change.patch" 2>"$tmp/apply.log"; then
        echo "The patch does not reverse cleanly on the working tree:" >&2
        head -5 "$tmp/apply.log" >&2
        exit 5
    fi
    restore_hint="git apply --whitespace=nowarn $(printf '%q' "$patch")"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    fingerprint_before=$(fingerprint)
    taken_away=1
    git apply -R --whitespace=nowarn "$tmp/change.patch" || { echo "git apply could not take the patch away." >&2; exit 5; }
    taken_away=2
else
    # The process id keeps the name apart from a run started in the same second in another worktree.
    stash_name="prove-red $(date +%Y%m%d-%H%M%S) $$"
    restore_hint="git stash pop --index   (the entry named \"$stash_name\")"
    echo "If this run is interrupted, put the change back with: $restore_hint"
    taken_away=1
    if ! git stash push -q --include-untracked -m "$stash_name" -- "${files[@]}"; then
        taken_away=0
        if git stash list -n 1 | grep -qF "$stash_name"; then
            echo "git stash failed but recorded \"$stash_name\": compare the working tree with that entry before anything else." >&2
        else
            echo "git stash failed; nothing was taken away." >&2
        fi
        exit 5
    fi
    if ! git stash list -n 1 | grep -qF "$stash_name"; then
        taken_away=0
        echo "git stash recorded nothing; nothing was taken away." >&2
        exit 5
    fi
    taken_away=2
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
    if [ ${#mut_files[@]} -gt 0 ]; then
        echo "Nothing builds with the mutation in place. If the errors are in the mutated code, pick a mutation that compiles."
    else
        echo "Nothing builds without the change: the test uses what the change adds, or the code without it does not compile."
        echo "Take away only the behavioural part with --patch, or prove it another way and say so in the commit."
    fi
    put_back || exit 5
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

[ -n "$without_what" ] || without_what="without $what"
if [ "$red_outcome" = passed ]; then
    echo "STILL GREEN: $red_total of $red_total passed $without_what; the test does not depend on it."
    exit 1
fi

echo "Failed $without_what:"
sed 's/^/  /' "$tmp/failed-without.txt"
echo
echo "RED as expected: $red_failed of $red_total failed $without_what (${#files[@]} file(s)); $green_label: $total of $total passed."
exit 0
