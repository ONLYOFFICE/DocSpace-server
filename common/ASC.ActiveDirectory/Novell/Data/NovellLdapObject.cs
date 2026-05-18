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

namespace ASC.ActiveDirectory.Novell.Data;
/// <summary>
/// Novell LDAP object class
/// </summary>
public class NovellLdapObject : LdapObject
{
    private LdapEntry _ldapEntry;
    private readonly ILogger _logger;
    private string _sid;
    private string _sidAttribute;
    private readonly NovellLdapEntryExtension _novellLdapEntryExtension;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">init ldap entry</param>
    /// <param name="novellLdapEntryExtension"></param>
    public NovellLdapObject(ILogger logger, NovellLdapEntryExtension novellLdapEntryExtension)
    {
        _novellLdapEntryExtension = novellLdapEntryExtension;
        _logger = logger;
    }

    public void Init(LdapEntry ldapEntry, string ldapUniqueIdAttribute = null)
    {
        _ldapEntry = ldapEntry ?? throw new ArgumentNullException(nameof(ldapEntry));

        if (string.IsNullOrEmpty(ldapUniqueIdAttribute))
        {
            return;
        }

        try
        {
            _sid = GetValue(ldapUniqueIdAttribute) as string;
            _sidAttribute = ldapUniqueIdAttribute;
        }
        catch (Exception e)
        {
            _logger.ErrorCanNotGetSidProperty(e);
        }
    }

    #region .Public

    public override string DistinguishedName => _ldapEntry.Dn;

    public override string Sid => _sid;

    public override string SidAttribute => _sidAttribute;

    public override bool IsDisabled
    {
        get
        {
            var userAccauntControl = LdapConstants.UserAccountControl.EMPTY;
            try
            {
                var uac = Convert.ToInt32(GetValue(LdapConstants.ADSchemaAttributes.USER_ACCOUNT_CONTROL));
                userAccauntControl = (LdapConstants.UserAccountControl)uac;
            }
            catch (Exception e)
            {
                _logger.ErrorCanNotGetUserAccountControlProperty(e);
            }

            return (userAccauntControl & LdapConstants.UserAccountControl.ADS_UF_ACCOUNTDISABLE) > 0;
        }
    }

    #endregion

    /// <summary>
    /// Get property object
    /// </summary>
    /// <param name="propertyName">property name</param>
    /// <param name="getBytes"></param>
    /// <returns>value object</returns>
    public sealed override object GetValue(string propertyName, bool getBytes = false)
    {
        return _novellLdapEntryExtension.GetAttributeValue(_ldapEntry, propertyName, getBytes);
    }

    /// <summary>
    /// Get property values
    /// </summary>
    /// <param name="propertyName">property name</param>
    /// <returns>list of values</returns>
    public override List<string> GetValues(string propertyName)
    {
        var propertyValueArray = _novellLdapEntryExtension.GetAttributeArrayValue(_ldapEntry, propertyName);
        if (propertyValueArray == null)
        {
            return [];
        }

        var properties = propertyValueArray.ToList();
        return properties;
    }
}