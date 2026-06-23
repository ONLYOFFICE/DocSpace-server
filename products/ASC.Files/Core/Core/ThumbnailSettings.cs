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

    public int MaxConcurrentProcessing
    {
        get => field != 0 ? field : 4;
        set;
    }

    public long ImageMagickMemoryLimit
    {
        get => field != 0 ? field : 256L * 1024L * 1024L;
        set;
    }

    public int ImageMagickThreadLimit
    {
        get => field != 0 ? field : 2;
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