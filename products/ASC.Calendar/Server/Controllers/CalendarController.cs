﻿

using System.Collections.Generic;

using ASC.Api.Core;
using ASC.Common.Logging;
using ASC.Common.Utils;
using ASC.Core;
using ASC.Core.Common.Settings;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.Data.Reassigns;
using ASC.FederatedLogin;
using ASC.MessagingSystem;
using ASC.Calendar.Models;
using ASC.Security.Cryptography;
using ASC.Web.Api.Routing;
using ASC.Web.Core;
using ASC.Web.Core.Users;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.Core.Notify;
using ASC.Web.Studio.UserControls.Statistics;
using ASC.Web.Studio.Utility;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SecurityContext = ASC.Core.SecurityContext;
using ASC.Calendar.Core;
using ASC.Calendar.Core.Dao;
using ASC.Calendar.BusinessObjects;
using ASC.Web.Core.Calendars;
using ASC.Calendar.ExternalCalendars;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
using System.Web;
using ASC.Calendar.Notification;
using ASC.Common;
using System.Net;
using System.IO;
using ASC.Calendar.iCalParser;
using ASC.Common.Security;
using System.Globalization;

namespace ASC.Calendar.Controllers
{
    [DefaultRoute]
    [ApiController]
    public class CalendarController : ControllerBase
    {

        public Tenant Tenant { get { return ApiContext.Tenant; } }
        public ApiContext ApiContext { get; }
        public AuthContext AuthContext { get; }
        public UserManager UserManager { get; }
        public DataProvider DataProvider { get; }
        public ILog Log { get; }
        private TenantManager TenantManager { get; }
        public TimeZoneConverter TimeZoneConverter { get; }
        public CalendarWrapperHelper CalendarWrapperHelper { get; }
        public DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }
        private AuthorizationManager AuthorizationManager { get; }
        private AuthManager Authentication { get; }
        private CalendarNotifyClient CalendarNotifyClient { get; }
        public DDayICalParser DDayICalParser { get; }
        
        public HttpContext HttpContext { get; set; }

        public PermissionContext PermissionContext { get; }
        public EventHistoryWrapperHelper EventHistoryWrapperHelper { get; }
        

        public CalendarController(
           
            ApiContext apiContext,
            AuthContext authContext,
            AuthorizationManager authorizationManager,
            UserManager userManager,
            TenantManager tenantManager,
            TimeZoneConverter timeZoneConverter,
            DisplayUserSettingsHelper displayUserSettingsHelper,
            IOptionsMonitor<ILog> option,
            DDayICalParser dDayICalParser,
            DataProvider dataProvider,
            IHttpContextAccessor httpContextAccessor,
            CalendarWrapperHelper calendarWrapperHelper,
            AuthManager authentication,
            CalendarNotifyClient calendarNotifyClient,
            PermissionContext permissionContext,
            EventHistoryWrapperHelper eventHistoryWrapperHelper)
        {
            AuthContext = authContext;
            Authentication = authentication;
            AuthorizationManager = authorizationManager;
            TenantManager = tenantManager;
            Log = option.Get("ASC.Api");
            TimeZoneConverter = timeZoneConverter;
            ApiContext = apiContext;
            UserManager = userManager;
            DataProvider = dataProvider;
            CalendarWrapperHelper = calendarWrapperHelper;
            DisplayUserSettingsHelper = displayUserSettingsHelper;
            CalendarNotifyClient = calendarNotifyClient;
            DDayICalParser = dDayICalParser;
            PermissionContext = permissionContext;
            EventHistoryWrapperHelper = eventHistoryWrapperHelper;


            CalendarManager.Instance.RegistryCalendar(new SharedEventsCalendar(AuthContext, TimeZoneConverter, TenantManager));
            var birthdayReminderCalendar = new BirthdayReminderCalendar(AuthContext, TimeZoneConverter, UserManager, DisplayUserSettingsHelper);
            if (UserManager.IsUserInGroup(AuthContext.CurrentAccount.ID, Constants.GroupVisitor.ID))
            {
                CalendarManager.Instance.UnRegistryCalendar(birthdayReminderCalendar.Id);
            }
            else
            {
                CalendarManager.Instance.RegistryCalendar(birthdayReminderCalendar);
            }
            HttpContext = httpContextAccessor?.HttpContext;
        }
        public class SharingParam : SharingOptions.PublicItem
        {
            public string actionId { get; set; }
            public Guid itemId
            {
                get { return Id; }
                set { Id = value; }
            }
            public bool isGroup
            {
                get { return IsGroup; }
                set { IsGroup = value; }
            }
        }

        [Read("info")]
        public Module GetModule()
        {
            var product = new CalendarProduct();
            product.Init();
            return new Module(product, true);
        }

        [Read("{calendarId}")]
        public CalendarWrapper GetCalendarById(string calendarId)
        {
            int calId;
            if (int.TryParse(calendarId, out calId))
            {
                var calendars = DataProvider.GetCalendarById(calId);

                return (calendars != null ? CalendarWrapperHelper.Get(calendars) : null);
            }

            var extCalendar = CalendarManager.Instance.GetCalendarForUser(AuthContext.CurrentAccount.ID, calendarId, UserManager);
            if (extCalendar != null)
            {
                var viewSettings = DataProvider.GetUserViewSettings(AuthContext.CurrentAccount.ID, new List<string> { calendarId });
                return CalendarWrapperHelper.Get(extCalendar, viewSettings.FirstOrDefault());
            }
            return null;
        }

        [Create]
        public CalendarWrapper CreateCalendar(CalendarModel calendar)
        {
            var sharingOptionsList = calendar.sharingOptions ?? new List<SharingParam>();
            var timeZoneInfo = TimeZoneConverter.GetTimeZone(calendar.TimeZone);
    
            calendar.Name = (calendar.Name ?? "").Trim();
            if (String.IsNullOrEmpty(calendar.Name))
                throw new Exception(Resources.CalendarApiResource.ErrorEmptyName);

            calendar.Description = (calendar.Description ?? "").Trim();
            calendar.TextColor = (calendar.TextColor ?? "").Trim();
            calendar.BackgroundColor = (calendar.BackgroundColor ?? "").Trim();

            Guid calDavGuid = Guid.NewGuid();

            var myUri = HttpContext.Request.GetUrlRewriter();

            var _email = UserManager.GetUsers(AuthContext.CurrentAccount.ID).Email;
            var currentUserName = _email.ToLower() + "@" + myUri.Host;

            string currentAccountPaswd = Authentication.GetUserPasswordHash(TenantManager.GetCurrentTenant().TenantId, UserManager.GetUserByEmail(_email).ID);

            //TODO caldav

            /*var caldavTask = new Task(() => CreateCalDavCalendar(name, description, backgroundColor, calDavGuid.ToString(), myUri, currentUserName, _email, currentAccountPaswd));
            caldavTask.Start();*/

            var cal = DataProvider.CreateCalendar(
                        AuthContext.CurrentAccount.ID,
                        calendar.Name,
                        calendar.Description,
                        calendar.TextColor,
                        calendar.BackgroundColor, 
                        timeZoneInfo,
                        calendar.AlertType,
                        null,
                        sharingOptionsList.Select(o => o as SharingOptions.PublicItem).ToList(),
                        new List<UserViewSettings>(), 
                        calDavGuid,
                        calendar.IsTodo);

            if (cal == null) throw new Exception("calendar is null");

           foreach (var opt in sharingOptionsList)
                if (String.Equals(opt.actionId, AccessOption.FullAccessOption.Id, StringComparison.InvariantCultureIgnoreCase))
                    AuthorizationManager.AddAce(new AzRecord(opt.Id, CalendarAccessRights.FullAccessAction.ID, Common.Security.Authorizing.AceType.Allow, cal));

            //notify
            CalendarNotifyClient.NotifyAboutSharingCalendar(cal);

            //iCalUrl
            if (!string.IsNullOrEmpty(calendar.iCalUrl))
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(calendar.iCalUrl);
                    using (var resp = req.GetResponse())
                    using (var stream = resp.GetResponseStream())
                    {
                        var ms = new MemoryStream();
                        stream.StreamCopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        using (var tempReader = new StreamReader(ms))
                        {

                            var cals = DDayICalParser.DeserializeCalendar(tempReader);
                            ImportEvents(Convert.ToInt32(cal.Id), cals);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Info(String.Format("Error import events to new calendar by ical url: {0}", ex.Message));
                }

            }

            return CalendarWrapperHelper.Get(cal);
        }

        [Read("events/{eventUid}/historybyuid")]
        public EventHistoryWrapper GetEventHistoryByUid(string eventUid)
        {
            if (string.IsNullOrEmpty(eventUid))
            {
                throw new ArgumentException("eventUid");
            }

            var evt = DataProvider.GetEventByUid(eventUid);

            return GetEventHistoryWrapper(evt);
        }
        private EventHistoryWrapper GetEventHistoryWrapper(Event evt, bool fullHistory = false)
        {
            if (evt == null) return null;

            int calId;
            BusinessObjects.Calendar cal = null;

            if (int.TryParse(evt.CalendarId, out calId))
                cal = DataProvider.GetCalendarById(calId);

            if (cal == null) return null;

            int evtId;
            EventHistory history = null;

            if (int.TryParse(evt.Id, out evtId))
                history = DataProvider.GetEventHistory(evtId);

            if (history == null) return null;

            return ToEventHistoryWrapper(evt, cal, history, fullHistory);
        }
        private EventHistoryWrapper ToEventHistoryWrapper(Event evt, BusinessObjects.Calendar cal, EventHistory history, bool fullHistory = false)
        {
            var canNotify = false;
            bool canEdit;

            var calIsShared = cal.SharingOptions.SharedForAll || cal.SharingOptions.PublicItems.Count > 0;
            if (calIsShared)
            {
                canEdit = canNotify = CheckPermissions(cal, CalendarAccessRights.FullAccessAction, true);
                return EventHistoryWrapperHelper.Get(history, canEdit, canNotify, cal, fullHistory);
            }

            var evtIsShared = evt.SharingOptions.SharedForAll || evt.SharingOptions.PublicItems.Count > 0;
            if (evtIsShared)
            {
                canEdit = canNotify = CheckPermissions(evt, CalendarAccessRights.FullAccessAction, true);
                return EventHistoryWrapperHelper.Get(history, canEdit, canNotify, cal, fullHistory);
            }

            canEdit = CheckPermissions(evt, CalendarAccessRights.FullAccessAction, true);
            if (canEdit)
            {
                //TODO
                //canNotify = CheckIsOrganizer(history);
            }

            return EventHistoryWrapperHelper.Get(history, canEdit, canNotify, cal, fullHistory);
        }
        private bool CheckIsOrganizer(EventHistory history)
        {
            //TODO
            /*  var canNotify = false;

              var apiServer = new ApiServer();
              var apiResponse = apiServer.GetApiResponse(String.Format("{0}mail/accounts.json", SetupInfo.WebApiBaseUrl), "GET");
              var obj = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(apiResponse)));

              if (obj["response"] != null)
              {
                  var accounts = (from account in JArray.Parse(obj["response"].ToString())
                                  let email = account.Value<String>("email")
                                  let enabled = account.Value<Boolean>("enabled")
                                  let isGroup = account.Value<Boolean>("isGroup")
                                  where enabled && !isGroup
                                  select email).ToList();

                  if (accounts.Any())
                  {
                      var mergedHistory = history.GetMerged();
                      if (mergedHistory != null && mergedHistory.Events != null)
                      {
                          var eventObj = mergedHistory.Events.FirstOrDefault();
                          if (eventObj != null && eventObj.Organizer != null)
                          {
                              var organizerEmail = eventObj.Organizer.Value.ToString()
                                                           .ToLowerInvariant()
                                                           .Replace("mailto:", "");

                              canNotify = accounts.Contains(organizerEmail);
                          }
                      }
                  }
              }

              return canNotify;*/
            return false;
        }
        private int ImportEvents(int calendarId, IEnumerable<Ical.Net.Calendar> cals)
        {
            var counter = 0;

            CheckPermissions(DataProvider.GetCalendarById(calendarId), CalendarAccessRights.FullAccessAction);

            if (cals == null) return counter;

            var calendars = cals.Where(x => string.IsNullOrEmpty(x.Method) ||
                                            x.Method == Ical.Net.CalendarMethods.Publish ||
                                            x.Method == Ical.Net.CalendarMethods.Request ||
                                            x.Method == Ical.Net.CalendarMethods.Reply ||
                                            x.Method == Ical.Net.CalendarMethods.Cancel).ToList();

            foreach (var calendar in calendars)
            {
                if (calendar.Events == null) continue;

                if (string.IsNullOrEmpty(calendar.Method))
                    calendar.Method = Ical.Net.CalendarMethods.Publish;

               foreach (var eventObj in calendar.Events)
               {
                    if (eventObj == null) continue;

                    var tmpCalendar = calendar.Copy<Ical.Net.Calendar>();
                    tmpCalendar.Events.Clear();
                    tmpCalendar.Events.Add(eventObj);

                    string rrule;
                    var ics = DDayICalParser.SerializeCalendar(tmpCalendar);

                    var eventHistory = DataProvider.GetEventHistory(eventObj.Uid);

                    if (eventHistory == null)
                    {
                        rrule = GetRRuleString(eventObj);

                        var utcStartDate = eventObj.IsAllDay ? eventObj.Start.Value : DDayICalParser.ToUtc(eventObj.Start);
                        var utcEndDate = eventObj.IsAllDay ? eventObj.End.Value : DDayICalParser.ToUtc(eventObj.End);

                        var existCalendar = DataProvider.GetCalendarById(calendarId);
                        if (!eventObj.IsAllDay && eventObj.Created != null && !eventObj.Start.IsUtc)
                        {
                            var offset = existCalendar.TimeZone.GetUtcOffset(eventObj.Created.Value);

                            var _utcStartDate = eventObj.Start.Subtract(offset).Value;
                            var _utcEndDate = eventObj.End.Subtract(offset).Value;

                            utcStartDate = _utcStartDate;
                            utcEndDate = _utcEndDate;
                        }
                        else if (!eventObj.IsAllDay && eventObj.Created != null)
                        {
                            var createOffset = existCalendar.TimeZone.GetUtcOffset(eventObj.Created.Value);
                            var startOffset = existCalendar.TimeZone.GetUtcOffset(eventObj.Start.Value);
                            var endOffset = existCalendar.TimeZone.GetUtcOffset(eventObj.End.Value);

                            if (createOffset != startOffset)
                            {
                                var _utcStartDate = eventObj.Start.Subtract(createOffset).Add(startOffset).Value;
                                utcStartDate = _utcStartDate;
                            }
                            if (createOffset != endOffset)
                            {
                                var _utcEndDate = eventObj.End.Subtract(createOffset).Add(endOffset).Value;
                                utcEndDate = _utcEndDate;
                            }
                        }

                        if (eventObj.IsAllDay && utcStartDate.Date < utcEndDate.Date)
                            utcEndDate = utcEndDate.AddDays(-1);

                        try
                        {
                            var uid = eventObj.Uid;
                            string[] split = uid.Split(new Char[] { '@' });

                            var calDavGuid = existCalendar != null ? existCalendar.calDavGuid : "";
                            var myUri = HttpContext.Request.GetUrlRewriter();
                            var currentUserEmail = UserManager.GetUsers(AuthContext.CurrentAccount.ID).Email.ToLower();
                            string currentAccountPaswd = Authentication.GetUserPasswordHash(TenantManager.GetCurrentTenant().TenantId, AuthContext.CurrentAccount.ID);
                            
                            //TODO caldav
                           /*var updateCaldavThread = new Thread(() => updateCaldavEvent(ics, split[0], true, calDavGuid, myUri, currentUserEmail, currentAccountPaswd, DateTime.Now, tmpCalendar.TimeZones[0], existCalendar.TimeZone));
                            updateCaldavThread.Start();*/
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.Message);
                        }

                        /*var result = CreateEvent(calendarId,
                                                 eventObj.Summary,
                                                 eventObj.Description,
                                                 utcStartDate,
                                                 utcEndDate,
                                                 RecurrenceRule.Parse(rrule),
                                                 EventAlertType.Default,
                                                 eventObj.IsAllDay,
                                                 null,
                                                 eventObj.Uid,
                                                 calendar.Method == Ical.Net.CalendarMethods.Cancel ? EventStatus.Cancelled : DDayICalParser.ConvertEventStatus(eventObj.Status), eventObj.Created != null ? eventObj.Created.Value : DateTime.Now);*/

                       // var eventId = result != null && result.Any() ? Int32.Parse(result.First().Id) : 0;

                        //if (eventId > 0)
                       // {
                           //DataProvider.AddEventHistory(calendarId, eventObj.Uid, eventId, ics);
                          //  counter++;
                       // }
                    }
                    else
                    {
                       /* if (eventHistory.Contains(tmpCalendar)) continue;

                        eventHistory = _dataProvider.AddEventHistory(eventHistory.CalendarId, eventHistory.EventUid,
                                                                     eventHistory.EventId, ics);

                        var mergedCalendar = eventHistory.GetMerged();

                        if (mergedCalendar == null || mergedCalendar.Events == null || !mergedCalendar.Events.Any()) continue;

                        var mergedEvent = mergedCalendar.Events.First();

                        rrule = GetRRuleString(mergedEvent);

                        var utcStartDate = mergedEvent.IsAllDay ? mergedEvent.Start.Value : DDayICalParser.ToUtc(mergedEvent.Start);
                        var utcEndDate = mergedEvent.IsAllDay ? mergedEvent.End.Value : DDayICalParser.ToUtc(mergedEvent.End);

                        var existCalendar = _dataProvider.GetCalendarById(calendarId);
                        if (!eventObj.IsAllDay && eventObj.Created != null && !eventObj.Start.IsUtc)
                        {
                            var offset = existCalendar.TimeZone.GetUtcOffset(eventObj.Created.Value);

                            var _utcStartDate = eventObj.Start.Subtract(offset).Value;
                            var _utcEndDate = eventObj.End.Subtract(offset).Value;

                            utcStartDate = _utcStartDate;
                            utcEndDate = _utcEndDate;
                        }

                        if (mergedEvent.IsAllDay && utcStartDate.Date < utcEndDate.Date)
                            utcEndDate = utcEndDate.AddDays(-1);

                        var targetEvent = DataProvider.GetEventById(eventHistory.EventId);
                        var permissions = PublicItemCollection.GetForEvent(targetEvent);
                        var sharingOptions = permissions.Items
                            .Where(x => x.SharingOption.Id != AccessOption.OwnerOption.Id)
                            .Select(x => new SharingParam
                            {
                                Id = x.Id,
                                actionId = x.SharingOption.Id,
                                isGroup = x.IsGroup
                            }).ToList();

                        try
                        {
                            var uid = eventObj.Uid;
                            string[] split = uid.Split(new Char[] { '@' });

                            var calDavGuid = existCalendar != null ? existCalendar.calDavGuid : "";
                            var myUri = HttpContext.Current.Request.GetUrlRewriter();
                            var currentUserEmail = CoreContext.UserManager.GetUsers(SecurityContext.CurrentAccount.ID).Email.ToLower();
                            string currentAccountPaswd = CoreContext.Authentication.GetUserPasswordHash(SecurityContext.CurrentAccount.ID);

                           //TODO caldav
                           // var updateCaldavThread = new Thread(() => updateCaldavEvent(ics, split[0], true, calDavGuid, myUri, currentUserEmail, currentAccountPaswd, DateTime.Now, tmpCalendar.TimeZones[0], existCalendar.TimeZone));
                           // updateCaldavThread.Start();
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.Message);
                        }

                        //updateEvent(ics, split[0], calendarId.ToString(), true, DateTime.Now, tmpCalendar.TimeZones[0], existCalendar.TimeZone);

                        CreateEvent(eventHistory.CalendarId,
                                    mergedEvent.Summary,
                                    mergedEvent.Description,
                                    utcStartDate,
                                    utcEndDate,
                                    RecurrenceRule.Parse(rrule),
                                    EventAlertType.Default,
                                    mergedEvent.IsAllDay,
                                    sharingOptions,
                                    mergedEvent.Uid,
                                    DDayICalParser.ConvertEventStatus(mergedEvent.Status), eventObj.Created != null ? eventObj.Created.Value : DateTime.Now);

                        counter++;*/
                    }
                }
            }

            return counter;
            
        }

        private void CheckPermissions(ISecurityObject securityObj, Common.Security.Authorizing.Action action)
        {
            CheckPermissions(securityObj, action, false);
        }
        private bool CheckPermissions(ISecurityObject securityObj, Common.Security.Authorizing.Action action, bool silent)
        {
            if (securityObj == null)
                throw new Exception(Resources.CalendarApiResource.ErrorItemNotFound);

            if (silent)
                return PermissionContext.CheckPermissions(securityObj, action);

            PermissionContext.DemandPermissions(securityObj, action);

            return true;
        }

        private string GetRRuleString(Ical.Net.CalendarComponents.CalendarEvent evt)
        {
            var rrule = string.Empty;

            if (evt.RecurrenceRules != null && evt.RecurrenceRules.Any())
            {
                var recurrenceRules = evt.RecurrenceRules.ToList();

                rrule = DDayICalParser.SerializeRecurrencePattern(recurrenceRules.First());

                if (evt.ExceptionDates != null && evt.ExceptionDates.Any())
                {
                    rrule += ";exdates=";

                    var exceptionDates = evt.ExceptionDates.ToList();

                    foreach (var periodList in exceptionDates)
                    {
                        var date = periodList.ToString();

                        //has time
                        if (date.ToLowerInvariant().IndexOf('t') >= 0)
                        {
                            //is utc time
                            if (date.ToLowerInvariant().IndexOf('z') >= 0)
                            {
                                rrule += date;
                            }
                            else
                            {
                                //convert to utc time
                                DateTime dt;
                                if (DateTime.TryParseExact(date.ToUpper(), "yyyyMMdd'T'HHmmssK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dt))
                                {
                                    var tzid = periodList.TzId ?? evt.Start.TzId;
                                    if (!String.IsNullOrEmpty(tzid))
                                    {
                                        dt = TimeZoneInfo.ConvertTime(dt, TimeZoneConverter.GetTimeZone(tzid), TimeZoneInfo.Utc);
                                    }
                                    rrule += dt.ToString("yyyyMMdd'T'HHmmssK");
                                }
                                else
                                {
                                    rrule += date;
                                }
                            }
                        }
                        //for yyyyMMdd/P1D date. Bug in the ical.net
                        else if (date.ToLowerInvariant().IndexOf("/p") >= 0)
                        {
                            try
                            {
                                rrule += date.Split('/')[0];
                            }
                            catch (Exception ex)
                            {
                                Log.Error(String.Format("Error: {0}, Date string: {1}", ex, date));
                                rrule += date;
                            }
                        }
                        else
                        {
                            rrule += date;
                        }

                        rrule += ",";
                    }

                    rrule = rrule.TrimEnd(',');
                }
            }

            return rrule;
        }

        private List<EventWrapper> CreateEvent(int calendarId, string name, string description, DateTime utcStartDate, DateTime utcEndDate, RecurrenceRule rrule, EventAlertType alertType, bool isAllDayLong, List<SharingParam> sharingOptions, string uid, EventStatus status, DateTime createDate)
        {
            var sharingOptionsList = sharingOptions ?? new List<SharingParam>();

            name = (name ?? "").Trim();
            description = (description ?? "").Trim();

            if (!string.IsNullOrEmpty(uid))
            {
                var existEvent = DataProvider.GetEventByUid(uid);

                /*if (existEvent != null)
                {
                    return UpdateEvent(existEvent.CalendarId,
                                       int.Parse(existEvent.Id),
                                       name,
                                       description,
                                       new ApiDateTime(utcStartDate, TimeZoneInfo.Utc),
                                       new ApiDateTime(utcEndDate, TimeZoneInfo.Utc),
                                       rrule.ToString(),
                                       alertType,
                                       isAllDayLong,
                                       sharingOptions,
                                       status,
                                       createDate);
                }*/
            }

           /*CheckPermissions(_dataProvider.GetCalendarById(calendarId), CalendarAccessRights.FullAccessAction);

            var evt = _dataProvider.CreateEvent(calendarId,
                                                SecurityContext.CurrentAccount.ID,
                                                name,
                                                description,
                                                utcStartDate,
                                                utcEndDate,
                                                rrule,
                                                alertType,
                                                isAllDayLong,
                                                sharingOptionsList.Select(o => o as SharingOptions.PublicItem).ToList(),
                                                uid,
                                                status,
                                                createDate);

            if (evt != null)
            {
                foreach (var opt in sharingOptionsList)
                    if (String.Equals(opt.actionId, AccessOption.FullAccessOption.Id, StringComparison.InvariantCultureIgnoreCase))
                        CoreContext.AuthorizationManager.AddAce(new AzRecord(opt.Id, CalendarAccessRights.FullAccessAction.ID, Common.Security.Authorizing.AceType.Allow, evt));

                //notify
                CalendarNotifyClient.NotifyAboutSharingEvent(evt);

                return new EventWrapper(evt, SecurityContext.CurrentAccount.ID,
                                        _dataProvider.GetTimeZoneForCalendar(SecurityContext.CurrentAccount.ID, calendarId))
                                        .GetList(utcStartDate, utcStartDate.AddMonths(_monthCount));
            }*/
            return null;
        }
    }

    public static class CalendarControllerExtention
    {
        public static DIHelper AddCalendarController(this DIHelper services)
        {
            return services
                .AddApiContextService()
                .AddSecurityContextService()
                .AddPermissionContextService()
                .AddCommonLinkUtilityService()
                .AddDisplayUserSettingsService()
                .AddCalendarDbContextService()
                .AddCalendarDataProviderService()
                .AddCalendarWrapper()
                .AddCalendarNotifyClient()
                .AddDDayICalParser()
                .AddEventHistoryWrapper();
        }
    }
}