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

/// <summary>
/// The answers that tell <c>ThirdPartyStorageQuota</c> a connected storage is out of space, built from the
/// providers' own SDK types and error bodies, next to the refusals that must not read as such. The stand
/// cannot mint the OAuth tokens of these providers, so the decisions are checked here; only WebDAV is
/// driven end to end (ThirdPartyStorageQuotaTests of the Files suite).
/// </summary>
public class ThirdPartyStorageQuotaDecisionTests
{
    // The bodies as the providers document them for an account out of space, and a refusal of another kind
    private const string GoogleQuotaBody =
        """{"error":{"errors":[{"domain":"usageLimits","reason":"storageQuotaExceeded","message":"The user's Drive storage quota has been exceeded."}],"code":403,"message":"The user's Drive storage quota has been exceeded."}}""";

    private const string GooglePermissionBody =
        """{"error":{"errors":[{"domain":"global","reason":"insufficientFilePermissions","message":"The user does not have sufficient permissions for this file."}],"code":403,"message":"The user does not have sufficient permissions for this file."}}""";

    private const string OneDriveQuotaBody =
        """{"error":{"code":"quotaLimitReached","message":"Insufficient Space Available"}}""";

    private const string OneDriveDeniedBody =
        """{"error":{"code":"accessDenied","message":"Access denied"}}""";

    private const string ProxyPage = "<html><body>Bad gateway</body></html>";

    public static TheoryData<object, bool> DropboxErrors => new()
    {
        { new UploadError.Path(new UploadWriteFailed(WriteError.InsufficientSpace.Instance, "session")), true },
        { new UploadError.Path(new UploadWriteFailed(WriteError.NoWritePermission.Instance, "session")), false },
        { new UploadSessionFinishError.Path(WriteError.InsufficientSpace.Instance), true },
        { new UploadSessionFinishError.Path(WriteError.NoWritePermission.Instance), false },
        { new CreateFolderError.Path(WriteError.InsufficientSpace.Instance), true },
        { new CreateFolderError.Path(WriteError.NoWritePermission.Instance), false },
        { RelocationError.InsufficientQuota.Instance, true },
        { new RelocationError.To(WriteError.InsufficientSpace.Instance), true },
        { new RelocationError.FromWrite(WriteError.InsufficientSpace.Instance), true },
        { new RelocationError.To(WriteError.NoWritePermission.Instance), false },
        { RelocationError.CantCopySharedFolder.Instance, false },
        { "path/insufficient_space/", false }
    };

    [Theory]
    [MemberData(nameof(DropboxErrors))]
    public void IsDropboxError_ReadsTheSpaceReasonOfEachWriteRoute(object errorResponse, bool outOfSpace)
    {
        ThirdPartyStorageQuota.IsDropboxError(errorResponse).Should().Be(outOfSpace);
    }

    [Theory]
    [InlineData("storageQuotaExceeded", true)]
    [InlineData("insufficientFilePermissions", false)]
    public void IsGoogleDrive_MatchesTheQuotaReasonOnly(string reason, bool outOfSpace)
    {
        var exception = new GoogleApiException("drive", "refused")
        {
            HttpStatusCode = HttpStatusCode.Forbidden,
            Error = new GoogleRequestError { Code = 403, Errors = [new GoogleSingleError { Domain = "usageLimits", Reason = reason }] }
        };

        ThirdPartyStorageQuota.IsGoogleDrive(exception).Should().Be(outOfSpace);
    }

    [Fact]
    public void IsGoogleDrive_ErrorWithoutDetails_IsNotOutOfSpace()
    {
        ThirdPartyStorageQuota.IsGoogleDrive(new GoogleApiException("drive", "refused") { HttpStatusCode = HttpStatusCode.Forbidden })
            .Should().BeFalse();
        ThirdPartyStorageQuota.IsGoogleDrive(new GoogleApiException("drive", "refused") { Error = new GoogleRequestError() })
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(GoogleQuotaBody, true)]
    [InlineData(GooglePermissionBody, false)]
    [InlineData("""{"error":{"errors":[null]}}""", false)]
    [InlineData(ProxyPage, false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsGoogleDriveResponse_ReadsTheReasonFromTheBody(string? body, bool outOfSpace)
    {
        ThirdPartyStorageQuota.IsGoogleDriveResponse(body).Should().Be(outOfSpace);
    }

    [Theory]
    [InlineData("quotaLimitReached", HttpStatusCode.BadRequest, true)]
    [InlineData("generalException", HttpStatusCode.InsufficientStorage, true)]
    [InlineData("accessDenied", HttpStatusCode.Forbidden, false)]
    [InlineData("generalException", (HttpStatusCode)0, false)]
    public void IsOneDrive_MatchesTheQuotaCodeOr507(string code, HttpStatusCode status, bool outOfSpace)
    {
        var exception = new GraphServiceException(new GraphError { Code = code, Message = "refused" }, null, status, null);

        ThirdPartyStorageQuota.IsOneDrive(exception).Should().Be(outOfSpace);
    }

    [Fact]
    public void IsOneDrive_QuotaCodeOfAnInnerError_IsOutOfSpace()
    {
        var error = new GraphError { Code = "generalException", InnerError = new GraphError { Code = "quotaLimitReached" } };

        ThirdPartyStorageQuota.IsOneDrive(new GraphServiceException(error, null)).Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.InsufficientStorage, "", true)]
    [InlineData(HttpStatusCode.BadRequest, OneDriveQuotaBody, true)]
    [InlineData(HttpStatusCode.Forbidden, OneDriveDeniedBody, false)]
    [InlineData(HttpStatusCode.BadGateway, ProxyPage, false)]
    public void IsOneDriveResponse_ReadsThe507OrTheCodeOfTheBody(HttpStatusCode status, string body, bool outOfSpace)
    {
        ThirdPartyStorageQuota.IsOneDriveResponse(status, body).Should().Be(outOfSpace);
    }

    [Theory]
    [InlineData("storage_limit_exceeded", true)]
    [InlineData("access_denied_insufficient_permissions", false)]
    [InlineData(null, false)]
    public void IsBox_MatchesTheStorageLimitCode(string? code, bool outOfSpace)
    {
        // BoxAPIException has no public constructor: the library builds it from the answer with this one
        var exception = (Exception)Activator.CreateInstance(
            typeof(BoxAPIException), BindingFlags.Instance | BindingFlags.NonPublic, null,
            ["refused", new BoxError { Code = code }, HttpStatusCode.InsufficientStorage, null], CultureInfo.InvariantCulture)!;

        ThirdPartyStorageQuota.IsBox(exception).Should().Be(outOfSpace);
    }

    [Theory]
    [InlineData(507, true)]
    [InlineData(403, false)]
    [InlineData(500, false)]
    public void IsWebDav_Matches507Only(int status, bool outOfSpace)
    {
        ThirdPartyStorageQuota.IsWebDav(status).Should().Be(outOfSpace);
    }

    [Fact]
    public void KeepReason_OutOfSpaceRefusalStaysOverLaterErrors()
    {
        var full = ThirdPartyStorageQuota.CreateException(new HttpRequestException("Insufficient Storage"));
        var later = new InvalidOperationException("a later item failed");

        ThirdPartyStorageQuota.KeepReason(full, later).Should().BeSameAs(full);
        ThirdPartyStorageQuota.KeepReason(later, full).Should().BeSameAs(full);
        ThirdPartyStorageQuota.KeepReason(new InvalidOperationException("first"), later).Should().BeSameAs(later);
        ThirdPartyStorageQuota.KeepReason(null, later).Should().BeSameAs(later);
    }

    // A part of a move or copy: the out-of-space refusal it kept, and the error of the item it is on now ("" or
    // null once a later item passed its checks)
    [Theory]
    [InlineData("full", "", null, null, "full")]
    [InlineData("full", "denied", null, null, "full")]
    [InlineData(null, "denied", "full", "", "full")]
    [InlineData("", "denied", "full", "locked", "full")]
    [InlineData(null, "denied", null, "locked", "denied")]
    [InlineData(null, "", null, "locked", "locked")]
    [InlineData("", "", "", "", null)]
    [InlineData(null, null, null, null, null)]
    public void OperationError_OutOfSpaceRefusalWinsOverLaterItemsAndTheOtherPart(
        string? thirdPartyStorageFull, string? thirdPartyError, string? portalStorageFull, string? portalError, string? expected)
    {
        ThirdPartyStorageQuota.OperationError(thirdPartyStorageFull, thirdPartyError, portalStorageFull, portalError).Should().Be(expected);
    }

    [Theory]
    [InlineData(100L, 101L, true)]
    [InlineData(100L, 100L, true)]
    [InlineData(101L, 100L, false)]
    [InlineData(1L, -5L, false)]
    [InlineData(null, 100L, true)]
    [InlineData(100L, null, true)]
    public void CopyFits_ComparesTheSizeWithTheRemainingSpace(long? size, long? remaining, bool fits)
    {
        ThirdPartyStorageQuota.CopyFits(size, remaining).Should().Be(fits);
    }
}
