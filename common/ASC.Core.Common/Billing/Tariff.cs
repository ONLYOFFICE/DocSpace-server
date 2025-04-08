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

namespace ASC.Core.Billing;

/// <summary>
/// The tariff parameters.
/// </summary>
[DebuggerDisplay("{State} before {DueDate}")]
[ProtoContract]
public class Tariff
{
    /// <summary>
    /// The tariff ID.
    /// </summary>
    [ProtoMember(1)]
    public int Id { get; set; }

    /// <summary>
    /// The tariff state.
    /// </summary>
    [ProtoMember(2)]
    public TariffState State { get; set; }

    /// <summary>
    /// The tariff due date.
    /// </summary>
    [ProtoMember(3)]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// The tariff delay due date.
    /// </summary>
    [ProtoMember(4)]
    public DateTime DelayDueDate { get; set; }

    /// <summary>
    /// The tariff license date.
    /// </summary>
    [ProtoMember(5)]
    public DateTime LicenseDate { get; set; }

    /// <summary>
    /// The tariff customer ID.
    /// </summary>
    [ProtoMember(6)]
    public string CustomerId { get; set; }

    /// <summary>
    /// The list of tariff quotas.
    /// </summary>
    [ProtoMember(7)]
    public List<Quota> Quotas { get; set; }

    public override int GetHashCode()
    {
        return DueDate.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is Tariff t && t.DueDate == DueDate;
    }

    public bool EqualsByParams(Tariff t)
    {
        return t != null
            && t.DueDate == DueDate
            && t.Quotas.Count == Quotas.Count
            && t.Quotas.TrueForAll(Quotas.Contains)
            && t.CustomerId == CustomerId;
    }
}

/// <summary>
/// The quota parameters.
/// </summary>
[ProtoContract]
public class Quota : IEquatable<Quota>
{
    /// <summary>
    /// The quota ID.
    /// </summary>
    [ProtoMember(1)]
    public int Id { get; set; }

    /// <summary>
    /// The quota quantity.
    /// </summary>
    [ProtoMember(2)]
    public int Quantity { get; set; }

    public Quota()
    {
        
    }

    public Quota(int id, int quantity)
    {
        Id = id;
        Quantity = quantity;
    }

    public bool Equals(Quota other)
    {
        return other != null && other.Id == Id && other.Quantity == Quantity;
    }
}
