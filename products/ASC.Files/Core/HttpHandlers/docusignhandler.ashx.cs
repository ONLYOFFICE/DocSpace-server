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

namespace ASC.Web.Files.HttpHandlers;

public class DocuSignHandler
{
    public DocuSignHandler(RequestDelegate _)
    {
    }

    public async Task Invoke(HttpContext context, DocuSignHandlerService docuSignHandlerService)
    {
        await docuSignHandlerService.InvokeAsync(context);
    }
}

[Scope]
public class DocuSignHandlerService(ILogger<DocuSignHandlerService> logger,
    TenantExtra tenantExtra,
    DocuSignHelper docuSignHelper,
    SecurityContext securityContext,
    NotifyClient notifyClient)
{
    public static string Path(FilesLinkUtility filesLinkUtility)
    {
        return filesLinkUtility.FilesBaseAbsolutePath + "docusignhandler.ashx";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (await tenantExtra.IsNotPaidAsync())
        {
            context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
            await context.Response.WriteAsync("Payment Required.");
            return;
        }

        try
        {
            switch ((context.Request.Query[FilesLinkUtility.Action].FirstOrDefault() ?? "").ToLower())
            {
                case "redirect":
                    Redirect(context);
                    break;
                case "webhook":
                    await WebhookAsync(context);
                    break;
                default:
                    throw new HttpException((int)HttpStatusCode.BadRequest, FilesCommonResource.ErrorMessage_BadRequest);
            }
        }
        catch (InvalidOperationException e)
        {
            throw new HttpException((int)HttpStatusCode.InternalServerError, FilesCommonResource.ErrorMessage_BadRequest, e);
        }
    }

    private void Redirect(HttpContext context)
    {
        logger.InformationDocuSignRedirectQuery(context.Request.QueryString);

        var eventRedirect = context.Request.Query["event"].FirstOrDefault();
        switch (eventRedirect?.ToLower())
        {
            case "send":
                context.Response.Redirect(PathProvider.StartURL + "#message/" + HttpUtility.UrlEncode(FilesCommonResource.DocuSignStatusSended), true);
                break;
            case "save":
            case "cancel":
                context.Response.Redirect(PathProvider.StartURL + "#error/" + HttpUtility.UrlEncode(FilesCommonResource.DocuSignStatusNotSended), true);
                break;
            case "error":
            case "sessionend":
                context.Response.Redirect(PathProvider.StartURL + "#error/" + HttpUtility.UrlEncode(FilesCommonResource.DocuSignStatusError), true);
                break;
        }
        context.Response.Redirect(PathProvider.StartURL, true);
    }

    private const string XmlPrefix = "docusign";

    private async Task WebhookAsync(HttpContext context)
    {
        logger.InformationDocuSignWebhook(context.Request.QueryString);
        try
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(context.Request.Body);
            logger.InformationDocuSignWebhookOuterXml(xmlDocument.OuterXml);

            var mgr = new XmlNamespaceManager(xmlDocument.NameTable);
            mgr.AddNamespace(XmlPrefix, "http://www.docusign.net/API/3.0");

            var envelopeStatusNode = GetSingleNode(xmlDocument, "DocuSignEnvelopeInformation/" + XmlPrefix + ":EnvelopeStatus", mgr);
            var envelopeId = GetSingleNode(envelopeStatusNode, "EnvelopeID", mgr).InnerText;
            var subject = GetSingleNode(envelopeStatusNode, "Subject", mgr).InnerText;

            var statusString = GetSingleNode(envelopeStatusNode, "Status", mgr).InnerText;

            if (!DocuSignStatusExtensions.TryParse(statusString, true, out var status))
            {
                throw new Exception("DocuSign webhook unknown status: " + statusString);
            }

            logger.InformationDocuSignWebhook2(envelopeId, subject, status);

            var customFieldUserIdNode = GetSingleNode(envelopeStatusNode, "CustomFields/" + XmlPrefix + ":CustomField[" + XmlPrefix + ":Name='" + DocuSignHelper.UserField + "']", mgr);
            var userIdString = GetSingleNode(customFieldUserIdNode, "Value", mgr).InnerText;
            await AuthAsync(userIdString);

            switch (status)
            {
                case DocuSignStatus.Completed:

                    var documentStatuses = GetSingleNode(envelopeStatusNode, "DocumentStatuses", mgr);
                    foreach (XmlNode documentStatus in documentStatuses.ChildNodes)
                    {
                        try
                        {
                            var documentId = GetSingleNode(documentStatus, "ID", mgr).InnerText;
                            var documentName = GetSingleNode(documentStatus, "Name", mgr).InnerText;

                            string folderId = null;
                            string sourceTitle = null;

                            var documentFiels = GetSingleNode(documentStatus, "DocumentFields", mgr, true);
                            if (documentFiels != null)
                            {
                                var documentFieldFolderNode = GetSingleNode(documentFiels, "DocumentField[" + XmlPrefix + ":Name='" + FilesLinkUtility.FolderId + "']", mgr, true);
                                if (documentFieldFolderNode != null)
                                {
                                    folderId = GetSingleNode(documentFieldFolderNode, "Value", mgr).InnerText;
                                }
                                var documentFieldTitleNode = GetSingleNode(documentFiels, "DocumentField[" + XmlPrefix + ":Name='" + FilesLinkUtility.FileTitle + "']", mgr, true);
                                if (documentFieldTitleNode != null)
                                {
                                    sourceTitle = GetSingleNode(documentFieldTitleNode, "Value", mgr).InnerText;
                                }
                            }

                            var file = await docuSignHelper.SaveDocumentAsync(envelopeId, documentId, documentName, folderId);

                            await notifyClient.SendDocuSignCompleteAsync(file, sourceTitle ?? documentName);
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorDocuSignWebhookSaveDocument(documentStatus.InnerText, ex);
                        }
                    }
                    break;
                case DocuSignStatus.Declined:
                case DocuSignStatus.Voided:
                    var statusFromResource = status == DocuSignStatus.Declined
                                                 ? FilesCommonResource.DocuSignStatusDeclined
                                                 : FilesCommonResource.DocuSignStatusVoided;
                    await notifyClient.SendDocuSignStatusAsync(subject, statusFromResource);
                    break;
            }
        }
        catch (Exception e)
        {
            logger.ErrorDocuSignWebhook(e);

            throw new HttpException((int)HttpStatusCode.BadRequest, e.Message);
        }
    }

    private async Task AuthAsync(string userIdString)
    {
        if (!Guid.TryParse(userIdString ?? "", out var userId))
        {
            throw new Exception("DocuSign incorrect User ID: " + userIdString);
        }

        await securityContext.AuthenticateMeWithoutCookieAsync(userId);
    }

    private static XmlNode GetSingleNode(XmlNode node, string xpath, XmlNamespaceManager mgr, bool canMiss = false)
    {
        var result = node.SelectSingleNode(XmlPrefix + ":" + xpath, mgr);
        if (!canMiss && result == null)
        {
            throw new Exception(xpath + " is null");
        }

        return result;
    }
}

public static class DocuSignHandlerExtension
{
    public static IApplicationBuilder UseDocuSignHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DocuSignHandler>();
    }
}