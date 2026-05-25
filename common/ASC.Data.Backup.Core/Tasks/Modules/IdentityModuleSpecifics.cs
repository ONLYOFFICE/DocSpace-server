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
        new("identity_clients", "client_id", "identity_consent_scopes", "registered_client_id")
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