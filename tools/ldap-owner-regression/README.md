# Existing-account LDAP rename regression

This standalone check compiles the actual `UserManager.UpdateUserInfoAsync`
method extracted from the source tree against bounded in-memory dependencies.
It needs Python 3 and the .NET 10 SDK, and has no external NuGet dependencies.
It never connects to LDAP or a running DocSpace/database.

From the repository root:

```sh
python3 tools/ldap-owner-regression/generate.py
dotnet run --project tools/ldap-owner-regression/Regression.csproj -c Release
```

Expected result with the fix: **13/13 passed**.

To reproduce against an unpatched revision:

```sh
python3 tools/ldap-owner-regression/generate.py --revision 7620686352c16f59d76a47e48c75969cb4a74c12
dotnet run --project tools/ldap-owner-regression/Regression.csproj -c Release
```

Expected result: **9/13 passed**, exit code 1. The existing-owner rename and
repeated-update cases fail with `User not found.`. The missing-ID and removed-user
checks also demonstrate why an update must resolve a persisted, nonremoved ID.

Regenerate without `--revision` afterwards to test the working tree again.
`Program.cs`, `bin/`, and `obj/` are generated and ignored.

Coverage includes LDAP rename, stable ID/email/resource references, repeated
update without inserting another account, unchanged-login updates, authorization
refusal, invitation authorization, owner-disable protection, missing/deleted IDs,
tenant isolation, duplicate-login rejection, system accounts, quota notification,
and reading persisted status when a cached user object was already modified.

The resource links, permissions, store, and quota helpers are test doubles.
These checks exercise the real source method, not the full `UserManager` service
graph. They do not replace integration tests with a database and LDAP server.
