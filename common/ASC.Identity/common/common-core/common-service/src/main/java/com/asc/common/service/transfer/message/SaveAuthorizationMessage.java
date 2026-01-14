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

package com.asc.common.service.transfer.message;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Message object used for saving authorization information to a remote region via messaging (RPC).
 *
 * <p>This DTO is used to request authorization persistence in a remote region when the
 * authorization code's region prefix doesn't match the current region.
 */
@Builder
@Getter
@Setter
@ToString
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class SaveAuthorizationMessage {
  @JsonProperty(value = "id")
  private String id;

  @JsonProperty(value = "registered_client_id", required = true)
  private String registeredClientId;

  @JsonProperty(value = "principal_id", required = true)
  private String principalId;

  @JsonProperty(value = "tenant_id")
  private Long tenantId;

  @JsonProperty(value = "state")
  private String state;

  @JsonProperty(value = "attributes")
  private String attributes;

  @JsonProperty(value = "authorization_grant_type", required = true)
  private String authorizationGrantType;

  @JsonProperty(value = "authorized_scopes")
  private String authorizedScopes;

  @JsonProperty(value = "authorization_code_value")
  private String authorizationCodeValue;

  @JsonProperty(value = "authorization_code_metadata")
  private String authorizationCodeMetadata;

  @JsonProperty(value = "authorization_code_issued_at")
  private ZonedDateTime authorizationCodeIssuedAt;

  @JsonProperty(value = "authorization_code_expires_at")
  private ZonedDateTime authorizationCodeExpiresAt;

  @JsonProperty(value = "access_token_type")
  private String accessTokenType;

  @JsonProperty(value = "access_token_value")
  private String accessTokenValue;

  @JsonProperty(value = "access_token_hash")
  private String accessTokenHash;

  @JsonProperty(value = "access_token_scopes")
  private String accessTokenScopes;

  @JsonProperty(value = "access_token_metadata")
  private String accessTokenMetadata;

  @JsonProperty(value = "access_token_issued_at")
  private ZonedDateTime accessTokenIssuedAt;

  @JsonProperty(value = "access_token_expires_at")
  private ZonedDateTime accessTokenExpiresAt;

  @JsonProperty(value = "refresh_token_value")
  private String refreshTokenValue;

  @JsonProperty(value = "refresh_token_hash")
  private String refreshTokenHash;

  @JsonProperty(value = "refresh_token_metadata")
  private String refreshTokenMetadata;

  @JsonProperty(value = "refresh_token_issued_at")
  private ZonedDateTime refreshTokenIssuedAt;

  @JsonProperty(value = "refresh_token_expires_at")
  private ZonedDateTime refreshTokenExpiresAt;

  @JsonProperty(value = "id_token_value")
  private String idTokenValue;

  @JsonProperty(value = "id_token_claims")
  private String idTokenClaims;

  @JsonProperty(value = "id_token_metadata")
  private String idTokenMetadata;

  @JsonProperty(value = "id_token_issued_at")
  private ZonedDateTime idTokenIssuedAt;

  @JsonProperty(value = "id_token_expires_at")
  private ZonedDateTime idTokenExpiresAt;
}
