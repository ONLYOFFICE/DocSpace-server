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


namespace ASC.Api.Core.Core;

/// <summary>
/// Writes the type discriminator of a <see cref="JsonPolymorphicAttribute"/> base as an ordinary property of every
/// derived type it names, whatever type an answer declares.
/// </summary>
/// <remarks>
/// The serializer writes a type discriminator only where the declared type is the base: a list of
/// <c>FileEntryBaseDto</c> carried <c>entryKind</c>, a <c>FileDto&lt;int&gt;</c> returned on its own did not. The
/// generated SDKs read the discriminator to build the concrete type, and a strict one (Jackson in the Java SDK) turns an
/// object without it into nothing. So the value comes from the base's <see cref="JsonDerivedTypeAttribute"/> list and is
/// written by the derived type itself, and the base is left to serialize the runtime type without the discriminator of
/// its own: the serializer refuses a property that collides with that metadata.
/// </remarks>
public static class TypeDiscriminatorModifier
{
    private static readonly ConcurrentDictionary<Type, (string Name, string Value)?> _discriminators = new();

    public static void Modify(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        if (typeInfo.PolymorphismOptions is { } polymorphism && typeInfo.Type.IsDefined(typeof(JsonPolymorphicAttribute), false))
        {
            var derivedTypes = polymorphism.DerivedTypes.Select(d => new JsonDerivedType(d.DerivedType)).ToList();
            polymorphism.DerivedTypes.Clear();

            foreach (var derivedType in derivedTypes)
            {
                polymorphism.DerivedTypes.Add(derivedType);
            }
        }

        if (FindDiscriminator(typeInfo.Type) is not { } discriminator || typeInfo.Properties.Any(p => p.Name == discriminator.Name))
        {
            return;
        }

        var property = typeInfo.CreateJsonPropertyInfo(typeof(string), discriminator.Name);
        property.Get = _ => discriminator.Value;
        property.Order = int.MinValue;
        typeInfo.Properties.Add(property);
    }

    private static (string Name, string Value)? FindDiscriminator(Type type)
    {
        return _discriminators.GetOrAdd(type, static t =>
        {
            for (var baseType = t.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                var polymorphic = baseType.GetCustomAttribute<JsonPolymorphicAttribute>(false);

                if (polymorphic == null)
                {
                    continue;
                }

                var derived = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(false)
                    .FirstOrDefault(a => a.DerivedType == t && a.TypeDiscriminator is string);

                return derived == null
                    ? null
                    : (polymorphic.TypeDiscriminatorPropertyName ?? "$type", (string)derived.TypeDiscriminator);
            }

            return null;
        });
    }
}
