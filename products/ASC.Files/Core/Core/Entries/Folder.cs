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

public enum FolderType
{
    [OpenApiEnum(Description = "Default")]
    DEFAULT = 0,

    [OpenApiEnum(Description = "Coomon")]
    COMMON = 1,

    [OpenApiEnum(Description = "Bunch")]
    BUNCH = 2,

    [OpenApiEnum(Description = "Trash")]
    TRASH = 3,

    [OpenApiEnum(Description = "User")]
    USER = 5,

    [OpenApiEnum(Description = "Share")]
    SHARE = 6,

    [OpenApiEnum(Description = "Projects")]
    Projects = 8,

    [OpenApiEnum(Description = "Favourites")]
    Favorites = 10,

    [OpenApiEnum(Description = "Recent")]
    Recent = 11,

    [OpenApiEnum(Description = "Templates")]
    Templates = 12,

    [OpenApiEnum(Description = "Privacy")]
    Privacy = 13,

    [OpenApiEnum(Description = "Virtual rooms")]
    VirtualRooms = 14,

    [OpenApiEnum(Description = "Filling forms room")]
    FillingFormsRoom = 15,

    [OpenApiEnum(Description = "Editing room")]
    EditingRoom = 16,

    [OpenApiEnum(Description = "Custom room")]
    CustomRoom = 19,

    [OpenApiEnum(Description = "Archive")]
    Archive = 20,

    [OpenApiEnum(Description = "Thirdparty backup")]
    ThirdpartyBackup = 21,

    [OpenApiEnum(Description = "Public room")]
    PublicRoom = 22,

    [OpenApiEnum(Description = "Ready form folder")]
    ReadyFormFolder = 25,

    [OpenApiEnum(Description = "In process form folder")]
    InProcessFormFolder = 26,

    [OpenApiEnum(Description = "Form filling folder done")]
    FormFillingFolderDone = 27,
    [OpenApiEnum(Description = "Form filling folder in progress")]
    FormFillingFolderInProgress = 28,

    [OpenApiEnum(Description = "Virtual Data Room")]
    VirtualDataRoom = 29
}

public interface IFolder
{
    public FolderType FolderType { get; set; }
    public int FilesCount { get; set; }
    public int FoldersCount { get; set; }
    public bool Shareable { get; set; }
    public int NewForMe { get; set; }
    public string FolderUrl { get; set; }
    public bool Pinned { get; set; }
    public IEnumerable<Tag> Tags { get; set; }
}

[DebuggerDisplay("{Title} ({Id})")]
[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
public class Folder<T> : FileEntry<T>, IFolder
{
    public FolderType FolderType { get; set; }
    public int FilesCount { get; set; }
    public int FoldersCount { get; set; }
    public bool Shareable { get; set; }
    public int NewForMe { get; set; }
    public string FolderUrl { get; set; }
    public bool Pinned { get; set; }
    public bool SettingsPrivate { get; set; }
    public bool SettingsHasLogo { get; set; }
    public string SettingsColor { get; set; }
    public string SettingsCover { get; set; }
    public WatermarkSettings SettingsWatermark { get; set; }
    public bool SettingsIndexing { get; set; }
    public long SettingsQuota { get; set; }
    public RoomDataLifetime SettingsLifetime { get; set; }
    public bool SettingsDenyDownload { get; set; }
    public long Counter { get; set; }
    public override bool IsNew
    {
        get => Convert.ToBoolean(NewForMe);
        set => NewForMe = Convert.ToInt32(value);
    }

    public bool IsFavorite { get; set; }
    public bool ProviderMapped { get; set; }

    public Folder()
    {
        Title = string.Empty;
        FileEntryType = FileEntryType.Folder;
    }

    public Folder(
        FileHelper fileHelper,
        Global global,
        SecurityContext securityContext) : base(fileHelper, global, securityContext)
    {
        Title = string.Empty;
        FileEntryType = FileEntryType.Folder;
    }

    public override string UniqID => $"folder_{Id}";
    public bool IsRoot => FolderType == RootFolderType;
}
