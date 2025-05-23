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

namespace ASC.Core.Security.Authentication;

class UserAccount(UserInfo info, int tenant, UserFormatter userFormatter) : MarshalByRefObject, IUserAccount
{
    public Guid ID { get; private set; } = info.Id;
    public string Name { get; private set; } = userFormatter.GetUserName(info);
    public string AuthenticationType => "ASC";
    public bool IsAuthenticated => true;
    public string FirstName { get; private set; } = info.FirstName;
    public string LastName { get; private set; } = info.LastName;
    public string Title { get; private set; } = info.Title;
    public int Tenant { get; private set; } = tenant;
    public string Email { get; private set; } = info.Email;

    public object Clone()
    {
        return MemberwiseClone();
    }

    public override bool Equals(object obj)
    {
        return obj is IUserAccount a && ID.Equals(a.ID);
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}
