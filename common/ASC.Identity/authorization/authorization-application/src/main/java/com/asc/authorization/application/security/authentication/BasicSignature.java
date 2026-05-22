// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
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

package com.asc.authorization.application.security.authentication;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.PropertyNamingStrategies;
import com.fasterxml.jackson.databind.annotation.JsonNaming;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

/**
 * Model class representing a basic signature containing user and tenant information.
 *
 * <p>This class is used to encapsulate essential user and tenant details in the context of
 * authentication and authorization. It supports JSON serialization and deserialization using
 * snake_case naming strategy and ignores unknown properties during deserialization.
 */
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
@JsonNaming(value = PropertyNamingStrategies.SnakeCaseStrategy.class)
public class BasicSignature {

  /** The unique identifier for the user. */
  private String userId;

  /** The username of the user. */
  private String userName;

  /** The email address of the user. */
  private String userEmail;

  /** The unique identifier for the tenant associated with the user. */
  private long tenantId;

  /** The URL of the tenant associated with the user. */
  private String tenantUrl;

  /** Indicates whether the user has administrative privileges. */
  @JsonProperty("is_admin")
  private boolean isAdmin;

  /** Indicates whether the user is a guest. */
  @JsonProperty("is_guest")
  private boolean isGuest;

  /** Indicates whether the user has public access (not for admins). */
  @JsonProperty("is_public")
  private boolean isPublic;
}
