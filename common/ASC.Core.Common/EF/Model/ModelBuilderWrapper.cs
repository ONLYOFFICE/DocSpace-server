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

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ASC.Core.Common.EF.Model;

public class ModelBuilderWrapper
{
    private ModelBuilder ModelBuilder { get; set; }
    private Provider Provider { get; set; }

    private ModelBuilderWrapper(ModelBuilder modelBuilder, Provider provider)
    {
        ModelBuilder = modelBuilder;
        Provider = provider;
    }

    public static ModelBuilderWrapper From(ModelBuilder modelBuilder, DatabaseFacade database)
    {
        var provider = Provider.MySql;

        if (database.IsMySql())
        {
            provider = Provider.MySql;
        }
        else if (database.IsNpgsql())
        {
            provider = Provider.PostgreSql;
        }

        return new ModelBuilderWrapper(modelBuilder, provider);
    }

    public static ModelBuilderWrapper From(ModelBuilder modelBuilder, Provider provider)
    {
        return new ModelBuilderWrapper(modelBuilder, provider);
    }

    public ModelBuilderWrapper Add(Action<ModelBuilder> action, Provider provider)
    {
        if (provider == Provider)
        {
            action(ModelBuilder);
        }

        return this;
    }

    public ModelBuilderWrapper HasData<T>(params T[] data) where T : class
    {
        ModelBuilder.Entity<T>().HasData(data);

        return this;
    }

    public EntityTypeBuilder<T> Entity<T>() where T : class
    {
        return ModelBuilder.Entity<T>();
    }

    public void AddDbFunctions()
    {


        switch (Provider)
        {
            case Provider.MySql:
                ModelBuilder
                    .HasDbFunction(typeof(DbFunctionsExtension).GetMethod(nameof(DbFunctionsExtension.SubstringIndex),
                        [typeof(string), typeof(char), typeof(int)])!)
                    .HasName("SUBSTRING_INDEX");
                ModelBuilder
                    .HasDbFunction(typeof(DbFunctionsExtension).GetMethod(nameof(DbFunctionsExtension.JsonExtract))!)
                    .HasTranslation(e =>
                    {
                        var res = new List<SqlExpression>();
                        if (e is List<SqlExpression> list)
                        {
                            if (list[0] is SqlConstantExpression key)
                            {
                                res.Add(new SqlFragmentExpression($"`{key.Value}`"));
                            }

                            if (list[1] is SqlConstantExpression val)
                            {
                                res.Add(new SqlConstantExpression($"$.{val.Value}", val.TypeMapping));
                            }
                        }

                        return new SqlFunctionExpression("JSON_EXTRACT", res, true, res.Select(_ => false), typeof(string), null);
                    });
                break;
            case Provider.PostgreSql:
                ModelBuilder
                    .HasDbFunction(typeof(DbFunctionsExtension).GetMethod(nameof(DbFunctionsExtension.SubstringIndex),
                        [typeof(string), typeof(char), typeof(int)])!)
                    .HasName("SPLIT_PART");
                ModelBuilder
                    .HasDbFunction(typeof(DbFunctionsExtension).GetMethod(nameof(DbFunctionsExtension.JsonExtract))!)
                    .HasTranslation(e =>
                    {
                        var res = new List<SqlExpression>();
                        if (e is List<SqlExpression> list)
                        {
                            if (list[0] is SqlConstantExpression key)
                            {
                                res.Add(new SqlFragmentExpression($"{key.Value}"));
                            }

                            if (list[1] is SqlConstantExpression val)
                            {
                                res.Add(new SqlConstantExpression($"{val.Value}", val.TypeMapping));
                            }
                        }

                        return new SqlFunctionExpression("jsonb_extract_path", res, true, res.Select(_ => false), typeof(string), null);
                    });
                break;
            default:
                throw new InvalidOperationException();
        }

        ModelBuilder.HasDbFunction(typeof(DbFunctionsExtension).GetMethod(nameof(DbFunctionsExtension.JsonValue))!)
            .HasTranslation(expressions =>
            {
                var result = new List<SqlExpression>();

                var jsonDoc = expressions[0];
                switch (jsonDoc)
                {
                    case SqlConstantExpression key:
                        result.Add(new SqlFragmentExpression($"`{key.Value}`"));
                        break;
                    case SqlFunctionExpression function:
                        result.Add(function);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                var path = expressions[1];
                if (path is SqlConstantExpression value)
                {
                    var strValue = value.Value?.ToString();

                    if (strValue != null && strValue.StartsWith('[') && strValue.EndsWith(']'))
                    {
                        result.Add(new SqlConstantExpression($"${strValue}", value.TypeMapping));
                    }
                    else
                    {
                        result.Add(new SqlConstantExpression($"$.{strValue}", value.TypeMapping));
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }

                return new SqlFunctionExpression("JSON_VALUE", result, true, result.Select(_ => false), typeof(string), null);
            });
    }
}