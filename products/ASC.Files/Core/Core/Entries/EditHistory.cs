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

namespace ASC.Files.Core;

/// <summary>
/// The file editing history parameters.
/// </summary>
[Transient]
[DebuggerDisplay("{ID} v{Version}")]
public class EditHistory(ILogger<EditHistory> logger,
    TenantUtil tenantUtil,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper)
{

    /// <summary>
    /// The document ID.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// The document identifier used to unambiguously identify the document file.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The document version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The document version group.
    /// </summary>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The date and time of the document modifications.
    /// </summary>
    public DateTime ModifiedOn { get; set; }

    /// <summary>
    /// The author ID who modified the document.
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// The file editing changes.
    /// </summary>
    public string ChangesString { get; set; }

    /// <summary>
    /// The current server version number.
    /// </summary>
    public string ServerVersion { get; set; }

    /// <summary>
    /// The list of changes of the file editing history.
    /// </summary>
    public List<EditHistoryChanges> Changes
    {
        get
        {
            var changes = new List<EditHistoryChanges>();
            if (string.IsNullOrEmpty(ChangesString))
            {
                return changes;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                };

                var jObject = JsonSerializer.Deserialize<ChangesDataList>(ChangesString, options);
                ServerVersion = jObject.ServerVersion;

                if (string.IsNullOrEmpty(ServerVersion))
                {
                    return changes;
                }

                changes = jObject.Changes.Select(r =>
                {
                    var result = new EditHistoryChanges
                    {
                        Author = new EditHistoryAuthor(userManager, displayUserSettingsHelper)
                        {
                            Id = r.User.Id ?? "",
                            Name = r.User.Name
                        }
                    };


                    if (DateTime.TryParse(r.Created, out var _date))
                    {
                        _date = tenantUtil.DateTimeFromUtc(_date);
                    }
                    result.Date = _date;
                    result.DocumentSha256 = r.DocumentSha256;

                    return result;
                })
                .ToList();

                return changes;
            }
            catch (Exception ex)
            {
                logger.ErrorDeSerializeOldScheme(ex);
            }

            return changes;
        }
        set => throw new NotImplementedException();
    }
}

/// <summary>
/// The data list of the file changes.
/// </summary>
class ChangesDataList
{
    /// <summary>
    /// The current server version number.
    /// </summary>
    public string ServerVersion { get; set; }

    /// <summary>
    /// The array of the file changes.
    /// </summary>
    public ChangesData[] Changes { get; set; }
}

/// <summary>
/// The data item of the file changes.
/// </summary>
class ChangesData
{
    /// <summary>
    /// The date when the file change was created.
    /// </summary>
    public string Created { get; set; }

    /// <summary>
    /// The user who changed the file.
    /// </summary>
    public ChangesUserData User { get; set; }

    /// <summary>
    /// The document hash generated by the SHA-256 algorithm.
    /// </summary>
    public string DocumentSha256 { get; set; }
}

/// <summary>
/// The parameters of the user who changed the file.
/// </summary>
class ChangesUserData
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The user name.
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// The information about the file editing history author.
/// </summary>
[Transient]
[DebuggerDisplay("{Id} {Name}")]
public class EditHistoryAuthor(UserManager userManager, DisplayUserSettingsHelper displayUserSettingsHelper)
{
    /// <summary>
    /// The author ID.
    /// </summary>
    public string Id { get; init; }

    private readonly string _name;

    /// <summary>
    /// The author name.
    /// </summary>
    public string Name
    {
        get
        {
            if (!Guid.TryParse(Id, out var idInternal))
            {
                return _name;
            }

            UserInfo user;
            return
                idInternal.Equals(Guid.Empty)
                      || idInternal.Equals(ASC.Core.Configuration.Constants.Guest.ID)
                      || (user = userManager.GetUsers(idInternal)).Equals(Constants.LostUser)
                          ? string.IsNullOrEmpty(_name)
                                ? FilesCommonResource.Guest
                                : _name
                          : user.DisplayUserName(false, displayUserSettingsHelper);
        }
        init => _name = value;
    }
}

/// <summary>
/// The information about file editing history changes.
/// </summary>
[DebuggerDisplay("{Author.Name}")]
public class EditHistoryChanges
{
    /// <summary>
    /// The author who changed the file.
    /// </summary>
    public EditHistoryAuthor Author { get; init; }

    /// <summary>
    /// The date and time of the file changes.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The document hash generated by the SHA-256 algorithm.
    /// </summary>
    public string DocumentSha256 { get; set; }
}

/// <summary>
/// The file editing history data.
/// </summary>
[DebuggerDisplay("{Version}")]
public class EditHistoryDataDto
{
    /// <summary>
    /// The url address of the file with the document changes data.
    /// </summary>
    [Url]
    public string ChangesUrl { get; set; }

    /// <summary>
    /// The document identifier used to unambiguously identify the document file.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The object of the previous version of the document.
    /// </summary>
    public EditHistoryUrl Previous { get; set; }

    /// <summary>
    /// The encrypted signature added to the parameter in the form of a token.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// The url address of the current document version.
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// The document version number.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// The document extension.
    /// </summary>
    public string FileType { get; set; }
}

/// <summary>
/// The file editing history URL parameters.
/// </summary>
[DebuggerDisplay("{Key} - {Url}")]
public class EditHistoryUrl
{
    /// <summary>
    /// The document identifier of the previous version of the document.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// The url address of the previous version of the document.
    /// </summary>
    [Url]
    public string Url { get; init; }

    /// <summary>
    /// The document extension.
    /// </summary>
    public string FileType { get; set; }
}
