// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

#nullable enable
namespace ASC.ElasticSearch.VectorData;

internal sealed class OpenSearchFilterTranslator<T>(Inferrer inferrer)
{
    private ParameterExpression _recordParameter = null!;

    public QueryContainer Translate(Expression<Func<T, bool>> filter)
    {
        _recordParameter = filter.Parameters[0];

        return TranslateExpression(filter.Body);
    }

    private QueryContainer TranslateExpression(Expression node)
    {
        return node switch
        {
            BinaryExpression { NodeType: ExpressionType.Equal } equal => 
                TranslateEqual(equal.Left, equal.Right),
            BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso => 
                TranslateAndAlso(andAlso.Left, andAlso.Right),
            BinaryExpression { NodeType: ExpressionType.OrElse } orElse => 
                TranslateOrElse(orElse.Left, orElse.Right),
            _ => throw new NotSupportedException($"Unsupported expression: {node.NodeType}")
        };
    }

    private QueryContainer TranslateEqual(Expression left, Expression right)
    {
        if (TryGetPropertyInfo(left, out var propertyInfo) && TryGetValue(right, out var value))
        {
            var field = inferrer.Field(propertyInfo);
            return new MatchQuery { Field = field, Query = value?.ToString() };
        }

        if (TryGetPropertyInfo(right, out propertyInfo) && TryGetValue(left, out var value2))
        {
            var field = inferrer.Field(propertyInfo);
            return new MatchQuery { Field = field, Query = value2?.ToString() };
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

    private QueryContainer TranslateAndAlso(Expression left, Expression right)
    {
        return new BoolQuery
        {
            Must = [TranslateExpression(left), TranslateExpression(right)]
        };
    }

    private QueryContainer TranslateOrElse(Expression left, Expression right)
    {
        return new BoolQuery
        {
            Should = [TranslateExpression(left), TranslateExpression(right)]
        };
    }
}