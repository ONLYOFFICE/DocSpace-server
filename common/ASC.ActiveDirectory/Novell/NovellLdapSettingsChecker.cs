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

namespace ASC.ActiveDirectory.Novell;

[Scope]
public class NovellLdapSettingsChecker(ILogger<LdapSettingsChecker> logger) : LdapSettingsChecker(logger)
{
    public LdapCertificateConfirmRequest CertificateConfirmRequest { get; set; }

    public LdapHelper LdapHelper => LdapImporter.LdapHelper;

    public new void Init(LdapUserImporter importer)
    {
        base.Init(importer);
    }

    public async Task<bool> CheckConnection()
    {
        const int timeoutMilliseconds = 5000;

        using var ldapConnection = new LdapConnection();
        try
        {
            if (Settings.Ssl)
            {
                ldapConnection.SecureSocketLayer = true;
            }

            ldapConnection.ConnectionTimeout = timeoutMilliseconds;
            ldapConnection.Connect(Settings.Server["LDAP://".Length..], Settings.PortNumber);

            using var cts = new CancellationTokenSource(timeoutMilliseconds);
            var bindTask = Task.Run(() => ldapConnection.Bind(Settings.Login, Settings.Password), cts.Token);

            if (await Task.WhenAny(bindTask, Task.Delay(timeoutMilliseconds, cts.Token)) == bindTask)
            {
                return !bindTask.IsFaulted;
            }
            return false;
        }
        catch (LdapException ex)
        {
            _logger.ErrorSocketException(ex);
            return false;
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Connect Error") || ex.Message.StartsWith("Unavailable"))
            {
                _logger.ErrorCheckSettingsException(ex);
                return false;
            }
            return true;
        }
        finally
        {
            ldapConnection.Disconnect();
        }
    }

    public override async Task<LdapSettingsStatus> CheckSettings()
    {
        if (!Settings.EnableLdapAuthentication)
        {
            return LdapSettingsStatus.Ok;
        }

        if (Settings.Server.Equals("LDAP://", StringComparison.InvariantCultureIgnoreCase))
        {
            return LdapSettingsStatus.WrongServerOrPort;
        }

        if (!await CheckConnection())
        {
            return LdapSettingsStatus.ConnectError;
        }

        if (!LdapHelper.IsConnected)
        {
            try
            {
                LdapHelper.Connect();
            }
            catch (NovellLdapTlsCertificateRequestedException ex)
            {
                _logger.ErrorNovellLdapTlsCertificateRequestedException(Settings.AcceptCertificate, ex);
                CertificateConfirmRequest = ex.CertificateConfirmRequest;
                return LdapSettingsStatus.CertificateRequest;
            }
            catch (NotSupportedException ex)
            {
                _logger.ErrorNotSupportedException(ex);
                return LdapSettingsStatus.TlsNotSupported;
            }
            catch (SocketException ex)
            {
                _logger.ErrorSocketException(ex);
                return LdapSettingsStatus.ConnectError;
            }
            catch (ArgumentException ex)
            {
                _logger.ErrorArgumentException(ex);
                return LdapSettingsStatus.WrongServerOrPort;
            }
            catch (SecurityException ex)
            {
                _logger.ErrorSecurityException(ex);
                return LdapSettingsStatus.StrongAuthRequired;
            }
            catch (LdapException ex)
            {

                if (ex.ResultCode == LdapException.InvalidCredentials)
                {
                    _logger.ErrorCheckSettingsException(ex);
                    return LdapSettingsStatus.CredentialsNotValid;
                }

                _logger.ErrorSocketException(ex);
                return LdapSettingsStatus.ConnectError;
            }
            catch (SystemException ex)
            {
                _logger.ErrorSystemException(ex);
                return LdapSettingsStatus.WrongServerOrPort;
            }
            catch (Exception ex)
            {
                _logger.ErrorCheckSettingsException(ex);
                return LdapSettingsStatus.ConnectError;
            }
        }

        if (!CheckUserDn(Settings.UserDN))
        {
            return LdapSettingsStatus.WrongUserDn;
        }

        if (Settings.GroupMembership)
        {
            if (!CheckGroupDn(Settings.GroupDN))
            {
                return LdapSettingsStatus.WrongGroupDn;
            }

            try
            {
                new RfcFilter(Settings.GroupFilter);
            }
            catch
            {
                return LdapSettingsStatus.IncorrectGroupLDAPFilter;
            }

            if (!LdapImporter.TryLoadLDAPGroups())
            {
                if (LdapImporter.AllSkipedDomainGroups.Count == 0)
                {
                    return LdapSettingsStatus.IncorrectGroupLDAPFilter;
                }

                if (LdapImporter.AllSkipedDomainGroups.All(kv => kv.Value == LdapSettingsStatus.WrongSidAttribute))
                {
                    return LdapSettingsStatus.WrongSidAttribute;
                }

                if (LdapImporter.AllSkipedDomainGroups.All(kv => kv.Value == LdapSettingsStatus.WrongGroupAttribute))
                {
                    return LdapSettingsStatus.WrongGroupAttribute;
                }

                if (LdapImporter.AllSkipedDomainGroups.All(kv => kv.Value == LdapSettingsStatus.WrongGroupNameAttribute))
                {
                    return LdapSettingsStatus.WrongGroupNameAttribute;
                }
            }

            if (LdapImporter.AllDomainGroups.Count == 0)
            {
                return LdapSettingsStatus.GroupsNotFound;
            }
        }

        try
        {
            new RfcFilter(Settings.UserFilter);
        }
        catch
        {
            return LdapSettingsStatus.IncorrectLDAPFilter;
        }

        if (!LdapImporter.TryLoadLDAPUsers())
        {
            if (LdapImporter.AllSkipedDomainUsers.Count == 0)
            {
                return LdapSettingsStatus.IncorrectLDAPFilter;
            }

            if (LdapImporter.AllSkipedDomainUsers.All(kv => kv.Value == LdapSettingsStatus.WrongSidAttribute))
            {
                return LdapSettingsStatus.WrongSidAttribute;
            }

            if (LdapImporter.AllSkipedDomainUsers.All(kv => kv.Value == LdapSettingsStatus.WrongLoginAttribute))
            {
                return LdapSettingsStatus.WrongLoginAttribute;
            }

            if (LdapImporter.AllSkipedDomainUsers.All(kv => kv.Value == LdapSettingsStatus.WrongUserAttribute))
            {
                return LdapSettingsStatus.WrongUserAttribute;
            }
        }

        if (LdapImporter.AllDomainUsers.Count == 0)
        {
            return LdapSettingsStatus.UsersNotFound;
        }

        return string.IsNullOrEmpty(LdapImporter.LDAPDomain)
            ? LdapSettingsStatus.DomainNotFound
            : LdapSettingsStatus.Ok;
    }

    private bool CheckUserDn(string userDn)
    {
        try
        {
            return LdapHelper.CheckUserDn(userDn);
        }
        catch (Exception e)
        {
            _logger.ErrorWrongUserDn(userDn, e);
            return false;
        }
    }

    private bool CheckGroupDn(string groupDn)
    {
        try
        {
            return LdapHelper.CheckGroupDn(groupDn);
        }
        catch (Exception e)
        {
            _logger.ErrorWrongGroupDn(groupDn, e);
            return false;
        }
    }
}
