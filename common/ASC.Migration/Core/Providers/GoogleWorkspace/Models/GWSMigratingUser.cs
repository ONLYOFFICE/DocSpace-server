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

using System.Text.Json;

using ASC.Common.Security.Authentication;

namespace ASC.Migration.GoogleWorkspace.Models;

[Transient]
public class GwsMigratingUser(
    UserManager userManager,
    IServiceProvider serviceProvider,
    QuotaSocketManager quotaSocketManager,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper)
    : MigratingUser<GwsMigratingFiles>
{
    public override string Email => _userInfo.Email;
    public Guid Guid => _userInfo.Id;

    public override string DisplayName => $"{_userInfo.FirstName} {_userInfo.LastName}".Trim();

    private readonly Regex _emailRegex = new Regex(@"(\S*@\S*\.\S*)");
    private readonly Regex _phoneRegex = new Regex(@"(\+?\d+)");

    private string _rootFolder;
    private UserInfo _userInfo;
    private bool _hasPhoto;
    private IAccount _currentUser;

    public void Init(string key, string rootFolder, Action<string, Exception> log, IAccount currentUser)
    {
        Key = key;
        _rootFolder = rootFolder;
        Log = log;
        _currentUser = currentUser;
    }

    public override void Parse()
    {
        _userInfo = new UserInfo()
        {
            Id = Guid.NewGuid()
        };

        ParseRootHtml();
        ParseProfile();
        ParseAccount();

        Action<string, Exception> log = (m, e) => { Log($"{DisplayName} ({Email}): {m}", e); };

        MigratingFiles = serviceProvider.GetService<GwsMigratingFiles>();
        MigratingFiles.Init(_rootFolder, this, log, _currentUser);
        MigratingFiles.Parse();

        _userInfo.UserName = _userInfo.Email.Split('@').First();
        if (string.IsNullOrEmpty(_userInfo.FirstName))
        {
            _userInfo.FirstName = _userInfo.Email.Split('@').First();
        }
        _userInfo.ActivationStatus = EmployeeActivationStatus.Activated;
    }

    public void DataСhange()
    {
        if (_userInfo.LastName == null)
        {
            _userInfo.LastName = "NOTPROVIDED";
        }
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }
        var saved = await userManager.GetUserByEmailAsync(_userInfo.Email);
        if (saved.Equals(ASC.Core.Users.Constants.LostUser) || saved.Removed)
        {
            if (string.IsNullOrWhiteSpace(_userInfo.FirstName))
            {
                _userInfo.FirstName = FilesCommonResource.UnknownFirstName;
            }
            if (string.IsNullOrWhiteSpace(_userInfo.LastName))
            {
                _userInfo.LastName = FilesCommonResource.UnknownLastName;
            }
            _userInfo.Id = Guid.Empty;
            saved = await userManager.SaveUserInfo(_userInfo, UserType);

            var groupId = UserType switch
            {
                EmployeeType.Collaborator => ASC.Core.Users.Constants.GroupCollaborator.ID,
                EmployeeType.DocSpaceAdmin => ASC.Core.Users.Constants.GroupAdmin.ID,
                EmployeeType.RoomAdmin => ASC.Core.Users.Constants.GroupManager.ID,
                _ => Guid.Empty,
            };

            if (groupId != Guid.Empty)
            {
                await userManager.AddUserIntoGroupAsync(saved.Id, groupId, true);
            }
            else if (UserType == EmployeeType.RoomAdmin)
            {
                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            }

            if (_hasPhoto)
            {
                await using var fs = File.OpenRead(Key);
                using var zip = new ZipArchive(fs);
                using var ms = new MemoryStream();
                await using (var imageStream = zip.GetEntry(string.Join("/", "Takeout", "Profile", "ProfilePhoto.jpg")).Open())
                {
                    await imageStream.CopyToAsync(ms);
                }
                await userManager.SaveUserPhotoAsync(saved.Id, ms.ToArray());
            }
        }
        _userInfo = saved;
    }

    private void ParseRootHtml()
    {
        var htmlFiles = Directory.GetFiles(_rootFolder, "*.html");
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

        _userInfo.Email = matches.Groups[1].Value;
    }

    private void ParseProfile()
    {
        var profilePath = Path.Combine(_rootFolder, "Profile", "Profile.json");
        if (!File.Exists(profilePath))
        {
            return;
        }

        var googleProfile = JsonSerializer.Deserialize<GwsProfile>(File.ReadAllText(profilePath));

        if (googleProfile.Birthday != null)
        {
            _userInfo.BirthDate = googleProfile.Birthday.Value.DateTime;
        }

        if (googleProfile.Gender != null)
        {
            switch (googleProfile.Gender.Type)
            {
                case "male":
                    _userInfo.Sex = true;
                    break;

                case "female":
                    _userInfo.Sex = false;
                    break;

                default:
                    _userInfo.Sex = null;
                    break;
            }
        }

        _userInfo.FirstName = googleProfile.Name.GivenName;
        _userInfo.LastName = googleProfile.Name.FamilyName;

        if (googleProfile.Emails != null)
        {
            foreach (var email in googleProfile.Emails.Distinct())
            {
                AddEmailToUser(_userInfo, email.Value);
            }
        }

        var profilePhotoPath = Path.Combine(_rootFolder, "Profile", "ProfilePhoto.jpg");
        _hasPhoto = File.Exists(profilePhotoPath);
    }

    private void ParseAccount()
    {
        var accountPath = Path.Combine(_rootFolder, "Google Account");
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
            AddEmailToUser(_userInfo, match.Value);
        }

        var contactEmail = _emailRegex.Match(doc.DocumentNode.SelectNodes("//div[@class='section'][3]/ul/li[1]")[0].InnerText);
        if (contactEmail.Success)
        {
            AddEmailToUser(_userInfo, contactEmail.Groups[1].Value);
        }

        var recoveryEmail = _emailRegex.Match(doc.DocumentNode.SelectNodes("//div[@class='section'][3]/ul/li[2]")[0].InnerText);
        if (recoveryEmail.Success)
        {
            AddEmailToUser(_userInfo, recoveryEmail.Groups[1].Value);
        }

        var recoverySms = _phoneRegex.Match(doc.DocumentNode.SelectNodes("//div[@class='section'][3]/ul/li[3]")[0].InnerText);
        if (recoverySms.Success)
        {
            AddPhoneToUser(_userInfo, recoverySms.Groups[1].Value);
        }
    }

    private void AddEmailToUser(UserInfo userInfo, string email)
    {
        if (userInfo.Email != email && !userInfo.Contacts.Contains(email))
        {
            userInfo.ContactsList.Add(email.EndsWith("@gmail.com") ? "gmail" : "mail"); // SocialContactsManager.ContactType_gmail in ASC.WebStudio
            userInfo.ContactsList.Add(email);
        }
    }

    private void AddPhoneToUser(UserInfo userInfo, string phone)
    {
        if (userInfo.MobilePhone != phone && !userInfo.Contacts.Contains(phone))
        {
            userInfo.ContactsList.Add("mobphone"); // SocialContactsManager.ContactType_mobphone in ASC.WebStudio
            userInfo.ContactsList.Add(phone);
        }
    }
}
