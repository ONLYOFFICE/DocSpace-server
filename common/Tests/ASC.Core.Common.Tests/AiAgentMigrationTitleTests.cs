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


namespace ASC.Core.Common.Tests;

public class AiAgentMigrationTitleTests
{
    private const string Suffix = " (Knowledge)";

    [Fact]
    public void Compose_ShortTitle_AppendsTheFolderTitleInBrackets()
    {
        AiAgentMigrationTitle.Compose("Sales bot", "Knowledge").Should().Be("Sales bot (Knowledge)");
    }

    [Fact]
    public void Compose_DefaultLimit_IsTheLimitOfAFolderTitle()
    {
        var agentTitle = new string('a', 170);

        AiAgentMigrationTitle.Compose(agentTitle, "Knowledge").Should().HaveLength(170);
    }

    [Fact]
    public void Compose_ResultExactlyAtTheLimit_IsNotCut()
    {
        var agentTitle = new string('a', 30 - Suffix.Length);

        AiAgentMigrationTitle.Compose(agentTitle, "Knowledge", 30).Should().Be(agentTitle + Suffix);
    }

    [Fact]
    public void Compose_ResultOneOverTheLimit_CutsOneCharacterOfTheAgentTitle()
    {
        var agentTitle = new string('a', 30 - Suffix.Length) + "b";

        var title = AiAgentMigrationTitle.Compose(agentTitle, "Knowledge", 30);

        title.Should().Be(new string('a', 30 - Suffix.Length) + Suffix);
        title.Should().HaveLength(30);
    }

    [Fact]
    public void Compose_AgentTitleAtTheLimit_KeepsTheLengthAndTheSuffix()
    {
        var agentTitle = new string('a', 30);

        var title = AiAgentMigrationTitle.Compose(agentTitle, "Knowledge", 30);

        title.Should().HaveLength(30);
        title.Should().EndWith(Suffix);
    }

    [Fact]
    public void Compose_SuffixLongerThanTheLimit_CutsTheWholeTitleAtTheLimit()
    {
        var title = AiAgentMigrationTitle.Compose("Bot", new string('k', 40), 30);

        title.Should().Be("Bot (" + new string('k', 25));
    }
}
