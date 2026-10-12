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

import com.asc.common.service.transfer.response.ClientResponse;
import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Schema;
import java.time.ZonedDateTime;
import java.util.List;
import lombok.*;

/**
 * OpenAPI companion for {@link PageableResponse}{@code <ClientResponse>}. Declares {@code data} as
 * an array of {@link ClientResponse} so generators are not left with an untyped page payload after
 * generic erasure.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Schema(
    name = "PageableClientResponse",
    description = "One page of full client records together with the next-page cursor.")
public class PageableClientResponse {
  @ArraySchema(
      schema = @Schema(implementation = ClientResponse.class),
      arraySchema =
          @Schema(
              description =
                  "The items on this page, at most as many as the requested limit. An empty array "
                      + "means there is nothing further to read."))
  private List<ClientResponse> data;

  @JsonProperty("last_client_id")
  @Schema(
      description =
          "The cursor to send back as last_client_id to ask for the next page, together with "
              + "last_created_on. It is null when the page is empty.",
      example = "6c7cf17b-1bd3-47d5-94c6-be2d3570e168")
  private String lastClientId;

  @JsonProperty("last_created_on")
  @Schema(
      description =
          "The cursor to send back as last_created_on to ask for the next page, together with "
              + "last_client_id. It is null when the page is empty.",
      example = "2024-04-04T12:00:00Z")
  private ZonedDateTime lastCreatedOn;

  @Schema(
      description = "The page size that was applied to this request, between 1 and 50.",
      example = "50")
  private int limit;
}
