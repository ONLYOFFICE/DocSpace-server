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

#nullable enable

using AuthenticationException = System.Security.Authentication.AuthenticationException;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ASC.Api.Settings.Smtp;

[Transient]
public class SmtpJob(UserManager userManager,
        SecurityContext securityContext,
        TenantManager tenantManager,
        ILogger<SmtpJob> logger)
    : DistributedTaskProgress
{
    private int? _tenantId;
    public int TenantId
    {
        get => _tenantId ?? this[nameof(_tenantId)];
        set
        {
            _tenantId = value;
            this[nameof(_tenantId)] = value;
        }
    }

    private string? _currentOperation;
    public string CurrentOperation
    {
        get => _currentOperation ?? this[nameof(_currentOperation)];
        set
        {
            _currentOperation = value;
            this[nameof(_currentOperation)] = value;
        }
    }

    private Guid _currentUser;
    private SmtpSettingsDto _smtpSettings = new();

    public void Init(SmtpSettingsDto smtpSettings, int tenant, Guid user)
    {
        TenantId = tenant;
        _currentUser = user;
        _smtpSettings = smtpSettings;
    }

    protected override async Task DoJob()
    {
        try
        {
            await SetProgress(5, "Setup tenant");

            await tenantManager.SetCurrentTenantAsync(TenantId);

            await SetProgress(10, "Setup user");

            await securityContext.AuthenticateMeWithoutCookieAsync(_currentUser);

            await SetProgress(15, "Find user data");

            var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

            await SetProgress(20, "Create mime message");

            var toAddress = new MailboxAddress(currentUser.UserName, currentUser.Email);

            var fromAddress = new MailboxAddress(_smtpSettings.SenderDisplayName, _smtpSettings.SenderAddress);

            var mimeMessage = new MimeMessage
            {
                Subject = WebstudioNotifyPatternResource.subject_smtp_test
            };

            mimeMessage.From.Add(fromAddress);

            mimeMessage.To.Add(toAddress);

            var bodyBuilder = new BodyBuilder
            {
                TextBody = WebstudioNotifyPatternResource.pattern_smtp_test
            };

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            mimeMessage.Headers.Add("Auto-Submitted", "auto-generated");

            using var client = GetSmtpClient();
            await SetProgress(40, "Connect to host");

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port.GetValueOrDefault(25),
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
            logger.ErrorWithException(Exception);
        }
        catch (AggregateException ae)
        {
            ae.Flatten().Handle(e => e is TaskCanceledException or OperationCanceledException);
        }
        catch (SocketException ex)
        {
            Exception = ex; //TODO: Add translates of ordinary cases
            logger.ErrorWithException(ex);
        }
        catch (AuthenticationException ex)
        {
            Exception = ex; //TODO: Add translates of ordinary cases
            logger.ErrorWithException(ex);
        }
        catch (Exception ex)
        {
            Exception = ex; //TODO: Add translates of ordinary cases
            logger.ErrorWithException(ex);
        }
        finally
        {
            try
            {
                IsCompleted = true;
                await PublishChanges();

                securityContext.Logout();
            }
            catch (Exception ex)
            {
                logger.ErrorLdapOperationFinalizationProblem(ex);
            }
        }
    }

    private async Task SetProgress(int percentage, string? status = null)
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