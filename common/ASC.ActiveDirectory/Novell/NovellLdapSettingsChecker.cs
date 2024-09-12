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

namespace ASC.ActiveDirectory.Novell;

[Scope]
public class NovellLdapSettingsChecker(ILogger<LdapSettingsChecker> logger) : LdapSettingsChecker(logger)
{
    public LdapCertificateConfirmRequest CertificateConfirmRequest { get; set; }

    public LdapHelper LdapHelper
    {
        get { return LdapImporter.LdapHelper; }
    }

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

    public async override Task<LdapSettingsStatus> CheckSettings()
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
            catch (LdapException ex) {

                if (ex.ResultCode == LdapException.InvalidCredentials)
                {
                    _logger.ErrorCheckSettingsException(ex);
                    return LdapSettingsStatus.CredentialsNotValid;
                }
                else
                {
                    _logger.ErrorSocketException(ex);
                    return LdapSettingsStatus.ConnectError;
                }
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
