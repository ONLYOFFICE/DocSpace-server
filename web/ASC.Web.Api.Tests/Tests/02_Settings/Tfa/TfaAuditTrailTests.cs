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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Tfa;

/// <summary>
/// TFA actions are recorded in the audit trail / login history. Whether a given action actually
/// shows up here depends on how it reaches storage
/// (<c>MessagesRepository.IsForceSave</c>, <c>common/ASC.MessagingSystem/Data/MessagesRepository.cs</c>):
/// login events (<c>action &lt; 2000</c>) and a short explicit force-save list are written
/// synchronously by the service handling the request; every other audit action is published to
/// the event bus and persisted by Web.Studio's consumer, which this suite does not start, so it
/// never appears here.
///
/// Of the four TFA actions the TS suite checks, only <see cref="MessageAction.LoginSuccessViaApiTfa"/>
/// (1024) is a login event and therefore observable. <see cref="MessageAction.TwoFactorAuthenticationEnabledByTfaApp"/>
/// (6038), <see cref="MessageAction.TwoFactorAuthenticationDisabled"/> (6036) and
/// <see cref="MessageAction.UserDisconnectedTfaApp"/> (4033) are ordinary audit actions, not on
/// the force-save list — they are ported below but commented out, since asserting on them here
/// would either hang until the poll deadline or (worse) pass by accident once Web.Studio's
/// consumer is wired into this suite for an unrelated reason. Re-enable once that gap is closed.
/// </summary>
[Trait("Category", "Settings")]
public class TfaAuditTrailTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    /// <summary>
    /// Polls the login history for an event carrying the given action. Login history's
    /// <c>action</c> filter is confirmed loose (it can return other, unrelated recent events
    /// alongside matches), so this checks the returned list directly rather than trusting the
    /// filter to have narrowed it down — the same reasoning
    /// <c>ASC.Web.Api.Tests.Tests._05_Security.LoginHistory.LoginHistoryTestBase</c> applies one
    /// level up, duplicated here in miniature because this suite derives from
    /// <see cref="TfaTestBase"/> instead.
    /// </summary>
    private async Task<List<LoginEventDto>> PollLoginEventsByActionAsync(MessageAction action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var events = new List<LoginEventDto>();

        while (true)
        {
            events = (await _loginHistoryApi.GetLoginEventsByFilterAsync(
                action: action, cancellationToken: TestContext.Current.CancellationToken)).Response ?? [];

            if (events.Any(e => e.ActionId == action) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task LinkTfaApp_Owner_LogsLoginSuccessViaApiTfaEvent()
    {
        // Act
        await LinkOwnerTfaAppAsync();

        // Assert
        var events = await PollLoginEventsByActionAsync(MessageAction.LoginSuccessViaApiTfa);
        events.Should().Contain(e => e.ActionId == MessageAction.LoginSuccessViaApiTfa);
    }

    // TwoFactorAuthenticationEnabledByTfaApp (6038) is published to the event bus, not
    // force-saved — see the class summary. Web.Studio's consumer isn't running in this suite, so
    // the event never reaches the audit log here. Ported for when that gap is closed.
    /*
    [Fact]
    public async Task EnableTfaApp_Owner_LogsTwoFactorAuthenticationEnabledByTfaAppEvent()
    {
        // Act
        await LinkOwnerTfaAppAsync();

        // Assert
        var events = await PollAuditEventsByActionAsync(MessageAction.TwoFactorAuthenticationEnabledByTfaApp);
        events.Should().Contain(e => e.ActionId == MessageAction.TwoFactorAuthenticationEnabledByTfaApp);
    }
    */

    // TwoFactorAuthenticationDisabled (6036) is published to the event bus, not force-saved — see
    // the class summary. Ported for when that gap is closed.
    /*
    [Fact]
    public async Task DisableTfa_Owner_LogsTwoFactorAuthenticationDisabledEvent()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();
        var disable = await _tfaSettingsApi.UpdateTfaSettingsWithHttpInfoAsync(
            new TfaRequestsDto(TfaRequestsDtoType.None), TestContext.Current.CancellationToken);
        disable.StatusCode.Should().Be(HttpStatusCode.OK);

        // Disabling invalidates the session same as any TFA settings change — a plain re-login
        // works now, no TFA challenge since type is back to None.
        await _webApiClient.Authenticate(null);
        var relogin = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: Owner.Email, password: Owner.Password), TestContext.Current.CancellationToken);
        Owner.Token = relogin.Response.Token;
        await _webApiClient.Authenticate(Owner);

        // Act & Assert
        var events = await PollAuditEventsByActionAsync(MessageAction.TwoFactorAuthenticationDisabled);
        events.Should().Contain(e => e.ActionId == MessageAction.TwoFactorAuthenticationDisabled);
    }
    */

    // UserDisconnectedTfaApp (4033) is published to the event bus, not force-saved — see the
    // class summary. Ported for when that gap is closed.
    /*
    [Fact]
    public async Task UnlinkTfaApp_Owner_LogsUserDisconnectedTfaAppEvent()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();
        var target = await InviteContact(EmployeeType.User);
        await LinkTfaAppAsync(target);
        await _webApiClient.Authenticate(Owner);

        // Act
        var unlink = await _tfaSettingsApi.UnlinkTfaAppWithHttpInfoAsync(
            new TfaRequestsDto(id: target.Id), TestContext.Current.CancellationToken);
        unlink.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var events = await PollAuditEventsByActionAsync(MessageAction.UserDisconnectedTfaApp);
        events.Should().Contain(e => e.ActionId == MessageAction.UserDisconnectedTfaApp);
    }
    */
}
