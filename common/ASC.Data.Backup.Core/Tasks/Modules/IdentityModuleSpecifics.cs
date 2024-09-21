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


namespace ASC.Data.Backup.Tasks.Modules;
public class IdentityModuleSpecifics(Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override ModuleName ModuleName => ModuleName.Identity;

    public override IEnumerable<TableInfo> Tables => _tables;

    public override IEnumerable<RelationInfo> TableRelations => _tableRelations;

    private readonly TableInfo[] _tables =
    [
        new("identity_clients", "tenant_id", "client_id", IdType.Guid),
        new("identity_authorizations", "tenant_id", "id", IdType.Guid){ UserIDColumns = ["principal_id"] },
        new("identity_client_allowed_origins"),
        new("identity_client_authentication_methods"),
        new("identity_client_redirect_uris"),
        new("identity_client_scopes"),
        new("identity_consents"){ UserIDColumns = ["principal_id"] },
        new("identity_consent_scopes"){ UserIDColumns = ["principal_id"] }
    ];

    private readonly RelationInfo[] _tableRelations =
    [
        new("identity_clients", "client_id", "identity_authorizations", "registered_client_id"),
        new("identity_clients", "client_id", "identity_client_allowed_origins", "client_id"),
        new("identity_clients", "client_id", "identity_client_authentication_methods", "client_id"),
        new("identity_clients", "client_id", "identity_client_redirect_uris", "client_id"),
        new("identity_clients", "client_id", "identity_client_scopes", "client_id"),
        new("identity_clients", "client_id", "identity_consents", "registered_client_id"),
        new("identity_clients", "client_id", "identity_consent_scopes", "registered_client_id"),
    ];

    protected override string GetSelectCommandConditionText(int tenantId, TableInfo table)
    {
        if (table.Name is "identity_client_allowed_origins" or "identity_client_authentication_methods" or "identity_client_redirect_uris" or "identity_client_scopes")
        {
            return "inner join identity_clients as t1 on t1.client_id = t.client_id where t1.tenant_id = " + tenantId;
        }

        if (table.Name is "identity_consents" or "identity_consent_scopes")
        {
            return "inner join identity_clients as t1 on t1.client_id = t.registered_client_id where t1.tenant_id = " + tenantId;
        }

        return base.GetSelectCommandConditionText(tenantId, table);
    }
}
