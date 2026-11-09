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


namespace ASC.Web.Api.Billing.Tests.Tests;

/// <summary>
/// Every action of the payment controller is either sent by the role checks of <see cref="PaymentPermissionsTestBase"/>,
/// as the roles it refuses and as the callers it lets through, or named here with the reason it is not. A new action,
/// or a changed route, fails here until it is one or the other - so no operation ends up with a role check no test sends.
/// </summary>
/// <remarks>
/// The actions are read from the controller in the ASC.Web.Api assembly, not from the OpenAPI document, which leaves out
/// the hidden ones (<c>GET payment/accounting/currencies</c>).
/// </remarks>
public class PaymentOperationsCoverageTests
{
    // The actions the role checks here do not send, each with the reason.
    private static readonly Dictionary<string, string> _checkedElsewhere = new()
    {
        ["GET payment/prices"] = RoleCheckedFirst,
        ["GET payment/currencies"] = RoleCheckedFirst,
        ["GET payment/quotas"] = RoleCheckedFirst,
        ["GET payment/walletservices"] = RoleCheckedFirst,
        ["GET payment/walletservice"] = RoleCheckedFirst,
        ["GET payment/quota"] = RoleCheckedFirst,
        ["POST payment/request"] = RoleCheckedFirst,
        ["GET payment/topupsettings"] = RoleCheckedFirst,
        ["GET payment/ai-prices"] = NeedsAiGateway,
        ["GET payment/ai-model/restrictions"] = NeedsAiGateway,
        ["PUT payment/ai-model/restrictions"] = NeedsAiGateway
    };

    private const string RoleCheckedFirst =
        "checks the role before it needs a payment service, so ASC.Web.Api.Tests sends it on the stack without them";

    private const string NeedsAiGateway = "answers or refuses by the AI gateway, which this stack does not run";

    [Fact]
    public void PaymentController_EveryAction_IsSentByTheRoleChecksOrNamedWithAReason()
    {
        var actions = typeof(PaymentController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(attribute => attribute.HttpMethods.Select(method => $"{method} payment/{Template(attribute.Template)}"))
            .ToList();

        actions.Should().OnlyHaveUniqueItems();
        actions.Should().BeEquivalentTo(PaymentPermissionsTestBase.RequestedOperations.Concat(_checkedElsewhere.Keys),
            "an action of the payment controller is either sent by the role checks or named here with the reason it is not");
    }

    [Fact]
    public void RoleChecks_EveryOperation_BelongsToOneGroup()
    {
        // A group is what a refusal class and a control class send; an operation outside every group is sent by neither.
        PaymentPermissionsTestBase.GroupedOperations.Should().OnlyHaveUniqueItems()
            .And.BeEquivalentTo(PaymentPermissionsTestBase.RequestedOperations);
    }

    // The route template without its constraints: "servicelimit/{id:int}" is the operation "servicelimit/{id}".
    private static string Template(string? template)
    {
        return Regex.Replace(template ?? string.Empty, @"\{(\w+):[^}]+\}", "{$1}");
    }
}
