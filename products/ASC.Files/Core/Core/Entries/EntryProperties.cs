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

namespace ASC.Files.Core;

/// <summary>
/// The entry properties.
/// </summary>
[DebuggerDisplay("")]
public class EntryProperties<T>
{
    /// <summary>
    /// The form filling properties.
    /// </summary>
    public FormFillingProperties<T> FormFilling { get; set; }

    public bool CopyToFillOut { get; set; }

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
            return JsonSerializer.Serialize(entryProperties, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception e)
        {
            logger.ErrorWithException("Error serialize EntryProperties", e);
            return null;
        }
    }
}

/// <summary>
/// The form filling properties.
/// </summary>
[Transient]
public class FormFillingProperties<T>
{
    /// <summary>
    /// Specifies if the form filling has started or not.
    /// </summary>
    public bool StartFilling { get; set; }

    public Guid StartedByUserId { get; set; }
    /// <summary>
    /// The form title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The room ID of the form.
    /// </summary>
    public T RoomId { get; set; }

    /// <summary>
    /// The ID of the folder where the form is added.
    /// </summary>
    public T ToFolderId { get; set; }

    /// <summary>
    /// The original form ID.
    /// </summary>
    public T OriginalFormId { get; set; }

    /// <summary>
    /// The original form version.
    /// </summary>
    public int OriginalFormVersion { get; set; }

    /// <summary>
    /// The ID of the folder where the form filling results are saved.
    /// </summary>
    public T ResultsFolderId { get; set; }

    /// <summary>
    /// The ID of the file with form filling results.
    /// </summary>
    public T ResultsFileID { get; set; }

    /// <summary>
    /// Indicates whether the original form version has changed.
    /// </summary>
    public bool IsVersionChanged { get; set; }

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

    public bool? CollectFillForm { get; set; }

    /// <summary>
    /// The name of the table in the external database that corresponds to this form.
    /// </summary>
    public string ExternalDbTableName { get; set; }

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