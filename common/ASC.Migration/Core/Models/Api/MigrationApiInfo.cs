// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Migration.Core.Models.Api;

[ProtoContract]
public class MigrationApiInfo
{
    [ProtoMember(1)]
    public string MigratorName { get; set; }

    [ProtoMember(2)]
    public string Operation { get; set; }

    [ProtoMember(3)]
    public List<string> FailedArchives { get; set; } = new List<string>();

    [ProtoMember(4)]
    public List<MigratingApiUser> Users { get; set; } = new List<MigratingApiUser>();

    [ProtoMember(5)]
    public List<MigratingApiUser> WithoutEmailUsers { get; set; } = new List<MigratingApiUser>();

    [ProtoMember(6)]
    public List<MigratingApiUser> ExistUsers { get; set; } = new List<MigratingApiUser>();
    
    [ProtoMember(7)]
    public List<MigratingApiGroup> Groups { get; set; } = new List<MigratingApiGroup>();

    [ProtoMember(8)]
    public bool ImportPersonalFiles { get; set; } 
    [ProtoMember(9)]
    public bool ImportSharedFiles { get; set; }
    
    [ProtoMember(10)]
    public bool ImportSharedFolders { get; set; } 
    
    [ProtoMember(11)]
    public bool ImportCommonFiles { get; set; }
    
    [ProtoMember(12)]
    public bool ImportProjectFiles { get; set; } 
    
    [ProtoMember(13)]
    public bool ImportGroups { get; set; }
    
    [ProtoMember(14)]
    public int SuccessedUsers { get; set; }
    [ProtoMember(15)]
    public int FailedUsers { get; set; }
    [ProtoMember(16)]
    public List<string> Files { get; set; }
}
