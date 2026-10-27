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
/// Writes the contract as block YAML: multi-paragraph texts as literal blocks, long sentences folded, everything
/// else plain unless YAML needs quotes (<c>$ref</c> values, the <c>"*"</c> trigger).
/// </summary>
internal static class ContractYamlWriter
{
    private const int LineWidth = 100;

    /// <summary>
    /// A text scalar in the style that reads best for its length. A trailing line break alone does not make a text
    /// multi-line: a long sentence that ends with one is folded (<c>&gt;</c>) and keeps it.
    /// </summary>
    public static YamlScalarNode Text(string value)
    {
        var style = value.TrimEnd('\n').Contains('\n')
            ? ScalarStyle.Literal
            : value.Length > LineWidth ? ScalarStyle.Folded : ScalarStyle.Any;

        return new YamlScalarNode(value) { Style = style };
    }

    public static string Write(YamlNode root, string comment)
    {
        using var writer = new StringWriter { NewLine = "\n" };

        foreach (var line in comment.Split('\n'))
        {
            writer.WriteLine("# " + line);
        }

        writer.WriteLine();

        var emitter = new Emitter(writer, new EmitterSettings(2, LineWidth, false, 1024, false, true, "\n"));

        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart(null, null, true));
        Emit(emitter, root);
        emitter.Emit(new DocumentEnd(true));
        emitter.Emit(new StreamEnd());

        return writer.ToString();
    }

    private static void Emit(IEmitter emitter, YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                emitter.Emit(new MappingStart(null, null, true, mapping.Children.Count == 0 ? MappingStyle.Flow : MappingStyle.Block));

                foreach (var (key, value) in mapping.Children)
                {
                    Emit(emitter, key);
                    Emit(emitter, value);
                }

                emitter.Emit(new MappingEnd());
                break;

            case YamlSequenceNode sequence:
                emitter.Emit(new SequenceStart(null, null, true, sequence.Children.Count == 0 ? SequenceStyle.Flow : SequenceStyle.Block));

                foreach (var item in sequence.Children)
                {
                    Emit(emitter, item);
                }

                emitter.Emit(new SequenceEnd());
                break;

            case YamlScalarNode scalar:
                emitter.Emit(new YamlDotNet.Core.Events.Scalar(null, null, scalar.Value ?? "", GetStyle(scalar), true, true));
                break;
        }
    }

    /// <summary>
    /// A block scalar (<c>|</c> or <c>&gt;</c>), as <see cref="Text"/> chooses it, keeps that style, a plain
    /// scalar (the <c>null</c> of the catch-all trigger, a boolean, a number) stays plain; everything else is quoted
    /// only when YAML needs it, unless it was made single-quoted on purpose (a status code, which YAML would read as
    /// a number).
    /// </summary>
    private static ScalarStyle GetStyle(YamlScalarNode scalar)
    {
        return scalar.Style switch
        {
            ScalarStyle.Literal or ScalarStyle.Folded => scalar.Style,
            ScalarStyle.Plain or ScalarStyle.SingleQuoted => scalar.Style,
            _ => ScalarStyle.Any
        };
    }
}
