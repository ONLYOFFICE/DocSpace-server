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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Notify.Engine;

public class NotifyRequest(ILoggerProvider options, INotifySource notifySource, INotifyAction action, string objectID, IRecipient recipient)
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
    private readonly ILogger _log = options.CreateLogger("ASC.Notify");

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
                    _log.ErrorIntercept(interceptor.Name, NotifyAction, Recipient, err);
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

        var newRequest = new NotifyRequest(options, _notifySource, NotifyAction, ObjectID, recipient)
        {
            _tenantId = _tenantId,
            _senderNames = _senderNames,
            _patterns = _patterns,
            Arguments = [..Arguments],
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

    public async Task<IPatternProvider> GetPatternProvider(IServiceScope scope)
    {
        return await ((INotifySource)scope.ServiceProvider.GetService(_notifySource.GetType())).GetPatternProvider(this);
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
