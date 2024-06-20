package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.*;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import com.asc.registration.service.transfer.response.ConsentResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.cache.annotation.Caching;
import org.springframework.stereotype.Service;
import org.springframework.validation.annotation.Validated;

/**
 * Service class providing core client application functionalities. This service handles client
 * creation, update, deletion, and retrieval operations. It also manages consent operations for
 * clients.
 */
@Service
@Validated
@RequiredArgsConstructor
public class CoreClientApplicationService implements ClientApplicationService {
  private final ConsentUpdateCommandHandler consentUpdateCommandHandler;
  private final ClientCreateCommandHandler clientCreateCommandHandler;
  private final ClientUpdateCommandHandler clientUpdateCommandHandler;
  private final ConsentQueryHandler consentQueryHandler;
  private final ClientQueryHandler clientQueryHandler;

  /**
   * Retrieves detailed client information based on tenant client query.
   *
   * @param query the tenant client query containing client ID and tenant ID.
   * @return the client response containing detailed client information.
   */
  @Cacheable(value = "clients", key = "#query.clientId", unless = "#result == null")
  public ClientResponse getClient(TenantClientQuery query) {
    return clientQueryHandler.getClient(query);
  }

  /**
   * Retrieves basic client information based on tenant client info query.
   *
   * @param query the tenant client info query containing client ID.
   * @return the client info response containing basic client information.
   */
  @Cacheable(value = "clientsInfo", key = "#query.clientId", unless = "#result == null")
  public ClientInfoResponse getClientInfo(ClientInfoQuery query) {
    return clientQueryHandler.getClientInfo(query);
  }

  /**
   * Retrieves a paginated list of basic client information.
   *
   * @param query the client info pagination query containing tenant ID, page, and limit.
   * @return a pageable response containing a list of client info responses.
   */
  public PageableResponse<ClientInfoResponse> getClientsInfo(ClientInfoPaginationQuery query) {
    return clientQueryHandler.getClientsInfo(query);
  }

  /**
   * Retrieves a paginated list of clients for a given tenant.
   *
   * @param query the tenant clients pagination query containing tenant ID, page, and limit.
   * @return a pageable response containing a list of client responses.
   */
  public PageableResponse<ClientResponse> getClients(TenantClientsPaginationQuery query) {
    return clientQueryHandler.getClients(query);
  }

  /**
   * Retrieves consents for a principal (user) with pagination.
   *
   * @param query the consents pagination query containing the principal name, page, and limit.
   * @return a pageable response containing the consents.
   */
  public PageableResponse<ConsentResponse> getConsents(ConsentsPaginationQuery query) {
    return consentQueryHandler.getConsents(query);
  }

  /**
   * Creates a new client.
   *
   * @param audit the audit information related to the creation.
   * @param command the command containing client creation details.
   * @return the client response containing detailed client information.
   */
  public ClientResponse createClient(Audit audit, CreateTenantClientCommand command) {
    return clientCreateCommandHandler.createClient(audit, command);
  }

  /**
   * Regenerates the client secret for a given client.
   *
   * @param audit the audit information related to the operation.
   * @param command the command containing client ID and tenant ID.
   * @return the client secret response containing the new client secret.
   */
  @CacheEvict(value = "clients", key = "#command.clientId")
  public ClientSecretResponse regenerateSecret(
      Audit audit, RegenerateTenantClientSecretCommand command) {
    return clientUpdateCommandHandler.regenerateSecret(audit, command);
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit the audit information related to the operation.
   * @param command the command containing client ID, tenant ID, and the new activation status.
   */
  @Caching(
      evict = {
        @CacheEvict(value = "clients", key = "#command.clientId"),
        @CacheEvict(value = "clientsInfo", key = "#command.clientId")
      })
  public void changeActivation(Audit audit, ChangeTenantClientActivationCommand command) {
    clientUpdateCommandHandler.changeActivation(audit, command);
  }

  /**
   * Changes the visibility status of a client.
   *
   * @param audit the audit information related to the operation.
   * @param command the command containing client ID, tenant ID, and the new visibility status.
   */
  @Caching(
      evict = {
        @CacheEvict(value = "clients", key = "#command.clientId"),
        @CacheEvict(value = "clientsInfo", key = "#command.clientId")
      })
  public void changeVisibility(Audit audit, ChangeTenantClientVisibilityCommand command) {
    clientUpdateCommandHandler.changeVisibility(audit, command);
  }

  /**
   * Updates the client information.
   *
   * @param audit the audit information related to the operation.
   * @param command the command containing updated client details.
   * @return the client response containing updated client information.
   */
  @Caching(
      evict = {
        @CacheEvict(value = "clients", key = "#command.clientId"),
        @CacheEvict(value = "clientsInfo", key = "#command.clientId")
      })
  public ClientResponse updateClient(Audit audit, UpdateTenantClientCommand command) {
    return clientUpdateCommandHandler.updateClient(audit, command);
  }

  /**
   * Deletes a client.
   *
   * @param audit the audit information related to the operation.
   * @param command the command containing client ID and tenant ID.
   */
  @Caching(
      evict = {
        @CacheEvict(value = "clients", key = "#command.clientId"),
        @CacheEvict(value = "clientsInfo", key = "#command.clientId")
      })
  public void deleteClient(Audit audit, DeleteTenantClientCommand command) {
    clientUpdateCommandHandler.deleteClient(audit, command);
  }

  /**
   * Revokes a client's consent.
   *
   * @param audit the audit information related to the operation.
   * @param command the command containing client ID and principal name.
   */
  public void revokeClientConsent(Audit audit, RevokeClientConsentCommand command) {
    consentUpdateCommandHandler.revokeConsent(command);
  }
}
