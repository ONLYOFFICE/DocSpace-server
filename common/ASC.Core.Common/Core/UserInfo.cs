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

namespace ASC.Core.Users;

/// <summary>
/// The user information.
/// </summary>
public sealed class UserInfo : IDirectRecipient, ICloneable, IMapFrom<User>
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user first name.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// The user username.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// The user birthday.
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// The user sex (male or female).
    /// </summary>
    public bool? Sex { get; set; }

    /// <summary>
    /// The user status.
    /// </summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// The user activation status.
    /// </summary>
    public EmployeeActivationStatus ActivationStatus { get; set; } = EmployeeActivationStatus.NotActivated;

    /// <summary>
    /// The date and time when the user account was terminated.
    /// </summary>
    public DateTime? TerminatedDate { get; set; }

    /// <summary>
    /// The user title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The user registration date.
    /// </summary>
    public DateTime? WorkFromDate { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    [EmailAddress]
    public string Email { get; set; }

    private string _contacts;

    /// <summary>
    /// The list of user contacts in the string format.
    /// </summary>
    public string Contacts
    {
        get => _contacts;
        set
        {
            _contacts = value;
            ContactsFromString(_contacts);
        }
    }

    /// <summary>
    /// The list of user contacts.
    /// </summary>
    public List<string> ContactsList { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// The user notes.
    /// </summary>
    public string Notes { get; set; }

    /// <summary>
    /// Specifies if the user account was removed or not.
    /// </summary>
    public bool Removed { get; set; }

    /// <summary>
    /// The date and time when the user account was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Specifies if the user is active or not.
    /// </summary>
    public bool IsActive => ActivationStatus.HasFlag(EmployeeActivationStatus.Activated);

    /// <summary>
    /// The user culture code.
    /// </summary>
    public string CultureName { get; set; }

    /// <summary>
    /// The user mobile phone.
    /// </summary>
    public string MobilePhone { get; set; }

    /// <summary>
    /// The user mobile phone activation status.
    /// </summary>
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// The LDAP user identificator.
    /// </summary>
    public string Sid { get; set; } // LDAP user identificator

    /// <summary>
    /// The LDAP user quota attribute.
    /// </summary>
    public long LdapQouta { get; init; } // LDAP user quota attribute

    /// <summary>
    /// The SSO SAML user identificator.
    /// </summary>
    public string SsoNameId { get; set; } // SSO SAML user identificator

    /// <summary>
    /// The SSO SAML user session identificator.
    /// </summary>
    public string SsoSessionId { get; set; } // SSO SAML user session identificator

    /// <summary>
    /// The date and time when the user account was created.
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// The ID of the user who created the current user account.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Specifies if tips, updates and offers are allowed to be sent to the user or not.
    /// </summary>
    public bool? Spam { get; set; }

    public override string ToString()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is UserInfo ui && Id.Equals(ui.Id);
    }

    public bool Equals(UserInfo obj)
    {
        return obj != null && Id.Equals(obj.Id);
    }

    public CultureInfo GetCulture()
    {
        return string.IsNullOrEmpty(CultureName) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(CultureName);
    }

    string[] IDirectRecipient.Addresses => !string.IsNullOrEmpty(Email) ? [Email] : [];
    public bool CheckActivation => !IsActive; /*if user already active we don't need activation*/
    string IRecipient.ID => Id.ToString();
    string IRecipient.Name => ToString();

    public object Clone()
    {
        return MemberwiseClone();
    }


    internal string ContactsToString()
    {
        if (ContactsList == null || ContactsList.Count == 0)
        {
            return null;
        }

        var sBuilder = new StringBuilder();
        foreach (var contact in ContactsList)
        {
            sBuilder.Append($"{contact}|");
        }

        return sBuilder.ToString();
    }

    internal UserInfo ContactsFromString(string contacts)
    {
        if (string.IsNullOrEmpty(contacts))
        {
            return this;
        }

        if (ContactsList == null)
        {
            ContactsList = [];
        }
        else
        {
            ContactsList.Clear();
        }

        ContactsList.AddRange(contacts.Split(['|'], StringSplitOptions.RemoveEmptyEntries));

        return this;
    }
}
