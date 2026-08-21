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

namespace ASC.Notify.Tests;

/// <summary>
/// That the stack the letter tests stand on actually holds: a portal is registered, the DocSpace
/// service graph resolves a notify action, and the action's real <c>Init</c> runs and produces tags.
///
/// It exists separately from the letter tests because it fails for entirely different reasons than a
/// letter does — a missing service registration, a connection string, an unreachable portal. When the
/// whole suite goes red, this is the test that says whether the problem is the stack or the letters.
/// </summary>
public class LetterStackSmokeTests
{
    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    [Fact]
    public async Task Portal_IsRegisteredAndAnswersOnThePublishedAddress()
    {
        var stack = await GetStackAsync();

        stack.Portal.TenantId.Should().BeGreaterThan(0);
        stack.Portal.Owner.Id.Should().NotBeEmpty();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        scope.Tenant.Id.Should().Be(stack.Portal.TenantId);

        // The owner registration created, not the one the migrations seed: that one has no email, which
        // is the whole reason a portal is registered at all.
        scope.Recipient.Email.Should().NotBeNullOrEmpty();
        scope.Recipient.FirstName.Should().NotBeNullOrEmpty();

        scope.PortalUrl.Should().Be(LetterEnvironment.PortalUrl);
    }

    [Fact]
    public async Task NotifyAction_ResolvesFromTheContainerAndItsInitFillsTags()
    {
        var stack = await GetStackAsync();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        // The heaviest Init of the lot: it needs the container (Autofac, for the shortener's consumer
        // factory), the tenant (for the confirmation link) and the database (the shortener stores the
        // link it hands back). If this resolves and runs, the cheaper letters will too.
        var action = scope.Services.GetRequiredService<SaasUserActivationV1NotifyAction>();

        await action.Init(scope.Recipient);

        action.Tags.Should().NotBeNull().And.NotBeEmpty();

        var tags = action.Tags.ToDictionary(tag => tag.Tag, tag => tag.Value?.ToString());

        tags.Should().ContainKey(CommonTags.Footer).WhoseValue.Should().Be("social");
        tags.Should().ContainKey(CommonTags.TopGif).WhoseValue.Should().Contain("join_docspace.gif");
        tags.Should().ContainKey("OrangeButton");

        TestContext.Current.TestOutputHelper?.WriteLine(
            string.Join(Environment.NewLine, tags.Select(tag => $"{tag.Key} = {tag.Value}")));
    }
}
