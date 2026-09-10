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
import io.swagger.v3.oas.models.Components;
import io.swagger.v3.oas.models.media.ArraySchema;
import io.swagger.v3.oas.models.media.Content;
import io.swagger.v3.oas.models.media.MediaType;
import io.swagger.v3.oas.models.media.Schema;
import io.swagger.v3.oas.models.responses.ApiResponse;
import io.swagger.v3.oas.models.responses.ApiResponses;
import io.swagger.v3.oas.models.tags.Tag;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.springdoc.core.customizers.OpenApiCustomizer;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Customizes the OpenAPI spec for path filtering and shared error documentation. */
@Configuration
public class OpenApiCustomizerConfiguration {
  private static final String PROBLEM_DETAIL_REF = "#/components/schemas/ProblemDetail";
  private static final String PATH_NOT_FOUND = "PathNotFound";
  private static final String METHOD_NOT_ALLOWED = "MethodNotAllowed";
  private static final String UNSUPPORTED_MEDIA_TYPE = "UnsupportedMediaType";
  private static final String NOT_ACCEPTABLE = "NotAcceptable";

  private static final String PATH_NOT_FOUND_DESCRIPTION =
      "No route matches this path. Distinct from a 404 returned by an operation when a client ID "
          + "is unknown or not visible to the caller.";
  private static final String METHOD_NOT_ALLOWED_DESCRIPTION =
      "The HTTP method is not allowed for this path";
  private static final String UNSUPPORTED_MEDIA_TYPE_DESCRIPTION =
      "The Content-Type header is not application/json";
  private static final String NOT_ACCEPTABLE_DESCRIPTION =
      "The Accept header does not allow application/json";

  private static final String OAUTH_TAG_GROUP = "OAuth 2.0";
  private static final String X_TAG_GROUPS = "x-tagGroups";
  private static final String X_DISPLAY_NAME = "x-displayName";

  /** Redoc short labels for registration tags (prefix stripped for the sidebar). */
  private static final Map<String, String> TAG_DISPLAY_NAMES =
      Map.of(
          "OAuth 2.0 / Client Management", "Client Management",
          "OAuth 2.0 / Client Querying", "Client Querying",
          "OAuth 2.0 / Scope Management", "Scope Management");

  private static final List<String> TAG_GROUP_TAGS =
      List.of(
          "OAuth 2.0 / Client Management",
          "OAuth 2.0 / Client Querying",
          "OAuth 2.0 / Scope Management");

  @Value("${spring.application.web.api}")
  private String webApi;

  @SuppressWarnings("rawtypes")
  private static void describeProperty(Schema<?> schema, String name, String description) {
    if (schema.getProperties() == null) return;
    var property = schema.getProperties().get(name);
    if (property != null) {
      property.setDescription(description);
    }
  }

  @SuppressWarnings("rawtypes")
  private static void clearDuplicatedArrayItemDescription(Schema<?> schema) {
    if (schema == null) return;

    var items = schema.getItems();
    if (items == null || items.get$ref() == null) return;

    var arrayDescription = schema.getDescription();
    var itemsDescription = items.getDescription();
    if (arrayDescription != null && arrayDescription.equals(itemsDescription)) {
      items.setDescription(null);
    }
  }

  private static Content problemDetailContent() {
    return new Content()
        .addMediaType(
            org.springframework.http.MediaType.APPLICATION_JSON_VALUE,
            new MediaType().schema(new Schema<>().$ref(PROBLEM_DETAIL_REF)));
  }

  private static ApiResponse problemDetailResponse(String description) {
    return new ApiResponse().description(description).content(problemDetailContent());
  }

  private static void putIfAbsent(ApiResponses responses, String code, String componentName) {
    if (!responses.containsKey(code))
      responses.addApiResponse(
          code, new ApiResponse().$ref("#/components/responses/" + componentName));
  }

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
   * Documents Spring's {@code ProblemDetail} schema (RFC 7807), registers {@link
   * ValidationErrorResponse} / {@code FieldError}, and attaches the {@code errors} extension used
   * by validation and invalid-scope handlers.
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

      problemDetail.setDescription(
          "RFC 7807 problem details returned by the registration API for failed requests.");
      describeProperty(
          problemDetail,
          "type",
          "A URI reference that identifies the problem type. This service sets it to the "
              + "DocSpace API getting-started page.");
      describeProperty(
          problemDetail,
          "title",
          "A short, human-readable summary of the problem type, typically the HTTP status "
              + "reason phrase.");
      describeProperty(
          problemDetail, "status", "The HTTP status code for this occurrence of the problem.");
      describeProperty(
          problemDetail,
          "detail",
          "A human-readable explanation specific to this occurrence of the problem.");
      describeProperty(
          problemDetail,
          "instance",
          "A URI reference that identifies the specific occurrence, set to the request path.");
      describeProperty(
          problemDetail,
          "properties",
          "Extension members carried on the problem. Usually empty; validation failures also "
              + "surface as the top-level errors array.");

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

  /**
   * Declares framework-level statuses that Spring raises before an operation runs: unmatched paths
   * (404), wrong method (405), unsupported Content-Type on JSON bodies (415), and unacceptable
   * Accept (406).
   *
   * <p>Route-level 404 is registered under {@code components.responses} only, so it is not confused
   * with operation 404s that mean an unknown or invisible client ID. 405 and 406 are attached to
   * every operation. 415 is attached only where the operation declares a request body.
   */
  @Bean
  public OpenApiCustomizer frameworkErrorResponsesCustomizer() {
    return openApi -> {
      var components = openApi.getComponents();
      if (components == null) {
        components = new Components();
        openApi.setComponents(components);
      }

      components.addResponses(PATH_NOT_FOUND, problemDetailResponse(PATH_NOT_FOUND_DESCRIPTION));
      components.addResponses(
          METHOD_NOT_ALLOWED, problemDetailResponse(METHOD_NOT_ALLOWED_DESCRIPTION));
      components.addResponses(
          UNSUPPORTED_MEDIA_TYPE, problemDetailResponse(UNSUPPORTED_MEDIA_TYPE_DESCRIPTION));
      components.addResponses(NOT_ACCEPTABLE, problemDetailResponse(NOT_ACCEPTABLE_DESCRIPTION));

      if (openApi.getPaths() == null) return;

      openApi
          .getPaths()
          .values()
          .forEach(
              pathItem ->
                  pathItem
                      .readOperations()
                      .forEach(
                          operation -> {
                            var responses = operation.getResponses();
                            if (responses == null) {
                              responses = new ApiResponses();
                              operation.setResponses(responses);
                            }

                            putIfAbsent(responses, "405", METHOD_NOT_ALLOWED);
                            putIfAbsent(responses, "406", NOT_ACCEPTABLE);

                            if (operation.getRequestBody() != null) {
                              putIfAbsent(responses, "415", UNSUPPORTED_MEDIA_TYPE);
                              var existing415 = responses.get("415");
                              if (existing415 != null
                                  && existing415.get$ref() == null
                                  && ("Unsupported media type".equals(existing415.getDescription())
                                      || existing415.getDescription() == null
                                      || existing415.getDescription().isBlank())) {
                                existing415.setDescription(UNSUPPORTED_MEDIA_TYPE_DESCRIPTION);
                                if (existing415.getContent() == null) {
                                  existing415.setContent(problemDetailContent());
                                }
                              }
                            }
                          }));
    };
  }

  /**
   * Adds Redoc {@code x-tagGroups} / {@code x-displayName} so the published oauth document can join
   * this service's tags under the shared "OAuth 2.0" group without a manual post-pass.
   */
  @Bean
  public OpenApiCustomizer oauthTagGroupsCustomizer() {
    return openApi -> {
      if (openApi.getTags() != null) {
        for (Tag tag : openApi.getTags()) {
          var displayName = TAG_DISPLAY_NAMES.get(tag.getName());
          if (displayName != null) tag.addExtension(X_DISPLAY_NAME, displayName);
        }
      }

      var group = new LinkedHashMap<String, Object>();
      group.put("name", OAUTH_TAG_GROUP);
      group.put("tags", TAG_GROUP_TAGS);
      openApi.addExtension(X_TAG_GROUPS, List.of(group));
    };
  }

  /**
   * Under OpenAPI 3.1, swagger-core can copy an {@code @ArraySchema(arraySchema=...)} description
   * onto {@code items} as a {@code $ref} sibling. That wrongly documents each element as the whole
   * list (for example getScopes). Drop the item copy when it matches the array description and the
   * item is a ref; the component schema keeps the per-item docs.
   */
  @Bean
  public OpenApiCustomizer clearDuplicatedArrayItemDescriptionsCustomizer() {
    return openApi -> {
      if (openApi.getPaths() == null) return;

      openApi
          .getPaths()
          .values()
          .forEach(
              pathItem ->
                  pathItem
                      .readOperations()
                      .forEach(
                          operation -> {
                            var responses = operation.getResponses();
                            if (responses == null) return;

                            responses
                                .values()
                                .forEach(
                                    response -> {
                                      if (response.getContent() == null) return;
                                      response
                                          .getContent()
                                          .values()
                                          .forEach(
                                              mediaType ->
                                                  clearDuplicatedArrayItemDescription(
                                                      mediaType.getSchema()));
                                    });
                          }));
    };
  }
}
