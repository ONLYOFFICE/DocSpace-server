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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Notify.Engine;

public class NotifyRequest(ILoggerFactory loggerFactory, INotifySource notifySource, INotifyAction action, string objectID, IRecipient recipient)
{
    private readonly INotifySource _notifySource = notifySource ?? throw new ArgumentNullException(nameof(notifySource));
    public INotifyAction NotifyAction { get; internal set; } = action ?? throw new ArgumentNullException(nameof(action));
    public string ObjectID { get; internal set; } = objectID;
    public IRecipient Recipient { get; internal set; } = recipient ?? throw new ArgumentNullException(nameof(recipient));
    public List<ITagValue> Arguments { get; internal init; } = [];
    public string CurrentSender { get; internal set; }
    public INoticeMessage CurrentMessage { get; internal set; }
    public Hashtable Properties { get; private set; } = new();

    internal int _tenantId;
    internal string[] _senderNames;
    internal IPattern[] _patterns;
    internal List<string> _requaredTags = [];
    internal List<ISendInterceptor> _interceptors = [];
    internal bool _isNeedCheckSubscriptions = true;
    private readonly ILogger _log = loggerFactory.CreateLogger("ASC.Notify");

    internal async Task<bool> Intercept(InterceptorPlace place, IServiceScope serviceScope)
    {
        var result = false;
        foreach (var interceptor in _interceptors)
        {
            if ((interceptor.PreventPlace & place) == place)
            {
                try
                {
                    if (await interceptor.PreventSend(this, place, serviceScope))
                    {
                        result = true;
                    }
                }
                catch (Exception err)
                {
                    _log.ErrorIntercept(interceptor.Name, NotifyAction.ToString(), Recipient.ToString(), err);
                }
            }
        }

        return result;
    }

    internal IPattern GetSenderPattern(string senderName)
    {
        if (_senderNames == null || _patterns == null ||
            _senderNames.Length == 0 || _patterns.Length == 0 ||
            _senderNames.Length != _patterns.Length)
        {
            return null;
        }

        var index = Array.IndexOf(_senderNames, senderName);
        if (index < 0)
        {
            throw new ApplicationException($"Sender with tag {senderName} dnot found");
        }

        return _patterns[index];
    }

    internal NotifyRequest Split(IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        var newRequest = new NotifyRequest(loggerFactory, _notifySource, NotifyAction, ObjectID, recipient)
        {
            _tenantId = _tenantId,
            _senderNames = _senderNames,
            _patterns = _patterns,
            Arguments = [.. Arguments],
            _requaredTags = _requaredTags,
            CurrentSender = CurrentSender,
            CurrentMessage = CurrentMessage
        };
        newRequest._interceptors.AddRange(_interceptors);

        return newRequest;
    }

    internal NoticeMessage CreateMessage(IDirectRecipient recipient)
    {
        return new NoticeMessage(recipient, NotifyAction, ObjectID);
    }
    
    public IRecipientProvider GetRecipientsProvider(IServiceScope scope)
    {
        return ((INotifySource)scope.ServiceProvider.GetService(_notifySource.GetType())).GetRecipientsProvider();
    }

    public ISubscriptionProvider GetSubscriptionProvider(IServiceScope scope)
    {
        return ((INotifySource)scope.ServiceProvider.GetService(_notifySource.GetType())).GetSubscriptionProvider();
    }

    public async Task<CultureInfo> GetCulture(TenantManager tenantManager, UserManager userManager)
    {
        var tagCulture = Arguments.FirstOrDefault(a => a.Tag == "Culture");
        if (tagCulture != null)
        {
            return CultureInfo.GetCultureInfo((string)tagCulture.Value);
        }

        CultureInfo culture = null;

        var tenant = tenantManager.GetCurrentTenant(false);

        if (tenant != null)
        {
            culture = tenant.GetCulture();
        }

        var user = await userManager.SearchUserAsync(Recipient.ID);

        if (!Constants.LostUser.Equals(user) && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture;
    }
}