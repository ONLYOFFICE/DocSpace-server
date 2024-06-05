package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.TenantClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.TenantConsentsPaginationQuery;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.validation.annotation.Validated;

/**
 * Service class providing core client application functionalities. This service handles client
 * creation, update, deletion, and retrieval operations. It also handles consent operations for
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
   * Retrieves client information based on tenant client query.
   *
   * @param query the tenant client query containing client ID and tenant ID
   * @return the client response containing detailed client information
   */
  public ClientResponse getClient(TenantClientQuery query) {
    return clientQueryHandler.getClient(query);
  }

  /**
   * Retrieves basic client information based on tenant client info query.
   *
   * @param query the tenant client info query containing client ID
   * @return the client info response containing basic client information
   */
  public ClientInfoResponse getClientInfo(TenantClientInfoQuery query) {
    return clientQueryHandler.getClientInfo(query);
  }

  /**
   * Retrieves a paginated list of clients for a given tenant.
   *
   * @param query the tenant clients pagination query containing tenant ID, page, and limit
   * @return a pageable response containing a list of client responses
   */
  public PageableResponse<ClientResponse> getClients(TenantClientsPaginationQuery query) {
    return clientQueryHandler.getClients(query);
  }

  /**
   * Retrieves a paginated list of consents for a given tenant and principal name.
   *
   * @param query the tenant consents pagination query containing tenant ID, principal name, page,
   *     and limit
   * @return a pageable response containing a list of consent responses
   */
  public PageableResponse<ConsentResponse> getConsents(TenantConsentsPaginationQuery query) {
    return consentQueryHandler.getConsents(query);
  }

  /**
   * Creates a new client.
   *
   * @param audit the audit information related to the creation
   * @param command the command containing client creation details
   * @return the client response containing detailed client information
   */
  public ClientResponse createClient(Audit audit, CreateTenantClientCommand command) {
    return clientCreateCommandHandler.createClient(audit, command);
  }

  /**
   * Regenerates the client secret for a given client.
   *
   * @param audit the audit information related to the operation
   * @param command the command containing client ID and tenant ID
   * @return the client secret response containing the new client secret
   */
  public ClientSecretResponse regenerateSecret(
      Audit audit, RegenerateTenantClientSecretCommand command) {
    return clientUpdateCommandHandler.regenerateSecret(audit, command);
  }

  /**
   * Changes the activation status of a client.
   *
   * @param audit the audit information related to the operation
   * @param command the command containing client ID, tenant ID, and the new activation status
   */
  public void changeActivation(Audit audit, ChangeTenantClientActivationCommand command) {
    clientUpdateCommandHandler.changeActivation(audit, command);
  }

  /**
   * Updates the client information.
   *
   * @param audit the audit information related to the operation
   * @param command the command containing updated client details
   * @return the client response containing updated client information
   */
  public ClientResponse updateClient(Audit audit, UpdateTenantClientCommand command) {
    return clientUpdateCommandHandler.updateClient(audit, command);
  }

  /**
   * Deletes a client.
   *
   * @param audit the audit information related to the operation
   * @param command the command containing client ID and tenant ID
   */
  public void deleteClient(Audit audit, DeleteTenantClientCommand command) {
    clientUpdateCommandHandler.deleteClient(audit, command);
  }

  /**
   * Revokes a client's consent.
   *
   * @param audit the audit information related to the operation
   * @param command the command containing client ID, tenant ID, and principal name
   */
  public void revokeClientConsent(Audit audit, RevokeClientConsentCommand command) {
    consentUpdateCommandHandler.revokeConsent(command);
  }
}
