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

namespace ASC.Webhooks.Contract;

/// <summary>
/// Reads the documentation of types and properties from the XML documentation files the compiler writes next to
/// each assembly: a type is described by its <c>&lt;summary&gt;</c> and <c>&lt;remarks&gt;</c>, a property by its
/// <c>&lt;summary&gt;</c>.
/// </summary>
internal sealed class XmlDocumentation
{
    private const string ParagraphBreak = "\u0001";

    private readonly Dictionary<string, XElement> _members = new(StringComparer.Ordinal);

    public XmlDocumentation(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            var path = Path.ChangeExtension(assembly.Location, ".xml");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"No XML documentation for {assembly.GetName().Name}: the project must set GenerateDocumentationFile.", path);
            }

            foreach (var member in XDocument.Load(path).Descendants("member"))
            {
                var name = member.Attribute("name")?.Value;

                if (name != null)
                {
                    _members[name] = member;
                }
            }
        }
    }

    /// <summary>
    /// The summary of <paramref name="type"/>, followed by its remarks as further paragraphs; null without a summary.
    /// </summary>
    public string GetDescription(Type type)
    {
        var member = Find("T:" + GetId(type));
        var summary = GetText(member?.Element("summary"));

        if (summary == null)
        {
            return null;
        }

        var remarks = GetText(member.Element("remarks"));

        return remarks == null ? summary : summary + "\n\n" + remarks;
    }

    /// <summary>
    /// The summary of the declaration of <paramref name="property"/>, following <c>&lt;inheritdoc/&gt;</c> to the
    /// overridden property.
    /// </summary>
    public string GetSummary(PropertyInfo property)
    {
        for (var current = property; current != null; current = GetOverridden(current))
        {
            var member = Find("P:" + GetId(current.DeclaringType) + "." + current.Name);

            if (member?.Element("inheritdoc") == null)
            {
                return GetText(member?.Element("summary"));
            }
        }

        return null;
    }

    private XElement Find(string id)
    {
        return _members.GetValueOrDefault(id);
    }

    private static PropertyInfo GetOverridden(PropertyInfo property)
    {
        var getter = property.GetMethod;

        if (getter == null || getter.GetBaseDefinition() == getter)
        {
            return null;
        }

        var baseType = getter.GetBaseDefinition().DeclaringType;

        return baseType?.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    private static string GetId(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        return definition.FullName.Replace('+', '.');
    }

    private static string GetText(XElement element)
    {
        if (element == null)
        {
            return null;
        }

        var builder = new StringBuilder();
        AppendNodes(builder, element);

        var paragraphs = builder.ToString()
            .Split(ParagraphBreak)
            .Select(p => string.Join(' ', p.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)))
            .Where(p => p.Length > 0);

        var text = string.Join("\n\n", paragraphs);

        return text.Length > 0 ? text : null;
    }

    private static void AppendNodes(StringBuilder builder, XElement element)
    {
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement { Name.LocalName: "para" } para:
                    builder.Append(ParagraphBreak);
                    AppendNodes(builder, para);
                    builder.Append(ParagraphBreak);
                    break;
                case XElement { Name.LocalName: "item" } item:
                    builder.Append(ParagraphBreak).Append("- ");
                    AppendNodes(builder, item);
                    builder.Append(ParagraphBreak);
                    break;
                case XElement { Name.LocalName: "c" } code:
                    builder.Append('`').Append(code.Value).Append('`');
                    break;
                case XElement { Name.LocalName: "see" or "seealso" } see:
                    builder.Append(GetReferenceName(see));
                    break;
                case XElement { Name.LocalName: "paramref" or "typeparamref" } reference:
                    builder.Append(reference.Attribute("name")?.Value);
                    break;
                case XElement other:
                    AppendNodes(builder, other);
                    break;
            }
        }
    }

    /// <summary>
    /// <c>T:ASC.Files.Core.FileEntry`1</c> reads as <c>FileEntry</c>: the receiver of a webhook knows nothing about
    /// the server's namespaces.
    /// </summary>
    private static string GetReferenceName(XElement see)
    {
        if (see.Attribute("langword")?.Value is { } langword)
        {
            return langword;
        }

        if (!see.IsEmpty)
        {
            return see.Value;
        }

        var cref = see.Attribute("cref")?.Value ?? "";
        var name = cref[(cref.IndexOf(':') + 1)..];
        var parenthesis = name.IndexOf('(');

        if (parenthesis >= 0)
        {
            name = name[..parenthesis];
        }

        name = name[(name.LastIndexOf('.') + 1)..];
        var arity = name.IndexOf('`');

        return arity >= 0 ? name[..arity] : name;
    }
}
