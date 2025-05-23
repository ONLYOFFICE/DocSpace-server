// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Notify.Cron;

public class CronExpression : ICloneable, IDeserializationCallback
{
    private const int Second = 0;
    private const int Minute = 1;
    private const int Hour = 2;
    private const int DayOfMonth = 3;
    private const int Month = 4;
    private const int DayOfWeek = 5;
    private const int Year = 6;
    private const int AllSpecInt = 99;
    private const int NoSpecInt = 98;
    private const int AllSpec = AllSpecInt;
    private const int NoSpec = NoSpecInt;

    private static readonly Hashtable _monthMap = new(20);
    private static readonly Hashtable _dayMap = new(60);
    [NonSerialized] private bool _calendardayOfMonth;
    [NonSerialized] private bool _calendardayOfWeek;

    [NonSerialized] private TreeSet _daysOfMonth;

    [NonSerialized] private TreeSet _daysOfWeek;
    [NonSerialized] private TreeSet _hours;

    [NonSerialized] private bool _lastdayOfMonth;
    [NonSerialized] private bool _lastdayOfWeek;
    [NonSerialized] private TreeSet _minutes;
    [NonSerialized] private TreeSet _months;

    [NonSerialized] private bool _nearestWeekday;
    [NonSerialized] private int _nthdayOfWeek;
    [NonSerialized] private TreeSet _seconds;
    private TimeZoneInfo _timeZone;
    [NonSerialized] private TreeSet _years;

    static CronExpression()
    {
        _monthMap.Add("JAN", 0);
        _monthMap.Add("FEB", 1);
        _monthMap.Add("MAR", 2);
        _monthMap.Add("APR", 3);
        _monthMap.Add("MAY", 4);
        _monthMap.Add("JUN", 5);
        _monthMap.Add("JUL", 6);
        _monthMap.Add("AUG", 7);
        _monthMap.Add("SEP", 8);
        _monthMap.Add("OCT", 9);
        _monthMap.Add("NOV", 10);
        _monthMap.Add("DEC", 11);
        _dayMap.Add("SUN", 1);
        _dayMap.Add("MON", 2);
        _dayMap.Add("TUE", 3);
        _dayMap.Add("WED", 4);
        _dayMap.Add("THU", 5);
        _dayMap.Add("FRI", 6);
        _dayMap.Add("SAT", 7);
    }

    public CronExpression(string cronExpression)
    {
        if (cronExpression == null)
        {
            throw new ArgumentException("cronExpression cannot be null");
        }
        CronExpressionString = cronExpression.ToUpper(CultureInfo.InvariantCulture);
        BuildExpression(cronExpression);
    }

    protected virtual TimeZoneInfo TimeZone
    {
        init { _timeZone = value; }
        get
        {
            return _timeZone ??= TimeZoneInfo.Utc;
        }
    }

    private string CronExpressionString { get; }

    public TimeSpan? Period()
    {
        var date = new DateTime(2014, 1, 1);
        DateTime.SpecifyKind(date, DateTimeKind.Utc);
        var after = GetTimeAfter(date);

        return after?.Subtract(date);
    }

    #region ICloneable Members

    public object Clone()
    {
        CronExpression copy;
        try
        {
            copy = new CronExpression(CronExpressionString)
            {
                TimeZone = TimeZone
            };
        }
        catch (FormatException)
        {
            throw new Exception("Not Cloneable.");
        }

        return copy;
    }

    #endregion

    #region IDeserializationCallback Members

    public void OnDeserialization(object sender)
    {
        BuildExpression(CronExpressionString);
    }

    #endregion

    public virtual bool IsSatisfiedBy(DateTime dateUtc)
    {
        var test =
            new DateTime(dateUtc.Year, dateUtc.Month, dateUtc.Day, dateUtc.Hour, dateUtc.Minute, dateUtc.Second).
                AddSeconds(-1);
        var timeAfter = GetTimeAfter(test);
        if (timeAfter.HasValue && timeAfter.Value.Equals(dateUtc))
        {
            return true;
        }

        return false;
    }

    public virtual DateTime? GetNextValidTimeAfter(DateTime date)
    {
        return GetTimeAfter(date);
    }

    public virtual DateTime? GetNextInvalidTimeAfter(DateTime date)
    {
        long difference = 1000;

        var lastDate = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second).AddSeconds(-1);

        while (difference == 1000)
        {
            var newDate = GetTimeAfter(lastDate).Value;
            difference = (long)(newDate - lastDate).TotalMilliseconds;
            if (difference == 1000)
            {
                lastDate = newDate;
            }
        }

        return lastDate.AddSeconds(1);
    }

    public override string ToString()
    {
        return CronExpressionString;
    }

    public static bool IsValidExpression(string cronExpression)
    {
        try
        {
            new CronExpression(cronExpression);
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    protected void BuildExpression(string expression)
    {
        try
        {
            _seconds ??= new TreeSet();
            _minutes ??= new TreeSet();
            _hours ??= new TreeSet();
            _daysOfMonth ??= new TreeSet();
            _months ??= new TreeSet();
            _daysOfWeek ??= new TreeSet();
            _years ??= new TreeSet();
            var exprOn = Second;


#if NET_20
                string[] exprsTok = expression.Trim().Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
#else
            var exprsTok = expression.Trim().Split(' ', '\t', '\r', '\n');
#endif
            foreach (var exprTok in exprsTok)
            {
                var expr = exprTok.Trim();
                if (expr.Length == 0)
                {
                    continue;
                }
                if (exprOn > Year)
                {
                    break;
                }

                if (exprOn == DayOfMonth && expr.Contains('L') && expr.Length > 1 && expr.Contains(','))
                {
                    throw new FormatException(
                        "Support for specifying 'L' and 'LW' with other days of the month is not implemented");
                }

                if (exprOn == DayOfWeek && expr.Contains('L') && expr.Length > 1 && expr.Contains(','))
                {
                    throw new FormatException("Support for specifying 'L' with other days of the week is not implemented");
                }
                var vTok = expr.Split(',');
                foreach (var v in vTok)
                {
                    StoreExpressionVals(0, v, exprOn);
                }
                exprOn++;
            }
            if (exprOn <= DayOfWeek)
            {
                throw new FormatException("Unexpected end of expression.");
            }
            if (exprOn <= Year)
            {
                StoreExpressionVals(0, "*", Year);
            }
           
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                                                    "Illegal cron expression format ({0})", e));
        }
    }

    protected virtual int StoreExpressionVals(int pos, string s, int type)
    {
        var incr = 0;
        var i = SkipWhiteSpace(pos, s);
        if (i >= s.Length)
        {
            return i;
        }
        var c = s[i];
        if (c is >= 'A' and <= 'Z' && (!s.Equals("L")) && (!s.Equals("LW")))
        {
            var sub = s.Substring(i, 3);
            int sval;
            var eval = -1;
            if (type == Month)
            {
                sval = GetMonthNumber(sub) + 1;
                if (sval <= 0)
                {
                    throw new FormatException($"Invalid Month value: '{sub}'");
                }
                if (s.Length > i + 3)
                {
                    c = s[i + 3];
                    if (c == '-')
                    {
                        i += 4;
                        sub = s.Substring(i, 3);
                        eval = GetMonthNumber(sub) + 1;
                        if (eval <= 0)
                        {
                            throw new FormatException($"Invalid Month value: '{sub}'");
                        }
                    }
                }
            }
            else if (type == DayOfWeek)
            {
                sval = GetDayOfWeekNumber(sub);
                if (sval < 0)
                {
                    throw new FormatException($"Invalid Day-of-Week value: '{sub}'");
                }
                if (s.Length > i + 3)
                {
                    c = s[i + 3];
                    if (c == '-')
                    {
                        i += 4;
                        sub = s.Substring(i, 3);
                        eval = GetDayOfWeekNumber(sub);
                        if (eval < 0)
                        {
                            throw new FormatException($"Invalid Day-of-Week value: '{sub}'");
                        }
                    }
                    else if (c == '#')
                    {
                        try
                        {
                            i += 4;
                            _nthdayOfWeek = Convert.ToInt32(s[i..], CultureInfo.InvariantCulture);
                            if (_nthdayOfWeek is < 1 or > 5)
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception)
                        {
                            throw new FormatException(
                                "A numeric value between 1 and 5 must follow the '#' option");
                        }
                    }
                    else if (c == 'L')
                    {
                        _lastdayOfWeek = true;
                        i++;
                    }
                }
            }
            else
            {
                throw new FormatException($"Illegal characters for this position: '{sub}'");
            }
            if (eval != -1)
            {
                incr = 1;
            }
            AddToSet(sval, eval, incr, type);

            return i + 3;
        }
        if (c == '?')
        {
            i++;
            if ((i + 1) < s.Length
                && s[i] != ' ' && s[i + 1] != '\t')
            {
                throw new FormatException("Illegal character after '?': "
                                          + s[i]);
            }
            if (type != DayOfWeek && type != DayOfMonth)
            {
                throw new FormatException(
                    "'?' can only be specified for Day-of-Month or Day-of-Week.");
            }
            if (type == DayOfWeek && !_lastdayOfMonth)
            {
                var val = (int)_daysOfMonth[^1];
                if (val == NoSpecInt)
                {
                    throw new FormatException(
                        "'?' can only be specified for Day-of-Month -OR- Day-of-Week.");
                }
            }
            AddToSet(NoSpecInt, -1, 0, type);

            return i;
        }
        if (c is '*' or '/')
        {
            if (c == '*' && (i + 1) >= s.Length)
            {
                AddToSet(AllSpecInt, -1, incr, type);

                return i + 1;
            }

            if (c == '/'
                && ((i + 1) >= s.Length || s[i + 1] == ' ' || s[i + 1] == '\t'))
            {
                throw new FormatException("'/' must be followed by an integer.");
            }

            if (c == '*')
            {
                i++;
            }
            c = s[i];
            if (c == '/')
            {
                i++;
                if (i >= s.Length)
                {
                    throw new FormatException("Unexpected end of string.");
                }
                incr = GetNumericValue(s, i);
                i++;
                if (incr > 10)
                {
                    i++;
                }
                if (incr > 59 && type is Second or Minute)
                {
                    throw new FormatException(
                        string.Format(CultureInfo.InvariantCulture, "Increment > 60 : {0}", incr));
                }

                if (incr > 23 && (type == Hour))
                {
                    throw new FormatException(
                        string.Format(CultureInfo.InvariantCulture, "Increment > 24 : {0}", incr));
                }

                if (incr > 31 && (type == DayOfMonth))
                {
                    throw new FormatException(
                        string.Format(CultureInfo.InvariantCulture, "Increment > 31 : {0}", incr));
                }

                if (incr > 7 && (type == DayOfWeek))
                {
                    throw new FormatException(
                        string.Format(CultureInfo.InvariantCulture, "Increment > 7 : {0}", incr));
                }

                if (incr > 12 && (type == Month))
                {
                    throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Increment > 12 : {0}",
                        incr));
                }
            }
            else
            {
                incr = 1;
            }
            AddToSet(AllSpecInt, -1, incr, type);

            return i;
        }

        if (c == 'L')
        {
            i++;
            if (type == DayOfMonth)
            {
                _lastdayOfMonth = true;
            }
            if (type == DayOfWeek)
            {
                AddToSet(7, 7, 0, type);
            }
            if (type == DayOfMonth && s.Length > i)
            {
                c = s[i];
                if (c == 'W')
                {
                    _nearestWeekday = true;
                    i++;
                }
            }

            return i;
        }

        if (c is >= '0' and <= '9')
        {
            var val = Convert.ToInt32(c.ToString(), CultureInfo.InvariantCulture);
            i++;
            if (i >= s.Length)
            {
                AddToSet(val, -1, -1, type);
            }
            else
            {
                c = s[i];
                if (c is >= '0' and <= '9')
                {
                    var vs = GetValue(val, s, i);
                    val = vs.TheValue;
                    i = vs.Pos;
                }
                i = CheckNext(i, s, val, type);

                return i;
            }
        }
        else
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Unexpected character: {0}", c));
        }

        return i;
    }

    protected virtual int CheckNext(int pos, string s, int val, int type)
    {
        var end = -1;
        var i = pos;
        if (i >= s.Length)
        {
            AddToSet(val, end, -1, type);

            return i;
        }
        var c = s[pos];
        if (c == 'L')
        {
            if (type == DayOfWeek)
            {
                _lastdayOfWeek = true;
            }
            else
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                                                        "'L' option is not valid here. (pos={0})", i));
            }
            var data = GetSet(type);
            data.Add(val);
            i++;

            return i;
        }
        if (c == 'W')
        {
            if (type == DayOfMonth)
            {
                _nearestWeekday = true;
            }
            else
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                                                        "'W' option is not valid here. (pos={0})", i));
            }
            var data = GetSet(type);
            data.Add(val);
            i++;

            return i;
        }
        if (c == '#')
        {
            if (type != DayOfWeek)
            {
                throw new FormatException(
                    string.Format(CultureInfo.InvariantCulture, "'#' option is not valid here. (pos={0})", i));
            }
            i++;
            try
            {
                _nthdayOfWeek = Convert.ToInt32(s[i..], CultureInfo.InvariantCulture);
                if (_nthdayOfWeek is < 1 or > 5)
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                throw new FormatException(
                    "A numeric value between 1 and 5 must follow the '#' option");
            }
            var data = GetSet(type);
            data.Add(val);
            i++;

            return i;
        }
        if (c == 'C')
        {
            if (type == DayOfWeek)
            {
                _calendardayOfWeek = true;
            }
            else if (type == DayOfMonth)
            {
                _calendardayOfMonth = true;
            }
            else
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                                                        "'C' option is not valid here. (pos={0})", i));
            }
            var data = GetSet(type);
            data.Add(val);
            i++;

            return i;
        }
        if (c == '-')
        {
            i++;
            c = s[i];
            var v = Convert.ToInt32(c.ToString(), CultureInfo.InvariantCulture);
            end = v;
            i++;
            if (i >= s.Length)
            {
                AddToSet(val, end, 1, type);

                return i;
            }
            c = s[i];
            if (c is >= '0' and <= '9')
            {
                var vs = GetValue(v, s, i);
                var v1 = vs.TheValue;
                end = v1;
                i = vs.Pos;
            }
            if (i < s.Length && (s[i] == '/'))
            {
                i++;
                c = s[i];
                var v2 = Convert.ToInt32(c.ToString(), CultureInfo.InvariantCulture);
                i++;
                if (i >= s.Length)
                {
                    AddToSet(val, end, v2, type);

                    return i;
                }
                c = s[i];
                if (c is >= '0' and <= '9')
                {
                    var vs = GetValue(v2, s, i);
                    var v3 = vs.TheValue;
                    AddToSet(val, end, v3, type);
                    i = vs.Pos;

                    return i;
                }

                AddToSet(val, end, v2, type);

                return i;
            }

            AddToSet(val, end, 1, type);

            return i;
        }
        if (c == '/')
        {
            i++;
            c = s[i];
            var v2 = Convert.ToInt32(c.ToString(), CultureInfo.InvariantCulture);
            i++;
            if (i >= s.Length)
            {
                AddToSet(val, end, v2, type);

                return i;
            }
            c = s[i];
            if (c is >= '0' and <= '9')
            {
                var vs = GetValue(v2, s, i);
                var v3 = vs.TheValue;
                AddToSet(val, end, v3, type);
                i = vs.Pos;

                return i;
            }

            throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                "Unexpected character '{0}' after '/'", c));
        }

        AddToSet(val, end, 0, type);
        i++;

        return i;
    }

    public virtual string GetExpressionSummary()
    {
        var buf = new StringBuilder();
        buf.Append("seconds: ");
        buf.Append(GetExpressionSetSummary(_seconds));
        buf.Append('\n');
        buf.Append("minutes: ");
        buf.Append(GetExpressionSetSummary(_minutes));
        buf.Append('\n');
        buf.Append("hours: ");
        buf.Append(GetExpressionSetSummary(_hours));
        buf.Append('\n');
        buf.Append("daysOfMonth: ");
        buf.Append(GetExpressionSetSummary(_daysOfMonth));
        buf.Append('\n');
        buf.Append("months: ");
        buf.Append(GetExpressionSetSummary(_months));
        buf.Append('\n');
        buf.Append("daysOfWeek: ");
        buf.Append(GetExpressionSetSummary(_daysOfWeek));
        buf.Append('\n');
        buf.Append("lastdayOfWeek: ");
        buf.Append(_lastdayOfWeek);
        buf.Append('\n');
        buf.Append("nearestWeekday: ");
        buf.Append(_nearestWeekday);
        buf.Append('\n');
        buf.Append("NthDayOfWeek: ");
        buf.Append(_nthdayOfWeek);
        buf.Append('\n');
        buf.Append("lastdayOfMonth: ");
        buf.Append(_lastdayOfMonth);
        buf.Append('\n');
        buf.Append("calendardayOfWeek: ");
        buf.Append(_calendardayOfWeek);
        buf.Append('\n');
        buf.Append("calendardayOfMonth: ");
        buf.Append(_calendardayOfMonth);
        buf.Append('\n');
        buf.Append("years: ");
        buf.Append(GetExpressionSetSummary(_years));
        buf.Append('\n');

        return buf.ToString();
    }

    protected virtual string GetExpressionSetSummary(ISet data)
    {
        if (data.Contains(NoSpec))
        {
            return "?";
        }
        if (data.Contains(AllSpec))
        {
            return "*";
        }
        var buf = new StringBuilder();
        var first = true;
        foreach (int iVal in data)
        {
            var val = iVal.ToString(CultureInfo.InvariantCulture);
            if (!first)
            {
                buf.Append(',');
            }
            buf.Append(val);
            first = false;
        }

        return buf.ToString();
    }

    protected virtual int SkipWhiteSpace(int i, string s)
    {
        for (; i < s.Length && (s[i] == ' ' || s[i] == '\t'); i++)
        {

        }

        return i;
    }

    protected virtual int FindNextWhiteSpace(int i, string s)
    {
        for (; i < s.Length && (s[i] != ' ' || s[i] != '\t'); i++)
        {

        }

        return i;
    }

    protected virtual void AddToSet(int val, int end, int incr, int type)
    {
        var data = GetSet(type);
        if (type is Second or Minute)
        {
            if ((val < 0 || val > 59 || end > 59) && (val != AllSpecInt))
            {
                throw new FormatException(
                    "Minute and Second values must be between 0 and 59");
            }
        }
        else if (type == Hour)
        {
            if ((val < 0 || val > 23 || end > 23) && (val != AllSpecInt))
            {
                throw new FormatException(
                    "Hour values must be between 0 and 23");
            }
        }
        else if (type == DayOfMonth)
        {
            if ((val < 1 || val > 31 || end > 31) && (val != AllSpecInt)
                && (val != NoSpecInt))
            {
                throw new FormatException(
                    "Day of month values must be between 1 and 31");
            }
        }
        else if (type == Month)
        {
            if ((val < 1 || val > 12 || end > 12) && (val != AllSpecInt))
            {
                throw new FormatException(
                    "Month values must be between 1 and 12");
            }
        }
        else if (type == DayOfWeek)
        {
            if ((val == 0 || val > 7 || end > 7) && (val != AllSpecInt)
                && (val != NoSpecInt))
            {
                throw new FormatException(
                    "Day-of-Week values must be between 1 and 7");
            }
        }
        if (incr is 0 or -1 && val != AllSpecInt)
        {
            if (val != -1)
            {
                data.Add(val);
            }
            else
            {
                data.Add(NoSpec);
            }

            return;
        }
        var startAt = val;
        var stopAt = end;
        if (val == AllSpecInt && incr <= 0)
        {
            incr = 1;
            data.Add(AllSpec);
        }
        switch (type)
        {
            case Second or Minute:
                {
                    if (stopAt == -1)
                    {
                        stopAt = 59;
                    }
                    if (startAt is -1 or AllSpecInt)
                    {
                        startAt = 0;
                    }

                    break;
                }
            case Hour:
                {
                    if (stopAt == -1)
                    {
                        stopAt = 23;
                    }
                    if (startAt is -1 or AllSpecInt)
                    {
                        startAt = 0;
                    }

                    break;
                }
            case DayOfMonth:
                {
                    if (stopAt == -1)
                    {
                        stopAt = 31;
                    }
                    if (startAt is -1 or AllSpecInt)
                    {
                        startAt = 1;
                    }

                    break;
                }
            case Month:
                {
                    if (stopAt == -1)
                    {
                        stopAt = 12;
                    }
                    if (startAt is -1 or AllSpecInt)
                    {
                        startAt = 1;
                    }

                    break;
                }
            case DayOfWeek:
                {
                    if (stopAt == -1)
                    {
                        stopAt = 7;
                    }
                    if (startAt is -1 or AllSpecInt)
                    {
                        startAt = 1;
                    }

                    break;
                }
            case Year:
                {
                    if (stopAt == -1)
                    {
                        stopAt = 2099;
                    }
                    if (startAt is -1 or AllSpecInt)
                    {
                        startAt = 1970;
                    }

                    break;
                }
        }

        var max = -1;
        if (stopAt < startAt)
        {
            max = type switch
            {
                Second => 60,
                Minute => 60,
                Hour => 24,
                Month => 12,
                DayOfWeek => 7,
                DayOfMonth => 31,
                Year => throw new ArgumentException("Start year must be less than stop year"),
                _ => throw new ArgumentException("Unexpected type encountered")
            };
            stopAt += max;
        }
        for (var i = startAt; i <= stopAt; i += incr)
        {
            if (max == -1)
            {
                data.Add(i);
            }
            else
            {
                var i2 = i % max;

                if (i2 == 0 && type is Month or DayOfWeek or DayOfMonth)
                {
                    i2 = max;
                }
                data.Add(i2);
            }
        }
    }

    protected virtual TreeSet GetSet(int type)
    {
        return type switch
        {
            Second => _seconds,
            Minute => _minutes,
            Hour => _hours,
            DayOfMonth => _daysOfMonth,
            Month => _months,
            DayOfWeek => _daysOfWeek,
            Year => _years,
            _ => null
        };
    }

    protected virtual ValueSet GetValue(int v, string s, int i)
    {
        var c = s[i];
        var sb = new StringBuilder();
        sb.Append(v.ToString(CultureInfo.InvariantCulture));
        while (c is >= '0' and <= '9')
        {
            sb.Append(c);
            i++;
            if (i >= s.Length)
            {
                break;
            }
            c = s[i];
        }
        var val = new ValueSet();
        if (i < s.Length)
        {
            val.Pos = i;
        }
        else
        {
            val.Pos = i + 1;
        }
        val.TheValue = Convert.ToInt32(sb.ToString(), CultureInfo.InvariantCulture);

        return val;
    }

    protected virtual int GetNumericValue(string s, int i)
    {
        var endOfVal = FindNextWhiteSpace(i, s);
        var val = s[i..endOfVal];

        return Convert.ToInt32(val, CultureInfo.InvariantCulture);
    }

    protected virtual int GetMonthNumber(string s)
    {
        if (_monthMap.ContainsKey(s))
        {
            return (int)_monthMap[s];
        }

        return -1;
    }

    protected virtual int GetDayOfWeekNumber(string s)
    {
        if (_dayMap.ContainsKey(s))
        {
            return (int)_dayMap[s];
        }

        return -1;
    }

    protected virtual DateTime? GetTime(int sc, int mn, int hr, int dayofmn, int mon)
    {
        try
        {
            if (sc == -1)
            {
                sc = 0;
            }
            if (mn == -1)
            {
                mn = 0;
            }
            if (hr == -1)
            {
                hr = 0;
            }
            if (dayofmn == -1)
            {
                dayofmn = 0;
            }
            if (mon == -1)
            {
                mon = 0;
            }

            return new DateTime(DateTime.UtcNow.Year, mon, dayofmn, hr, mn, sc);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private DateTime ProcessDayOfMonth(DateTime d, int day, int mon, DateTime afterTimeUtc, bool _lastdayOfMonth, bool _nearestWeekday, TreeSet _daysOfMonth)
    {
        var st = _daysOfMonth.TailSet(day);
        var t = day;

        if (_lastdayOfMonth)
        {
            if (!_nearestWeekday)
            {
                day = GetLastDayOfMonth(mon, d.Year);
            }
            else
            {
                day = AdjustLastDayOfMonth(d, mon, afterTimeUtc);
            }
        }
        else if (_nearestWeekday)
        {
            day = AdjustNearestWeekday(d, mon, afterTimeUtc, _daysOfMonth);
        }
        else if (st != null && st.Count != 0)
        {
            day = (int)st.First();
            var lastDay = GetLastDayOfMonth(mon, d.Year);
            if (day > lastDay)
            {
                day = (int)_daysOfMonth.First();
                mon++;
            }
        }
        else
        {
            day = (int)_daysOfMonth.First();
            mon++;
        }

        if (day != t)
        {
            if (mon > 12)
            {
                d = new DateTime(d.Year, 12, day, d.Hour, d.Minute, d.Second).AddMonths(mon - 12);
            }
            else
            {
                var lDay = DateTime.DaysInMonth(d.Year, mon);
                d = day <= lDay ? new DateTime(d.Year, mon, day, d.Hour, d.Minute, d.Second) : new DateTime(d.Year, mon, lDay, d.Hour, d.Minute, d.Second).AddDays(day - lDay);
            }
        }

        return d;
    }

    private int AdjustLastDayOfMonth(DateTime d, int mon, DateTime afterTimeUtc)
    {
        var day = GetLastDayOfMonth(mon, d.Year);
        var tcal = new DateTime(d.Year, mon, day, d.Hour, d.Minute, d.Second);
        var ldom = GetLastDayOfMonth(mon, d.Year);
        var dow = tcal.DayOfWeek;

        if (dow == System.DayOfWeek.Saturday && day == 1)
        {
            day += 2;
        }
        else if (dow == System.DayOfWeek.Saturday)
        {
            day -= 1;
        }
        else if (dow == System.DayOfWeek.Sunday && day == ldom)
        {
            day -= 2;
        }
        else if (dow == System.DayOfWeek.Sunday)
        {
            day += 1;
        }

        var nTime = new DateTime(tcal.Year, mon, day, tcal.Hour, tcal.Minute, tcal.Second, tcal.Millisecond);
        if (nTime.ToUniversalTime() < afterTimeUtc)
        {
            day = 1;
        }

        return day;
    }

    private int AdjustNearestWeekday(DateTime d, int mon, DateTime afterTimeUtc, TreeSet _daysOfMonth)
    {
        var day = (int)_daysOfMonth.First();
        var tcal = new DateTime(d.Year, mon, day, d.Hour, d.Minute, d.Second);
        var ldom = GetLastDayOfMonth(mon, d.Year);
        var dow = tcal.DayOfWeek;

        if (dow == System.DayOfWeek.Saturday && day == 1)
        {
            day += 2;
        }
        else if (dow == System.DayOfWeek.Saturday)
        {
            day -= 1;
        }
        else if (dow == System.DayOfWeek.Sunday && day == ldom)
        {
            day -= 2;
        }
        else if (dow == System.DayOfWeek.Sunday)
        {
            day += 1;
        }

        tcal = new DateTime(tcal.Year, mon, day, tcal.Hour, tcal.Minute, tcal.Second);
        if (tcal.ToUniversalTime() < afterTimeUtc)
        {
            day = (int)_daysOfMonth.First();
        }

        return day;
    }

    private DateTime ProcessDayOfWeek(DateTime d, int day, int mon)
    {
        var dow = (int)_daysOfWeek.First();
        var cDow = ((int)d.DayOfWeek) == 7 ? 1 : (int)d.DayOfWeek + 1;
        var daysToAdd = cDow < dow ? dow - cDow : dow + (7 - cDow);
        var lDay = GetLastDayOfMonth(mon, d.Year);

        if (_lastdayOfWeek)
        {
            daysToAdd = 0;
            if (cDow < dow)
            {
                daysToAdd = dow - cDow;
            }
            if (cDow > dow)
            {
                daysToAdd = dow + (7 - cDow);
            }
            if (day + daysToAdd > lDay)
            {
                if (mon == 12)
                {
                    if (d.Year == DateTime.MaxValue.Year)
                    {
                        throw new InvalidOperationException("Date exceeds maximum supported value.");
                    }

                    d = new DateTime(d.Year, mon - 11, 1, d.Hour, d.Minute, d.Second).AddYears(1);
                }
                else
                {
                    d = new DateTime(d.Year, mon + 1, 1, d.Hour, d.Minute, d.Second);
                }
                daysToAdd = day + daysToAdd - lDay - 1;
            }

            if (daysToAdd > 0)
            {
                d = new DateTime(d.Year, d.Month, d.Day + daysToAdd, d.Hour, d.Minute, d.Second);
            }
        }
        else if (_nthdayOfWeek != 0)
        {
            daysToAdd = 0;
            if (cDow < dow)
            {
                daysToAdd = dow - cDow;
            }
            else if (cDow > dow)
            {
                daysToAdd = dow + (7 - cDow);
            }
            var dayShifted = daysToAdd > 0;
            day += daysToAdd;
            var weekOfMonth = day / 7;
            if (day % 7 > 0)
            {
                weekOfMonth++;
            }
            daysToAdd = (_nthdayOfWeek - weekOfMonth) * 7;
            day += daysToAdd;
            if (daysToAdd < 0 || day > GetLastDayOfMonth(mon, d.Year))
            {
                if (mon == 12)
                {
                    if (d.Year == DateTime.MaxValue.Year)
                    {
                        throw new InvalidOperationException("Date exceeds maximum supported value.");
                    }

                    d = new DateTime(d.Year, mon - 11, 1, d.Hour, d.Minute, d.Second).AddYears(1);
                }
                else
                {
                    d = new DateTime(d.Year, mon + 1, 1, d.Hour, d.Minute, d.Second);
                }
            }

            if (daysToAdd > 0 || dayShifted)
            {
                d = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
            }
        }
        else
        {
            var st = _daysOfWeek.TailSet(cDow);
            if (st is { Count: > 0 })
            {
                dow = (int)st.First();
            }
            daysToAdd = 0;
            if (cDow < dow)
            {
                daysToAdd = dow - cDow;
            }
            if (cDow > dow)
            {
                daysToAdd = dow + (7 - cDow);
            }
            if (day + daysToAdd > lDay)
            {
                if (mon == 12)
                {
                    if (d.Year == DateTime.MaxValue.Year)
                    {
                        throw new InvalidOperationException("Date exceeds maximum supported value.");
                    }

                    d = new DateTime(d.Year, mon - 11, 1, d.Hour, d.Minute, d.Second).AddYears(1);
                }
                else
                {
                    d = new DateTime(d.Year, mon + 1, 1, d.Hour, d.Minute, d.Second);
                }
                daysToAdd = day + daysToAdd - lDay - 1;
            }

            if (daysToAdd > 0)
            {
                d = new DateTime(d.Year, d.Month, d.Day + daysToAdd, d.Hour, d.Minute, d.Second);
            }
        }
        return d;
    }

    private DateTime GetNearestDate(DateTime referenceDate, DateTime date1, DateTime date2)
    {
        var diff1 = (date1 - referenceDate).Duration();
        var diff2 = (date2 - referenceDate).Duration();

        return diff1 <= diff2 ? date1 : date2;
    }

    public virtual DateTime? GetTimeAfter(DateTime afterTimeUtc)
    {
        if (afterTimeUtc == DateTime.MaxValue)
        {
            return null;
        }

        afterTimeUtc = afterTimeUtc.AddSeconds(1);

        var d = CreateDateTimeWithoutMillis(afterTimeUtc);

        d = TimeZoneInfo.ConvertTimeFromUtc(d, TimeZone);
        var gotOne = false;

        while (!gotOne)
        {
            var sec = d.Second;

            var st = _seconds.TailSet(sec);
            if (st != null && st.Count != 0)
            {
                sec = (int)st.First();
            }
            else
            {
                sec = (int)_seconds.First();
                d = d.AddMinutes(1);
            }
            d = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, sec, d.Millisecond);
            var min = d.Minute;
            var hr = d.Hour;
            var t = -1;

            st = _minutes.TailSet(min);
            if (st != null && st.Count != 0)
            {
                t = min;
                min = (int)st.First();
            }
            else
            {
                min = (int)_minutes.First();
                hr++;
            }
            if (min != t)
            {
                d = new DateTime(d.Year, d.Month, d.Day, d.Hour, min, 0, d.Millisecond);
                d = SetCalendarHour(d, hr);

                continue;
            }
            d = new DateTime(d.Year, d.Month, d.Day, d.Hour, min, d.Second, d.Millisecond);
            hr = d.Hour;
            var day = d.Day;
            t = -1;

            st = _hours.TailSet(hr);
            if (st != null && st.Count != 0)
            {
                t = hr;
                hr = (int)st.First();
            }
            else
            {
                hr = (int)_hours.First();
                day++;
            }
            if (hr != t)
            {
                var daysInMonth = DateTime.DaysInMonth(d.Year, d.Month);
                if (day > daysInMonth)
                {
                    d = new DateTime(d.Year, d.Month, daysInMonth, d.Hour, 0, 0, d.Millisecond).AddDays(day - daysInMonth);
                }
                else
                {
                    d = new DateTime(d.Year, d.Month, day, d.Hour, 0, 0, d.Millisecond);
                }
                d = SetCalendarHour(d, hr);

                continue;
            }
            d = new DateTime(d.Year, d.Month, d.Day, hr, d.Minute, d.Second, d.Millisecond);
            day = d.Day;
            var mon = d.Month;

            var dayOfMSpec = !_daysOfMonth.Contains(NoSpec);
            var dayOfWSpec = !_daysOfWeek.Contains(NoSpec);

            var allDayOfMSpec = _daysOfMonth.Contains(AllSpec);
            var allDayOfWSpec = _daysOfWeek.Contains(AllSpec);
            
            if (dayOfMSpec && !dayOfWSpec)
            {
                d = ProcessDayOfMonth(d, day, mon, afterTimeUtc, _lastdayOfMonth, _nearestWeekday, _daysOfMonth);
            }
            else if (dayOfWSpec && !dayOfMSpec)
            {
                d = ProcessDayOfWeek(d, day, mon);
            }
            else
            {
                if (!allDayOfMSpec && allDayOfWSpec)
                {
                    d = ProcessDayOfMonth(d, day, mon, afterTimeUtc, _lastdayOfMonth, _nearestWeekday, _daysOfMonth);
                }
                else if (allDayOfMSpec && !allDayOfWSpec)
                {
                    d = ProcessDayOfWeek(d, day, mon);
                }
                else
                {
                    var tmpD = d;
                    var d1 = ProcessDayOfMonth(tmpD, day, mon, afterTimeUtc, _lastdayOfMonth, _nearestWeekday, _daysOfMonth);
                    var d2 = ProcessDayOfWeek(tmpD, day, mon);

                    d = GetNearestDate(d, d1, d2);
                }
            }

            mon = d.Month;
            var year = d.Year;
            t = -1;

            if (year > 2099)
            {
                return null;
            }

            st = _months.TailSet(mon);
            if (st != null && st.Count != 0)
            {
                t = mon;
                mon = (int)st.First();
            }
            else
            {
                mon = (int)_months.First();
                year++;
            }
            if (mon != t)
            {
                d = new DateTime(year, mon, 1, 0, 0, 0);

                continue;
            }
            d = new DateTime(d.Year, mon, d.Day, d.Hour, d.Minute, d.Second);
            year = d.Year;
            st = _years.TailSet(year);
            if (st != null && st.Count != 0)
            {
                t = year;
                year = (int)st.First();
            }
            else
            {
                return null;
            }
            if (year != t)
            {
                d = new DateTime(year, 1, 1, 0, 0, 0);

                continue;
            }
            d = new DateTime(year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
            gotOne = true;
        }

        return TimeZoneInfo.ConvertTimeToUtc(d, TimeZone);
    }

    protected static DateTime CreateDateTimeWithoutMillis(DateTime time)
    {
        return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
    }

    protected static DateTime SetCalendarHour(DateTime date, int hour)
    {
        var hourToSet = hour;
        if (hourToSet == 24)
        {
            hourToSet = 0;
        }
        var d =
            new DateTime(date.Year, date.Month, date.Day, hourToSet, date.Minute, date.Second, date.Millisecond);
        if (hour == 24)
        {
            d = d.AddDays(1);
        }

        return d;
    }

    public virtual DateTime? GetTimeBefore(DateTime? endTime)
    {
        return null;
    }

    public virtual DateTime? GetFinalFireTime()
    {
        return null;
    }

    protected virtual bool IsLeapYear(int year)
    {
        return DateTime.IsLeapYear(year);
    }

    protected virtual int GetLastDayOfMonth(int monthNum, int year)
    {
        return DateTime.DaysInMonth(year, monthNum);
    }
}

public class ValueSet
{
    public int Pos { get; set; }
    public int TheValue { get; set; }
}
