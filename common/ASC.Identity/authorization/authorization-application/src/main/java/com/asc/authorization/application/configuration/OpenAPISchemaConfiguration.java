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
import org.springdoc.core.customizers.OpenApiCustomizer;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** Configuration class for setting up OpenAPI schema for the OAuth2 Authorization Server API. */
@Configuration
@OpenAPIDefinition(
    info =
        @Info(
            title = "ASC.Identity.Authorization",
            version = "1.3.0",
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
    tags = {
      @Tag(
          name = "OAuth 2.0 / Authorization",
          description =
              "The OAuth2 authorization flow: the authorization request, the consent submission "
                  + "and the exchange of an authorization code for tokens."),
      @Tag(
          name = "OAuth 2.0 / Discovery",
          description =
              "The OAuth 2.0 Authorization Server metadata endpoint a client probes before it "
                  + "starts a flow.")
    },
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
                    .summary("Start the authorization flow")
                    .description(
                        "Starts the OAuth2 authorization code flow for the client named by "
                            + "client_id. The caller has to present the portal signature cookie, "
                            + "and a request without a valid one is not refused with 401 or 403 but "
                            + "redirected to the portal login page, carrying the client ID so the "
                            + "flow can resume after signing in. When the user has not yet "
                            + "consented to the requested scopes the browser is redirected to the "
                            + "consent page; once the consent exists the browser is redirected to "
                            + "the client's redirect URI with the authorization code and, when one "
                            + "was sent, the original state. A caller that cannot follow redirects "
                            + "may send the X-Disable-Redirect header, and then the response is 200 "
                            + "with an empty body and the target URL in the X-Redirect-URI header. "
                            + "The code returned here is exchanged for tokens at the token "
                            + "endpoint.")
                    .addTagsItem("OAuth 2.0 / Authorization")
                    .addSecurityItem(
                        new io.swagger.v3.oas.models.security.SecurityRequirement()
                            .addList("x-signature"))
                    .responses(
                        new io.swagger.v3.oas.models.responses.ApiResponses()
                            .addApiResponse(
                                "302",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description(
                                        "Redirect to the login page, to the consent page, or back "
                                            + "to the client's redirect URI with an authorization "
                                            + "code"))
                            .addApiResponse(
                                "200",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description(
                                        "Returned instead of the redirect when the request carries "
                                            + "the X-Disable-Redirect header: the target URL is "
                                            + "sent in the X-Redirect-URI response header and the "
                                            + "body is empty"))
                            .addApiResponse(
                                "400",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description("Invalid request parameters")))
                    .addParametersItem(
                        new Parameter()
                            .name("response_type")
                            .in("query")
                            .required(true)
                            .description(
                                "The OAuth 2.0 response type. Only code is supported: this server "
                                    + "issues an authorization code, never a token, from this endpoint.")
                            .schema(new Schema<String>().type("string").example("code")))
                    .addParametersItem(
                        new Parameter()
                            .name("client_id")
                            .in("query")
                            .required(true)
                            .description(
                                "The identifier the client was given when it was registered. It "
                                    + "selects both the client shown on the consent screen and the set of "
                                    + "redirect URIs the request is checked against.")
                            .schema(
                                new Schema<String>()
                                    .type("string")
                                    .example("6c7cf17b-1bd3-47d5-94c6-be2d3570e168")))
                    .addParametersItem(
                        new Parameter()
                            .name("redirect_uri")
                            .in("query")
                            .required(true)
                            .description(
                                "Where to send the user once authorization is complete. It has to be "
                                    + "one of the redirect URIs registered for the client, otherwise the "
                                    + "request is refused.")
                            .schema(
                                new Schema<String>().type("string").example("https://example.com")))
                    .addParametersItem(
                        new Parameter()
                            .name("scope")
                            .in("query")
                            .required(true)
                            .description(
                                "The permissions being asked for, as a space-separated list. Every "
                                    + "scope has to be one the client is registered for, and the consent "
                                    + "screen lists exactly these.")
                            .schema(new Schema<String>().type("string").example("files:read"))))
            .post(
                new Operation()
                    .summary("Submit the consent decision")
                    .description(
                        "Submits the user's consent decision for the scopes an authorization "
                            + "request asked for. It is the form post the consent page makes, so it "
                            + "carries the client ID, the state and the agreed scopes as multipart "
                            + "form data, along with the same portal signature cookie the "
                            + "authorization request needed. On success the browser is redirected "
                            + "to the client's redirect URI with an authorization code, or, when "
                            + "the request carries the X-Disable-Redirect header, answered 200 with "
                            + "that URL in the X-Redirect-URI header. The consent is stored per "
                            + "user and client, so a later authorization request for the same "
                            + "scopes no longer stops at the consent page.")
                    .addTagsItem("OAuth 2.0 / Authorization")
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
                                "200",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description(
                                        "Returned instead of the redirect when the request carries "
                                            + "the X-Disable-Redirect header: the target URL is "
                                            + "sent in the X-Redirect-URI response header and the "
                                            + "body is empty"))
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
                                                            .description(
                                                                "The client the consent is being "
                                                                    + "given to. It has to be the same "
                                                                    + "client the authorization request "
                                                                    + "named.")
                                                            .example(
                                                                "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"))
                                                    .addProperty(
                                                        "state",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .description(
                                                                "The opaque value carried through "
                                                                    + "from the authorization request, "
                                                                    + "returned unchanged on the redirect "
                                                                    + "so the client can match the answer "
                                                                    + "to its request.")
                                                            .example("abcde"))
                                                    .addProperty(
                                                        "scope",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .description(
                                                                "The scopes the user agreed to, as a "
                                                                    + "space-separated list. Anything the "
                                                                    + "user declined is left out, so this "
                                                                    + "may be narrower than what was "
                                                                    + "requested.")
                                                            .example("files:read")))))));
    paths.addPathItem("/oauth2/authorize", authorizePathItem);
    paths.addPathItem(
        "/oauth2/token",
        new PathItem()
            .post(
                new Operation()
                    .summary("Exchange the authorization code")
                    .description(
                        "Exchanges an authorization code for an access token. The request is "
                            + "form-encoded and has to carry the grant type, the code, the same "
                            + "redirect URI that was used to obtain the code, and the client "
                            + "credentials: the client authenticates itself here rather than "
                            + "through the portal signature cookie the authorization endpoint uses. "
                            + "The response carries the access token, its type and its lifetime in "
                            + "seconds, plus a refresh token when the client is configured for the "
                            + "refresh token grant. Client authentication that fails is answered "
                            + "with 401, while a malformed, unknown or expired code is answered "
                            + "with 400. The code is single use, so replaying it fails.")
                    .addTagsItem("OAuth 2.0 / Authorization")
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
                                                                    .description(
                                                                        "The token to send as a "
                                                                            + "Bearer credential when "
                                                                            + "calling the portal on the "
                                                                            + "user behalf.")
                                                                    .example(
                                                                        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."))
                                                            .addProperty(
                                                                "token_type",
                                                                new Schema<String>()
                                                                    .type("string")
                                                                    .description(
                                                                        "How the access token is to "
                                                                            + "be presented. It is always "
                                                                            + "Bearer.")
                                                                    .example("Bearer"))
                                                            .addProperty(
                                                                "expires_in",
                                                                new Schema<Integer>()
                                                                    .type("integer")
                                                                    .description(
                                                                        "How many seconds the access "
                                                                            + "token stays valid, counted "
                                                                            + "from the moment it was "
                                                                            + "issued.")
                                                                    .example(3600))
                                                            .addProperty(
                                                                "refresh_token",
                                                                new Schema<String>()
                                                                    .type("string")
                                                                    .description(
                                                                        "The token that buys a new "
                                                                            + "access token once the "
                                                                            + "current one expires. It is "
                                                                            + "present only when the client "
                                                                            + "is registered for the "
                                                                            + "refresh token grant.")
                                                                    .example("def502..."))))))
                            .addApiResponse(
                                "400",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description("Invalid request parameters"))
                            .addApiResponse(
                                "401",
                                new io.swagger.v3.oas.models.responses.ApiResponse()
                                    .description(
                                        "Client authentication failed: the client ID is unknown or "
                                            + "the client secret does not match")))
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
                                                            .description(
                                                                "Which exchange is being performed: "
                                                                    + "authorization_code to redeem a code, "
                                                                    + "refresh_token to renew an access "
                                                                    + "token.")
                                                            .example("authorization_code"))
                                                    .addProperty(
                                                        "code",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .description(
                                                                "The authorization code returned by "
                                                                    + "the authorization endpoint. It may "
                                                                    + "be redeemed once.")
                                                            .example("abcde"))
                                                    .addProperty(
                                                        "redirect_uri",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .description(
                                                                "The same redirect URI that was used "
                                                                    + "to obtain the code. The exchange "
                                                                    + "fails when it differs.")
                                                            .example("https://example.com"))
                                                    .addProperty(
                                                        "client_id",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .description(
                                                                "The identifier of the client "
                                                                    + "redeeming the code.")
                                                            .example(
                                                                "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"))
                                                    .addProperty(
                                                        "client_secret",
                                                        new Schema<String>()
                                                            .type("string")
                                                            .description(
                                                                "The secret of the client redeeming "
                                                                    + "the code. It is omitted by a public "
                                                                    + "client, which proves itself with a "
                                                                    + "PKCE code verifier instead.")
                                                            .example(
                                                                "6c7cf17b-1bd3-47d5-94c6-be2d3570e168"))))))));
    return new OpenAPI().paths(paths);
  }

  /** Removes the auto-generated servers section from the OpenAPI spec */
  @Bean
  public OpenApiCustomizer removeServersCustomizer() {
    return openApi -> openApi.setServers(null);
  }
}
