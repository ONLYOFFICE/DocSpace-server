# Setting up textstat, Vale and vacuum

The skill works without these tools. `tools.py` computes the form thresholds itself where Vale and
vacuum are missing, and `check_texts.py` names what it skipped on its last line, `MISSING TOOLS:`.
What the tools add is a second opinion kept outside this repo: Vale's sentence and grade styles,
vacuum running the same ruleset a Spectral-style linter would, textstat's readability numbers.

Install them only when the user asks for it. An install changes the user's machine, and a tool that
appears changes who judges the queue (the last section). Use a package manager or a release archive;
never pipe an installer script into a shell (`curl … | sh`).

## What each one adds

| Tool | What it is | Used by | Adds | Without it |
|---|---|---|---|---|
| textstat | Python package | `check_texts.py` | Flesch-Kincaid grade, reading ease, fog index and difficult words per text | the `textstat` line is not printed |
| Vale | CLI, one binary | `scan.py`, `check_texts.py` | the `Docs` styles of `readability/vale` (sentence length, grade); in `check_texts.py` also the `Microsoft` style once synced | the stand-in in `tools.py` |
| vacuum | CLI, one binary | `scan.py`, `check_texts.py` | `readability/vacuum/ruleset.yaml` with its JavaScript function | the stand-in in `tools.py` |

Last checked on 2026-11-10, on Windows: Vale 3.26.0 from winget, vacuum 0.39.3 from npm and textstat
0.7.13 pass step 4. The macOS rows were not run on a Mac; their notes come from the Homebrew formula,
the vacuum cask and the release archives. Whichever version gets installed, step 4 is what proves it
works with the settings of this skill.

`python` in the commands here and in `SKILL.md` is the interpreter's name on Windows. On macOS and
Linux it is usually `python3`; `python` exists there only through pyenv, an active virtual environment
or an alias. Use the name `command -v python3 python` finds.

## 1. See what is missing

```bash
python .claude/skills/openapi-desc-opt/check_texts.py --check-tools
```

Its `MISSING TOOLS:` line is the list to install. Then see which package managers the machine has
(`command -v winget choco brew npm go` in Git Bash, `Get-Command` in PowerShell) and take, per tool,
the first row below the machine can run.

## 2. Install

**textstat** goes into the Python that runs the scripts (`python` on Windows, `python3` elsewhere):

```bash
python -m pip install --user textstat
```

Where the system Python refuses pip (`externally-managed-environment`), create a virtual environment,
install textstat there and set `TEXTSTAT_PYTHON` to that environment's `python` — the scripts call it
for the numbers. Homebrew's Python on macOS is such a Python; `/usr/bin/python3` of the Command Line
Tools is not. For macOS or Linux:

```bash
python3 -m venv ~/.venvs/textstat
~/.venvs/textstat/bin/python -m pip install textstat
export TEXTSTAT_PYTHON=~/.venvs/textstat/bin/python
```

The `export` lasts as long as the shell; for later sessions the user puts it into their shell profile
(`~/.zshrc` on macOS) — a change to their configuration, so ask before making it.

**Vale**

| Machine | Command |
|---|---|
| Windows with winget | `winget install --id errata-ai.Vale --exact --source winget` |
| Windows with Chocolatey | `choco install vale` (elevated shell) |
| macOS or Linux with Homebrew | `brew install vale` |
| any | the archive from https://github.com/vale-cli/vale/releases (`vale_<version>_Windows_64-bit.zip`, `…_Linux_64-bit.tar.gz`, `…_macOS_arm64.tar.gz`, `…_macOS_64-bit.tar.gz` for an Intel Mac), its `vale` / `vale.exe` put into a folder on `PATH` |

Homebrew has ready builds of Vale (bottles) for Apple silicon and Linux only. On an Intel Mac
(`uname -m` prints `x86_64`) `brew install vale` builds it from source, which takes much longer; the
`macOS_64-bit` archive is the quicker route there.

winget may ask to accept the source and package agreements, and Chocolatey needs an elevated shell.
Both are the user's to answer: run the command where they can, or ask them before adding
`--accept-source-agreements --accept-package-agreements`. Vale parses source code through tree-sitter
and CGo, so `go install` needs a C compiler; prefer the rows above.

**vacuum**

| Machine | Command |
|---|---|
| any with Node.js | `npm install -g @quobix/vacuum` (the package downloads the release binary from GitHub when it installs) |
| macOS or Linux with Homebrew | `brew install --cask daveshanley/vacuum/vacuum` |
| any with Go | `go install github.com/daveshanley/vacuum@latest` (the build lacks the `html-report` UI, which the skill does not use) |
| any | the archive from https://github.com/daveshanley/vacuum/releases (`vacuum_<version>_windows_x86_64.tar.gz`, `…_linux_x86_64.tar.gz`, `…_darwin_arm64.tar.gz`, `…_darwin_x86_64.tar.gz` for an Intel Mac), its binary put into a folder on `PATH` |

Neither winget nor Chocolatey has vacuum; Chocolatey's `vacuum-im` is a different program.

Two macOS traps, neither seen on a real Mac yet:

- **`npm install -g` fails with `EACCES`** when Node came from the nodejs.org installer: the global
  folder under `/usr/local` belongs to root. Do not retry with `sudo` on your own. Take the Homebrew
  row instead, or ask the user whether to run it with `sudo` or move npm's global folder into their
  home (`npm config set prefix`, a change to their configuration). Node from Homebrew has no such
  problem.
- **macOS refuses to run `vacuum`** ("cannot be opened because the developer cannot be verified" or
  "Apple could not verify…") after the cask install: the cask has no step that clears the download's
  quarantine, and the binary may not be notarized. Install through npm instead, which downloads the
  binary itself and leaves no quarantine mark. Do not remove the quarantine attribute (`xattr`) on your
  own: that bypasses Gatekeeper, and it is the user's decision, as is allowing the program in System
  Settings → Privacy & Security.

When both apply, the Go row and the release archive remain. Fetch the archive with `curl -fLO`, not
a browser: a browser marks the download with the same quarantine.

## 3. Make the scripts find them

The scripts look for `--vale` / `--vacuum`, then `VALE_BIN` / `VACUUM_BIN`, then `PATH`. A tool
installed during a session is often not on the `PATH` of a shell that was already running: a new
terminal sees it, the current one may not. Until then, point the scripts at the file:

| Installed by | Where the command lands |
|---|---|
| winget | `%LOCALAPPDATA%\Microsoft\WinGet\Packages\errata-ai.Vale_Microsoft.Winget.Source_8wekyb3d8bbwe\vale.exe`; winget adds that folder to the user `PATH` and says "restart your shell to use the new value" |
| Chocolatey | `C:\ProgramData\chocolatey\bin\vale.exe` |
| npm | `vacuum.cmd` in the folder `npm prefix -g` prints on Windows, `bin/vacuum` under it elsewhere; that folder is usually on `PATH` already, so vacuum is found at once |
| go install | `vacuum` / `vacuum.exe` in the `bin` folder under `go env GOPATH` |
| Homebrew | the `bin` folder under `brew --prefix` |

```bash
export VALE_BIN="$LOCALAPPDATA/Microsoft/WinGet/Packages/errata-ai.Vale_Microsoft.Winget.Source_8wekyb3d8bbwe/vale.exe"
```

In PowerShell: `$env:VALE_BIN = "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\errata-ai.Vale_Microsoft.Winget.Source_8wekyb3d8bbwe\vale.exe"`.
The variable lives as long as that shell; a new terminal finds Vale on `PATH` and needs none.

Then sync Vale's `Microsoft` style: one download from GitHub into `readability/vale/styles/Microsoft/`,
which `.gitignore` keeps out of the repo. Without it Vale runs the `Docs` styles only, and
`check_texts.py` says so. From the repository root (`"$VALE_BIN"` in place of `vale` in a shell that
does not see it yet):

```bash
vale --config .claude/skills/openapi-desc-opt/readability/vale/.vale.ini sync
```

## 4. Prove they work

```bash
python .claude/skills/openapi-desc-opt/check_texts.py --check-tools
```

It runs every tool it finds on two fixed texts: one sentence of 26 words, which breaks the sentence
length, the sentence count and the average of `description-rules.md` §4, and three short sentences,
which break nothing. Each tool has to flag the first and pass the second. Every installed tool must
print `ok`, and the run must end with `TOOLS OK` (exit code 0).

A `FAILED` line means the tool runs but does not apply this skill's settings: another version, a
changed flag or output format, a ruleset it cannot load. That is worse than a missing tool. The queue
trusts a tool it finds, so a tool that reports nothing closes every form finding it never saw. Until
the line is fixed, run `scan.py` with `--own-checks`, and give the user the `FAILED` line with the
version the check printed.

## What changes once a tool is there

- `scan.py` prints `FORM: judged by Vale + vacuum` instead of `own checks`, and the counts of
  `off-form-description` and `heavy-description` can move by themselves. `--batch` records the judge
  per document and `--verify-batch` reports a change of it, so install between passes, never between
  step 3 and step 7 — or keep `--own-checks` on both.
- A found tool's verdict replaces the stand-in's for the checks it covers. Vale counts no words in
  backticks, so Vale without vacuum passes sentences that vacuum and the stand-in flag: install both.
  On the 737 published descriptions of 2026-11-10, vacuum and the stand-in agreed on every sentence
  length, average, sentence count and paragraph verdict; of the 712 descriptions with a sentence over
  20 words, Vale passed 2.
- Vale computes the grade with its own syllable count, and the queue takes the higher of Vale's and
  vacuum's. On the same 737 descriptions 554 came out over grade 10 with both tools, 520 with the
  stand-in alone, so `heavy-description` grows a little once the tools are in.
- `check_texts.py` adds a `Vale`, a `vacuum` and a `textstat` line per operation. Read them like the
  rest of its output: places to look, not a score.
