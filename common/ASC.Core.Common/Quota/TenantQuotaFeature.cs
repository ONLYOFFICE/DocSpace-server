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

namespace ASC.Core.Common.Quota;

[DebuggerDisplay("{Name}")]
public class TenantQuotaFeature(string name, bool paid = false)
{
    public int Order { get; init; }
    public bool Visible { get; init; } = true;
    public string Name { get; } = name;
    public bool Paid { get; } = paid;
    public bool Standalone { get; init; }

    public EmployeeType EmployeeType { get; init; } = EmployeeType.All;

    protected internal virtual void Multiply(int quantity)
    {

    }
}

public class TenantQuotaFeature<T>(TenantQuota tenantQuota, string name, T @default = default, bool paid = false) : TenantQuotaFeature(name, paid)
{
    public virtual T Value
    {
        get
        {
            var parsed = tenantQuota.GetFeature(Name);

            if (parsed == null)
            {
                return Default;
            }

            if (!TryParse(parsed, out var result))
            {
                return Default;
            }

            return result;
        }
        set => tenantQuota.ReplaceFeature(Name, value, Default);
    }

    public T Default { get; } = @default;

    protected virtual bool TryParse(string s, out T result)
    {
        result = default;
        return false;
    }
}

public class TenantQuotaFeatureCount(TenantQuota tenantQuota, string name, bool paid = false) : TenantQuotaFeature<int>(tenantQuota, name, int.MaxValue, paid)
{
    protected override bool TryParse(string s, out int result)
    {
        return int.TryParse(s[(s.IndexOf(':') + 1)..], out result);
    }

    protected internal override void Multiply(int quantity)
    {
        try
        {
            if (Value != int.MaxValue)
            {
                Value = checked(Value * quantity);
            }
        }
        catch (OverflowException)
        {
            Value = int.MaxValue;
        }
    }
}

public class TenantQuotaFeatureFixedCount(TenantQuota tenantQuota, string name) : TenantQuotaFeature<int>(tenantQuota, name)
{
    protected override bool TryParse(string s, out int result)
    {
        result = 0;
        var parts = s.Split([':'], 3, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3 && parts[2] == "fixed" && int.TryParse(parts[1], out result);
    }

    protected internal override void Multiply(int quantity)
    {
    }
}

public class TenantQuotaFeatureSize(TenantQuota tenantQuota, string name, bool paid = false) : TenantQuotaFeature<long>(tenantQuota, name, long.MaxValue, paid)
{
    protected override bool TryParse(string s, out long result)
    {
        return long.TryParse(s[(s.IndexOf(':') + 1)..], out result);
    }

    protected internal override void Multiply(int quantity)
    {
        try
        {
            if (Value != long.MaxValue)
            {
                Value = checked(Value * quantity);
            }
        }
        catch (OverflowException)
        {
            Value = long.MaxValue;
        }
    }

    public override long Value
    {
        get => ByteConverter.GetInBytes(base.Value);
        set => base.Value = ByteConverter.GetInMBytes(value);
    }
}

public class TenantQuotaFeatureFlag(TenantQuota tenantQuota, string name, bool paid = false) : TenantQuotaFeature<bool>(tenantQuota, name, false, paid)
{
    protected override bool TryParse(string s, out bool result)
    {
        result = true;
        return true;
    }
}