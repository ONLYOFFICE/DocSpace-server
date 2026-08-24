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

namespace ASC.Site.Core.Classes
{
    [Singleton]
    public class NotifyConfigurator(NotifyConfiguration notifyConfiguration)
    {
        private bool _initialized;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            notifyConfiguration.Configure();
            _initialized = true;
        }
    }

    [Scope]
    public class EmailSender(
        IServiceScopeFactory scopeFactory,
        CommonConstants commonConstants,
        NotifyConfigurator notifyConfigurator,
        DispatchEngine dispatchEngine)
    {
        public async Task<bool> SendEmail(string email, string subject, string body)
        {
            notifyConfigurator.Initialize();

            var addresses = email.Split(',');

            var to = new DirectRecipient(Guid.NewGuid().ToString(), addresses.FirstOrDefault(), addresses);

            var message = new NoticeMessage(to, subject, body, commonConstants.NotifyContentType);

            message.AddArgument(new TagValue(commonConstants.NotifyFromTag, commonConstants.NotifyFrom ?? ""));

            var scope = scopeFactory.CreateAsyncScope();

            var sendResponse = await dispatchEngine.Dispatch(message, commonConstants.NotifySenderName, scope);

            return sendResponse.Result == SendResult.OK || sendResponse.Result == SendResult.Inprogress;
        }
    }
}
