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

[DebuggerDisplay("{State} before {DueDate}")]
public class Tariff
{
    [SwaggerSchemaCustom("ID")]
    public int Id { get; set; }

    [SwaggerSchemaCustomString("Tariff state", Example = "Trial")]
    public TariffState State { get; set; }

    [SwaggerSchemaCustom("Due date")]
    public DateTime DueDate { get; set; }

    [SwaggerSchemaCustom("Delay due date")]
    public DateTime DelayDueDate { get; set; }

    [SwaggerSchemaCustom("License date")]
    public DateTime LicenseDate { get; set; }

    [SwaggerSchemaCustom("Customer ID")]
    public string CustomerId { get; set; }

    [SwaggerSchemaCustom<List<Quota>>("List of quotas")]
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
            && t.Quotas.Exists(Quotas.Contains)
            && t.CustomerId == CustomerId;
    }
}

public class Quota(int id, int quantity) : IEquatable<Quota>
{
    [SwaggerSchemaCustom("ID")]
    public int Id { get; set; } = id;

    [SwaggerSchemaCustom("Quantity")]
    public int Quantity { get; set; } = quantity;

    public bool Equals(Quota other)
    {
        return other != null && other.Id == Id && other.Quantity == Quantity;
    }
}
