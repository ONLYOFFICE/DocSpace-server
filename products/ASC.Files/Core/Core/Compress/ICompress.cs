﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Web.Files.Core.Compress;

///<summary>
/// The archiving class unification interface.
///</summary>
public interface ICompress : IDisposable
{
    /// <summary>
    /// Initializes and sets a new output stream for archiving.
    /// </summary>
    /// /// <param name="stream">Accepts a new stream, it will contain an archive upon completion of work.</param>
    Task SetStream(Stream stream);

    /// <summary>
    /// Creates an archive entity (a separate file in the archive).
    /// </summary>
    /// <param name="title">The file name with an extension.</param>
    /// <param name="lastModification">The date and time when the file was last modified.</param>
    Task CreateEntry(string title, DateTime? lastModification = null);

    /// <summary>
    /// Transfers the file itself to the archive.
    /// </summary>
    /// <param name="readStream">The file data.</param>
    Task PutStream(Stream readStream);

    /// <summary>
    /// Puts an entry to the output stream.
    /// </summary>
    Task PutNextEntry();

    /// <summary>
    /// Closes the current entry.
    /// </summary>
    Task CloseEntry();

    /// <summary>
    /// Returns the resource title (does not affect the work of the class).
    /// </summary>
    Task<string> GetTitle();

    /// <summary>
    /// Returns the archive extension (does not affect the work of the class).
    /// </summary>
    Task<string> GetArchiveExtension();
}
