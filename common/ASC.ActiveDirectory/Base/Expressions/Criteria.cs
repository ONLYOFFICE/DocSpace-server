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

namespace ASC.ActiveDirectory.Base.Expressions;
/// <summary>
/// Criteria
/// </summary>
public class Criteria : ICloneable
{
    private readonly CriteriaType _type;
    private readonly List<Expression> _expressions = [];
    private readonly List<Criteria> _nestedCriteras = [];

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="type">Type of critera</param>
    /// <param name="expressions">Expressions</param>
    public Criteria(CriteriaType type, params Expression[] expressions)
    {
        _expressions.AddRange(expressions);
        _type = type;
    }

    /// <summary>
    /// Add nested expressions as And criteria
    /// </summary>
    /// <param name="expressions">Expressions</param>
    /// <returns>Self</returns>
    public Criteria And(params Expression[] expressions)
    {
        _nestedCriteras.Add(All(expressions));
        return this;
    }

    /// <summary>
    /// Add nested expressions as Or criteria
    /// </summary>
    ///  <param name="expressions">Expressions</param>
    /// <returns>Self</returns>
    public Criteria Or(params Expression[] expressions)
    {
        _nestedCriteras.Add(Any(expressions));
        return this;
    }

    /// <summary>
    /// Add nested Criteria
    /// </summary>
    /// <param name="nested"></param>
    /// <returns>Self</returns>
    public Criteria Add(Criteria nested)
    {
        _nestedCriteras.Add(nested);
        return this;
    }

    /// <summary>
    ///  Criteria as a string
    /// </summary>
    /// <returns>Criteria string</returns>
    public override string ToString()
    {
        var criteria = "({0}{1}{2})";
        var expressions = _expressions.Aggregate(string.Empty, (current, expr) => current + expr);
        var criterias = _nestedCriteras.Aggregate(string.Empty, (current, crit) => current + crit);
        return string.Format(criteria, _type == CriteriaType.And ? "&" : "|", expressions, criterias);
    }

    /// <summary>
    /// Group of Expression union as And
    /// </summary>
    /// <param name="expressions">Expressions</param>
    /// <returns>new Criteria</returns>
    public static Criteria All(params Expression[] expressions)
    {
        return new Criteria(CriteriaType.And, expressions);
    }

    /// <summary>
    /// Group of Expression union as Or
    /// </summary>
    /// <param name="expressions">Expressions</param>
    /// <returns>new Criteria</returns>
    public static Criteria Any(params Expression[] expressions)
    {
        return new Criteria(CriteriaType.Or, expressions);
    }

    #region ICloneable Members

    /// <summary>
    /// ICloneable implemetation
    /// </summary>
    /// <returns>Clone object</returns>
    public object Clone()
    {
        var cr = new Criteria(_type);
        foreach (var ex in _expressions)
        {
            cr._expressions.Add(ex.Clone() as Expression);
        }
        foreach (var nc in _nestedCriteras)
        {
            cr._nestedCriteras.Add(nc.Clone() as Criteria);
        }
        return cr;
    }

    #endregion
}