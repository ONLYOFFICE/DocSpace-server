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

namespace ASC.Files.Core.Core.AI;

public enum SkillFrontmatterError
{
    None,
    Missing,
    InvalidName,
    InvalidDescription
}

public sealed partial class SkillFrontmatter
{
    public const int MaxHeadLength = 64 * 1024;
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 1024;

    public const string Extension = ".md";

    private const string NameKey = "name";
    private const string DescriptionKey = "description";

    private static readonly byte[] _delimiter = [.. "---"u8];
    private static readonly byte[] _bom = [0xEF, 0xBB, 0xBF];
    private static readonly UTF8Encoding _strictUtf8 = new(false, true);
    private static readonly Regex _nameRegex = NameRegex();
    private static readonly Regex _xmlTagRegex = XmlTagRegex();
    private static readonly YamlIDeserializer _deserializer = new YamlDeserializerBuilder().WithDuplicateKeyChecking().Build();

    public required string Name { get; init; }
    public required string Description { get; init; }

    public static bool IsSkillFile(string title)
    {
        return FileUtility.GetFileExtension(title) == Extension;
    }

    public static SkillFrontmatterError TryParse(ReadOnlySpan<byte> head, out SkillFrontmatter frontmatter)
    {
        frontmatter = null;

        if (!TryExtractYaml(head, out var yaml))
        {
            return SkillFrontmatterError.Missing;
        }

        Dictionary<string, object> values;
        try
        {
            values = _deserializer.Deserialize<Dictionary<string, object>>(yaml);
        }
        catch (YamlException)
        {
            return SkillFrontmatterError.Missing;
        }

        if (values == null)
        {
            return SkillFrontmatterError.Missing;
        }

        var name = Normalize(values.GetValueOrDefault(NameKey));
        if (!IsValidName(name))
        {
            return SkillFrontmatterError.InvalidName;
        }

        var description = Normalize(values.GetValueOrDefault(DescriptionKey));
        if (!IsValidDescription(description))
        {
            return SkillFrontmatterError.InvalidDescription;
        }

        frontmatter = new SkillFrontmatter { Name = name, Description = description };

        return SkillFrontmatterError.None;
    }

    private static bool TryExtractYaml(ReadOnlySpan<byte> head, out string yaml)
    {
        yaml = null;

        if (head.StartsWith(_bom))
        {
            head = head[_bom.Length..];
        }

        if (!TryReadDelimiterLine(head, out var yamlStart))
        {
            return false;
        }

        var rest = head[yamlStart..];
        var offset = 0;
        var yamlLength = -1;

        while (offset < rest.Length)
        {
            var newLine = rest[offset..].IndexOf((byte)'\n');
            var lineLength = newLine < 0 ? rest.Length - offset : newLine;

            if (IsDelimiterLine(rest.Slice(offset, lineLength)))
            {
                yamlLength = offset;
                break;
            }

            if (newLine < 0)
            {
                break;
            }

            offset += newLine + 1;
        }

        if (yamlLength < 0)
        {
            return false;
        }

        try
        {
            yaml = _strictUtf8.GetString(rest[..yamlLength]);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        return true;
    }

    private static bool TryReadDelimiterLine(ReadOnlySpan<byte> head, out int nextLineStart)
    {
        nextLineStart = 0;

        if (!head.StartsWith(_delimiter))
        {
            return false;
        }

        var position = _delimiter.Length;

        if (position < head.Length && head[position] == '\r')
        {
            position++;
        }

        if (position >= head.Length || head[position] != '\n')
        {
            return false;
        }

        nextLineStart = position + 1;

        return true;
    }

    private static bool IsDelimiterLine(ReadOnlySpan<byte> line)
    {
        if (line.Length > 0 && line[^1] == '\r')
        {
            line = line[..^1];
        }

        return line.SequenceEqual(_delimiter);
    }

    private static string Normalize(object value)
    {
        return value is string text ? text.Trim() : null;
    }

    private static bool IsValidName(string name)
    {
        return !string.IsNullOrEmpty(name)
               && name.Length <= MaxNameLength
               && _nameRegex.IsMatch(name)
               && !_xmlTagRegex.IsMatch(name);
    }

    private static bool IsValidDescription(string description)
    {
        return !string.IsNullOrEmpty(description)
               && description.Length <= MaxDescriptionLength
               && !_xmlTagRegex.IsMatch(description);
    }

    [GeneratedRegex(@"^[\p{Ll}\p{Lo}\p{Nd}]+(-[\p{Ll}\p{Lo}\p{Nd}]+)*$", RegexOptions.Compiled)]
    private static partial Regex NameRegex();
    [GeneratedRegex(@"</?[A-Za-z][^>]*>", RegexOptions.Compiled)]
    private static partial Regex XmlTagRegex();
}
