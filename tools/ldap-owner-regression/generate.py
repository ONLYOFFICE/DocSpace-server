"""Compile the real UserManager update method against bounded in-memory dependencies.

No connections to DocSpace, LDAP, or production databases are made.
"""
import argparse
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("--revision", help="Read the method from this Git revision instead of the working tree")
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
source_path = "common/ASC.Core.Common/Context/Impl/UserManager.cs"
source = (
    subprocess.check_output(["git", "show", f"{args.revision}:{source_path}"], cwd=root, text=True)
    if args.revision else (root / source_path).read_text(encoding="utf-8-sig")
)
signature = "    public async Task<UserInfo> UpdateUserInfoAsync("
start = source.index(signature)
brace = source.index("{", start)
depth = 1
end = brace + 1
while depth:
    depth += (source[end] == "{") - (source[end] == "}")
    end += 1
method = source[start:end]
template = Path(__file__).with_name("Program.template").read_text()
Path(__file__).with_name("Program.cs").write_text(template.replace("/* ACTUAL_METHOD */", method))
print("Generated actual upstream method from: " + (args.revision or "working tree"))
