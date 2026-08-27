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
/// Shared helpers for the TFA App suites: enabling the portal-wide policy and completing the
/// TFA-aware login dance to link an account and obtain a fresh Bearer token for it. The dance
/// itself mirrors <c>AuthenticateWithCodeTests</c> one level up, which exercises the two
/// authentication calls directly; this base class wraps the same calls for suites that only care
/// about the TFA settings/codes/confirm endpoints reached once an account is already linked.
/// </summary>
public abstract class TfaTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Enables the TFA App requirement for the whole portal, as <paramref name="actor"/> (Owner by
    /// default, who is always a <see cref="EmployeeType.DocSpaceAdmin"/> on a fresh portal).
    /// </summary>
    protected async Task EnableTfaAppAsync(
        User? actor = null,
        List<Guid>? mandatoryUsers = null,
        List<Guid>? mandatoryGroups = null,
        List<string>? trustedIps = null)
    {
        await _webApiClient.Authenticate(actor ?? Owner);
        await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.App, trustedIps: trustedIps!, mandatoryUsers: mandatoryUsers!, mandatoryGroups: mandatoryGroups!),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Completes the TFA-aware login flow for <paramref name="user"/> — TFA App must already be
    /// required on the portal (<see cref="EnableTfaAppAsync"/>). The first login returns a setup
    /// secret instead of a token; a TOTP code computed from it both links the app and returns a
    /// real token, which this stores back onto <paramref name="user"/> and authenticates
    /// <c>_webApiClient</c> with. Returns the secret so callers can generate further valid codes
    /// (e.g. for the backup-codes flow).
    /// </summary>
    protected async Task<string> LinkTfaAppAsync(User user)
    {
        await _webApiClient.Authenticate(null);
        var login = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: user.Email, password: user.Password), TestContext.Current.CancellationToken);

        var secret = login.Response.TfaKey;
        secret.Should().NotBeNullOrEmpty("TFA App must already be required on the portal before linking");

        var code = ASC.Web.Api.Tests.Tests._03_Authentication.TotpGenerator.GenerateCurrent(secret);
        var result = await _authenticationApi.AuthenticateMeFromBodyWithCodeAsync(
            code,
            new AuthWithCodeRequestsDto { UserName = user.Email, Password = user.Password, Code = code },
            TestContext.Current.CancellationToken);

        user.Token = result.Response.Token;
        await _webApiClient.Authenticate(user);

        return secret;
    }

    /// <summary>
    /// Enables TFA App on the portal and links it for <see cref="BaseTest.Owner"/> in one step —
    /// the combination most suites here actually need, since Owner is normally the one turning the
    /// policy on before anyone (including themselves) can link an app.
    /// </summary>
    protected async Task<string> LinkOwnerTfaAppAsync()
    {
        await EnableTfaAppAsync();
        return await LinkTfaAppAsync(Owner);
    }
}
