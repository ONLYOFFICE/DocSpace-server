﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Data.Backup.Tasks.Data;

internal static class DataRowInfoReader
{
    private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    public static IEnumerable<DataRowInfo> ReadFromStream(Stream stream)
    {
        var readerSettings = new XmlReaderSettings
        {
            CheckCharacters = false,
            CloseInput = false
        };

        using var xmlReader = XmlReader.Create(stream, readerSettings);
        xmlReader.MoveToContent();
        xmlReader.ReadToFollowing("schema", XmlSchemaNamespace);

        var schema = new Dictionary<string, string>();

        if (XNode.ReadFrom(xmlReader) is XElement schemaElement)
        {
            foreach (var entry in schemaElement.Descendants(XName.Get("sequence", XmlSchemaNamespace)).Single().Elements(XName.Get("element", XmlSchemaNamespace)))
            {
                schema.Add(entry.Attribute("name").ValueOrDefault(), entry.Attribute("type").ValueOrDefault());
            }
        }

        while (xmlReader.Read())
        {
            if (xmlReader.NodeType == XmlNodeType.Element && XNode.ReadFrom(xmlReader) is XElement el)
            {
                var dataRowInfo = new DataRowInfo(el.Name.LocalName);
                foreach (var column in schema)
                {
                    var value = ConvertToType(el.Element(column.Key).ValueOrDefault(), column.Value);
                    dataRowInfo.SetValue(column.Key, value);
                }

                yield return dataRowInfo;
            }
        }
    }

    private static object ConvertToType(string str, string schemaType)
    {
        if (str == null)
        {
            return null;
        }

        return schemaType switch
        {
            "xs:boolean" => Convert.ToBoolean(str),
            "xs:base64Binary" => Convert.FromBase64String(str),
            _ => str
        };
    }
}
