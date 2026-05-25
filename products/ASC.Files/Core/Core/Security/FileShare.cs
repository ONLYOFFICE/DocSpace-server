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

namespace ASC.Files.Core.Security;

/// <summary>
/// The access rights type.
/// </summary>
[EnumExtensions]
[JsonConverter(typeof(FileShareConverter))]
public enum FileShare
{
    [Description("None")]
    None,

    [Description("Read and write")]
    ReadWrite,

    [Description("Read")]
    Read,

    [Description("Restrict")]
    Restrict,

    [Description("Varies")]
    Varies,

    [Description("Review")]
    Review,

    [Description("Comment")]
    Comment,

    [Description("Fill forms")]
    FillForms,

    [Description("Custom filter")]
    CustomFilter,

    [Description("Room manager")]
    RoomManager,

    [Description("Editing")]
    Editing,

    [Description("Content creator")]
    ContentCreator
}

public class FileShareConverter : JsonConverter<FileShare>
{
    public override FileShare Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var result))
        {
            return (FileShare)result;
        }

        if (reader.TokenType == JsonTokenType.String && FileShareExtensions.TryParse(reader.GetString(), out var share))
        {
            return share;
        }

        return FileShare.None;
    }

    public override void Write(Utf8JsonWriter writer, FileShare value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
}

public static partial class FileShareExtensions
{
    public static string GetAccessString(
        FileShare fileShare,
        bool useRoomFormat = false,
        bool isAgent = false,
        CultureInfo cultureInfo = null)
    {
        if (isAgent && fileShare == FileShare.RoomManager)
        {
            return FilesCommonResource.AgentManager;
        }
        
        
        var prefix = useRoomFormat && fileShare != FileShare.ReadWrite ? "RoleEnum_" : "AceStatusEnum_";

        return fileShare switch
        {
            FileShare.Read or
            FileShare.ReadWrite or
            FileShare.CustomFilter or
            FileShare.Review or
            FileShare.FillForms or
            FileShare.Comment or
            FileShare.Restrict or
            FileShare.RoomManager or
            FileShare.Editing or
            FileShare.ContentCreator or
            FileShare.Varies or
            FileShare.None => FilesCommonResource.ResourceManager.GetString(prefix + fileShare.ToStringFast(), cultureInfo),
            _ => string.Empty
        };
    }
}