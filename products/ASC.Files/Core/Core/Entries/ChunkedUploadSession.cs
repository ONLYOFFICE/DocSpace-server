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
/// The chunked upload session data.
/// </summary>
[DebuggerDisplay("{Id} into {FolderId}")]
public class ChunkedUploadSession<T>(File<T> file, long bytesTotal) : CommonChunkedUploadSession(bytesTotal)
{
    /// <summary>
    /// The chunked upload session folder ID.
    /// </summary>
    public T FolderId { get; set; }

    /// <summary>
    /// The chunked upload session file.
    /// </summary>
    public File<T> File { get; set; } = file;

    /// <summary>
    /// Specifies if the chunked upload session data is encrypted or not.
    /// </summary>
    public bool Encrypted { get; set; }

    /// <summary>
    /// Specifies whether to keep the file version after the chunked upload session or not.
    /// </summary>
    public bool KeepVersion { get; set; }

    //hack for Backup bug 48873
    /// <summary>
    /// Specifies whether to check quota of the chunked upload session data or not.
    /// </summary>
    [NonSerialized]
    public bool CheckQuota = true;

    /// <summary>
    /// Clones the chunked upload session data.
    /// </summary>
    public override object Clone()
    {
        var clone = (ChunkedUploadSession<T>)MemberwiseClone();
        clone.File = (File<T>)File.Clone();

        return clone;
    }
}

[Scope]
public class ChunkedUploadSessionHelper(ILogger<ChunkedUploadSessionHelper> logger, BreadCrumbsManager breadCrumbsManager)
{
    public async Task<object> ToResponseObjectAsync<T>(ChunkedUploadSession<T> session, bool appendBreadCrumbs = false)
    {
        var breadCrumbs = await breadCrumbsManager.GetBreadCrumbsAsync(session.FolderId); //todo: check how?
        var pathFolder = appendBreadCrumbs
            ? breadCrumbs.Select(f =>
            {
                if (f == null)
                {
                    logger.ErrorInUserInfoRequest(session.FolderId.ToString());

                    return default;
                }

                if (f is Folder<string> f1)
                {
                    return IdConverter.Convert<T>(f1.Id);
                }

                if (f is Folder<int> f2)
                {
                    return IdConverter.Convert<T>(f2.Id);
                }

                return IdConverter.Convert<T>(0);
            })
            : new List<T> { session.FolderId };

        return new
        {
            id = session.Id,
            path = pathFolder,
            created = session.Created,
            expired = session.Expired,
            location = session.Location,
            bytes_total = session.BytesTotal
        };
    }
}
