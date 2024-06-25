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

using MailKit.Net.Smtp;

namespace ASC.Core.Notify.Senders;

[Singleton]
public class SmtpSender(IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILoggerProvider options)
    : INotifySender
{
    protected ILogger _logger = options.CreateLogger("ASC.Notify");
    private IDictionary<string, string> _initProperties = new Dictionary<string, string>();
    protected readonly IConfiguration _configuration = configuration;
    protected readonly IServiceProvider _serviceProvider = serviceProvider;

    private string _host;
    private int _port;
    private bool _ssl;
    private ICredentials _credentials;
    private SaslMechanism _saslMechanism;
    protected bool _useCoreSettings;
    const int NetworkTimeout = 30000;

    public virtual void Init(IDictionary<string, string> properties)
    {
        _initProperties = properties;
    }

    public virtual async Task<NoticeSendResult> SendAsync(NotifyMessage m)
    {
        using var scope = _serviceProvider.CreateScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        await tenantManager.SetCurrentTenantAsync(m.TenantId);

        var configuration = scope.ServiceProvider.GetService<CoreConfiguration>();

        var smtpClient = GetSmtpClient();
        var result = NoticeSendResult.TryOnceAgain;
        try
        {
            try
            {
                await BuildSmtpSettingsAsync(configuration);

                var mail = BuildMailMessage(m);

                _logger.DebugSmtpSender(_host, _port, _ssl, _credentials != null);

                await smtpClient.ConnectAsync(_host, _port,
                    _ssl ? SecureSocketOptions.Auto : SecureSocketOptions.None);

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

    private async Task BuildSmtpSettingsAsync(CoreConfiguration configuration)
    {
        if ((await configuration.GetDefaultSmtpSettingsAsync()).IsDefaultSettings && _initProperties.ContainsKey("host") && !string.IsNullOrEmpty(_initProperties["host"]))
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
            var s = await configuration.GetDefaultSmtpSettingsAsync();

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
    protected MimeMessage BuildMailMessage(NotifyMessage m)
    {
        var mimeMessage = new MimeMessage
        {
            Subject = m.Subject
        };

        var fromAddress = MailboxAddress.Parse(ParserOptions.Default, m.Sender);

        mimeMessage.From.Add(fromAddress);

        foreach (var to in m.Reciever.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
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
