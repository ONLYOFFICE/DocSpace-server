// (c) Copyright Ascensio System SIA 2009-2026
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
import java.io.Serializable;
import java.util.List;
import lombok.*;

/**
 * Message object containing a registered client from a remote region.
 *
 * <p>This DTO is used to return the client information from a remote region for cross-region client
 * lookup. Used in token introspection when validating tokens from different regions.
 */
@Builder
@Getter
@Setter
@ToString
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class ClientRetrievedEvent implements Serializable {
  /** The unique client identifier. */
  @JsonProperty(value = "client_id")
  private String clientId;

  /** The client display name. */
  @JsonProperty(value = "name")
  private String name;

  /** The client secret for authentication. */
  @JsonProperty(value = "client_secret")
  private String clientSecret;

  /** The authentication methods supported by the client. */
  @JsonProperty(value = "authentication_methods")
  private List<String> authenticationMethods;

  /** The registered redirect URIs for the client. */
  @JsonProperty(value = "redirect_uris")
  private List<String> redirectUris;

  /** The OAuth2 scopes granted to the client. */
  @JsonProperty(value = "scopes")
  private List<String> scopes;
}
