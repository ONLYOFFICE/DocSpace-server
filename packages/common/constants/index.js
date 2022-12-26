export const LANGUAGE = "asc_language";
export const COOKIE_EXPIRATION_YEAR = 31536000000;
export const ARTICLE_PINNED_KEY = "asc_article_pinned_key";

/**
 * Enum for employee activation status.
 * @readonly
 */
export const EmployeeActivationStatus = Object.freeze({
  NotActivated: 0,
  Activated: 1,
  Pending: 2,
  AutoGenerated: 4,
});
/**
 * Enum for employee status.
 * @readonly
 */
export const EmployeeStatus = Object.freeze({
  Active: 1,
  Disabled: 2,
});
/**
 * Enum for employee type.
 * @readonly
 */
export const EmployeeType = Object.freeze({
  User: 1,
  Guest: 2,
  Admin: 3,
  UserString: "user",
  RoomAdmin: "manager",
  DocSpaceAdmin: "admin",
  Owner: "Owner",
});
/**
 * Enum for user payments type.
 * @readonly
 */
export const PaymentsType = Object.freeze({
  Paid: "0",
  Free: "1",
});
/**
 * Enum for filter subject.
 * @readonly
 */
export const FilterSubject = Object.freeze({
  Owner: "0",
  Member: "1",
});
/**
 * Enum for filter type.
 * @readonly
 */
export const FilterType = Object.freeze({
  None: 0,
  FilesOnly: 1,
  FoldersOnly: 2,
  DocumentsOnly: 3,
  PresentationsOnly: 4,
  SpreadsheetsOnly: 5,
  ImagesOnly: 7,
  ByUser: 8,
  ByDepartment: 9,
  ArchiveOnly: 10,
  ByExtension: 11,
  MediaOnly: 12,
  OFormTemplateOnly: 18,
  OFormOnly: 19,
});

/**
 * Enum for file type.
 * @readonly
 */
export const FileType = Object.freeze({
  Unknown: 0,
  Archive: 1,
  Video: 2,
  Audio: 3,
  Image: 4,
  Spreadsheet: 5,
  Presentation: 6,
  Document: 7,
  OFormTemplate: 8,
  OForm: 9,
});
/**
 * Enum for room type.
 * @readonly
 */
export const RoomsType = Object.freeze({
  FillingFormsRoom: 1,
  EditingRoom: 2,
  ReviewRoom: 3,
  ReadOnlyRoom: 4,
  CustomRoom: 5,
});

export const RoomsTypeTranslations = Object.freeze({
  1: "Files:FillingFormRooms",
  2: "Files:CollaborationRooms",
  3: "Common:Review",
  4: "Files:ViewOnlyRooms",
  5: "Files:CustomRooms",
});
/**
 * Enum for room provider type.
 * @readonly
 */
export const RoomsProviderType = Object.freeze({
  Box: 1,
  DropBox: 2,
  GoogleDrive: 3,
  kDrive: 4,
  OneDrive: 5,
  SharePoint: 6,
  WebDav: 7,
  Yandex: 8,
});

export const RoomsProviderTypeName = Object.freeze({
  1: "Box",
  2: "DropBox",
  3: "Google Drive",
  4: "kDrive",
  5: "OneDrive",
  6: "SharePoint",
  7: "WebDav",
  8: "Yandex",
});

/**
 * Enum for room search area.
 * @readonly
 */
export const RoomSearchArea = Object.freeze({
  Any: "Any",
  Active: "Active",
  Archive: "Archive",
});
/**
 * Enum for file action.
 * @readonly
 */
export const FileAction = Object.freeze({
  Create: 0,
  Rename: 1,
});
/**
 * Enum for root folders type.
 * @readonly
 */
export const FolderType = Object.freeze({
  DEFAULT: 0,
  COMMON: 1,
  BUNCH: 2,
  TRASH: 3,
  USER: 5,
  SHARE: 6,
  Projects: 8,
  Favorites: 10,
  Recent: 11,
  Templates: 12,
  Privacy: 13,
  Rooms: 14,
  Archive: 20,
});

// extends FolderType keys
export const FolderNames = Object.freeze({
  0: "default",
  1: "common",
  2: "bunch",
  3: "trash",
  5: "personal",
  6: "share",
  8: "projects",
  10: "favorites",
  11: "recent",
  12: "templates",
  13: "privacy",
  14: "shared",
  20: "archive",
});

export const ShareAccessRights = Object.freeze({
  None: 0,
  FullAccess: 1,
  ReadOnly: 2,
  DenyAccess: 3,
  Varies: 4,
  Review: 5,
  Comment: 6,
  FormFilling: 7,
  CustomFilter: 8,
  RoomManager: 9,
  Editing: 10,
});
export const ConflictResolveType = Object.freeze({
  Skip: 0,
  Overwrite: 1,
  Duplicate: 2,
});
export const providersData = Object.freeze({
  google: {
    label: "google",
    icon: "/static/images/share.google.react.svg",
  },
  facebook: {
    label: "facebook",
    icon: "/static/images/share.facebook.react.svg",
  },
  twitter: {
    label: "twitter",
    icon: "/static/images/share.twitter.react.svg",
    iconOptions: { color: "#2AA3EF" },
  },
  linkedin: {
    label: "linkedin",
    icon: "/static/images/share.linkedin.react.svg",
  },
});
export const LoaderStyle = {
  title: "",
  width: "100%",
  height: "32",
  backgroundColor: "#000000",
  foregroundColor: "#000000",
  backgroundOpacity: 0.1,
  foregroundOpacity: 0.15,
  borderRadius: "3",
  radius: "3",
  speed: 2,
  animate: true,
};

/**
 * Enum for third-party storages.
 * @readonly
 */
export const ThirdPartyStorages = Object.freeze({
  GoogleId: "googlecloud",
  RackspaceId: "rackspace",
  SelectelId: "selectel",
  AmazonId: "s3",
});
/**
 * Enum for backup types.
 * @readonly
 */
export const BackupStorageType = Object.freeze({
  DocumentModuleType: 0,
  ResourcesModuleType: 1,
  LocalFileModuleType: 3,
  TemporaryModuleType: 4,
  StorageModuleType: 5,
});

export const AutoBackupPeriod = Object.freeze({
  EveryDayType: 0,
  EveryWeekType: 1,
  EveryMonthType: 2,
});

/**
 * Enum for Tenant trusted domains on registration.
 * @readonly
 */
export const TenantTrustedDomainsType = Object.freeze({
  None: 0,
  Custom: 1,
  All: 2,
});
export const PasswordLimitSpecialCharacters = "!@#$%^&*";

/**
 * Enum for file status.
 * @readonly
 */
export const FileStatus = Object.freeze({
  None: 0,
  IsEditing: 1,
  IsNew: 2,
  IsConverting: 4,
  IsOriginal: 8,
  IsEditingAlone: 16,
  IsFavorite: 32,
  IsTemplate: 64,
  IsFillFormDraft: 128,
});

/**
 * Enum for tenant status.
 * @readonly
 */
export const TenantStatus = Object.freeze({
  PortalRestore: 4,
});

/**
 * Enum for theme keys.
 * @readonly
 */
export const ThemeKeys = Object.freeze({
  Base: "0",
  BaseStr: "Base",
  Dark: "1",
  DarkStr: "Dark",
  System: "2",
  SystemStr: "System",
});

/**
 * Enum for global events.
 * @readonly
 */
export const Events = Object.freeze({
  CREATE: "create",
  RENAME: "rename",
  ROOM_CREATE: "create_room",
  ROOM_EDIT: "edit_room",
  CHANGE_COLUMN: "change_column",
  CHANGE_USER_TYPE: "change_user_type",
});

/**
 * Enum for feed action types.
 * @readonly
 */
export const FeedActionTypes = Object.freeze({
  Create: 0,
  Update: 1,
  Rename: 2,
  Move: 3,
  Delete: 4,
});

/**
 * Enum for feed item types.
 * @readonly
 */
export const FeedItemTypes = Object.freeze({
  File: "file",
  Folder: "folder",
  Room: "room",
  Tag: "tag",
  User: "sharedRoom",
});

/**
 * Enum for theme keys.
 * @readonly
 */
export const TariffState = Object.freeze({
  Trial: 0,
  Paid: 1,
  Delay: 2,
  NotPaid: 3,
});

/**
 * Enum for theme keys.
 * @readonly
 */
export const PortalFeaturesLimitations = Object.freeze({
  Limitless: -1,
  Unavailable: 0,
});

export const EDITOR_ID = "docspace_editor";
