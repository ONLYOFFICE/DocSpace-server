// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Core.Common.Tests;

/// <summary>
/// The numbers of the enums the database stores. A stored number keeps its meaning only while the member keeps it: a
/// value moved during a merge (<c>MessageAction.WebhookRetried</c> went from 6112 to 6117 in the merge commit that
/// brought in a hotfix whose service-limit actions took 6112-6116) turns every row already written into another event.
/// The snapshot lists every member of every stored enum, sorted by value, so two branches that take the same number
/// collide in it when they are merged, and a moved, reused or removed number fails here.
/// New members need no hand edit: when they are the only difference, the test writes them into the snapshot and fails
/// once, so that the file is committed with the change that added them. A moved or removed number is never written:
/// that one is a decision.
/// </summary>
/// <remarks>
/// The stored enums are found, not listed: every enum-typed property of an entity that some <see cref="DbContext"/>
/// maps through a <see cref="DbSet{TEntity}"/>, of every entity in the model of
/// <see cref="ASC.Migrations.Core.MigrationContext"/>, plus the enums the database holds as a plain number
/// (<see cref="_storedAsNumbers"/>). The contexts are searched in the ASC assemblies reachable from this one and from
/// <c>MigrationContext</c>. The migrations are generated from that context's model, so it holds every table of the
/// schema: those whose runtime context lives in a project this one does not reference (<c>BackupsContext</c> in
/// <c>ASC.Data.Backup.Core</c>, <c>LegacyAiDbContext</c> in the AI service), and those mapped only through its
/// <c>OnModelCreating</c> chain, with no <see cref="DbSet{TEntity}"/>. A new enum column needs a migration, so it
/// enters the check on its own. A database of its own with no migrations here (the resources database) stays outside.
/// </remarks>
public class PersistedEnumValuesTests
{
    private const string SnapshotPath = "common/Tests/ASC.Core.Common.Tests/Snapshots/PersistedEnums.txt";

    private const string SnapshotHeader =
        "# Every member of every enum the database stores, as Enum.Member=Value, sorted by enum and value.\n" +
        "# Checked by PersistedEnumValuesTests, which writes the lines of new members itself: commit them with the change.\n" +
        "# A changed or removed line is a decision: rows already stored with that number change their meaning.\n";

    /// <summary>
    /// Enums the database holds as a plain number column, so no enum-typed entity property leads to them.
    /// </summary>
    private static readonly Type[] _storedAsNumbers = [typeof(ASC.MessagingSystem.Core.MessageAction)];

    /// <summary>
    /// Members that share a value with another member of their enum on purpose. Any other shared value is a collision.
    /// </summary>
    private static readonly HashSet<string> _intendedAliases = [];

    [Fact]
    public void Discovery_FindsTheEnumsKnownToBeStored()
    {
        StoredEnums().Should().Contain(
        [
            typeof(ASC.MessagingSystem.Core.MessageAction),
            typeof(ASC.Files.Core.FolderType),
            typeof(ASC.Files.Core.Security.FileShare),
            typeof(ASC.Core.Tenants.TenantStatus),
            typeof(ASC.Core.Users.EmployeeStatus),
            // Both are mapped at runtime only by contexts in projects this one does not reference. ProviderType is what
            // proves that MigrationContext is searched: naming BackupStorageType here brings its assembly in by itself.
            typeof(ASC.Data.Backup.Contracts.BackupStorageType),
            typeof(ASC.Core.Common.EF.Model.Ai.Legacy.ProviderType)
        ]);
    }

    [Fact]
    public void StoredEnums_MatchTheSnapshot()
    {
        var actual = SnapshotLines();
        var snapshot = ReadSnapshot();

        if (actual.SequenceEqual(snapshot))
        {
            return;
        }

        var content = SnapshotHeader + string.Join('\n', actual) + "\n";
        var now = ToValues(actual);
        var before = ToValues(snapshot);

        var changed = before.Where(e => now.TryGetValue(e.Key, out var value) && value != e.Value)
            .Select(e => $"{e.Key}: {e.Value} -> {now[e.Key]}").ToList();
        var removed = before.Keys.Where(k => !now.ContainsKey(k)).Select(k => $"{k}={before[k]}").ToList();
        var added = now.Keys.Where(k => !before.ContainsKey(k)).Select(k => $"{k}={now[k]}").ToList();

        if (changed.Count == 0 && removed.Count == 0)
        {
            // Only new members, or the lines out of their sorted order after a merge: nothing stored before changes its
            // meaning, so the snapshot is brought up to date here instead of by hand.
            Assert.Fail(ExtendSnapshot(content, added));
        }

        var received = Path.Combine(AppContext.BaseDirectory, "PersistedEnums.received.txt");
        File.WriteAllText(received, content);

        var message = new StringBuilder($"The stored enum values differ from {SnapshotPath}.\n");

        Append(message, "Changed: rows stored with the old number now read as another member. Put the old value back, or decide it and update the snapshot", changed);
        Append(message, "Removed: rows stored with this number lose their name", removed);
        Append(message, "Added: written to the snapshot once the changes above are settled", added);

        message.Append($"The whole expected snapshot is in {received}.");

        Assert.Fail(message.ToString());
    }

    /// <summary>
    /// Writes the snapshot when the only difference is new members, and says what to commit. The test still fails, so
    /// that a run never passes while the committed file lacks the lines: CI stays red until they are committed.
    /// </summary>
    private static string ExtendSnapshot(string content, List<string> added)
    {
        var path = Path.Combine(FindRepositoryRoot(), SnapshotPath);

        try
        {
            File.WriteAllText(path, content);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var received = Path.Combine(AppContext.BaseDirectory, "PersistedEnums.received.txt");
            File.WriteAllText(received, content);

            return $"{SnapshotPath} could not be written ({exception.Message}). The expected snapshot is in {received}: copy it over and commit it.";
        }

        var message = new StringBuilder($"{SnapshotPath} was out of date and has been rewritten: commit it. ");

        message.Append(added.Count > 0
            ? "Only new members were found, so nothing stored before changes its meaning; the next run passes.\n"
            : "Its lines were out of their sorted order, the values are unchanged; the next run passes.\n");

        Append(message, "Added", added);

        return message.ToString();
    }

    [Fact]
    public void StoredEnums_GiveEveryValueOneName()
    {
        var collisions = StoredEnums()
            .SelectMany(type => Members(type).Select(m => (Type: type, m.Name, m.Value)))
            .GroupBy(m => (m.Type, m.Value))
            .Where(g => g.Count(m => !_intendedAliases.Contains($"{g.Key.Type.FullName}.{m.Name}")) > 1)
            .Select(g => $"{g.Key.Type.FullName}={g.Key.Value}: {string.Join(", ", g.Select(m => m.Name))}")
            .ToList();

        collisions.Should().BeEmpty(
            "two members with one number cannot be told apart once stored; give the new one a free number, " +
            "or list a deliberate alias in _intendedAliases");
    }

    private static List<string> SnapshotLines()
    {
        return
        [
            .. StoredEnums().SelectMany(type => Members(type)
                .OrderBy(m => m.Value)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .Select(m => $"{type.FullName}.{m.Name}={m.Value.ToString(CultureInfo.InvariantCulture)}"))
        ];
    }

    private static List<string> ReadSnapshot()
    {
        var path = Path.Combine(FindRepositoryRoot(), SnapshotPath);

        return File.Exists(path)
            ? [.. File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#'))]
            : [];
    }

    private static Dictionary<string, string> ToValues(IEnumerable<string> lines)
    {
        return lines
            .Select(l => l.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0], StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First()[1], StringComparer.Ordinal);
    }

    private static void Append(StringBuilder message, string title, List<string> list)
    {
        if (list.Count == 0)
        {
            return;
        }

        message.Append($"{title} ({list.Count}):\n");

        foreach (var item in list.Take(30))
        {
            message.Append($"  {item}\n");
        }

        if (list.Count > 30)
        {
            message.Append($"  ... and {list.Count - 30} more\n");
        }
    }

    private static IEnumerable<(string Name, decimal Value)> Members(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (f.Name, Convert.ToDecimal(f.GetRawConstantValue(), CultureInfo.InvariantCulture)));
    }

    private static List<Type> StoredEnums()
    {
        var enums = new HashSet<Type>(_storedAsNumbers);

        var contexts = AscAssemblies()
            .SelectMany(TypesOf)
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(DbContext).IsAssignableFrom(t));

        var entities = contexts
            .SelectMany(context => context.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0]))
            .Concat(MigrationModelEntities())
            .Distinct();

        foreach (var entity in entities)
        {
            foreach (var property in entity.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (type.IsEnum && type.FullName?.StartsWith("ASC.", StringComparison.Ordinal) == true)
                {
                    enums.Add(type);
                }
            }
        }

        return [.. enums.OrderBy(t => t.FullName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The entity types of the model the migrations are generated from, built the way <c>BackupCoverageTests</c> builds
    /// it: the provider is needed, a connection is never opened. Unlike the <see cref="DbSet{TEntity}"/> properties, the
    /// model also holds an entity mapped only through the <c>OnModelCreating</c> chain. Shared-type entities (property
    /// bags) have no class of their own to read properties from.
    /// </summary>
    private static List<Type> MigrationModelEntities()
    {
        var options = new DbContextOptionsBuilder<ASC.Migrations.Core.MigrationContext>()
            .UseMySql(
                "Server=localhost;Database=docspace",
                ServerVersion.Parse("9.2.0"),
                providerOptions => providerOptions.UseMicrosoftJson())
            .Options;

        using var context = new ASC.Migrations.Core.MigrationContext(options);

        return [.. context.Model.GetEntityTypes().Where(e => !e.HasSharedClrType).Select(e => e.ClrType)];
    }

    /// <summary>
    /// The ASC assemblies reachable through their references from this test assembly and from the one that holds
    /// <see cref="ASC.Migrations.Core.MigrationContext"/>. The compiler keeps a reference only to an assembly whose
    /// types the code uses, so a project reference alone would not bring that assembly in: it is named here.
    /// </summary>
    private static List<Assembly> AscAssemblies()
    {
        var roots = new[]
        {
            typeof(PersistedEnumValuesTests).Assembly,
            typeof(ASC.Migrations.Core.MigrationContext).Assembly
        };
        var found = roots.ToDictionary(a => a.GetName().Name!, StringComparer.Ordinal);
        var queue = new Queue<Assembly>(roots);

        while (queue.TryDequeue(out var assembly))
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is { } name && name.StartsWith("ASC.", StringComparison.Ordinal) && !found.ContainsKey(name))
                {
                    var loaded = Assembly.Load(reference);
                    found[name] = loaded;
                    queue.Enqueue(loaded);
                }
            }
        }

        return [.. found.Values];
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            // A type whose dependency is not deployed with the tests cannot be a stored enum's context here.
            return exception.Types.OfType<Type>();
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ASC.Tests.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"ASC.Tests.slnx not found above {AppContext.BaseDirectory}.");
    }
}
