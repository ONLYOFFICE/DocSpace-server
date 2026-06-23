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

using AuthenticationException = System.Security.Authentication.AuthenticationException;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ASC.Api.Settings.Smtp;

[Transient]
public class SmtpJob : DistributedTaskProgress
{
    public int TenantId { get; set; }
    public string CurrentOperation { get; set; }

    private Guid _currentUser;
    private SmtpSettingsDto _smtpSettings = new();
    private readonly UserManager _userManager;
    private readonly SecurityContext _securityContext;
    private readonly TenantManager _tenantManager;
    private readonly TenantLogoManager _tenantLogoManager;
    private readonly ILogger<SmtpJob> _logger;

    public SmtpJob()
    {

    }

    public SmtpJob(UserManager userManager,
        SecurityContext securityContext,
        TenantManager tenantManager,
        TenantLogoManager tenantLogoManager,
        ILogger<SmtpJob> logger)
    {
        _userManager = userManager;
        _securityContext = securityContext;
        _tenantManager = tenantManager;
        _logger = logger;
        _tenantLogoManager = tenantLogoManager;
    }

    public void Init(SmtpSettingsDto smtpSettings, int tenant, Guid user)
    {
        TenantId = tenant;
        CurrentOperation = string.Empty;
        _currentUser = user;
        _smtpSettings = smtpSettings;
    }

    protected override async Task DoJob()
    {
        try
        {
            await SetProgress(5, "Setup tenant");

            await _tenantManager.SetCurrentTenantAsync(TenantId);

            await SetProgress(10, "Setup user");

            await _securityContext.AuthenticateMeWithoutCookieAsync(_currentUser);

            await SetProgress(15, "Find user data");

            var currentUser = await _userManager.GetUsersAsync(_securityContext.CurrentAccount.ID);

            await SetProgress(20, "Create mime message");

            var toAddress = new MailboxAddress(currentUser.UserName, currentUser.Email);

            var fromAddress = new MailboxAddress(_smtpSettings.SenderDisplayName, _smtpSettings.SenderAddress);

            using var mimeMessage = new MimeMessage();

            mimeMessage.Subject = WebstudioNotifyPatternResource.subject_smtp_test;
            mimeMessage.From.Add(fromAddress);
            mimeMessage.To.Add(toAddress);

            var logoText = await _tenantLogoManager.GetLogoTextAsync();

            var trulyYoursText = WebstudioNotifyPatternResource.TrulyYoursText.Replace("${LetterLogoText}", logoText);

            var bodyBuilder = new BodyBuilder
            {
                TextBody = WebstudioNotifyPatternResource.pattern_smtp_test.Replace("$TrulyYours", trulyYoursText)
            };

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            mimeMessage.Headers.Add("Auto-Submitted", "auto-generated");

            using var client = GetSmtpClient();
            await SetProgress(40, "Connect to host");

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port.GetValueOrDefault(SmtpSettings.DefaultSmtpPort),
                _smtpSettings.EnableSSL ? SecureSocketOptions.Auto : SecureSocketOptions.None);

            if (_smtpSettings.EnableAuth)
            {
                await SetProgress(60, "Authenticate");

                if (_smtpSettings.UseNtlm)
                {
                    var saslMechanism = new SaslMechanismNtlm(_smtpSettings.CredentialsUserName, _smtpSettings.CredentialsUserPassword);
                    await client.AuthenticateAsync(saslMechanism);
                }
                else
                {
                    await client.AuthenticateAsync(_smtpSettings.CredentialsUserName,
                        _smtpSettings.CredentialsUserPassword);
                }
            }

            await SetProgress(80, "Send test message");

            await client.SendAsync(FormatOptions.Default, mimeMessage);

            Percentage = 100;
        }
        catch (AuthorizingException authError)
        {
            Exception = new SecurityException(Resource.ErrorAccessDenied, authError);
            _logger.ErrorWithException(Exception);
        }
        catch (AggregateException ae)
        {
            ae.Flatten().Handle(e => e is TaskCanceledException or OperationCanceledException);
        }
        catch (SocketException ex)
        {
            Exception = ex; //TODO: Add translates of ordinary cases
            _logger.ErrorWithException(ex);
        }
        catch (AuthenticationException ex)
        {
            Exception = ex; //TODO: Add translates of ordinary cases
            _logger.ErrorWithException(ex);
        }
        catch (Exception ex)
        {
            Exception = ex; //TODO: Add translates of ordinary cases
            _logger.ErrorWithException(ex);
        }
        finally
        {
            try
            {
                IsCompleted = true;
                await PublishChanges();

                _securityContext.Logout();
            }
            catch (Exception ex)
            {
                _logger.ErrorLdapOperationFinalizationProblem(ex);
            }
        }
    }

    private async Task SetProgress(int percentage, string status = null)
    {
        Percentage = percentage;
        CurrentOperation = status ?? CurrentOperation;
        await PublishChanges();
    }

    private SmtpClient GetSmtpClient()
    {
        return new SmtpClient
        {
            Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds
        };
    }
}
