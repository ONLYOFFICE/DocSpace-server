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

package com.asc.registration.application.configuration.serialization;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.value.*;
import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.databind.DeserializationContext;
import com.fasterxml.jackson.databind.JsonDeserializer;
import com.fasterxml.jackson.databind.JsonNode;
import java.io.IOException;
import java.time.ZonedDateTime;
import java.util.HashSet;
import java.util.UUID;

/**
 * Custom Jackson deserializer for {@link Client} entities.
 *
 * <p>This deserializer uses the Client.Builder pattern to reconstruct Client instances from JSON,
 * allowing the domain entity to remain unchanged without requiring special Jackson annotations or
 * constructors.
 */
public class ClientDeserializer extends JsonDeserializer<Client> {
  public Client deserialize(JsonParser p, DeserializationContext ctxt) throws IOException {
    JsonNode node = p.getCodec().readTree(p);

    var builder = Client.Builder.builder();
    if (node.has("id") && node.get("id").has("value")) {
      var id = UUID.fromString(node.get("id").get("value").asText());
      builder.id(new ClientId(id));
    }

    if (node.has("secret") && node.get("secret").has("value")) {
      var secret = node.get("secret").get("value").asText();
      builder.secret(new ClientSecret(secret));
    }

    if (node.has("authenticationMethods")) {
      var methods = new HashSet<AuthenticationMethod>();
      node.get("authenticationMethods")
          .forEach(methodNode -> methods.add(AuthenticationMethod.valueOf(methodNode.asText())));
      builder.authenticationMethods(methods);
    }

    if (node.has("scopes")) {
      var scopes = new HashSet<String>();
      node.get("scopes").forEach(scopeNode -> scopes.add(scopeNode.asText()));
      builder.scopes(scopes);
    }

    if (node.has("clientInfo")) {
      var infoNode = node.get("clientInfo");
      var name = infoNode.has("name") ? infoNode.get("name").asText() : null;
      var description = infoNode.has("description") ? infoNode.get("description").asText() : null;
      var logo = infoNode.has("logo") ? infoNode.get("logo").asText() : null;
      builder.clientInfo(new ClientInfo(name, description, logo));
    }

    if (node.has("clientTenantInfo")) {
      var tenantNode = node.get("clientTenantInfo");
      if (tenantNode.has("tenantId") && tenantNode.get("tenantId").has("value")) {
        var tenantIdValue = tenantNode.get("tenantId").get("value").asLong();
        builder.clientTenantInfo(new ClientTenantInfo(new TenantId(tenantIdValue)));
      }
    }

    if (node.has("clientWebsiteInfo")) {
      var websiteNode = node.get("clientWebsiteInfo");
      var websiteBuilder = ClientWebsiteInfo.Builder.builder();
      if (websiteNode.has("websiteUrl"))
        websiteBuilder.websiteUrl(websiteNode.get("websiteUrl").asText());
      if (websiteNode.has("policyUrl"))
        websiteBuilder.policyUrl(websiteNode.get("policyUrl").asText());
      if (websiteNode.has("termsUrl"))
        websiteBuilder.termsUrl(websiteNode.get("termsUrl").asText());
      builder.clientWebsiteInfo(websiteBuilder.build());
    }

    if (node.has("clientRedirectInfo")) {
      var redirectNode = node.get("clientRedirectInfo");
      var redirectUris = new HashSet<String>();
      if (redirectNode.has("redirectUris"))
        redirectNode.get("redirectUris").forEach(uri -> redirectUris.add(uri.asText()));
      var allowedOrigins = new HashSet<String>();
      if (redirectNode.has("allowedOrigins"))
        redirectNode.get("allowedOrigins").forEach(origin -> allowedOrigins.add(origin.asText()));
      var logoutRedirectUris = new HashSet<String>();
      if (redirectNode.has("logoutRedirectUris"))
        redirectNode.get("logoutRedirectUris").forEach(uri -> logoutRedirectUris.add(uri.asText()));
      builder.clientRedirectInfo(
          new ClientRedirectInfo(redirectUris, allowedOrigins, logoutRedirectUris));
    }

    if (node.has("clientCreationInfo")) {
      var creationNode = node.get("clientCreationInfo");
      var creationBuilder = ClientCreationInfo.Builder.builder();
      if (creationNode.has("createdOn")) {
        var createdOn = ZonedDateTime.parse(creationNode.get("createdOn").asText());
        creationBuilder.createdOn(createdOn);
      }

      if (creationNode.has("createdBy") && creationNode.get("createdBy").has("value")) {
        var createdByValue = UUID.fromString(creationNode.get("createdBy").get("value").asText());
        creationBuilder.createdBy(new UserId(createdByValue.toString()));
      }

      builder.clientCreationInfo(creationBuilder.build());
    }

    if (node.has("clientModificationInfo") && !node.get("clientModificationInfo").isNull()) {
      var modificationNode = node.get("clientModificationInfo");
      var hasModifiedOn =
          modificationNode.has("modifiedOn") && !modificationNode.get("modifiedOn").isNull();
      var hasModifiedBy =
          modificationNode.has("modifiedBy") && !modificationNode.get("modifiedBy").isNull();

      if (hasModifiedOn && hasModifiedBy) {
        var modificationBuilder = ClientModificationInfo.Builder.builder();
        var modifiedOn = ZonedDateTime.parse(modificationNode.get("modifiedOn").asText());
        modificationBuilder.modifiedOn(modifiedOn);

        if (modificationNode.get("modifiedBy").has("value")) {
          var modifiedByValue =
              UUID.fromString(modificationNode.get("modifiedBy").get("value").asText());
          modificationBuilder.modifiedBy(new UserId(modifiedByValue.toString()));
        }

        builder.clientModificationInfo(modificationBuilder.build());
      }
    }

    if (node.has("clientStatus"))
      builder.clientStatus(ClientStatus.valueOf(node.get("clientStatus").asText()));

    if (node.has("clientVisibility"))
      builder.clientVisibility(ClientVisibility.valueOf(node.get("clientVisibility").asText()));

    if (node.has("version")) builder.clientVersion(node.get("version").asInt());

    return builder.build();
  }
}
