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

using System.Runtime.InteropServices;

namespace ASC.ActiveDirectory.Base.Settings;

/// <summary>
/// The LDAP settings parameters.
/// </summary>
[Scope]
public class LdapSettings : ISettings<LdapSettings>, ICloneable
{
    /// <summary>
    /// The LDAP settings ID.
    /// </summary>
    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{197149b3-fbc9-44c2-b42a-232f7e729c16}"); }
    }

    ///<summary>
    /// The LDAP settings mapping.
    /// </summary>
    public enum MappingFields
    {
        FirstNameAttribute,
        SecondNameAttribute,
        BirthDayAttribute,
        GenderAttribute,
        MobilePhoneAttribute,
        MailAttribute,
        TitleAttribute,
        LocationAttribute,
        AvatarAttribute,

        AdditionalPhone,
        AdditionalMobilePhone,
        AdditionalMail,
        Skype,

        UserQuotaLimit
    }

    /// <summary>The access rights type.</summary>
    public enum AccessRight
    {
        FullAccess,
        Documents,
        Projects,
        CRM,
        Community,
        People,
        Mail
    }

    public static readonly Dictionary<AccessRight, Guid> AccessRightsGuids = new()
        {
            { AccessRight.FullAccess, Guid.Empty },
            { AccessRight.Documents, WebItemManager.DocumentsProductID },
            { AccessRight.Projects, WebItemManager.ProjectsProductID },
            { AccessRight.CRM, WebItemManager.CRMProductID },
            { AccessRight.Community, WebItemManager.CommunityProductID },
            { AccessRight.People, WebItemManager.PeopleProductID },
            { AccessRight.Mail, WebItemManager.MailProductID }
        };

    public LdapSettings GetDefault()
    {
        var isNotWindows = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        var settings = new LdapSettings
        {
            Server = "",
            UserDN = "",
            PortNumber = LdapConstants.STANDART_LDAP_PORT,
            UserFilter = string.Format("({0}=*)",
                isNotWindows
                    ? LdapConstants.RfcLDAPAttributes.UID
                    : LdapConstants.ADSchemaAttributes.USER_PRINCIPAL_NAME),
            LoginAttribute = isNotWindows
                ? LdapConstants.RfcLDAPAttributes.UID
                : LdapConstants.ADSchemaAttributes.ACCOUNT_NAME,
            FirstNameAttribute = LdapConstants.ADSchemaAttributes.FIRST_NAME,
            SecondNameAttribute = LdapConstants.ADSchemaAttributes.SURNAME,
            MailAttribute = LdapConstants.ADSchemaAttributes.MAIL,
            TitleAttribute = LdapConstants.ADSchemaAttributes.TITLE,
            MobilePhoneAttribute = LdapConstants.ADSchemaAttributes.MOBILE,
            LocationAttribute = LdapConstants.ADSchemaAttributes.STREET,
            GroupDN = "",
            GroupFilter = string.Format("({0}={1})", LdapConstants.ADSchemaAttributes.OBJECT_CLASS,
                isNotWindows
                    ? LdapConstants.ObjectClassKnowedValues.POSIX_GROUP
                    : LdapConstants.ObjectClassKnowedValues.GROUP),
            UserAttribute =
                isNotWindows
                    ? LdapConstants.RfcLDAPAttributes.UID
                    : LdapConstants.ADSchemaAttributes.DISTINGUISHED_NAME,
            GroupAttribute = isNotWindows ? LdapConstants.RfcLDAPAttributes.MEMBER_UID : LdapConstants.ADSchemaAttributes.MEMBER,
            GroupNameAttribute = LdapConstants.ADSchemaAttributes.COMMON_NAME,
            Authentication = true,
            AcceptCertificate = false,
            AcceptCertificateHash = null,
            StartTls = false,
            Ssl = false,
            SendWelcomeEmail = false,
            DisableEmailVerification = false
        };

        return settings;
    }

    public override bool Equals(object obj)
    {
        var settings = obj as LdapSettings;

        return settings != null
               && EnableLdapAuthentication == settings.EnableLdapAuthentication
               && StartTls == settings.StartTls
               && Ssl == settings.Ssl
               && SendWelcomeEmail == settings.SendWelcomeEmail
               && DisableEmailVerification == settings.DisableEmailVerification
               && (string.IsNullOrEmpty(Server)
                   && string.IsNullOrEmpty(settings.Server)
                   || Server == settings.Server)
               && (string.IsNullOrEmpty(UserDN)
                   && string.IsNullOrEmpty(settings.UserDN)
                   || UserDN == settings.UserDN)
               && PortNumber == settings.PortNumber
               && UserFilter == settings.UserFilter
               && LoginAttribute == settings.LoginAttribute
               && LdapMapping.Count == settings.LdapMapping.Count
               && LdapMapping.All(pair => settings.LdapMapping.ContainsKey(pair.Key)
                   && pair.Value == settings.LdapMapping[pair.Key])
               && AccessRights.Count == settings.AccessRights.Count
               && AccessRights.All(pair => settings.AccessRights.ContainsKey(pair.Key)
                   && pair.Value == settings.AccessRights[pair.Key])
               && GroupMembership == settings.GroupMembership
               && (string.IsNullOrEmpty(GroupDN)
                   && string.IsNullOrEmpty(settings.GroupDN)
                   || GroupDN == settings.GroupDN)
               && GroupFilter == settings.GroupFilter
               && UserAttribute == settings.UserAttribute
               && GroupAttribute == settings.GroupAttribute
               && (string.IsNullOrEmpty(Login)
                   && string.IsNullOrEmpty(settings.Login)
                   || Login == settings.Login)
               && Authentication == settings.Authentication;
    }

    public override int GetHashCode()
    {
        var hash = 3;
        hash = (hash * 2) + EnableLdapAuthentication.GetHashCode();
        hash = (hash * 2) + StartTls.GetHashCode();
        hash = (hash * 2) + Ssl.GetHashCode();
        hash = (hash * 2) + SendWelcomeEmail.GetHashCode();
        hash = (hash * 2) + DisableEmailVerification.GetHashCode();
        hash = (hash * 2) + Server.GetHashCode();
        hash = (hash * 2) + UserDN.GetHashCode();
        hash = (hash * 2) + PortNumber.GetHashCode();
        hash = (hash * 2) + UserFilter.GetHashCode();
        hash = (hash * 2) + LoginAttribute.GetHashCode();
        hash = (hash * 2) + GroupMembership.GetHashCode();
        hash = (hash * 2) + GroupDN.GetHashCode();
        hash = (hash * 2) + GroupNameAttribute.GetHashCode();
        hash = (hash * 2) + GroupFilter.GetHashCode();
        hash = (hash * 2) + UserAttribute.GetHashCode();
        hash = (hash * 2) + GroupAttribute.GetHashCode();
        hash = (hash * 2) + Authentication.GetHashCode();
        hash = (hash * 2) + Login.GetHashCode();

        foreach (var pair in LdapMapping)
        {
            hash = (hash * 2) + pair.Value.GetHashCode();
        }

        foreach (var pair in AccessRights)
        {
            hash = (hash * 2) + pair.Value.GetHashCode();
        }

        return hash;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    /// <summary>
    /// Specifies whether the LDAP authentication is active in the system.
    /// </summary>
    public bool EnableLdapAuthentication { get; set; }

    /// <summary>
    /// Specifies whether the StartTLS (Transport Layer Security) protocol for secure LDAP communication is enabled or not.
    ///  </summary>
    public bool StartTls { get; set; }

    /// <summary>
    /// Specifies whether the SSL (Secure Sockets Layer) encryption is enabled for the LDAP communication or not.
    /// </summary>
    public bool Ssl { get; set; }

    /// <summary>
    /// Specifies whether the automatic welcome email dispatch to the new LDAP users is enabled or not.
    /// </summary>
    public bool SendWelcomeEmail { get; set; }

    /// <summary>
    /// Specifies if the email verification requirement is enabled for the LDAP users or not.
    /// </summary>
    public bool DisableEmailVerification { get; set; }

    /// <summary>
    /// The LDAP server's hostname or IP address.
    /// </summary>
    public string Server { get; set; }

    /// <summary>
    /// The absolute path to the top level directory containing users for the import.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string UserDN { get; set; }

    /// <summary>
    /// The network port number for the LDAP server connection.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int PortNumber { get; set; }

    /// <summary>
    /// The user filter value to import the users who correspond to the specified search criteria. The default filter value (uid=*) allows importing all users.
    /// </summary>
    public string UserFilter { get; set; }

    /// <summary>
    /// The attribute in a user record that corresponds to the login that LDAP server users will use to log in to ONLYOFFICE.
    /// </summary>
    public string LoginAttribute { get; set; }

    /// <summary>
    /// The correspondence between the user data fields on the portal and the attributes in the LDAP server user record.
    /// </summary>
    public Dictionary<MappingFields, string> LdapMapping { get; set; } = new();

    /// <summary>
    /// The group access rights.
    /// </summary>
    //ToDo: use SId instead of group name
    public Dictionary<AccessRight, string> AccessRights { get; set; } = new();

    /// <summary>
    /// The attribute in a user record that corresponds to the user's first name.
    /// </summary>
    public string FirstNameAttribute
    {
        get
        {
            return GetOldSetting(MappingFields.FirstNameAttribute);
        }

        set
        {
            SetOldSetting(MappingFields.FirstNameAttribute, value);
        }
    }

    /// <summary>
    /// The attribute in a user record that corresponds to the user's second name.
    /// </summary>
    public string SecondNameAttribute
    {
        get
        {
            return GetOldSetting(MappingFields.SecondNameAttribute);
        }

        set
        {
            SetOldSetting(MappingFields.SecondNameAttribute, value);
        }
    }

    /// <summary>
    /// The attribute in a user record that corresponds to the user's email address.
    /// </summary>
    public string MailAttribute
    {
        get
        {
            return GetOldSetting(MappingFields.MailAttribute);
        }

        set
        {
            SetOldSetting(MappingFields.MailAttribute, value);
        }
    }

    /// <summary>
    /// The attribute in a user record that corresponds to the user's title.
    /// </summary>
    public string TitleAttribute
    {
        get
        {
            return GetOldSetting(MappingFields.TitleAttribute);
        }

        set
        {
            SetOldSetting(MappingFields.TitleAttribute, value);
        }
    }

    /// <summary>
    /// The attribute in a user record that corresponds to the user's mobile phone number.
    /// </summary>
    public string MobilePhoneAttribute
    {
        get
        {
            return GetOldSetting(MappingFields.MobilePhoneAttribute);
        }

        set
        {
            SetOldSetting(MappingFields.MobilePhoneAttribute, value);
        }
    }

    /// <summary>
    /// The attribute in a user record that corresponds to the user's location.
    /// </summary>
    public string LocationAttribute
    {
        get
        {
            return GetOldSetting(MappingFields.LocationAttribute);
        }

        set
        {
            SetOldSetting(MappingFields.LocationAttribute, value);
        }
    }

    /// <summary>
    /// Specifies if the groups from the LDAP server are added to the portal or not.
    /// </summary>
    public bool GroupMembership { get; set; }

    /// <summary>
    /// The absolute path to the top level directory containing groups for the import.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string GroupDN { get; set; }

    /// <summary>
    /// The attribute that corresponds to a name of the group where the user is included.
    /// </summary>
    public string GroupNameAttribute { get; set; }

    /// <summary>
    /// The group filter value to import the groups who correspond to the specified search criteria. The default filter value (objectClass=posixGroup) allows importing all groups.
    /// </summary>
    public string GroupFilter { get; set; }

    /// <summary>
    /// The attribute that determines whether the user is a member of the groups.
    /// </summary>
    public string UserAttribute { get; set; }

    /// <summary>
    /// The attribute that specifies the users that the group includes.
    /// </summary>
    public string GroupAttribute { get; set; }

    /// <summary>
    /// Specifies if the user has rights to read data from the LDAP server or not.
    /// </summary>
    public bool Authentication { get; set; }

    /// <summary>
    /// The username for the LDAP server authentication.
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// The password for the LDAP server authentication.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// The password for the LDAP server in bytes.
    /// </summary>
    public byte[] PasswordBytes { get; set; }

    /// <summary>
    /// Specifies if the default LDAP settings are used or not.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Specifies whether the SSL certificate is accepted or not.
    /// </summary>
    public bool AcceptCertificate { get; set; }

    /// <summary>
    /// The hash that is used to accept the SSL certificate.
    /// </summary>
    public string AcceptCertificateHash { get; set; }

    /// <summary>
    /// The default user type assigned to the imported LDAP users.
    /// </summary>
    public EmployeeType UsersType { get; set; }

    private string GetOldSetting(MappingFields field)
    {
        LdapMapping ??= new Dictionary<MappingFields, string>();

        return LdapMapping.GetValueOrDefault(field, "");
    }
    private void SetOldSetting(MappingFields field, string value)
    {
        LdapMapping ??= new Dictionary<MappingFields, string>();

        if (string.IsNullOrEmpty(value))
        {
            if (LdapMapping.ContainsKey(field))
            {
                LdapMapping.Remove(field);
            }
            return;
        }

        LdapMapping[field] = value;
    }
}

[Scope]
public class LdapCronSettings : ISettings<LdapCronSettings>
{
    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{58C42C54-56CD-4BEF-A3ED-C60ACCF6E975}"); }
    }

    public LdapCronSettings GetDefault()
    {
        return new LdapCronSettings
        {
            Cron = null
        };
    }

    public string Cron { get; set; }
}

public class LdapCurrentAcccessSettings : ISettings<LdapCurrentAcccessSettings>
{
    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{134B5EAA-F612-4834-AEAB-34C90515EA4E}"); }
    }

    public LdapCurrentAcccessSettings GetDefault()
    {
        return new LdapCurrentAcccessSettings { CurrentAccessRights = null };
    }

    public Dictionary<LdapSettings.AccessRight, List<string>> CurrentAccessRights { get; set; } = new();
}

public class LdapCurrentUserPhotos : ISettings<LdapCurrentUserPhotos>
{
    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{50AE3C2B-0783-480F-AF30-679D0F0A2D3E}"); }
    }

    public LdapCurrentUserPhotos GetDefault()
    {
        return new LdapCurrentUserPhotos { CurrentPhotos = null };
    }

    public Dictionary<Guid, string> CurrentPhotos { get; set; } = new();
}

public class LdapCurrentDomain : ISettings<LdapCurrentDomain>
{
    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{75A5F745-F697-4418-B38D-0FE0D277E258}"); }
    }

    public LdapCurrentDomain GetDefault()
    {
        return new LdapCurrentDomain { CurrentDomain = null };
    }

    public string CurrentDomain { get; set; }
}
