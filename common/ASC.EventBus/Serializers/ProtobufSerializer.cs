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

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ASC.EventBus.Serializers;

public class ProtobufSerializer : IIntegrationEventSerializer
{
    private readonly HashSet<string> _processedProtoTypes;

    public ProtobufSerializer()
    {
        _processedProtoTypes = [];

        Array.ForEach(AppDomain.CurrentDomain.GetAssemblies(), BuildTypeModelFromAssembly);
    }

    private void BuildTypeModelFromAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null || !name.StartsWith("ASC."))
        {
            return;
        }

        var types = assembly.GetExportedTypes()
            .Where(t => t.GetCustomAttributes<ProtoContractAttribute>().Any());

        foreach (var type in types)
        {
            ProcessProtoType(type);
        }
    }


    /// <inheritdoc/>
    public byte[] Serialize<T>(T? item)
    {
        if (item == null)
        {
            return [];
        }

        using var ms = new MemoryStream();

        Serializer.Serialize(ms, item);

        return ms.ToArray();
    }

    /// <inheritdoc/>
    public T Deserialize<T>(byte[] serializedObject)
    {
        using var ms = new MemoryStream(serializedObject);

        return Serializer.Deserialize<T>(ms);
    }

    /// <inheritdoc/>
    public object Deserialize(byte[] serializedObject, Type returnType)
    {
        using var ms = new MemoryStream(serializedObject);

        return Serializer.Deserialize(returnType, ms);
    }

    private void ProcessProtoType(Type protoType)
    {
        if (protoType.FullName == null || _processedProtoTypes.Contains(protoType.FullName))
        {
            return;
        }

        if (protoType.BaseType == null || protoType.BaseType == typeof(object))
        {
            return;
        }

        var itemType = RuntimeTypeModel.Default[protoType];

        var baseType = RuntimeTypeModel.Default[protoType.BaseType];

        if (baseType.GetSubtypes().All(s => s.DerivedType != itemType))
        {
            var hashed = MD5.HashData(Encoding.UTF8.GetBytes(protoType.FullName));
            var hashedAsInt32 = BitConverter.ToInt32(hashed, 0);

            // restriction: https://developers.google.com/protocol-buffers/docs/proto3
            var fieldNumber = (int)Math.Abs(hashedAsInt32 % (Math.Pow(2, 29) - 1));

            baseType.AddSubType(fieldNumber, protoType);

            _processedProtoTypes.Add(protoType.FullName);
        }
    }
}
