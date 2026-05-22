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

namespace ASC.Migrations;

public class MigrationGenerator
{
    private readonly DbContext _dbContext;
    private readonly string _providerInfoProjectPath;
    private readonly string _typeName;
    private readonly Regex _pattern = new(@"\d+$", RegexOptions.Compiled);
    private readonly string _providerName;

    private string ContextFolderName => _typeName;

    public MigrationGenerator(DbContext context, Provider provider, string providerInfoProjectPath)
    {
        _dbContext = context;
        _providerInfoProjectPath = providerInfoProjectPath;
        _typeName = _dbContext.GetType().Name;
        _providerName = provider.ToString();
    }

    public void Generate()
    {
        var scaffolder = EFCoreDesignTimeServices.GetServiceProvider(_dbContext)
            .GetService<IMigrationsScaffolder>();

        var name = GenerateMigrationName();

        var migration = scaffolder.ScaffoldMigration(name, $"ASC.Migrations.{_providerName}.SaaS", "Migrations");

        SaveMigration(migration);
    }

    private void SaveMigration(ScaffoldedMigration migration)
    {
        var path = Path.Combine(_providerInfoProjectPath, ContextFolderName);

        Directory.CreateDirectory(path);

        var migrationPath = Path.Combine(path, $"{migration.MigrationId}{migration.FileExtension}");
        var designerPath = Path.Combine(path, $"{migration.MigrationId}.Designer{migration.FileExtension}");
        var snapshotPath = Path.Combine(path, $"{migration.SnapshotName}{migration.FileExtension}");

        File.WriteAllText(migrationPath, migration.MigrationCode);
        File.WriteAllText(designerPath, migration.MetadataCode);
        File.WriteAllText(snapshotPath, migration.SnapshotCode);
    }

    private string GetLastMigrationName()
    {
        var scaffolderDependecies = EFCoreDesignTimeServices.GetServiceProvider(_dbContext)
            .GetService<MigrationsScaffolderDependencies>();

        var lastMigration = scaffolderDependecies.MigrationsAssembly.Migrations.LastOrDefault();

        return lastMigration.Key;
    }

    private string GenerateMigrationName()
    {
        var last = GetLastMigrationName();

        if (string.IsNullOrEmpty(last))
        {
            return ContextFolderName + "Migrate";
        }

        var migrationNumber = _pattern.Match(last).Value;

        if (string.IsNullOrEmpty(migrationNumber))
        {
            return ContextFolderName + "_Upgrade1";
        }

        return ContextFolderName + "_Upgrade" + (int.Parse(migrationNumber) + 1);
    }
}
