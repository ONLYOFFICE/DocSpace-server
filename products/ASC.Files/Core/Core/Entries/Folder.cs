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
/// The folder type.
/// </summary>
public enum FolderType
{
    [SwaggerEnum(Description = "Default")]
    DEFAULT = 0,

    [SwaggerEnum(Description = "Coomon")]
    COMMON = 1,

    [SwaggerEnum(Description = "Bunch")]
    BUNCH = 2,

    [SwaggerEnum(Description = "Trash")]
    TRASH = 3,

    [SwaggerEnum(Description = "User")]
    USER = 5,

    [SwaggerEnum(Description = "Share")]
    SHARE = 6,

    [SwaggerEnum(Description = "Projects")]
    Projects = 8,

    [SwaggerEnum(Description = "Favourites")]
    Favorites = 10,

    [SwaggerEnum(Description = "Recent")]
    Recent = 11,

    [SwaggerEnum(Description = "Templates")]
    Templates = 12,

    [SwaggerEnum(Description = "Privacy")]
    Privacy = 13,

    [SwaggerEnum(Description = "Virtual rooms")]
    VirtualRooms = 14,

    [SwaggerEnum(Description = "Filling forms room")]
    FillingFormsRoom = 15,

    [SwaggerEnum(Description = "Editing room")]
    EditingRoom = 16,

    [SwaggerEnum(Description = "Custom room")]
    CustomRoom = 19,

    [SwaggerEnum(Description = "Archive")]
    Archive = 20,

    [SwaggerEnum(Description = "Thirdparty backup")]
    ThirdpartyBackup = 21,

    [SwaggerEnum(Description = "Public room")]
    PublicRoom = 22,

    [SwaggerEnum(Description = "Ready form folder")]
    ReadyFormFolder = 25,

    [SwaggerEnum(Description = "In process form folder")]
    InProcessFormFolder = 26,

    [SwaggerEnum(Description = "Form filling folder done")]
    FormFillingFolderDone = 27,
    [SwaggerEnum(Description = "Form filling folder in progress")]
    FormFillingFolderInProgress = 28,

    [SwaggerEnum(Description = "Virtual Data Room")]
    VirtualDataRoom = 29,
        
    [SwaggerEnum(Description = "Room templates folder")]
    RoomTemplates = 30
}

/// <summary>
/// The folder parameters.
/// </summary>
public interface IFolder
{
    /// <summary>
    /// The folder type.
    /// </summary>
    public FolderType FolderType { get; set; }
    
    /// <summary>
    /// The number of files in the folder.
    /// </summary>
    public int FilesCount { get; set; }
    
    /// <summary>
    /// The number of folders in the folder.
    /// </summary>
    public int FoldersCount { get; set; }
    
    /// <summary>
    /// Specifies if the folder can be shared or not.
    /// </summary>
    public bool Shareable { get; set; }
    
    /// <summary>
    /// The number of files in the folder that the user has not seen yet.
    /// </summary>
    public int NewForMe { get; set; }
    
    /// <summary>
    /// The URL to the folder.
    /// </summary>
    public string FolderUrl { get; set; }
    
    /// <summary>
    /// Specifies if the folder is pinned to the top of the list or not.
    /// </summary>
    public bool Pinned { get; set; }
    
    /// <summary>
    /// The collection of folder tags.
    /// </summary>
    public IEnumerable<Tag> Tags { get; set; }
}

/// <summary>
/// The folder parameters.
/// </summary>
[DebuggerDisplay("{Title} ({Id})")]
[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
public class Folder<T> : FileEntry<T>, IFolder
{
    /// <summary>
    /// The folder type.
    /// </summary>
    public FolderType FolderType { get; set; }

    /// <summary>
    /// The number of files in the folder.
    /// </summary>
    public int FilesCount { get; set; }

    /// <summary>
    /// The number of folders in the folder.
    /// </summary>
    public int FoldersCount { get; set; }

    /// <summary>
    /// Specifies if the folder can be shared or not.
    /// </summary>
    public bool Shareable { get; set; }

    /// <summary>
    /// The number of files in the folder that the user has not seen yet.
    /// </summary>
    public int NewForMe { get; set; }

    /// <summary>
    /// The URL to the folder.
    /// </summary>
    public string FolderUrl { get; set; }

    /// <summary>
    /// Specifies if the folder is pinned to the top of the list or not.
    /// </summary>
    public bool Pinned { get; set; }
    
    /// <summary>
    /// Specifies if the folder is private or not.
    /// </summary>
    public bool SettingsPrivate { get; set; }
    
    /// <summary>
    /// Specifies if an icon will be displayed on the folder cover or not.
    /// </summary>
    public bool SettingsHasLogo { get; set; }
    
    /// <summary>
    /// The color in the HEX format for the folder cover.
    /// </summary>
    public string SettingsColor { get; set; }

    /// <summary>
    /// The ID of the folder cover icon.
    /// </summary>
    public string SettingsCover { get; set; }
    
    /// <summary>
    /// The watermark settings.
    /// </summary>
    public WatermarkSettings SettingsWatermark { get; set; }
    
    /// <summary>
    /// Specifies if the folder content is indexed or not.
    /// </summary>
    public bool SettingsIndexing { get; set; }
    
    /// <summary>
    /// The folder quota.
    /// </summary>
    public long SettingsQuota { get; set; }
    
    /// <summary>
    /// The file lifetime in this folder.
    /// </summary>
    public RoomDataLifetime SettingsLifetime { get; set; }
    
    /// <summary>
    /// Specifies if the files can be downloaded from this folder or not.
    /// </summary>
    public bool SettingsDenyDownload { get; set; }
    
    /// <summary>
    /// The folder used space.
    /// </summary>
    public long Counter { get; set; }
    
    /// <summary>
    /// Specifies if the folder has files that the user has not seen yet.
    /// </summary>
    public override bool IsNew
    {
        get => Convert.ToBoolean(NewForMe);
        set => NewForMe = Convert.ToInt32(value);
    }

    /// <summary>
    /// Specifies if the folder is favorite or not.
    /// </summary>
    public bool IsFavorite { get; set; }
    
    /// <summary>
    /// Specifies if the folder provider is mapped or not.
    /// </summary>
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

    /// <summary>
    /// The unique forlder ID.
    /// </summary>
    public override string UniqID => $"folder_{Id}";
    
    /// <summary>
    /// Specifies if the folder is root or not.
    /// </summary>
    public bool IsRoot => FolderType == RootFolderType;
}
