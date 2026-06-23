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

namespace ASC.Common.Radicale;

public class CardDavItem(Guid iD, string firstName, string lastName, string userName, DateTime? birthDate, bool? sex, string title, string email, List<string> contacts, string mobilePhone)
{
    public Guid ID { get; set; } = iD;

    public string FirstName { get; set; } = firstName;

    public string LastName { get; set; } = lastName;

    public string UserName { get; set; } = userName;

    public DateTime? BirthDate { get; set; } = birthDate;

    public bool? Sex { get; set; } = sex;

    public string Title { get; set; } = title;

    public string Email { get; set; } = email;

    public List<string> Contacts { get; set; } = contacts;

    public string MobilePhone { get; set; } = mobilePhone;

    public override string ToString()
    {
        return string.Format("{0} {1}", FirstName, LastName).Trim();
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
}