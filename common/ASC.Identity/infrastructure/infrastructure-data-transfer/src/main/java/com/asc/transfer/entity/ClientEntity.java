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
package com.asc.transfer.entity;

import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Set;
import lombok.*;

/** Represents a client entity. */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientEntity implements Serializable {

  /** The unique identifier for the client. */
  private String clientId;

  /** The identifier for the tenant associated with the client. */
  private long tenantId;

  /** The secret for the client. This field is unique. */
  private String clientSecret;

  /** The name of the client. */
  private String name;

  /** The description of the client. */
  private String description;

  /** The logo for the client. */
  private String logo;

  private Set<AuthenticationMethod> authenticationMethods;

  /** The URL for the client's website. */
  private String websiteUrl;

  /** The URL for the client's terms. */
  private String termsUrl;

  /** The URL for the client's policy. */
  private String policyUrl;

  /** The redirect URIs for the client. */
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  private Set<String> allowedOrigins;

  /** The logout redirect URI for the client. */
  private String logoutRedirectUri;

  /** Indicates whether the client is public. */
  private boolean accessible;

  /** Indicates whether the client is enabled. */
  private boolean enabled;

  /** Indicates whether the client is invalidated. */
  private boolean invalidated;

  /** The scopes for the client. */
  private Set<ScopeEntity> scopes;

  /** The date and time the client was created. */
  private ZonedDateTime createdOn;

  /** The user who created the client. */
  private String createdBy;

  /** The date and time the client was last modified. */
  private ZonedDateTime modifiedOn;

  /** The user who last modified the client. */
  private String modifiedBy;

  /**
   * The version field is used for optimistic locking. It tracks the number of updates made to the
   * entity. This helps to avoid conflicts during concurrent updates by ensuring that a transaction
   * updates an entity based on the most recent version.
   */
  private Integer version;
}
