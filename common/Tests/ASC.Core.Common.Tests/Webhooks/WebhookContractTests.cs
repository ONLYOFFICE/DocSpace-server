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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// The webhook SDKs are generated from the committed docspace-webhooks.yaml, and that file is generated from the
/// payload DTOs, <c>WebhookTrigger</c> and <c>WebhookDelivery</c>. Nothing else notices when one of them changes and
/// the contract is not regenerated: every other test stays green while the YAML and the SDKs describe the old format.
/// </summary>
[Trait("Category", "Webhooks")]
public class WebhookContractTests
{
    [Fact]
    public void CommittedContract_MatchesTheCode()
    {
        var directory = WebhookContractGenerator.FindContractDirectory(AppContext.BaseDirectory);
        var contractPath = Path.Combine(directory, WebhookContractGenerator.ContractFileName);

        // the generator writes LF; a checkout with core.autocrlf turns the committed file into CRLF
        var committed = File.ReadAllText(contractPath).ReplaceLineEndings("\n");

        var generated = WebhookContractGenerator.Generate();

        committed.Should().Be(generated,
            "{0} must be regenerated from the code: run `dotnet run --project common/Tools/ASC.Webhooks.Contract` from the " +
            "server repository root, commit the result and regenerate the webhook SDKs from it", contractPath);
    }

    [Fact]
    public void EmbeddedContract_IsTheCommittedOne()
    {
        var directory = WebhookContractGenerator.FindContractDirectory(AppContext.BaseDirectory);
        var committed = File.ReadAllText(Path.Combine(directory, WebhookContractGenerator.ContractFileName)).ReplaceLineEndings("\n");

        using var reader = new StreamReader(WebhookContract.OpenRead());
        var embedded = reader.ReadToEnd().ReplaceLineEndings("\n");

        embedded.Should().Be(committed,
            "Web.Studio serves the contract built into ASC.Webhooks.Core at {0}, so the build must embed the file itself",
            WebhookContract.Route);
    }

    /// <summary>
    /// The SDK docs and every tool that builds a sample body read the property's <c>example</c>; without one a
    /// receiver guesses the format of an ID or a timestamp. Checked on the generated document rather than the
    /// committed one, so it holds for every DTO the generator reaches, envelope included.
    /// </summary>
    [Fact]
    public void GeneratedContract_GivesEveryScalarPropertyAnExample()
    {
        var stream = new YamlDotNet.RepresentationModel.YamlStream();
        stream.Load(new StringReader(WebhookContractGenerator.Generate()));

        var root = (YamlDotNet.RepresentationModel.YamlMappingNode)stream.Documents[0].RootNode;
        var schemas = (YamlDotNet.RepresentationModel.YamlMappingNode)root["components"]["schemas"];

        string[] scalarTypes = ["string", "integer", "number", "boolean"];
        var missing = new List<string>();
        var checkedCount = 0;

        foreach (var (schemaName, schemaNode) in schemas.Children)
        {
            var schema = (YamlDotNet.RepresentationModel.YamlMappingNode)schemaNode;

            // a derived payload declares its own properties in the second part of its allOf
            var parts = schema.Children.TryGetValue("allOf", out var allOf)
                ? ((YamlDotNet.RepresentationModel.YamlSequenceNode)allOf).Children.Append(schema)
                : Enumerable.Repeat<YamlDotNet.RepresentationModel.YamlNode>(schema, 1);

            foreach (var part in parts.OfType<YamlDotNet.RepresentationModel.YamlMappingNode>())
            {
                if (!part.Children.TryGetValue("properties", out var properties))
                {
                    continue;
                }

                foreach (var (propertyName, propertyNode) in ((YamlDotNet.RepresentationModel.YamlMappingNode)properties).Children)
                {
                    var property = (YamlDotNet.RepresentationModel.YamlMappingNode)propertyNode;

                    if (!property.Children.TryGetValue("type", out var type) ||
                        !scalarTypes.Contains(((YamlDotNet.RepresentationModel.YamlScalarNode)type).Value))
                    {
                        continue;
                    }

                    checkedCount++;

                    if (!property.Children.ContainsKey("example"))
                    {
                        missing.Add($"{schemaName}.{propertyName}");
                    }
                }
            }
        }

        checkedCount.Should().BePositive("the contract publishes scalar properties, so the walk must reach them");
        missing.Should().BeEmpty("every scalar property of a webhook DTO needs an <example> in its doc comment");
    }
}
