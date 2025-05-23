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

using Constants = ASC.Core.Users.Constants;

namespace ASC.ActiveDirectory.Base;
[Scope]
public class LdapUserImporter(
        ILogger<LdapUserImporter> logger,
        UserManager userManager,
        IConfiguration configuration,
        NovellLdapHelper novellLdapHelper,
        LdapObjectExtension ldapObjectExtension)
    : IDisposable
{
    public List<LdapObject> AllDomainUsers { get; private set; } = [];
    public List<LdapObject> AllDomainGroups { get; private set; } = [];

    public Dictionary<LdapObject, LdapSettingsStatus> AllSkipedDomainUsers { get; private set; } = new();
    public Dictionary<LdapObject, LdapSettingsStatus> AllSkipedDomainGroups { get; private set; } = new();

    private string _ldapDomain;
    private readonly string _unknownDomain = configuration["ldap:domain"] ?? "LDAP";
    public string LDAPDomain
    {
        get
        {
            if (!string.IsNullOrEmpty(_ldapDomain))
            {
                return _ldapDomain;
            }

            _ldapDomain = LoadLDAPDomain();

            if (string.IsNullOrEmpty(_ldapDomain))
            {
                _ldapDomain = _unknownDomain;
            }

            return _ldapDomain;
        }
    }
    public List<string> PrimaryGroupIds { get; set; }

    public LdapSettings Settings
    {
        get { return LdapHelper.Settings; }
    }

    public LdapHelper LdapHelper { get; private set; } = novellLdapHelper;
    public LdapLocalization Resource { get; private set; }

    private List<string> _watchedNestedGroups = [];

    private UserManager UserManager { get; set; } = userManager;

    public void Init(LdapSettings settings, LdapLocalization resource)
    {
        ((NovellLdapHelper)LdapHelper).Init(settings);
        Resource = resource;
    }

    public async Task<List<UserInfo>> GetDiscoveredUsersByAttributesAsync()
    {
        var users = new List<UserInfo>();

        if (AllDomainUsers.Count == 0 && !TryLoadLDAPUsers())
        {
            return users;
        }

        var usersToAdd = await AllDomainUsers.ToAsyncEnumerable().SelectAwait(async ldapObject => await ldapObjectExtension.ToUserInfoAsync(ldapObject, this)).ToListAsync();

        users.AddRange(usersToAdd);

        return users;
    }

    public List<GroupInfo> GetDiscoveredGroupsByAttributes()
    {
        if (!Settings.GroupMembership)
        {
            return [];
        }

        if (AllDomainGroups.Count == 0 && !TryLoadLDAPGroups())
        {
            return [];
        }

        var groups = new List<GroupInfo>();

        var groupsToAdd = AllDomainGroups.ConvertAll(g => ldapObjectExtension.ToGroupInfo(g, Settings));

        groups.AddRange(groupsToAdd);

        return groups;
    }

    public async Task<List<UserInfo>> GetGroupUsersAsync(GroupInfo groupInfo)
    {
        return await GetGroupUsersAsync(groupInfo, true);
    }

    private async Task<List<UserInfo>> GetGroupUsersAsync(GroupInfo groupInfo, bool clearCache)
    {
        if (!LdapHelper.IsConnected)
        {
            LdapHelper.Connect();
        }

        logger.DebugGetGroupUsers(groupInfo.Name);

        var users = new List<UserInfo>();

        if (AllDomainGroups.Count == 0 && !TryLoadLDAPGroups())
        {
            return users;
        }

        var domainGroup = AllDomainGroups.FirstOrDefault(lg => lg.Sid.Equals(groupInfo.Sid));

        if (domainGroup == null)
        {
            return users;
        }

        var members = ldapObjectExtension.GetAttributes(domainGroup, Settings.GroupAttribute);

        foreach (var member in members)
        {
            var ldapUser = FindUserByMember(member);

            if (ldapUser == null)
            {
                var nestedLdapGroup = FindGroupByMember(member);

                if (nestedLdapGroup != null)
                {
                    logger.DebugFoundNestedLdapGroup(nestedLdapGroup.DistinguishedName);

                    if (clearCache)
                    {
                        _watchedNestedGroups = [];
                    }

                    if (_watchedNestedGroups.Contains(nestedLdapGroup.DistinguishedName))
                    {
                        logger.DebugSkipAlreadyWatched(nestedLdapGroup.DistinguishedName);
                        continue;
                    }

                    _watchedNestedGroups.Add(nestedLdapGroup.DistinguishedName);

                    var nestedGroupInfo = ldapObjectExtension.ToGroupInfo(nestedLdapGroup, Settings);

                    var nestedGroupUsers = await GetGroupUsersAsync(nestedGroupInfo, false);

                    foreach (var groupUser in nestedGroupUsers)
                    {
                        if (!users.Exists(u => u.Sid == groupUser.Sid))
                        {
                            users.Add(groupUser);
                        }
                    }
                }

                continue;
            }

            var userInfo = await ldapObjectExtension.ToUserInfoAsync(ldapUser, this);

            if (!users.Exists(u => u.Sid == userInfo.Sid))
            {
                users.Add(userInfo);
            }
        }

        if (PrimaryGroupIds != null && PrimaryGroupIds.Any(id => domainGroup.Sid.EndsWith("-" + id)))
        {
            // Domain Users found
            var ldapUsers = FindUsersByPrimaryGroup(domainGroup.Sid);

            foreach (var ldapUser in ldapUsers)
            {
                var userInfo = await ldapObjectExtension.ToUserInfoAsync(ldapUser, this);

                if (!users.Exists(u => u.Sid == userInfo.Sid))
                {
                    users.Add(userInfo);
                }
            }
        }

        return users;
    }

    const string GROUP_MEMBERSHIP = "groupMembership";

    private List<LdapObject> GetLdapUserGroups(LdapObject ldapUser)
    {
        var ldapUserGroups = new List<LdapObject>();
        try
        {
            if (!Settings.GroupMembership)
            {
                return ldapUserGroups;
            }

            if (ldapUser == null ||
                string.IsNullOrEmpty(ldapUser.Sid))
            {
                return ldapUserGroups;
            }

            if (!LdapHelper.IsConnected)
            {
                LdapHelper.Connect();
            }

            var userGroups = ldapObjectExtension.GetAttributes(ldapUser, LdapConstants.ADSchemaAttributes.MEMBER_OF)
                .Select(LdapUtils.UnescapeLdapString)
                .ToList();

            if (userGroups.Count == 0)
            {
                userGroups = ldapObjectExtension.GetAttributes(ldapUser, GROUP_MEMBERSHIP);
            }

            var searchExpressions = new List<Expression>();

            var primaryGroupId = ldapUser.GetValue(LdapConstants.ADSchemaAttributes.PRIMARY_GROUP_ID) as string;

            if (!string.IsNullOrEmpty(primaryGroupId))
            {
                var userSid = ldapUser.Sid;
                var index = userSid.LastIndexOf("-", StringComparison.InvariantCultureIgnoreCase);

                if (index > -1)
                {
                    var primaryGroupSid = userSid[..(index + 1)] + primaryGroupId;
                    searchExpressions.Add(Expression.Equal(ldapUser.SidAttribute, primaryGroupSid));
                }
            }

            if (userGroups.Count != 0)
            {
                var cnRegex = new Regex(",[A-z]{2}=");
                searchExpressions.AddRange(userGroups
                    .Select(g => g[..cnRegex.Match(g).Index])
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(Expression.Parse)
                    .Where(e => e != null));

                var criteria = Criteria.Any(searchExpressions.ToArray());

                var foundList = LdapHelper.GetGroups(criteria);

                if (foundList.Count != 0)
                {
                    ldapUserGroups.AddRange(foundList);
                }
            }
            else
            {
                var ldapGroups = LdapHelper.GetGroups();

                ldapUserGroups.AddRange(
                    ldapGroups.Where(
                        ldapGroup =>
                            LdapHelper.UserExistsInGroup(ldapGroup, ldapUser, Settings)));
            }
        }
        catch (Exception ex)
        {
            if (ldapUser != null)
            {
                logger.ErrorIsUserExistInGroups(ldapUser.DistinguishedName, ldapUser.Sid, ex);
            }
        }

        return ldapUserGroups;
    }

    public async Task<IEnumerable<GroupInfo>> GetAndCheckCurrentGroupsAsync(LdapObject ldapUser, IEnumerable<GroupInfo> portalGroups)
    {
        var result = new List<GroupInfo>();
        try
        {
            var searchExpressions = new List<Expression>();
            if (portalGroups != null && portalGroups.Any())
            {
                searchExpressions.AddRange(portalGroups.Select(g => Expression.Equal(LdapConstants.ADSchemaAttributes.OBJECT_SID, g.Sid)));
            }
            else
            {
                return result;
            }

            var criteria = Criteria.Any(searchExpressions.ToArray());
            var foundList = LdapHelper.GetGroups(criteria);

            if (foundList.Count != 0)
            {
                var stillExistingGroups = portalGroups.Where(g => foundList.Any(fg => fg.Sid == g.Sid));

                foreach (var group in stillExistingGroups)
                {
                    if ((await GetGroupUsersAsync(group)).Any(u => u.Sid == ldapUser.Sid))
                    {
                        result.Add(group);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (ldapUser != null)
            {
                logger.ErrorGetAndCheckCurrentGroups(ldapUser.DistinguishedName, ldapUser.Sid, ex);
            }
        }
        return result;
    }

    public async Task<bool> TrySyncUserGroupMembership(Tuple<UserInfo, LdapObject> ldapUserInfo)
    {
        if (ldapUserInfo == null ||
            !Settings.GroupMembership)
        {
            return false;
        }

        var userInfo = ldapUserInfo.Item1;
        var ldapUser = ldapUserInfo.Item2;

        var portalUserLdapGroups =
            (await UserManager.GetUserGroupsAsync(userInfo.Id, IncludeType.All))
                .Where(g => !string.IsNullOrEmpty(g.Sid))
                .ToList();

        var ldapUserGroupList = new List<LdapObject>();

        ldapUserGroupList.AddRange(GetLdapUserGroups(ldapUser));

        if (!LdapHelper.IsConnected)
        {
            LdapHelper.Connect();
        }

        var actualPortalLdapGroups = (await GetAndCheckCurrentGroupsAsync(ldapUser, portalUserLdapGroups)).ToList();

        foreach (var ldapUserGroup in ldapUserGroupList)
        {
            var groupInfo = await UserManager.GetGroupInfoBySidAsync(ldapUserGroup.Sid);

            if (Equals(groupInfo, Constants.LostGroupInfo))
            {
                logger.DebugTrySyncUserGroupMembershipCreatingPortalGroup(ldapUserGroup.DistinguishedName, ldapUserGroup.Sid);
                 groupInfo = await UserManager.SaveGroupInfoAsync(ldapObjectExtension.ToGroupInfo(ldapUserGroup, Settings));

                logger.DebugTrySyncUserGroupMembershipAddingUserToGroup(userInfo.UserName, ldapUser.Sid, groupInfo.Name, groupInfo.Sid);
                await UserManager.AddUserIntoGroupAsync(userInfo.Id, groupInfo.ID);
            }
            else if (!portalUserLdapGroups.Contains(groupInfo))
            {
                logger.DebugTrySyncUserGroupMembershipAddingUserToGroup(userInfo.UserName, ldapUser.Sid, groupInfo.Name, groupInfo.Sid);
                await UserManager.AddUserIntoGroupAsync(userInfo.Id, groupInfo.ID);
            }

            actualPortalLdapGroups.Add(groupInfo);
        }

        foreach (var portalUserLdapGroup in portalUserLdapGroups)
        {
            if (!actualPortalLdapGroups.Contains(portalUserLdapGroup))
            {
                logger.DebugTrySyncUserGroupMembershipRemovingUserFromGroup(userInfo.UserName, ldapUser.Sid, portalUserLdapGroup.Name, portalUserLdapGroup.Sid);
                await UserManager.RemoveUserFromGroupAsync(userInfo.Id, portalUserLdapGroup.ID);
            }
        }

        return actualPortalLdapGroups.Count != 0;
    }

    public bool TryLoadLDAPUsers()
    {
        try
        {
            if (!Settings.EnableLdapAuthentication)
            {
                return false;
            }

            if (!LdapHelper.IsConnected)
            {
                LdapHelper.Connect();
            }

            var users = LdapHelper.GetUsers();

            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.Sid))
                {
                    AllSkipedDomainUsers.Add(user, LdapSettingsStatus.WrongSidAttribute);
                    continue;
                }

                if (!CheckLoginAttribute(user, Settings.LoginAttribute))
                {
                    AllSkipedDomainUsers.Add(user, LdapSettingsStatus.WrongLoginAttribute);
                    continue;
                }

                if (!Settings.GroupMembership)
                {
                    AllDomainUsers.Add(user);
                    continue;
                }

                if (!Settings.UserAttribute.Equals(LdapConstants.RfcLDAPAttributes.DN,
                    StringComparison.InvariantCultureIgnoreCase) && !CheckUserAttribute(user, Settings.UserAttribute))
                {
                    AllSkipedDomainUsers.Add(user, LdapSettingsStatus.WrongUserAttribute);
                    continue;
                }

                AllDomainUsers.Add(user);
            }

            if (AllDomainUsers.Count != 0)
            {
                PrimaryGroupIds = AllDomainUsers.Select(u => u.GetValue(LdapConstants.ADSchemaAttributes.PRIMARY_GROUP_ID)).Cast<string>()
                    .Distinct().ToList();
            }

            return AllDomainUsers.Count != 0 || users.Count == 0;
        }
        catch (ArgumentException)
        {
            logger.ErrorTryLoadLDAPUsersIncorrectUserFilter(Settings.UserFilter);
        }

        return false;
    }

    public bool TryLoadLDAPGroups()
    {
        try
        {
            if (!Settings.EnableLdapAuthentication || !Settings.GroupMembership)
            {
                return false;
            }

            if (!LdapHelper.IsConnected)
            {
                LdapHelper.Connect();
            }

            var groups = LdapHelper.GetGroups();

            foreach (var group in groups)
            {
                if (string.IsNullOrEmpty(group.Sid))
                {
                    AllSkipedDomainGroups.Add(group, LdapSettingsStatus.WrongSidAttribute);
                    continue;
                }

                if (!CheckGroupAttribute(group, Settings.GroupAttribute))
                {
                    AllSkipedDomainGroups.Add(group, LdapSettingsStatus.WrongGroupAttribute);
                    continue;
                }

                if (!CheckGroupNameAttribute(group, Settings.GroupNameAttribute))
                {
                    AllSkipedDomainGroups.Add(group, LdapSettingsStatus.WrongGroupNameAttribute);
                    continue;
                }

                AllDomainGroups.Add(group);
            }

            return AllDomainGroups.Count != 0 || groups.Count == 0;
        }
        catch (ArgumentException)
        {
            logger.ErrorTryLoadLDAPUsersIncorrectGroupFilter(Settings.GroupFilter);
        }

        return false;
    }

    private string LoadLDAPDomain()
    {
        try
        {
            if (!Settings.EnableLdapAuthentication)
            {
                return null;
            }

            if (!LdapHelper.IsConnected)
            {
                LdapHelper.Connect();
            }

            string ldapDomain;

            if (AllDomainUsers.Count != 0)
            {
                ldapDomain = ldapObjectExtension.GetDomainFromDn(AllDomainUsers.First());

                if (!string.IsNullOrEmpty(ldapDomain))
                {
                    return ldapDomain;
                }
            }

            ldapDomain = LdapHelper.SearchDomain();

            if (!string.IsNullOrEmpty(ldapDomain))
            {
                return ldapDomain;
            }

            ldapDomain = LdapUtils.DistinguishedNameToDomain(Settings.UserDN);

            if (!string.IsNullOrEmpty(ldapDomain))
            {
                return ldapDomain;
            }

            ldapDomain = LdapUtils.DistinguishedNameToDomain(Settings.GroupDN);

            if (!string.IsNullOrEmpty(ldapDomain))
            {
                return ldapDomain;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorLoadLDAPDomain(ex);
        }

        return null;
    }

    protected bool CheckLoginAttribute(LdapObject user, string loginAttribute)
    {
        try
        {
            var member = user.GetValue(loginAttribute);
            if (member == null || string.IsNullOrWhiteSpace(member.ToString()))
            {
                logger.DebugLoginAttributeParameterNotFound(Settings.LoginAttribute, user.DistinguishedName);
                return false;
            }
        }
        catch (Exception e)
        {
            logger.ErrorLoginAttributeParameterNotFound(Settings.LoginAttribute, loginAttribute, e);
            return false;
        }
        return true;
    }

    protected bool CheckUserAttribute(LdapObject user, string userAttr)
    {
        try
        {
            var userAttribute = user.GetValue(userAttr);
            if (userAttribute == null || string.IsNullOrWhiteSpace(userAttribute.ToString()))
            {
                logger.DebugUserAttributeParameterNotFound(Settings.UserAttribute,
                    user.DistinguishedName);
                return false;
            }
        }
        catch (Exception e)
        {
            logger.ErrorUserAttributeParameterNotFound(Settings.UserAttribute, userAttr, e);
            return false;
        }
        return true;
    }

    protected bool CheckGroupAttribute(LdapObject group, string groupAttr)
    {
        try
        {
            group.GetValue(groupAttr); // Group attribute can be empty - example => Domain users
        }
        catch (Exception e)
        {
            logger.ErrorGroupAttributeParameterNotFound(Settings.GroupAttribute, groupAttr, e);
            return false;
        }
        return true;
    }

    protected bool CheckGroupNameAttribute(LdapObject group, string groupAttr)
    {
        try
        {
            var groupNameAttribute = group.GetValues(groupAttr);
            if (groupNameAttribute.Count == 0)
            {
                logger.DebugGroupNameAttributeParameterNotFound(Settings.GroupNameAttribute,
                    groupAttr);
                return false;
            }
        }
        catch (Exception e)
        {
            logger.ErrorGroupAttributeParameterNotFound(Settings.GroupNameAttribute,
                groupAttr, e);
            return false;
        }
        return true;
    }

    private List<LdapObject> FindUsersByPrimaryGroup(string sid)
    {
        logger.DebugFindUsersByPrimaryGroup();

        if (AllDomainUsers.Count == 0 && !TryLoadLDAPUsers())
        {
            return null;
        }

        return
            AllDomainUsers.Where(
                lu =>
                {
                    var primaryGroupId = lu.GetValue(LdapConstants.ADSchemaAttributes.PRIMARY_GROUP_ID) as string;

                    return !string.IsNullOrEmpty(primaryGroupId) &&
                           sid.EndsWith(primaryGroupId);
                })
                .ToList();

    }

    private LdapObject FindUserByMember(string userAttributeValue)
    {
        if (AllDomainUsers.Count == 0 && !TryLoadLDAPUsers())
        {
            return null;
        }

        logger.DebugFindUserByMember(userAttributeValue);

        return AllDomainUsers.FirstOrDefault(u =>
            u.DistinguishedName.Equals(userAttributeValue, StringComparison.InvariantCultureIgnoreCase)
            || Convert.ToString(u.GetValue(Settings.UserAttribute)).Equals(userAttributeValue,
                StringComparison.InvariantCultureIgnoreCase));
    }

    private LdapObject FindGroupByMember(string member)
    {
        if (AllDomainGroups.Count == 0 && !TryLoadLDAPGroups())
        {
            return null;
        }

        logger.DebugFindGroupByMember(member);

        return AllDomainGroups.FirstOrDefault(g =>
            g.DistinguishedName.Equals(member, StringComparison.InvariantCultureIgnoreCase));
    }

    public async Task<List<Tuple<UserInfo, LdapObject>>> FindLdapUsersAsync(string login)
    {
        var listResults = new List<Tuple<UserInfo, LdapObject>>();

        var ldapLogin = LdapLogin.ParseLogin(login);

        if (ldapLogin == null)
        {
            return listResults;
        }

        if (!LdapHelper.IsConnected)
        {
            LdapHelper.Connect();
        }

        var exps = new List<Expression> { Expression.Equal(Settings.LoginAttribute, ldapLogin.Username) };

        if (!ldapLogin.Username.Equals(login) && ldapLogin.ToString().Equals(login))
        {
            exps.Add(Expression.Equal(Settings.LoginAttribute, login));
        }

        string email = null;

        if (!string.IsNullOrEmpty(Settings.MailAttribute) && !string.IsNullOrEmpty(ldapLogin.Domain) && login.Contains('@'))
        {
            email = ldapLogin.ToString();
            exps.Add(Expression.Equal(Settings.MailAttribute, email));
        }

        var searchTerm = exps.Count > 1 ? Criteria.Any(exps.ToArray()).ToString() : exps.First().ToString();

        var users = await LdapHelper.GetUsers(searchTerm, !string.IsNullOrEmpty(email) ? -1 : 1).ToAsyncEnumerable()
            .Where(user => user != null)
            .ToLookupAwaitAsync(async lu =>
            {
                var ui = Constants.LostUser;

                try
                {
                    if (string.IsNullOrEmpty(_ldapDomain))
                    {
                        _ldapDomain = LdapUtils.DistinguishedNameToDomain(lu.DistinguishedName);
                    }

                    ui = await ldapObjectExtension.ToUserInfoAsync(lu, this);
                }
                catch (Exception ex)
                {
                    logger.ErrorToUserInfo(ex);
                }

                return Tuple.Create(ui, lu);

            });

        if (users.Count == 0)
        {
            return listResults;
        }

        foreach (var user in users)
        {
            var ui = user.Key.Item1;

            if (ui.Equals(Constants.LostUser))
            {
                continue;
            }

            var ul = user.Key.Item2;

            var ldapLoginAttribute = ul.GetValue(Settings.LoginAttribute) as string;

            if (string.IsNullOrEmpty(ldapLoginAttribute))
            {
                logger.WarnLoginAttributeIsEmpty(ul.DistinguishedName, Settings.LoginAttribute);
                continue;
            }

            if (ldapLoginAttribute.Equals(login))
            {
                listResults.Add(user.Key);
                continue;
            }

            if (!string.IsNullOrEmpty(email))
            {
                if (ui.Email.Equals(email, StringComparison.InvariantCultureIgnoreCase))
                {
                    listResults.Add(user.Key);
                    continue;
                }
            }

            if (LdapUtils.IsLoginAccepted(ldapLogin, ui, LDAPDomain))
            {
                listResults.Add(user.Key);
            }
        }

        return listResults;
    }

    public List<LdapObject> FindUsersByAttribute(string key, string value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        var users = new List<LdapObject>();

        if (AllDomainUsers.Count == 0 && !TryLoadLDAPUsers())
        {
            return users;
        }

        return users.Where(us => !us.IsDisabled && string.Equals((string)us.GetValue(key), value, comparison)).ToList();
    }

    public List<LdapObject> FindUsersByAttribute(string key, IEnumerable<string> value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        var users = new List<LdapObject>();

        if (AllDomainUsers.Count == 0 && !TryLoadLDAPUsers())
        {
            return users;
        }

        return AllDomainUsers.Where(us => !us.IsDisabled && value.Any(val => string.Equals(val, (string)us.GetValue(key), comparison))).ToList();
    }

    public List<LdapObject> FindGroupsByAttribute(string key, string value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        var gr = new List<LdapObject>();

        if (AllDomainGroups.Count == 0 && !TryLoadLDAPGroups())
        {
            return gr;
        }

        return gr.Where(g => !g.IsDisabled && string.Equals((string)g.GetValue(key), value, comparison)).ToList();
    }

    public List<LdapObject> FindGroupsByAttribute(string key, IEnumerable<string> value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        var gr = new List<LdapObject>();

        if (AllDomainGroups.Count == 0 && !TryLoadLDAPGroups())
        {
            return gr;
        }

        return AllDomainGroups.Where(g => !g.IsDisabled && value.Any(val => string.Equals(val, (string)g.GetValue(key), comparison))).ToList();
    }

    public async Task<Tuple<UserInfo, LdapObject>> LoginAsync(string login, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var ldapUsers = await FindLdapUsersAsync(login);

            logger.DebugFindLdapUsers(login, ldapUsers.Count);

            foreach (var (ldapUserInfo, ldapUserObject) in ldapUsers)
            {
                string currentLogin = null;
                try
                {
                    if (ldapUserInfo.Equals(Constants.LostUser) || ldapUserObject == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(ldapUserObject.DistinguishedName) || string.IsNullOrEmpty(ldapUserObject.Sid))
                    {
                        logger.DebugLdapUserImporterFailed(login, ldapUserObject.Sid);
                        continue;
                    }

                    currentLogin = ldapUserObject.DistinguishedName;

                    logger.DebugLdapUserImporterLogin(currentLogin);

                    LdapHelper.CheckCredentials(currentLogin, password, Settings.Server,
                        Settings.PortNumber, Settings.StartTls, Settings.Ssl, Settings.AcceptCertificate,
                        Settings.AcceptCertificateHash);

                    return new Tuple<UserInfo, LdapObject>(ldapUserInfo, ldapUserObject);
                }
                catch (Exception ex)
                {
                    logger.ErrorLdapUserImporterLoginFailed(currentLogin ?? login, ex);
                }
            }
        }
        catch (Exception ex)
        {
            logger.ErrorLdapUserImporterLoginFailed(login, ex);
        }

        return null;
    }

    public void Dispose()
    {
        LdapHelper?.Dispose();
    }
}
