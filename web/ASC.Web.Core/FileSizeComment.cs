// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Studio.Core;

[Scope]
public class FileSizeComment(SetupInfo setupInfo)
{
    public string FileImageSizeExceptionString
    {
        get { return GetFileSizeExceptionString(setupInfo.MaxImageUploadSize); }
    }

    public static string GetFileSizeExceptionString(long size)
    {
        return $"{Resource.FileSizeMaxExceed} ({FilesSizeToString(size)}).";
    }

    private static string GetRoomFreeSpaceExceptionString(long size)
    {
        return $"{Resource.RoomFreeSpaceException} ({FilesSizeToString(size)}).";
    }

    private static string GetUserFreeSpaceExceptionString(long size)
    {
        return $"{Resource.UserFreeSpaceException} ({FilesSizeToString(size)}).";
    }

    public static Exception GetFileSizeException(long size)
    {
        return new TenantQuotaException(GetFileSizeExceptionString(size));
    }

    public static Exception GetRoomFreeSpaceException(long size)
    {
        return new TenantQuotaException(GetRoomFreeSpaceExceptionString(size));
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
        var sizeNames = !string.IsNullOrEmpty(Resource.FileSizePostfix) ? 
            Resource.FileSizePostfix.Split(',', '،') : ["bytes", "KB", "MB", "GB", "TB"];
        var power = 0;

        double resultSize = size;
        if (1024 <= resultSize)
        {
            power = (int)Math.Log(resultSize, 1024);
            power = power < sizeNames.Length ? power : sizeNames.Length - 1;
            resultSize /= Math.Pow(1024d, power);
        }
        return $"{resultSize:#,0.##} {sizeNames[power]}";
    }
}
