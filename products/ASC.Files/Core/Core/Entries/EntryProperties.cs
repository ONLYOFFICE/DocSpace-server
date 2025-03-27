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

namespace ASC.Files.Core;

/// <summary>
/// The entry properties parameters.
/// </summary>
[DebuggerDisplay("")]
public class EntryProperties<T>
{
    /// <summary>
    /// The form filling properties.
    /// </summary>
    public FormFillingProperties<T> FormFilling { get; set; }

    public static EntryProperties<T> Deserialize(string data, ILogger logger)
    {
        var options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        try
        {
            return JsonSerializer.Deserialize<EntryProperties<T>>(data, options);
        }
        catch (Exception e)
        {
            logger.ErrorWithException("Error parse EntryProperties: " + data, e);
            return null;
        }
    }

    public static string Serialize(EntryProperties<T> entryProperties, ILogger logger)
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
/// The form filling properties parameters.
/// </summary>
[Transient]
public class FormFillingProperties<T>
{
    /// <summary>
    /// Specifies if the form filling has started or not.
    /// </summary>
    public bool StartFilling { get; set; }

    /// <summary>
    /// The title of the form.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The room ID of the form.
    /// </summary>
    public T RoomId { get; set; }

    /// <summary>
    /// The folder ID to which the form is added.
    /// </summary>
    public T ToFolderId { get; set; }

    /// <summary>
    /// The original form ID.
    /// </summary>
    public T OriginalFormId { get; set; }

    /// <summary>
    /// The results folder ID.
    /// </summary>
    public T ResultsFolderId { get; set; }

    /// <summary>
    /// The results file ID.
    /// </summary>
    public T ResultsFileID { get; set; }

    /// <summary>
    /// The result form number.
    /// </summary>
    public int ResultFormNumber { get; set; }

    /// <summary>
    /// The date when the form filling was stopped.
    /// </summary>
    public DateTime FillingStopedDate { get; set; }

    /// <summary>
    /// The form filling interruption.
    /// </summary>
    public FormFillingInterruption? FormFillingInterruption { get; set; }

}

/// <summary>
/// The form filling interruption parameters.
/// </summary>
public struct FormFillingInterruption
{
    /// <summary>
    /// The user ID of the form filling interruption.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The role name of the form filling interruption.
    /// </summary>
    public string RoleName { get; set; }
}