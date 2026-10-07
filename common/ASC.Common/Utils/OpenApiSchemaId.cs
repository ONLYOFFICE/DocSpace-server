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

namespace ASC.Api.Core.Extensions;

/// <summary>
/// Names the component schema a CLR type is documented under, the same way in every service document.
/// </summary>
/// <remarks>
/// The name is the type name, with a few conventions on top. A type that carries
/// <see cref="OpenApiSchemaNameAttribute"/> is published under the name given there instead: the class name
/// is an implementation detail (a core settings class, an EF entity, a type called <c>Delete</c> or
/// <c>Status</c>), while the schema name is what an SDK user and an AI agent read to understand what a
/// payload is. A file, a folder and every payload that carries their identifiers is generic in the
/// identifier type, which is <c>int</c> for an entry the portal stores itself and <c>string</c> for an entry
/// on a connected third-party account. Appending the argument to the name gave <c>FileDtoInteger</c> and
/// <c>FileDtoString</c>, which tell a reader of the SDK nothing. So the portal's own shape takes the plain
/// name, <c>FileDto</c>, and the third-party shape spells the storage out in front of it,
/// <c>ThirdPartyFileDto</c>. A payload closed over <c>JsonElement</c> takes the plain name too: that argument
/// only says the identifier may be a number or a string, which the schema already states. A generic type
/// closed over anything else keeps its argument names appended (<c>ItemKeyValuePairBooleanString</c>).
/// The plural <c>Requests</c> of the legacy Web API request models (<c>CspRequestsDto</c>) is published in
/// the singular, like every other request body.
/// A name is only free while no other type claims it; should two types meet on one, Swashbuckle refuses the
/// duplicate schema id while the document is generated, so the clash cannot pass unnoticed.
/// </remarks>
public static class OpenApiSchemaId
{
    private const string ThirdPartyPrefix = "ThirdParty";

    public static string Of(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var explicitName = definition.GetCustomAttribute<OpenApiSchemaNameAttribute>(false)?.Name;
        var name = explicitName ?? type.Name;

        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (type.IsGenericType)
        {
            name = name.Split('`')[0];
            var arguments = type.GetGenericArguments();

            if (arguments.Length == 1 && (arguments[0] == typeof(int) || arguments[0] == typeof(JsonElement)))
            {
                // The portal's own shape, or an identifier that may be either: the plain name.
            }
            else if (arguments.Length == 1 && arguments[0] == typeof(string))
            {
                name = ThirdPartyPrefix + name;
            }
            else
            {
                name += string.Join("", arguments.Select(Of));
            }
        }

        if (explicitName is null)
        {
            if (name.EndsWith("RequestsDto", StringComparison.Ordinal))
            {
                name = name[..^"RequestsDto".Length] + "RequestDto";
            }
            else if (name.EndsWith("Requests", StringComparison.Ordinal))
            {
                name = name[..^"Requests".Length] + "Request";
            }
        }

        // Fix for nested classes
        name = name.Replace("+", "_");
        name = name.Replace("Int32", "Integer");
        return name;
    }
}

/// <summary>
/// Publishes a type in the OpenAPI documents under the given schema name instead of its class name.
/// </summary>
/// <remarks>
/// For a generic type, put it on the definition: the third-party prefix of <see cref="OpenApiSchemaId"/>
/// is still applied to the <c>string</c> closure. It renames the schema only; the JSON on the wire does not
/// change, so it is safe for existing clients and changes nothing but the generated SDK model name.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false)]
public sealed class OpenApiSchemaNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
