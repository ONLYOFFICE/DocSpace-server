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

namespace ASC.Web.Studio.Core;

[Scope]
public class FileSizeComment(SetupInfo setupInfo)
{
    public string FileImageSizeExceptionString => GetFileSizeExceptionString(setupInfo.MaxImageUploadSize);

    public static string GetFileSizeExceptionString(long size)
    {
        return $"{Resource.FileSizeMaxExceed} ({FilesSizeToString(size)}).";
    }

    private static string GetRoomFreeSpaceExceptionString(long size)
    {
        return $"{Resource.RoomFreeSpaceException} ({FilesSizeToString(size)}).";
    }
    private static string GetAiAgentFreeSpaceExceptionString(long size)
    {
        return $"{Resource.AiAgentFreeSpaceException} ({FilesSizeToString(size)}).";
    }

    private static string GetUserFreeSpaceExceptionString(long size)
    {
        return $"{Resource.UserFreeSpaceException} ({FilesSizeToString(size)}).";
    }

    public static Exception GetFileSizeException(long size)
    {
        return new TenantQuotaException(GetFileSizeExceptionString(size));
    }

    public static Exception GetRoomFreeSpaceException(long size, bool isAiAgetn = false)
    {
        return new TenantQuotaException(isAiAgetn ? GetAiAgentFreeSpaceExceptionString(size) : GetRoomFreeSpaceExceptionString(size));
    }

    public static Exception GetUserFreeSpaceException(long size)
    {
        return new TenantQuotaException(GetUserFreeSpaceExceptionString(size));
    }


    /// <summary>
    /// Generates a string the file size
    /// </summary>
    /// <param name="size">Size in bytes</param>
    /// <returns>10 b, 100 Kb, 25 Mb, 1 Gb</returns>
    public static string FilesSizeToString(long size)
    {
        return CommonFileSizeComment.FilesSizeToString(Resource.FileSizePostfix, size);
    }
}