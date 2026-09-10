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

package com.asc.registration.application.configuration;

import com.asc.registration.application.transfer.ValidationErrorResponse;
import io.swagger.v3.core.converter.ModelConverters;
import io.swagger.v3.oas.models.media.ArraySchema;
import io.swagger.v3.oas.models.media.Schema;
import java.util.List;
import org.springdoc.core.customizers.OpenApiCustomizer;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Customizes the OpenAPI spec to exclude legacy backward-compatible paths from documentation. */
@Configuration
public class OpenApiCustomizerConfiguration {
  @Value("${spring.application.web.api}")
  private String webApi;

  /** Removes legacy client and scope paths from the generated OpenAPI spec. */
  @Bean
  public OpenApiCustomizer excludeLegacyPathsCustomizer() {
    return openApi -> {
      openApi.setServers(null);

      var oldPrefixes =
          List.of(String.format("%s/clients", webApi), String.format("%s/scopes", webApi));

      var toRemove =
          openApi.getPaths().keySet().stream()
              .filter(
                  path ->
                      oldPrefixes.stream().anyMatch(path::startsWith) && !path.contains("/oauth2/"))
              .toList();

      toRemove.forEach(openApi.getPaths()::remove);
    };
  }

  /**
   * Registers {@link ValidationErrorResponse} (and its nested {@code FieldError}) and documents the
   * {@code errors} extension that validation and invalid-scope handlers attach to {@code
   * ProblemDetail}.
   */
  @Bean
  public OpenApiCustomizer validationErrorSchemasCustomizer() {
    return openApi -> {
      var components = openApi.getComponents();
      if (components == null) return;

      var validationSchemas = ModelConverters.getInstance().readAll(ValidationErrorResponse.class);
      validationSchemas.forEach(components::addSchemas);

      var problemDetail = components.getSchemas().get("ProblemDetail");
      if (problemDetail == null) return;

      var fieldErrorSchemaName =
          validationSchemas.keySet().stream()
              .filter(name -> name.endsWith("FieldError"))
              .findFirst()
              .orElse(null);
      if (fieldErrorSchemaName == null) return;

      var fieldErrorRef = new Schema<>().$ref("#/components/schemas/" + fieldErrorSchemaName);
      var errorsSchema = new ArraySchema();
      errorsSchema.setDescription(
          "Field-specific validation errors. Present when the request body or parameters "
              + "failed validation, or when a named scope is not in the tenant catalogue.");
      errorsSchema.setItems(fieldErrorRef);
      problemDetail.addProperty("errors", errorsSchema);
    };
  }
}
