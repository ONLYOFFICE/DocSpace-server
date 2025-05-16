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
package com.asc.authorization.application.configuration;

import io.swagger.v3.oas.annotations.OpenAPIDefinition;
import io.swagger.v3.oas.annotations.enums.SecuritySchemeIn;
import io.swagger.v3.oas.annotations.enums.SecuritySchemeType;
import io.swagger.v3.oas.annotations.info.Contact;
import io.swagger.v3.oas.annotations.info.Info;
import io.swagger.v3.oas.annotations.info.License;
import io.swagger.v3.oas.annotations.security.SecurityScheme;
import io.swagger.v3.oas.annotations.security.SecuritySchemes;
import io.swagger.v3.oas.annotations.servers.Server;
import io.swagger.v3.oas.annotations.tags.Tag;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.PathItem;
import io.swagger.v3.oas.models.Paths;
import io.swagger.v3.oas.models.media.Content;
import io.swagger.v3.oas.models.media.MediaType;
import io.swagger.v3.oas.models.media.Schema;
import io.swagger.v3.oas.models.parameters.Parameter;
import io.swagger.v3.oas.models.parameters.RequestBody;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Configuration class for setting up OpenAPI schema for the OAuth2 Authorization Server API. */
@Configuration
@OpenAPIDefinition(
    info =
        @Info(
            title = "ASC.Identity.Authorization",
            version = "1.0.0",
            description = "OAuth2 Authorization Server API",
            contact =
                @Contact(
                    name = "ONLYOFFICE Support",
                    email = "support@onlyoffice.com",
                    url = "https://onlyoffice.com"),
            license =
                @License(
                    name = "Apache 2.0",
                    url = "https://www.apache.org/licenses/LICENSE-2.0.html")),
    tags = {@Tag(name = "Authorization")},
    servers = {
      @Server(
          url = "http://localhost:9090",
          description = "Local ASC.Identity.Authorization API Server")
    })
@SecuritySchemes({
  @SecurityScheme(
      name = "x-signature",
      paramName = "x-signature",
      description = "ASC JWT Signature",
      type = SecuritySchemeType.APIKEY,
      in = SecuritySchemeIn.COOKIE)
})
public class OpenAPISchemaConfiguration {
  /**
   * Bean method to create and return the OpenAPI schema.
   *
   * @return OpenAPI schema with defined paths and operations.
   */
  @Bean
  public OpenAPI schema() {
    var paths = new Paths();
    var authorizePathItem =
        new PathItem()
            .get(
                new Operation()
                    .summary("OAuth2 Authorization Endpoint")
                    .description("Initiates the OAuth2 authorization flow")
                    .addTagsItem("Authorization")
                    .addSecurityItem(
                        new io.swagger.v3.oas.models.security.SecurityRequirement()
                            .addList("x-signature"))
                    .responses(
                        new io.swagger.v3.oas.models.responses.ApiResponses()
                            .addApiResponse(
                                "200",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description("Authorization page"))
                            .addApiResponse(
                                "400",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description("Invalid request parameters")))
                    .addParametersItem(
                        new Parameter()
                            .name("response_type")
                            .in("query")
                            .required(true)
                            .schema(new Schema<String>().type("string").example("code")))
                    .addParametersItem(
                        new Parameter()
                            .name("client_id")
                            .in("query")
                            .required(true)
                            .schema(
                                new Schema<String>()
                                    .type("string")
                                    .example("6c7cf17b-1bd3-47d5-94c6-be2d3570e168")))
                    .addParametersItem(
                        new Parameter()
                            .name("redirect_uri")
                            .in("query")
                            .required(true)
                            .schema(
                                new Schema<String>().type("string").example("https://example.com")))
                    .addParametersItem(
                        new Parameter()
                            .name("scope")
                            .in("query")
                            .required(true)
                            .schema(new Schema<String>().type("string").example("files:read"))))
            .post(
                new Operation()
                    .summary("OAuth2 Consent Endpoint")
                    .description("Sends consent approval")
                    .addTagsItem("Authorization")
                    .addSecurityItem(
                        new io.swagger.v3.oas.models.security.SecurityRequirement()
                            .addList("x-signature"))
                    .responses(
                        new io.swagger.v3.oas.models.responses.ApiResponses()
                            .addApiResponse(
                                "302",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description(
                                        "Redirect to the client's redirect URI with authorization code"))
                            .addApiResponse(
                                "400",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description("Invalid request parameters")))
                    .requestBody(
                        new RequestBody()
                            .content(
                                new Content()
                                    .addMediaType(
                                        "multipart/form-data",
                                        new MediaType()
                                            .schema(
                                                new Schema<>()
                                                    .type("object")
                                                    .addProperty(
                                                        "client_id",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example(
                                                                "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"))
                                                    .addProperty(
                                                        "state",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example("abcde"))
                                                    .addProperty(
                                                        "scope",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example("files:read")))))));
    paths.addPathItem("/oauth2/authorize", authorizePathItem);
    paths.addPathItem(
        "/oauth2/token",
        new PathItem()
            .post(
                new Operation()
                    .summary("OAuth2 Token Endpoint")
                    .description("Exchange authorization code for access token")
                    .addTagsItem("Authorization")
                    .responses(
                        new io.swagger.v3.oas.models.responses.ApiResponses()
                            .addApiResponse(
                                "200",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description(
                                        "Successfully exchanged authorization code for access token")
                                    .content(
                                        new Content()
                                            .addMediaType(
                                                "application/json",
                                                new MediaType()
                                                    .schema(
                                                        new Schema<>()
                                                            .type("object")
                                                            .addProperty(
                                                                "access_token",
                                                                new Schema<String>()
                                                                    .type("string")
                                                                    .example(
                                                                        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."))
                                                            .addProperty(
                                                                "token_type",
                                                                new Schema<String>()
                                                                    .type("string")
                                                                    .example("Bearer"))
                                                            .addProperty(
                                                                "expires_in",
                                                                new Schema<Integer>()
                                                                    .type("integer")
                                                                    .example(3600))
                                                            .addProperty(
                                                                "refresh_token",
                                                                new Schema<String>()
                                                                    .type("string")
                                                                    .example("def502..."))))))
                            .addApiResponse(
                                "400",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description("Invalid request parameters")))
                    .requestBody(
                        new RequestBody()
                            .content(
                                new Content()
                                    .addMediaType(
                                        "application/x-www-form-urlencoded",
                                        new MediaType()
                                            .schema(
                                                new Schema<>()
                                                    .type("object")
                                                    .addProperty(
                                                        "grant_type",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example("authorization_code"))
                                                    .addProperty(
                                                        "code",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example("abcde"))
                                                    .addProperty(
                                                        "redirect_uri",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example("https://example.com"))
                                                    .addProperty(
                                                        "client_id",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example(
                                                                "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"))
                                                    .addProperty(
                                                        "client_secret",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .example(
                                                                "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"))))))));
    return new OpenAPI().paths(paths);
  }
}
