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

package com.asc.authorization.application.security.oauth.service;

import com.asc.authorization.application.exception.client.RegisteredClientPermissionException;
import com.asc.authorization.application.mapper.ClientMapper;
import io.grpc.Deadline;
import java.util.concurrent.TimeUnit;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import net.devh.boot.grpc.client.inject.GrpcClient;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Repository;

/**
 * Service for managing registered OAuth2 clients.
 *
 * <p>This service acts as a repository for read-only operations on registered clients. It interacts
 * with a gRPC service to fetch client information and provides methods for retrieving clients by ID
 * or client ID. It also validates client accessibility based on specific conditions.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class RegisteredClientService
    implements RegisteredClientRepository, RegisteredClientAccessibilityService {
  @GrpcClient("registrationService")
  com.asc.common.application.proto.ClientRegistrationServiceGrpc
          .ClientRegistrationServiceBlockingStub
      registrationService;

  private final ClientMapper clientMapper;

  /**
   * Saves a registered client.
   *
   * <p>This operation is not supported because the service is read-only.
   *
   * @param registeredClient the {@link RegisteredClient} to save.
   */
  public void save(RegisteredClient registeredClient) {
    MDC.put("client_id", registeredClient.getClientId());
    MDC.put("client_name", registeredClient.getClientName());
    log.error("ASC registered client repository supports only read operations");
    MDC.clear();
  }

  /**
   * Finds a registered client by its ID.
   *
   * <p>The client is retrieved from the gRPC service. If the client is disabled, a {@link
   * RegisteredClientPermissionException} is thrown. If the client is not found, null is returned.
   *
   * @param id the ID of the registered client.
   * @return the {@link RegisteredClient}, or {@code null} if not found.
   */
  public RegisteredClient findById(String id) {
    try {
      MDC.put("client_id", id);
      log.info("Trying to find registered client by id");

      var client =
          registrationService
              .withDeadline(Deadline.after(1100, TimeUnit.MILLISECONDS))
              .getClient(
                  com.asc.common.application.proto.GetClientRequest.newBuilder()
                      .setClientId(id)
                      .build());

      if (!client.getEnabled())
        throw new RegisteredClientPermissionException(
            String.format("Client with id %s is disabled", id));

      return clientMapper.toRegisteredClient(client);
    } catch (Exception e) {
      log.warn("Could not find registered client", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds a registered client by its client ID.
   *
   * <p>This method delegates to {@link #findById(String)} to retrieve the client. If the client is
   * not found, null is returned.
   *
   * @param clientId the client ID of the registered client.
   * @return the {@link RegisteredClient}, or {@code null} if not found.
   */
  public RegisteredClient findByClientId(String clientId) {
    try {
      MDC.put("client_id", clientId);
      log.info("Trying to get client by client id");

      return findById(clientId);
    } catch (Exception e) {
      log.warn("Could not get client by client_id", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Validates whether a registered client is accessible.
   *
   * <p>The client is considered accessible if it is public and enabled.
   *
   * @param clientId the client ID of the registered client.
   * @return {@code true} if the client is accessible, {@code false} otherwise.
   */
  public boolean validateClientAccessibility(String clientId) {
    try {
      var client =
          registrationService.getClient(
              com.asc.common.application.proto.GetClientRequest.newBuilder()
                  .setClientId(clientId)
                  .build());
      if (!client.getIsPublic()) {
        log.warn("Client {} is not accessible", client.getClientId());
        return false;
      }

      if (!client.getEnabled()) {
        log.warn("Client {} is disabled", client.getClientId());
        return false;
      }

      return true;
    } catch (Exception e) {
      log.warn("Registered client not found for client ID: {}", clientId);
      return false;
    }
  }
}
