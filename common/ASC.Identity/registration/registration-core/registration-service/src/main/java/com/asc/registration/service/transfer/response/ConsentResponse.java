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

package com.asc.registration.service.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import java.time.ZonedDateTime;
import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Schema(description = "One consent a user has granted to a client, with the scopes it covers.")
public class ConsentResponse {
  /** The ID of the registered client. */
  @JsonProperty("registered_client_id")
  @Schema(
      description = "The identifier of the client the user granted the consent to.",
      example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
  private String registeredClientId;

  /** The scopes associated with the consent. */
  @Schema(
      description =
          "The scopes covered by this consent, as a single space-separated string rather than a "
              + "list.",
      example = "files:read files:write")
  private String scopes;

  /** The date and time when the consent was last modified. */
  @JsonProperty("modified_at")
  @Schema(
      description =
          "When the consent was last granted or changed. This is the value to carry into the next "
              + "page as last_modified_on.",
      example = "2024-04-04T12:00:00Z")
  private ZonedDateTime modifiedOn;

  /** Detailed information about the client associated with the consent. */
  @Schema(
      description =
          "The consent-facing details of the client, embedded so that a list of consents can be "
              + "rendered without a further call per client. It is null when the client behind the "
              + "consent no longer exists.")
  private ClientInfoResponse client;
}
