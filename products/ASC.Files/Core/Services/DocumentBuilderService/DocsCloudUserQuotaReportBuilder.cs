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

namespace ASC.Files.Core.Services.DocumentBuilderService;

/// <summary>
/// DocsCloud user quota (editors and viewers with their expiration dates).
/// This report has a bespoke layout (two user tables + two summary blocks), so it uses its own
/// script and does not go through the single-table <c>RenderAsync</c> scaffolding.
/// </summary>
[Scope]
public class DocsCloudUserQuotaReportBuilder(
    TenantManager tenantManager,
    UserManager userManager,
    TenantUtil tenantUtil,
    TempPath tempPath,
    ReportHeaderService reportHeaderService,
    CoreSettings coreSettings,
    DocsCloudClient docsCloudClient,
    DisplayUserSettingsHelper displayUserSettingsHelper)
    : CustomerReportBuilderBase(tenantManager, userManager, tenantUtil, tempPath, reportHeaderService)
{
    private const string DocsCloudScriptName = "DocsCloudUserQuotaReport.docbuilder";

    protected override async Task<DocumentBuilderInputData> BuildCoreAsync(RenderContext context, CustomerOperationsReportTaskData taskData)
    {
        var header = context.Header;

        // The quota report is a point-in-time snapshot: it has no period, and the file name carries the
        // tenant-local generation date. The task data StartDate/EndDate are intentionally not used here.
        var reportDate = TenantUtil.DateTimeNow();

        var portalId = await coreSettings.GetKeyAsync(context.Tenant.Id);

        var quota = await docsCloudClient.GetTenantQuotaAsync(portalId);
        var tenantInfo = await docsCloudClient.GetTenantInfoAsync(portalId);

        var editLimit = tenantInfo?.UsersLimit?.Edit ?? 0;
        var viewLimit = tenantInfo?.UsersLimit?.View ?? 0;

        var (editors, editorsInternal, editorsExternal) = await BuildUsersAsync(quota?.Users);
        var (viewers, viewersInternal, viewersExternal) = await BuildUsersAsync(quota?.UsersView);

        var dateFormat = header.LongDateFormat;

        var userTypeHeader = Resource.DocsCloudQuotaReportColumnType;
        var expireHeader = Resource.DocsCloudQuotaReportColumnExpire;

        var inputData = new
        {
            resources = new
            {
                company = Resource.AccountingReportCompany + ":",
                report = Resource.AccountingReportTitle + ":",
                period = Resource.AccountingReportPeriod + ":",
                dateGenerated = Resource.AccountingReportDateGenerated + ":",
                sheetName = Resource.DocsCloudQuotaReportSheetName,
                dateGeneratedFormat = dateFormat,
                dateFormat,
                countFormat = CountFormat
            },
            info = new
            {
                company = header.Company,
                report = Resource.DocsCloudQuotaReportSheetName,
                dateGenerated = header.DateGenerated
            },
            logoSrc = header.LogoSrc,
            logoWidthMm = header.LogoWidthMm,
            logoHeightMm = header.LogoHeightMm,
            themeColors = new
            {
                mainBgColor = header.MainBgColor,
                lightBgColor = header.LightBgColor,
                mainFontColor = header.MainFontColor
            },
            editors = new
            {
                title = Resource.DocsCloudQuotaReportEditors,
                desc = Resource.DocsCloudQuotaReportEditorsDesc,
                headers = new[] { $"{Resource.DocsCloudQuotaReportEditors} — {Resource.DocsCloudQuotaReportColumnUserId}", userTypeHeader, expireHeader },
                users = editors,
                summary = BuildSummary(editors.Count, editorsInternal, editorsExternal, editLimit)
            },
            viewers = new
            {
                title = Resource.DocsCloudQuotaReportViewers,
                desc = Resource.DocsCloudQuotaReportViewersDesc,
                headers = new[] { $"{Resource.DocsCloudQuotaReportViewers} — {Resource.DocsCloudQuotaReportColumnUserId}", userTypeHeader, expireHeader },
                users = viewers,
                summary = BuildSummary(viewers.Count, viewersInternal, viewersExternal, viewLimit)
            }
        };

        var outputFileName = string.Format(Resource.DocsCloudQuotaReportName + ".xlsx", reportDate.ToShortDateString());

        return await WriteReportScriptAsync(context, DocsCloudScriptName, inputData, outputFileName);
    }

    private sealed record DocsCloudUserRow(string UserId, string UserType, string Expire);

    // Classifies each DocsCloud quota user: a Guid that resolves to a real DocSpace user is "Internal"
    // (shown by display name); anything else is "External" (shown by its raw identifier).
    private async Task<(List<DocsCloudUserRow> Users, int Internal, int External)> BuildUsersAsync(List<DocsCloudQuotaUser> source)
    {
        var users = new List<DocsCloudUserRow>();
        var internalCount = 0;
        var externalCount = 0;

        if (source == null)
        {
            return (users, internalCount, externalCount);
        }

        foreach (var user in source)
        {
            string display;
            string type;

            if (Guid.TryParse(user.UserId, out var guid) &&
                (await UserManager.GetUsersAsync(guid, returnLostUserIfRemoved: false)) is { } userInfo &&
                userInfo.Id != Constants.LostUser.Id)
            {
                display = displayUserSettingsHelper.GetFullUserName(userInfo, false);
                type = Resource.DocsCloudQuotaUserTypeInternal;
                internalCount++;
            }
            else
            {
                display = user.UserId;
                type = Resource.DocsCloudQuotaUserTypeExternal;
                externalCount++;
            }

            var expire = DateTime.TryParse(user.Expire, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expireDate)
                ? TenantUtil.DateTimeFromUtc(expireDate).ConvertNumerals("G")
                : user.Expire;

            users.Add(new DocsCloudUserRow(display, type, expire));
        }

        return (users, internalCount, externalCount);
    }

    private static object[] BuildSummary(int active, int internalCount, int externalCount, int limit)
    {
        return
        [
            new { label = Resource.DocsCloudQuotaReportActive, value = active },
            new { label = Resource.DocsCloudQuotaUserTypeInternal, value = internalCount },
            new { label = Resource.DocsCloudQuotaUserTypeExternal, value = externalCount },
            new { label = Resource.DocsCloudQuotaReportSubscriptionLimit, value = limit },
            new { label = Resource.DocsCloudQuotaReportRemaining, value = Math.Max(limit - active, 0) }
        ];
    }
}
