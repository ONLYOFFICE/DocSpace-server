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
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
package com.asc.transfer.mapper;

import com.asc.transfer.entity.ClientEntity;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import org.springframework.jdbc.core.RowMapper;

/**
 * A {@link RowMapper} implementation that maps rows of a SQL {@link ResultSet} to {@link
 * ClientEntity} instances.
 *
 * <p>This class expects the {@code ResultSet} to contain the following columns:
 *
 * <ul>
 *   <li><strong>client_id</strong>: the identifier of the client
 *   <li><strong>tenant_id</strong>: the tenant identifier
 *   <li><strong>client_secret</strong>: the client's secret
 *   <li><strong>name</strong>: the client's name
 *   <li><strong>description</strong>: a description of the client
 *   <li><strong>logo</strong>: the logo URL of the client
 *   <li><strong>website_url</strong>: the client's website URL
 *   <li><strong>terms_url</strong>: the URL for the terms and conditions
 *   <li><strong>policy_url</strong>: the URL for the privacy policy
 *   <li><strong>logout_redirect_uri</strong>: the logout redirect URI
 *   <li><strong>is_public</strong>: indicates if the client is publicly accessible
 *   <li><strong>is_enabled</strong>: indicates if the client is enabled
 *   <li><strong>created_on</strong>: the timestamp when the client was created
 *   <li><strong>created_by</strong>: the identifier of the creator
 *   <li><strong>modified_on</strong>: the timestamp when the client was last modified
 *   <li><strong>modified_by</strong>: the identifier of the person who last modified the client
 *   <li><strong>version</strong>: the version of the client record
 * </ul>
 *
 * Timestamps are converted to {@link ZonedDateTime} using the UTC time zone.
 */
public class ClientRowMapper implements RowMapper<ClientEntity> {
  /** The string representation of the UTC time zone. */
  private final String UTC = "UTC";

  /**
   * Maps the current row of the given {@link ResultSet} to a new {@link ClientEntity} instance.
   *
   * @param rs the {@link ResultSet} to map (never {@code null})
   * @param rowNum the number of the current row
   * @return the resulting {@link ClientEntity} instance
   * @throws SQLException if an error occurs while accessing the {@link ResultSet}
   */
  public ClientEntity mapRow(ResultSet rs, int rowNum) throws SQLException {
    var client = new ClientEntity();
    client.setClientId(rs.getString("client_id"));
    client.setTenantId(rs.getLong("tenant_id"));
    client.setClientSecret(rs.getString("client_secret"));
    client.setName(rs.getString("name"));
    client.setDescription(rs.getString("description"));
    client.setLogo(rs.getString("logo"));
    client.setWebsiteUrl(rs.getString("website_url"));
    client.setTermsUrl(rs.getString("terms_url"));
    client.setPolicyUrl(rs.getString("policy_url"));
    client.setLogoutRedirectUri(rs.getString("logout_redirect_uri"));
    client.setAccessible(rs.getBoolean("is_public"));
    client.setEnabled(rs.getBoolean("is_enabled"));

    var created = rs.getTimestamp("created_on");
    if (created != null)
      client.setCreatedOn(ZonedDateTime.ofInstant(created.toInstant(), ZoneId.of(UTC)));
    client.setCreatedBy(rs.getString("created_by"));

    var modified = rs.getTimestamp("modified_on");
    if (modified != null)
      client.setModifiedOn(ZonedDateTime.ofInstant(modified.toInstant(), ZoneId.of(UTC)));
    client.setModifiedBy(rs.getString("modified_by"));
    client.setVersion(rs.getInt("version"));

    return client;
  }
}
