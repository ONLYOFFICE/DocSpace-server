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

#nullable enable
namespace ASC.ElasticSearch.VectorData;

internal sealed class OpenSearchFilterTranslator<T>(Inferrer inferrer)
{
    private ParameterExpression _recordParameter = null!;

    public QueryContainer Translate(Expression<Func<T, bool>> filter)
    {
        _recordParameter = filter.Parameters[0];

        return TranslateCore<QueryContainer>(
            filter.Body,
            createMatch: (field, value) => new MatchQuery { Field = field, Query = value?.ToString() },
            createAnd: (left, right) => new BoolQuery { Must = [left, right] },
            createOr: (left, right) => new BoolQuery { Should = [left, right] });
    }

    public JsonElement TranslateToJsonElement(Expression<Func<T, bool>> filter)
    {
        _recordParameter = filter.Parameters[0];

        return JsonSerializer.SerializeToElement(TranslateCore<object>(
            filter.Body,
            createMatch: (field, value) => new Dictionary<string, object?>
            {
                ["match"] = new Dictionary<string, object?>
                {
                    [field] = new Dictionary<string, object?> { ["query"] = value?.ToString() }
                }
            },
            createAnd: (left, right) => new Dictionary<string, object?>
            {
                ["bool"] = new Dictionary<string, object?> { ["must"] = new[] { left, right } }
            },
            createOr: (left, right) => new Dictionary<string, object?>
            {
                ["bool"] = new Dictionary<string, object?> { ["should"] = new[] { left, right } }
            }));
    }

    private TResult TranslateCore<TResult>(
        Expression node,
        Func<string, object?, TResult> createMatch,
        Func<TResult, TResult, TResult> createAnd,
        Func<TResult, TResult, TResult> createOr)
    {
        return node switch
        {
            BinaryExpression { NodeType: ExpressionType.Equal } equal =>
                TranslateCoreEqual(equal.Left, equal.Right, createMatch),
            BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso =>
                createAnd(
                    TranslateCore(andAlso.Left, createMatch, createAnd, createOr),
                    TranslateCore(andAlso.Right, createMatch, createAnd, createOr)),
            BinaryExpression { NodeType: ExpressionType.OrElse } orElse =>
                createOr(
                    TranslateCore(orElse.Left, createMatch, createAnd, createOr),
                    TranslateCore(orElse.Right, createMatch, createAnd, createOr)),
            _ => throw new NotSupportedException($"Unsupported expression: {node.NodeType}")
        };
    }

    private TResult TranslateCoreEqual<TResult>(
        Expression left, Expression right,
        Func<string, object?, TResult> createMatch)
    {
        if (TryGetPropertyInfo(left, out var propertyInfo) && TryGetValue(right, out var value))
        {
            return createMatch(inferrer.Field(propertyInfo), value);
        }

        if (TryGetPropertyInfo(right, out propertyInfo) && TryGetValue(left, out var value2))
        {
            return createMatch(inferrer.Field(propertyInfo), value2);
        }

        throw new NotSupportedException("Invalid equality expression");
    }

    private bool TryGetPropertyInfo(Expression expression, out PropertyInfo? propertyInfo)
    {
        if (expression is MemberExpression member &&
            member.Expression == _recordParameter &&
            member.Member is PropertyInfo prop)
        {
            propertyInfo = prop;
            return true;
        }

        propertyInfo = null;
        return false;
    }

    private static bool TryGetValue(Expression expr, out object? value)
    {
        while (true)
        {
            switch (expr)
            {
                case ConstantExpression c:
                    value = c.Value;
                    return true;

                case MemberExpression m:
                    switch (m.Expression)
                    {
                        case ConstantExpression closure:
                            {
                                var container = closure.Value;
                                switch (m.Member)
                                {
                                    case FieldInfo fi:
                                        value = fi.GetValue(container);
                                        return true;
                                    case PropertyInfo pi:
                                        value = pi.GetValue(container);
                                        return true;
                                }

                                break;
                            }
                        case null when m.Member is FieldInfo staticFi:
                            value = staticFi.GetValue(null);
                            return true;
                    }

                    break;

                case UnaryExpression { NodeType: ExpressionType.Convert } u:
                    expr = u.Operand;
                    continue;
            }

            value = null;
            return false;
        }
    }
}
