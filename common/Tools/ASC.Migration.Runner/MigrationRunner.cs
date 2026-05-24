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

namespace Migration.Runner;

public class MigrationRunner
{
    private readonly DbContextActivator _dbContextActivator;

    public MigrationRunner(IServiceProvider serviceProvider)
    {
        _dbContextActivator = new DbContextActivator(serviceProvider);
    }

    public void RunApplyMigrations(ProviderInfo dbProvider, ConfigurationInfo configurationInfo, Type type, string targetMigration)
    {
        var migrationContext = _dbContextActivator.CreateInstance(type, dbProvider);
        Migrate(migrationContext, targetMigration);

        if (type.Name == typeof(MigrationContext).Name && configurationInfo == ConfigurationInfo.Standalone)
        {
            migrationContext = _dbContextActivator.CreateInstance(type, dbProvider, ConfigurationInfo.Standalone);
            Migrate(migrationContext, targetMigration);
        }
    }

    public string RunGenerateScript(ProviderInfo dbProvider, ConfigurationInfo configurationInfo, Type type, string targetMigration)
    {
        var migrationContext = _dbContextActivator.CreateInstance(type, dbProvider, skipConnection: true);
        var script = GenerateScript(migrationContext, targetMigration);

        if (type.Name == typeof(MigrationContext).Name && configurationInfo == ConfigurationInfo.Standalone)
        {
            migrationContext = _dbContextActivator.CreateInstance(type, dbProvider, ConfigurationInfo.Standalone, skipConnection: true);
            script += Environment.NewLine + GenerateScript(migrationContext, targetMigration);
        }

        return script;
    }

    private void Migrate(DbContext migrationContext, string targetMigration)
    {
        if (string.IsNullOrEmpty(targetMigration))
        {
            migrationContext.Database.Migrate();
        }
        else
        {
            var migrations = migrationContext.Database.GetMigrations();
            if (migrations.Contains(targetMigration))
            {
                Console.WriteLine("Migration to " + targetMigration);

                var migrator = migrationContext.Database.GetService<IMigrator>();
                migrator.Migrate(targetMigration);
            }
        }
    }

    private string GenerateScript(DbContext migrationContext, string targetMigration)
    {
        var migrator = migrationContext.Database.GetService<IMigrator>();
        return migrator.GenerateScript(fromMigration: null, toMigration: targetMigration);
    }
}