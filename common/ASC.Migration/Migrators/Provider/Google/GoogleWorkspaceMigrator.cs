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

using ASCShare = ASC.Files.Core.Security.FileShare;

namespace ASC.Migration.Core.Migrators.Provider.Google;

[Transient]
public class GoogleWorkspaceMigrator : Migrator
{

    private CancellationToken _cancellationToken;
    private string[] _takeouts;
    private readonly Regex _emailRegex = new Regex(@"(\S*@\S*\.\S*)");
    private readonly Regex _phoneRegex = new Regex(@"(\+?\d+)");

    private  readonly Regex _workspacesRegex = new Regex(@"Workspaces(\(\d+\))?.json");
    private readonly Regex _pinnedRegex = new Regex(@".*-at-.*-pinned\..*");
    private const string CommentsFile = "-comments.html";
    private const string InfoFile = "-info.json";
    private readonly Regex _commentsVersionFile = new Regex(@"-comments(\([\d]+\))\.html");
    private readonly Regex _infoVersionFile = new Regex(@"-info(\([\d]+\))\.json");
    private readonly Regex _versionRegex = new Regex(@"(\([\d]+\))");

    public GoogleWorkspaceMigrator(SecurityContext securityContext,
        UserManager userManager,
        TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
        QuotaSocketManager quotaSocketManager,
        FileStorageService fileStorageService,
        GlobalFolderHelper globalFolderHelper,
        IServiceProvider serviceProvider,
        IDaoFactory daoFactory,
        EntryManager entryManager,
        MigrationLogger migrationLogger,
        AuthContext authContext,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        UserManagerWrapper userManagerWrapper) : base(securityContext, userManager, tenantQuotaFeatureStatHelper, quotaSocketManager, fileStorageService, globalFolderHelper, serviceProvider, daoFactory, entryManager, migrationLogger, authContext, displayUserSettingsHelper, userManagerWrapper)
    {
        MigrationInfo = new MigrationInfo { Name = "GoogleWorkspace" };
    }

    public override async Task InitAsync(string path, CancellationToken cancellationToken, OperationType operation)
    {
        await MigrationLogger.InitAsync();
        _cancellationToken = cancellationToken;

        MigrationInfo.Operation = operation;

        var files = Directory.GetFiles(path);
        TmpFolder = path;
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".zip")))
        {
            MigrationInfo.FailedArchives = files.ToList();
            throw new Exception("Archives must be .zip");
        }

        _takeouts = files.Where(item => item.EndsWith(".zip")).ToArray();
        MigrationInfo.Files = _takeouts.Select(Path.GetFileName).ToList();
    }

    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            await ReportProgressAsync(5, MigrationResource.StartOfDataProcessing);
        }

        var progressStep = 90 / _takeouts.Length;
        var i = 1;
        foreach (var takeout in _takeouts)
        {
            if (_cancellationToken.IsCancellationRequested && reportProgress)
            {
                return null;
            }

            if (reportProgress)
            {
                await ReportProgressAsync(_lastProgressUpdate + progressStep, MigrationResource.DataProcessing + $" {takeout} ({i++}/{_takeouts.Length})");
            }

            var tmpFolder = Path.Combine(TmpFolder, Path.GetFileNameWithoutExtension(takeout));
            var key = Path.GetFileName(takeout);
            try
            {
                using (var archive = ZipFile.OpenRead(takeout))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(Path.Combine(tmpFolder, entry.FullName));
                        }
                        else
                        {
                            var dir = Path.GetDirectoryName(Path.Combine(tmpFolder, entry.FullName));
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            entry.ExtractToFile(Path.Combine(tmpFolder, entry.FullName));
                        }
                        if (_cancellationToken.IsCancellationRequested && reportProgress)
                        {
                            return null;
                        }
                    }
                }
                var rootFolder = Path.Combine(tmpFolder, "Takeout");

                if (!Directory.Exists(rootFolder))
                {
                    throw new Exception("Takeout zip does not contain root 'Takeout' folder.");
                }
                var directories = Directory.GetDirectories(rootFolder);
                if (directories.Length == 1 && directories[0].Split(Path.DirectorySeparatorChar).Last() == "Groups")
                {
                    ParseGroup(rootFolder);
                }
                else
                {
                    var user = ParseUser(rootFolder);
                    if (string.IsNullOrEmpty(user.Info.Email))
                    {
                        MigrationInfo.WithoutEmailUsers.Add(key, user);
                    }
                    else if (await UserManager.GetUserByEmailAsync(user.Info.Email) != ASC.Core.Users.Constants.LostUser)
                    {
                        if (!MigrationInfo.ExistUsers.ContainsKey(user.Info.Email))
                        {
                            MigrationInfo.ExistUsers.Add(user.Info.Email, user);
                        }
                        else
                        {
                            MergeStorages(MigrationInfo.ExistUsers[user.Info.Email], user);
                        }
                    }
                    else
                    {
                        if (!MigrationInfo.Users.ContainsKey(user.Info.Email))
                        {
                            MigrationInfo.Users.Add(user.Info.Email, user);
                        }
                        else
                        {
                            MergeStorages(MigrationInfo.Users[user.Info.Email], user);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MigrationInfo.FailedArchives.Add(key);
                Log(MigrationResource.CanNotParseArchives, ex);
                if (MigrationInfo.FailedArchives.Count == _takeouts.Length)
                {
                    await ReportProgressAsync(_lastProgressUpdate, MigrationResource.CanNotParseArchives);
                    throw new Exception(MigrationResource.CanNotParseArchives);
                }
            }
            finally
            {
                if (Directory.Exists(tmpFolder) && reportProgress)
                {
                    Directory.Delete(tmpFolder, true);
                }
            }
        }
        if (reportProgress)
        {
            await ReportProgressAsync(100, MigrationResource.DataProcessingCompleted);
        }

        return MigrationInfo.ToApiInfo();
    }

    private void MergeStorages(MigrationUser user1, MigrationUser user2)
    {
        user1.Storage.BytesTotal += user2.Storage.BytesTotal;
        user1.Storage.Files.AddRange(user2.Storage.Files);
        user1.Storage.Folders.AddRange(user2.Storage.Folders);
        user1.Storage.Securities.AddRange(user2.Storage.Securities);
    }
    private void ParseGroup(string tmpFolder)
    {
        var group = new MigrationGroup { Info = new(), UserKeys = new HashSet<string>() };
        var groupsFolder = Path.Combine(tmpFolder, "Groups");
        var groupInfo = Path.Combine(groupsFolder, "info.csv");
        using (var sr = new StreamReader(groupInfo))
        {
            var line = sr.ReadLine();
            line = sr.ReadLine();
            if (line != null)
            {
                var name = line.Split(',')[9];
                if (!string.IsNullOrWhiteSpace(name))
                {
                    group.Info.Name = name;
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(group.Info.Name))
        {
            var groupMembers = Path.Combine(groupsFolder, "members.csv");
            using var sr = new StreamReader(groupMembers);
            var line = sr.ReadLine();
            while ((line = sr.ReadLine()) != null)
            {
                group.UserKeys.Add(line.Split(',')[1]);
            }
        }
        MigrationInfo.Groups.Add(group.Info.Name, group);
    }

    private MigrationUser ParseUser(string tmpFolder)
    {
        var user = new MigrationUser(DisplayUserSettingsHelper) { Info = new() };

        ParseRootHtml(tmpFolder, user);
        ParseProfile(tmpFolder, user);
        ParseAccount(tmpFolder, user);

        user.Info.UserName = user.Info.Email.Split('@').First();
        if (string.IsNullOrEmpty(user.Info.FirstName))
        {
            user.Info.FirstName = user.Info.Email.Split('@').First();
        }

        ParseStorage(tmpFolder, user);

        return user;
    }

    private void ParseStorage(string tmpFolder, MigrationUser user)
    {
        user.Storage = new MigrationStorage();

        var drivePath = Path.Combine(tmpFolder, "Drive");
        if (!Directory.Exists(drivePath))
        {
            return;
        }

        var entries = Directory.GetFileSystemEntries(drivePath, "*", SearchOption.AllDirectories);

        user.Storage.RootKey = "0";
        var filteredEntries = new List<string>();

        foreach (var entry in entries)
        {
            if (ShouldIgnoreFile(entry, entries))
            {
                continue;
            }

            filteredEntries.Add(entry);
        }
        var i = 1;
        var foldersdictionary = new Dictionary<string, MigrationFolder>();
        foreach (var entry in filteredEntries)
        {
            var attr = File.GetAttributes(entry);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                ParseFolders(entry.Substring(drivePath.Length + 1), foldersdictionary, i);
            }
            else
            {
                var fi = new FileInfo(entry);
                user.Storage.BytesTotal += fi.Length;

                var substring = entry.Substring(drivePath.Length + 1);
                var split = substring.Split('\\');
                var path = Path.GetDirectoryName(substring);
                if (split.Count() != 1)
                {
                    ParseFolders(path, foldersdictionary, i);
                }
                var file = new MigrationFile
                {
                    Id = i++,
                    Folder = split.Count() == 1 ? 0 : foldersdictionary[path].Id,
                    Title = split.Last(),
                    Path = entry
                };
                user.Storage.Files.Add(file);
            }
        }
        user.Storage.Folders.AddRange(foldersdictionary.Select(f => f.Value));

        foreach(var file in user.Storage.Files)
        {
            ParseShare(user.Storage.Securities, file.Path, file.Id, 2);
        }

        foreach (var folder in foldersdictionary)
        {
            ParseShare(user.Storage.Securities, Path.Combine(drivePath, folder.Key), folder.Value.Id, 1);
        }
    }

    private void ParseShare(List<MigrationSecurity> list, string path, int id, int entryType)
    {
        if (!TryReadInfoFile(path, out var info))
        {
            return;
        }

        foreach (var shareInfo in info.Permissions)
        {
            if (shareInfo.Type is "user" or "group")
            {
                var shareType = GetPortalShare(shareInfo);
                var subject = shareInfo.Type is "group" ? shareInfo.Name : shareInfo.EmailAddress;
                if (shareType == null || string.IsNullOrEmpty(subject))
                {
                    continue;
                }

                var security = new MigrationSecurity
                {
                    Subject = subject,
                    EntryId = id,
                    EntryType = entryType,
                    Security = (int)shareType.Value
                };
                list.Add(security);
            }
        }
    }

    private ASCShare? GetPortalShare(GwsDriveFilePermission fileInfo)
    {
        switch (fileInfo.Role)
        {
            case "writer":
                return ASCShare.Editing;
            case "reader":
                if (fileInfo.AdditionalRoles == null)
                {
                    return ASCShare.Read;
                }

                if (fileInfo.AdditionalRoles.Contains("commenter"))
                {
                    return ASCShare.Comment;
                }
                else
                {
                    return ASCShare.Read;
                }

            default:
                return null;
        };
    }

    private void ParseFolders(string entry, Dictionary<string, MigrationFolder> foldersdictionary, int i)
    {
        var split = entry.Split('\\');
        var j = 1;
        foreach (var f in split)
        {
            var folder = new MigrationFolder
            {
                Id = i++,
                ParentId = j == 1 ? 0 : i - 1,
                Title = f,
                Level = j++
            };
            var key = string.Join(',', split[0..(j - 1)]);
            if (!foldersdictionary.ContainsKey(key))
            {
                foldersdictionary.Add(key, folder);
            }
        }
    }

    private bool ShouldIgnoreFile(string entry, string[] entries)
    {
        if (_workspacesRegex.IsMatch(Path.GetFileName(entry)))
        {
            return true; // ignore workspaces.json
        }

        if (_pinnedRegex.IsMatch(Path.GetFileName(entry)))
        {
            return true; // ignore pinned files
        }

        if (entry.EndsWith(CommentsFile) || entry.EndsWith(InfoFile)) // check if this really a meta for existing file
        {
            // folder - folder
            // folder-info.json - valid meta

            // file.docx - file
            // file.docx-info.json - valid meta
            // file-info.json - valid meta

            var baseName = entry.Substring(0, entry.Length - (entry.EndsWith(CommentsFile) ? CommentsFile.Length : InfoFile.Length));
            if (entries.Contains(baseName))
            {
                return true;
            }

            if (entries
                .Where(e => e.StartsWith(baseName + "."))
                .Select(e => e.Substring(0, e.Length - Path.GetExtension(e).Length))
                .Contains(baseName))
            {
                return true;
            }
        }

        // file(1).docx - file
        // file.docx-info(1).json - valid meta
        // file-info(1).json - valid meta
        var commentsVersionMatch = _commentsVersionFile.Match(entry);
        if (commentsVersionMatch.Success)
        {
            var baseName = entry.Substring(0, entry.Length - commentsVersionMatch.Groups[0].Value.Length);
            baseName = baseName.Insert(baseName.LastIndexOf("."), commentsVersionMatch.Groups[1].Value);

            if (entries.Contains(baseName))
            {
                return true;
            }

            if (entries
                .Where(e => e.StartsWith(baseName + "."))
                .Select(e => e.Substring(0, e.Length - Path.GetExtension(e).Length))
                .Contains(baseName))
            {
                return true;
            }
        }

        var infoVersionMatch = _infoVersionFile.Match(entry);
        if (infoVersionMatch.Success)
        {
            var baseName = entry.Substring(0, entry.Length - infoVersionMatch.Groups[0].Length);
            baseName = baseName.Insert(baseName.LastIndexOf("."), infoVersionMatch.Groups[1].Value);

            if (entries.Contains(baseName))
            {
                return true;
            }

            if (entries
                .Where(e => e.StartsWith(baseName + "."))
                .Select(e => e.Substring(0, e.Length - Path.GetExtension(e).Length))
                .Contains(baseName))
            {
                return true;
            }
        }

        return false;
    }

    private void ParseRootHtml(string tmpFolder, MigrationUser user)
    {
        var htmlFiles = Directory.GetFiles(tmpFolder, "*.html");
        if (htmlFiles.Count() != 1)
        {
            throw new Exception("Incorrect Takeout format.");
        }

        var htmlPath = htmlFiles[0];

        var doc = new HtmlDocument();
        doc.Load(htmlPath);

        var emailNode = doc.DocumentNode.SelectNodes("//body//h1[@class='header_title']")[0];
        var matches = _emailRegex.Match(emailNode.InnerText);
        if (!matches.Success)
        {
            throw new Exception("Couldn't parse root html.");
        }

        user.Info.Email = matches.Groups[1].Value;
    }

    private void ParseProfile(string tmpFolder, MigrationUser user)
    {
        var profilePath = Path.Combine(tmpFolder, "Profile", "Profile.json");
        if (!File.Exists(profilePath))
        {
            return;
        }

        var googleProfile = JsonSerializer.Deserialize<GwsProfile>(File.ReadAllText(profilePath));

        if (googleProfile.Birthday != null)
        {
            user.Info.BirthDate = googleProfile.Birthday.Value.DateTime;
        }

        if (googleProfile.Gender != null)
        {
            switch (googleProfile.Gender.Type)
            {
                case "male":
                    user.Info.Sex = true;
                    break;

                case "female":
                    user.Info.Sex = false;
                    break;

                default:
                    user.Info.Sex = null;
                    break;
            }
        }

        user.Info.FirstName = googleProfile.Name.GivenName;
        user.Info.LastName = googleProfile.Name.FamilyName;

        if (googleProfile.Emails != null)
        {
            foreach (var email in googleProfile.Emails.Distinct())
            {
                AddEmailToUser(user, email.Value);
            }
        }

        user.PathToPhoto = Path.Combine(tmpFolder, "Profile", "ProfilePhoto.jpg");
        user.HasPhoto = File.Exists(user.PathToPhoto);
    }

    private void ParseAccount(string tmpFolder, MigrationUser user)
    {
        var accountPath = Path.Combine(tmpFolder, "Google Account");
        if (!Directory.Exists(accountPath))
        {
            return;
        }

        var htmlFiles = Directory.GetFiles(accountPath, "*.SubscriberInfo.html");
        if (htmlFiles.Count() != 1)
        {
            return;
        }

        var htmlPath = htmlFiles[0];

        var doc = new HtmlDocument();
        doc.Load(htmlPath);

        var alternateEmails = _emailRegex.Matches(doc.DocumentNode.SelectNodes("//div[@class='section'][1]/ul/li[2]")[0].InnerText);
        foreach (Match match in alternateEmails)
        {
            AddEmailToUser(user, match.Value);
        }

        var contactEmail = _emailRegex.Match(doc.DocumentNode.SelectNodes("//div[@class='section'][3]/ul/li[1]")[0].InnerText);
        if (contactEmail.Success)
        {
            AddEmailToUser(user, contactEmail.Groups[1].Value);
        }

        var recoveryEmail = _emailRegex.Match(doc.DocumentNode.SelectNodes("//div[@class='section'][3]/ul/li[2]")[0].InnerText);
        if (recoveryEmail.Success)
        {
            AddEmailToUser(user, recoveryEmail.Groups[1].Value);
        }

        var recoverySms = _phoneRegex.Match(doc.DocumentNode.SelectNodes("//div[@class='section'][3]/ul/li[3]")[0].InnerText);
        if (recoverySms.Success)
        {
            AddPhoneToUser(user, recoverySms.Groups[1].Value);
        }
    }

    private void AddEmailToUser(MigrationUser user, string email)
    {
        if (user.Info.Email != email && !user.Info.Contacts.Contains(email))
        {
            user.Info.ContactsList.Add(email.EndsWith("@gmail.com") ? "gmail" : "mail"); // SocialContactsManager.ContactType_gmail in ASC.WebStudio
            user.Info.ContactsList.Add(email);
        }
    }

    private void AddPhoneToUser(MigrationUser user, string phone)
    {
        if (user.Info.MobilePhone != phone && !user.Info.Contacts.Contains(phone))
        {
            user.Info.ContactsList.Add("mobphone"); // SocialContactsManager.ContactType_mobphone in ASC.WebStudio
            user.Info.ContactsList.Add(phone);
        }
    }

    private bool TryReadInfoFile(string entry, out GwsDriveFileInfo info)
    {
        info = null;
        var infoFilePath = FindInfoFile(entry);

        if (infoFilePath == null)
        {
            return false;
        }

        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            info = JsonSerializer.Deserialize<GwsDriveFileInfo>(File.ReadAllText(infoFilePath), options);
            return true;
        }
        catch (Exception ex)
        {
            Log(string.Format(MigrationResource.CanNotReadInfoFile, entry), ex);
        }

        return false;
    }

    private string FindInfoFile(string entry)
    {
        var infoFilePath = entry + InfoFile;
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file.docx-info.json
        }

        var ext = Path.GetExtension(entry);
        infoFilePath = entry.Substring(0, entry.Length - ext.Length) + InfoFile;
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file-info.json
        }

        var versionMatch = _versionRegex.Match(entry);
        if (!versionMatch.Success)
        {
            return null;
        }

        var version = versionMatch.Groups[1].Value;
        infoFilePath = entry.Replace(version, "") + InfoFile.Replace(".", version + ".");
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file.docx-info(1).json
        }

        infoFilePath = entry.Substring(0, entry.Length - ext.Length).Replace(version, "") + InfoFile.Replace(".", version + ".");
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file-info(1).json
        }

        return null;
    }
}
