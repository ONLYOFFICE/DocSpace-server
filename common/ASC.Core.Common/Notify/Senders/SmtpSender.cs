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

using MailKit.Net.Smtp;

namespace ASC.Core.Notify.Senders;

[Singleton]
public class SmtpSender(
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory)
    : INotifySender
{
    protected ILogger _logger = loggerFactory.CreateLogger("ASC.Notify");
    private IDictionary<string, string> _initProperties = new Dictionary<string, string>();
    protected readonly IServiceProvider _serviceProvider = serviceProvider;

    private string _host;
    private int _port;
    private bool _ssl;
    private ICredentials _credentials;
    private SaslMechanism _saslMechanism;
    private const int NetworkTimeout = 30000;

    public virtual void Init(IDictionary<string, string> properties)
    {
        _initProperties = properties;
    }

    public virtual async Task<NoticeSendResult> SendAsync(NotifyMessage m)
    {
        using var scope = _serviceProvider.CreateScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        await tenantManager.SetCurrentTenantAsync(m.TenantId);

        var coreConfiguration = scope.ServiceProvider.GetService<CoreConfiguration>();
        var smtpClient = GetSmtpClient();
        var result = NoticeSendResult.TryOnceAgain;
        try
        {
            try
            {
                await BuildSmtpSettingsAsync(coreConfiguration);

                using var mail = BuildMailMessage(m);

                _logger.DebugSmtpSender(_host, _port, _ssl, _credentials != null);

                await smtpClient.ConnectAsync(_host, _port, _ssl ? SecureSocketOptions.Auto : SecureSocketOptions.None);

                if (_credentials != null)
                {
                    await smtpClient.AuthenticateAsync(_credentials);
                }
                else if (_saslMechanism != null)
                {
                    await smtpClient.AuthenticateAsync(_saslMechanism);
                }

                await smtpClient.SendAsync(mail);
                result = NoticeSendResult.OK;
            }
            catch (Exception e)
            {
                _logger.ErrorSend(m.TenantId, m.Reciever, e);

                throw;
            }
        }
        catch (ObjectDisposedException)
        {
            result = NoticeSendResult.SendingImpossible;
        }
        catch (InvalidOperationException)
        {
            result = string.IsNullOrEmpty(_host) || _port == 0
                ? NoticeSendResult.SendingImpossible
                : NoticeSendResult.TryOnceAgain;
        }
        catch (IOException)
        {
            result = NoticeSendResult.TryOnceAgain;
        }
        catch (SmtpProtocolException)
        {
            result = NoticeSendResult.SendingImpossible;
        }
        catch (SmtpCommandException e)
        {
            switch (e.StatusCode)
            {
                case SmtpStatusCode.MailboxBusy:
                case SmtpStatusCode.MailboxUnavailable:
                case SmtpStatusCode.ExceededStorageAllocation:
                    result = NoticeSendResult.TryOnceAgain;
                    break;
                case SmtpStatusCode.MailboxNameNotAllowed:
                case SmtpStatusCode.UserNotLocalWillForward:
                case SmtpStatusCode.UserNotLocalTryAlternatePath:
                    result = NoticeSendResult.MessageIncorrect;
                    break;
                default:
                    if (e.StatusCode != SmtpStatusCode.Ok)
                    {
                        result = NoticeSendResult.TryOnceAgain;
                    }
                    break;
            }
        }
        catch (Exception)
        {
            result = NoticeSendResult.SendingImpossible;
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(true);
            }

            smtpClient.Dispose();
        }

        return result;
    }

    private async Task BuildSmtpSettingsAsync(CoreConfiguration coreConfiguration)
    {
        if ("mailpit".Equals(configuration["core:notify:postman"], StringComparison.InvariantCultureIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("mailpit")?.Split("=")[1];
            if (connectionString != null && connectionString.StartsWith("smtp://"))
            {
                var uri = new Uri(connectionString);
                _host = uri.Host;
                _port = uri.Port;
            }
        }
        else
        {
            if ((await coreConfiguration.GetDefaultSmtpSettingsAsync()).IsDefaultSettings && _initProperties.ContainsKey("host") && !string.IsNullOrEmpty(_initProperties["host"]))
            {
                _host = _initProperties["host"];

                if (_initProperties.ContainsKey("port") && !string.IsNullOrEmpty(_initProperties["port"]))
                {
                    _port = int.Parse(_initProperties["port"]);
                }
                else
                {
                    _port = 25;
                }

                if (_initProperties.ContainsKey("enableSsl") && !string.IsNullOrEmpty(_initProperties["enableSsl"]))
                {
                    _ssl = bool.Parse(_initProperties["enableSsl"]);
                }
                else
                {
                    _ssl = false;
                }

                if (_initProperties.ContainsKey("userName"))
                {
                    var useNtlm = _initProperties.ContainsKey("useNtlm") && bool.Parse(_initProperties["useNtlm"]);
                    _credentials = !useNtlm ? new NetworkCredential(_initProperties["userName"], _initProperties["password"]) : null;
                    _saslMechanism = useNtlm ? new SaslMechanismNtlm(_initProperties["userName"], _initProperties["password"]) : null;
                }
            }
            else
            {
                var s = await coreConfiguration.GetDefaultSmtpSettingsAsync();

                _host = s.Host;
                _port = s.Port;
                _ssl = s.EnableSSL;

                if (!string.IsNullOrEmpty(s.CredentialsUserName))
                {
                    _credentials = !s.UseNtlm ? new NetworkCredential(s.CredentialsUserName, s.CredentialsUserPassword) : null;
                    _saslMechanism = s.UseNtlm ? new SaslMechanismNtlm(s.CredentialsUserName, s.CredentialsUserPassword) : null;
                }
            }
        }
    }
    protected MimeMessage BuildMailMessage(NotifyMessage m)
    {
        var mimeMessage = new MimeMessage
        {
            Subject = m.Subject
        };

        var fromAddress = MailboxAddress.Parse(ParserOptions.Default, m.Sender);

        mimeMessage.From.Add(fromAddress);

        foreach (var to in m.Reciever.Split(['|'], StringSplitOptions.RemoveEmptyEntries))
        {
            mimeMessage.To.Add(MailboxAddress.Parse(ParserOptions.Default, to));
        }

        if (m.ContentType == Pattern.HtmlContentType)
        {
            var textPart = new TextPart("plain")
            {
                Text = HtmlUtil.GetText(m.Content),
                ContentTransferEncoding = ContentEncoding.QuotedPrintable
            };

            var multipartAlternative = new MultipartAlternative { textPart };

            var htmlPart = new TextPart("html")
            {
                Text = GetHtmlView(m.Content),
                ContentTransferEncoding = ContentEncoding.QuotedPrintable
            };

            if (m.Attachments is { Length: > 0 })
            {
                var multipartRelated = new MultipartRelated
                {
                    Root = htmlPart
                };

                foreach (var attachment in m.Attachments)
                {
                    var mimeEntity = ConvertAttachmentToMimePart(attachment);
                    if (mimeEntity != null)
                    {
                        multipartRelated.Add(mimeEntity);
                    }
                }

                multipartAlternative.Add(multipartRelated);
            }
            else
            {
                multipartAlternative.Add(htmlPart);
            }

            mimeMessage.Body = multipartAlternative;
        }
        else
        {
            mimeMessage.Body = new TextPart("plain")
            {
                Text = m.Content,
                ContentTransferEncoding = ContentEncoding.QuotedPrintable
            };
        }

        if (!string.IsNullOrEmpty(m.ReplyTo))
        {
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(ParserOptions.Default, m.ReplyTo));
        }

        mimeMessage.Headers.Add("Auto-Submitted", string.IsNullOrEmpty(m.AutoSubmitted) ? "auto-generated" : m.AutoSubmitted);

        return mimeMessage;
    }

    protected string GetHtmlView(string body)
    {
        body = body.StartsWith("<body")
            ? body
            : $@"<body style=""background: linear-gradient(#ffffff, #ffffff); background-color: #ffffff;"">{body}</body>";

        return $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"">
                      <html>
                        <head>
                            <meta content=""text/html;charset=UTF-8"" http-equiv=""Content-Type"">
                            <meta name=""x-apple-disable-message-reformatting"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                            <meta name=""color-scheme"" content=""light"">
                            <meta name=""supported-color-schemes"" content=""light only"">
                            <link href=""https://fonts.googleapis.com/css?family=Open+Sans:900,800,700,600,500,400,300&subset=latin,latin-ext"" rel=""stylesheet"" type=""text/css"" />
                            <style type=""text/css"">
                                :root {{ color-scheme: light; supported-color-schemes: light; }}
                                [data-ogsc] body {{ background-color: #ffffff !important; }}
                            </style>
                            <!--[if !mso]><!-->
                            <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
                            <!--<![endif]-->
                            <!--[if (gte mso 9)|(IE)]>
                            <style type=""text/css"">
                                table {{ border-collapse: collapse !important !important !important; }}
                            </style>
                            <![endif]-->
                            <!--[if mso]>
                            <style type=""text/css"">
                                .fol {{ font-family: Helvetica, Arial, Tahoma, sans-serif !important; }}
                            </style>
                            <![endif]-->
                            <!--[if gte mso 9]>
                                <xml>
                                    <o:OfficeDocumentSettings>
                                    <o:AllowPNG/>
                                    <o:PixelsPerInch>96</o:PixelsPerInch>
                                    </o:OfficeDocumentSettings>
                                </xml>
                            <![endif]-->
                        </head>
                        {body}
                      </html>";
    }

    private SmtpClient GetSmtpClient()
    {
        var smtpClient = new SmtpClient
        {
            Timeout = NetworkTimeout
        };

        return smtpClient;
    }

    private static MimePart ConvertAttachmentToMimePart(NotifyMessageAttachment attachment)
    {
        try
        {
            if (attachment == null || string.IsNullOrEmpty(attachment.FileName) || string.IsNullOrEmpty(attachment.ContentId) || attachment.Content == null)
            {
                return null;
            }

            var extension = Path.GetExtension(attachment.FileName);

            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            return new MimePart("image", extension.TrimStart('.'))
            {
                ContentId = attachment.ContentId,
                Content = new MimeContent(new MemoryStream(attachment.Content)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = attachment.FileName
            };
        }
        catch (Exception)
        {
            return null;
        }
    }
}
