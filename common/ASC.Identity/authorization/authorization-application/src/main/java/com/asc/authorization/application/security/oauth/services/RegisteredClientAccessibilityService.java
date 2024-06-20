package com.asc.authorization.application.security.oauth.services;

/** Repository interface for validating the accessibility of registered clients. */
public interface RegisteredClientAccessibilityService {

  /**
   * Validates the accessibility of the client associated with the given tenant.
   *
   * @param clientId the ID of the registered client.
   * @param tenantId the tenant ID of the current caller to validate accessibility against.
   * @return true if the client is accessible, false otherwise.
   */
  boolean validateClientAccessibility(String clientId, int tenantId);
}
