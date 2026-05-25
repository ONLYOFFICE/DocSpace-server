// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Core.Users;

/// <summary>
/// The user information.
/// </summary>
public sealed class UserInfo : IDirectRecipient, ICloneable
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The user's first name.
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; }

    /// <summary>
    /// The user's last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; }

    /// <summary>
    /// The user username.
    /// </summary>
    /// <example>johndoe</example>
    public string UserName { get; set; }

    /// <summary>
    /// The user birthday.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// The user sex (male or female).
    /// </summary>
    /// <example>true</example>
    public bool? Sex { get; set; }

    /// <summary>
    /// The user status.
    /// </summary>
    /// <example>1</example>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// The user activation status.
    /// </summary>
    /// <example>0</example>
    public EmployeeActivationStatus ActivationStatus { get; set; } = EmployeeActivationStatus.NotActivated;

    /// <summary>
    /// The date and time when the user account was terminated.
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    public DateTime? TerminatedDate { get; set; }

    /// <summary>
    /// The user title.
    /// </summary>
    /// <example>Manager</example>
    public string Title { get; set; }

    /// <summary>
    /// The user registration date.
    /// </summary>
    /// <example>2020-01-15T00:00:00Z</example>
    public DateTime? WorkFromDate { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// The list of user contacts in the string format.
    /// </summary>
    /// <example>skype:johndoe|telegram:@johndoe</example>
    public string Contacts
    {
        get;
        set
        {
            field = value;
            ContactsFromString(field);
        }
    }

    /// <summary>
    /// The list of user contacts.
    /// </summary>
    /// <example>["skype:johndoe", "telegram:@johndoe"]</example>
    public List<string> ContactsList { get; set; }

    /// <summary>
    /// The user location.
    /// </summary>
    /// <example>New York, USA</example>
    public string Location { get; set; }

    /// <summary>
    /// The user notes.
    /// </summary>
    /// <example>Additional information about the user</example>
    public string Notes { get; set; }

    /// <summary>
    /// Specifies if the user account was removed or not.
    /// </summary>
    /// <example>false</example>
    public bool Removed { get; set; }

    /// <summary>
    /// The date and time when the user account was last modified.
    /// </summary>
    /// <example>2025-02-08T10:30:00Z</example>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; set; }

    /// <summary>
    /// Specifies if the user is active or not.
    /// </summary>
    /// <example>true</example>
    public bool IsActive => ActivationStatus.HasFlag(EmployeeActivationStatus.Activated);

    /// <summary>
    /// The user culture code.
    /// </summary>
    /// <example>en-US</example>
    public string CultureName { get; set; }

    /// <summary>
    /// The user mobile phone.
    /// </summary>
    /// <example>+1234567890</example>
    public string MobilePhone { get; set; }

    /// <summary>
    /// The user mobile phone activation status.
    /// </summary>
    /// <example>0</example>
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// The LDAP user identifier.
    /// </summary>
    /// <example>S-1-5-21-3623811015-3361044348-30300820-1013</example>
    public string Sid { get; set; } // LDAP user identifier

    /// <summary>
    /// The LDAP user quota attribute.
    /// </summary>
    /// <example>1073741824</example>
    public long LdapQouta { get; init; } // LDAP user quota attribute

    /// <summary>
    /// The SSO SAML user identifier.
    /// </summary>
    /// <example>johndoe@example.com</example>
    public string SsoNameId { get; set; } // SSO SAML user identifier

    /// <summary>
    /// The SSO SAML user session identifier.
    /// </summary>
    /// <example>_1a2b3c4d5e6f7g8h9i0j</example>
    public string SsoSessionId { get; set; } // SSO SAML user session identifier

    /// <summary>
    /// The date and time when the user account was created.
    /// </summary>
    /// <example>2020-01-15T00:00:00Z</example>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// The ID of the user who created the current user account.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Specifies if tips, updates and offers are allowed to be sent to the user or not.
    /// </summary>
    /// <example>false</example>
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

    /// <summary>
    /// Indicates whether the activation status of the employee or recipient is unchecked or inactive.
    /// Depending on the context, this property evaluates the activation or eligibility status accordingly.
    /// </summary>
    /// <example>false</example>
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

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class UserInfoMapper
{
    public static partial User Map(this UserInfo source);
    public static partial UserInfo Map(this User source);
    public static partial List<UserInfo> Map(this List<User> source);
    public static partial IQueryable<UserInfo> Project(this IQueryable<User> source);
}