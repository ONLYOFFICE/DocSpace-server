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

namespace ASC.Api.Core.Routing;

[AttributeUsage(AttributeTargets.Class)]
public class ConstraintRouteAttribute(string constraint) : Attribute
{
    /// <summary>
    /// Whether the constraint also raises the routing precedence of the controller's endpoints.
    /// Two generic controllers (int and third-party) register exactly the same routes, and the
    /// precedence bump on the constrained one is what keeps their <em>parameterless</em> actions
    /// unambiguous. A constraint put on the other side must therefore leave precedence alone, or
    /// every shared parameterless route ends up matching both controllers.
    /// </summary>
    public bool AffectsOrder { get; init; } = true;

    /// <summary>
    /// The shape of a third-party entry id: a provider selector, a numeric provider id and an
    /// optional path (<c>sbox-42</c>, <c>drive-42-some/path</c>). Mirrors <c>Selectors.Pattern</c>
    /// in ASC.Files.Core, which cannot be referenced from here. Without this constraint a
    /// third-party controller's <c>{id}</c> route swallows every literal sibling route, so a wrong
    /// verb on such a literal answers 404 instead of 405.
    /// </summary>
    private const string ThirdPartyIdPattern = @"^.*-\d+(-.*)?$";

    public IRouteConstraint GetRouteConstraint()
    {
        return constraint switch
        {
            "int" => new IntRouteConstraint(),
            "thirdparty" => new RegexRouteConstraint(ThirdPartyIdPattern),
            _ => null
        };
    }
}