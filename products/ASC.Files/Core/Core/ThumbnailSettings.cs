// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.ThumbnailBuilder;

[Singleton]
public class ThumbnailSettings
{
    public ThumbnailSettings(ConfigurationExtension configuration)
    {
        configuration.GetSetting("thumbnail", this);
    }

    #region worker settings

    public string ServerRoot
    {
        get => field ?? "http://localhost/";
        set;
    }

    #endregion


    #region data privider settings

    public string ConnectionStringName
    {
        get => field ?? "default";
        set;
    }

    public string Formats
    {
        get => field ?? ".dps|.dpt|.fodp|.odp|.otp|.pot|.potm|.potx|.pps|.ppsm|.ppsx|.ppt|.pptm|.pptx|.sxi|.csv|.et|.ett|.fods|.ods|.ots|.sxc|.xls|.xlsb|.xlsm|.xlsx|.xlt|.xltm|.xltx|.xml|.djvu|.doc|.docm|.docx|.docxf|.oform|.dot|.dotm|.dotx|.epub|.fb2|.fodt|.htm|.html|.mht|.mhtml|.odt|.ott|.oxps|.pdf|.rtf|.stw|.sxw|.txt|.wps|.wpt|.xml|.xps";
        set;
    }

    public string[] FormatsArray
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            field = (Formats ?? "").Split(['|', ','], StringSplitOptions.RemoveEmptyEntries);

            return field;
        }
    }

    public int SqlMaxResults
    {
        get => field != 0 ? field : 1000;
        set;
    }

    #endregion


    #region thumbnails generator settings

    public int MaxDegreeOfParallelism
    {
        get => field != 0 ? field : 1;
        set;
    }

    public long? MaxImageFileSize
    {
        get => field ?? 30L * 1024L * 1024L;
        set;
    }

    public long? MaxVideoFileSize
    {
        get => field ?? 1000L * 1024L * 1024L;
        set;
    }


    public int? AttemptsLimit
    {
        get => field ?? 3;
        set;
    }

    public int AttemptWaitInterval
    {
        get => field != 0 ? field : 1000;
        set;
    }

    public IEnumerable<ThumbnailSize> Sizes { get; set; }

    #endregion
}

public class ThumbnailSize
{
    public uint Height { get; set; }
    public uint Width { get; set; }
}