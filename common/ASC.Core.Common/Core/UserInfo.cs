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

namespace ASC.Core.Users;

public sealed class UserInfo : IDirectRecipient, ICloneable, IMapFrom<User>
{
    /// <summary>
    /// ID
    /// </summary>
    [OpenApiDescription("ID")]
    public Guid Id { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    [OpenApiDescription("First name")]
    public string FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    [OpenApiDescription("Last name")]
    public string LastName { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    [OpenApiDescription("User name")]
    public string UserName { get; set; }

    /// <summary>
    /// Birthday
    /// </summary>
    [OpenApiDescription("Birthday")]
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// Sex (male or female)
    /// </summary>
    [OpenApiDescription("Sex (male or female)")]
    public bool? Sex { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    [OpenApiDescription("Status")]
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// Activation status
    /// </summary>
    [OpenApiDescription("Activation status")]
    public EmployeeActivationStatus ActivationStatus { get; set; } = EmployeeActivationStatus.NotActivated;

    /// <summary>
    /// The date and time when the user account was terminated
    /// </summary>
    [OpenApiDescription("The date and time when the user account was terminated")]
    public DateTime? TerminatedDate { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    [OpenApiDescription("Title")]
    public string Title { get; set; }

    /// <summary>
    /// Registration date
    /// </summary>
    [OpenApiDescription("Registration date")]
    public DateTime? WorkFromDate { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    [EmailAddress]
    [OpenApiDescription("Email")]
    public string Email { get; set; }

    private string _contacts;

    /// <summary>
    /// List of contacts in the string format
    /// </summary>
    [OpenApiDescription("List of contacts in the string format")]
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
    /// List of contacts
    /// </summary>
    [OpenApiDescription("List of contacts")]
    public List<string> ContactsList { get; set; }

    /// <summary>
    /// Location
    /// </summary>
    [OpenApiDescription("Location")]
    public string Location { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    [OpenApiDescription("Notes")]
    public string Notes { get; set; }

    /// <summary>
    /// Specifies if the user account was removed or not
    /// </summary>
    [OpenApiDescription("Specifies if the user account was removed or not")]
    public bool Removed { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    [OpenApiDescription("Last modified date")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID
    /// </summary>
    [OpenApiDescription("Tenant ID")]
    public int TenantId { get; set; }

    /// <summary>
    /// Spceifies if the user is active or not
    /// </summary>
    [OpenApiDescription("Spceifies if the user is active or not")]
    public bool IsActive => ActivationStatus.HasFlag(EmployeeActivationStatus.Activated);

    /// <summary>
    /// Language
    /// </summary>
    [OpenApiDescription("Language")]
    public string CultureName { get; set; }

    /// <summary>
    /// Mobile phone
    /// </summary>
    [OpenApiDescription("Mobile phone")]
    public string MobilePhone { get; set; }

    /// <summary>
    /// Mobile phone activation status
    /// </summary>
    [OpenApiDescription("Mobile phone activation status")]
    public MobilePhoneActivationStatus MobilePhoneActivationStatus { get; set; }

    /// <summary>
    /// LDAP user identificator
    /// </summary>
    [OpenApiDescription("LDAP user identificator")]
    public string Sid { get; set; }

    /// <summary>
    /// LDAP user quota attribute
    /// </summary>
    [OpenApiDescription("LDAP user quota attribute")]
    public long LdapQouta { get; init; }

    /// <summary>
    /// SSO SAML user identificator
    /// </summary>
    [OpenApiDescription("SSO SAML user identificator")]
    public string SsoNameId { get; set; }

    /// <summary>
    /// SSO SAML user session identificator
    /// </summary>
    [OpenApiDescription("SSO SAML user session identificator")]
    public string SsoSessionId { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    [OpenApiDescription("Creation date")]
    public DateTime CreateDate { get; set; }


    [OpenApiDescription("Created by")]
    public Guid? CreatedBy { get; set; }


    [OpenApiDescription("Spam")]
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
