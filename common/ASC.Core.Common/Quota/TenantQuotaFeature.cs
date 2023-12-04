// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Core.Common.Quota;

public class TenantQuotaFeature
{
    public int Order { get; init; }
    public virtual bool Visible { get; init; } = true;
    public virtual string Name { get; init; }
    public virtual bool Paid { get; init; }
    public bool Standalone { get; init; }

    protected internal virtual void Multiply(int quantity)
    {

    }
}

public class TenantQuotaFeature<T>(TenantQuota tenantQuota) : TenantQuotaFeature
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
        set
        {
            tenantQuota.ReplaceFeature(Name, value, Default);
        }
    }

    public virtual T Default { get; }

    protected virtual bool TryParse(string s, out T result)
    {
        result = default;
        return false;
    }
}

public class TenantQuotaFeatureCount(TenantQuota tenantQuota) : TenantQuotaFeature<int>(tenantQuota)
{
    public override int Default => int.MaxValue;

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

public class TenantQuotaFeatureSize(TenantQuota tenantQuota) : TenantQuotaFeature<long>(tenantQuota)
{
    public override long Default => long.MaxValue;

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

public class TenantQuotaFeatureFlag(TenantQuota tenantQuota) : TenantQuotaFeature<bool>(tenantQuota)
{
    protected override bool TryParse(string s, out bool result)
    {
        result = true;
        return true;
    }
}