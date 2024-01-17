// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Core;

/// <summary>
/// </summary>
[DebuggerDisplay("")]
public class EntryProperties
{
    /// <summary>Form filling properties</summary>
    /// <type>ASC.Files.Core.FormFillingProperties, ASC.Files.Core</type>
    public FormFillingProperties FormFilling { get; set; }

    public static EntryProperties Deserialize(string data, ILogger logger)
    {
        try
        {
            return JsonSerializer.Deserialize<EntryProperties>(data);
        }
        catch (Exception e)
        {
            logger.ErrorWithException("Error parse EntryProperties: " + data, e);
            return null;
        }
    }

    public static string Serialize(EntryProperties entryProperties, ILogger logger)
    {
        try
        {
            return JsonSerializer.Serialize(entryProperties);
        }
        catch (Exception e)
        {
            logger.ErrorWithException("Error serialize EntryProperties", e);
            return null;
        }
    }
}

/// <summary>
/// </summary>
[Transient]
public class FormFillingProperties()
{
    public static readonly string DefaultTitleMask = "{0} - {1} ({2})";

    /// <summary>Specifies whether to collect the data from the filled forms or not</summary>
    /// <type>System.Boolean, System</type>
    public bool CollectFillForm { get; set; }

    /// <summary>Folder ID where a file will be saved</summary>
    /// <type>System.String, System</type>
    public string Title { get; set; }
    public string ToFolderId { get; set; }
    public string ResultsFolderId { get; set; }
    public string ResultsFileID { get; set; }

}