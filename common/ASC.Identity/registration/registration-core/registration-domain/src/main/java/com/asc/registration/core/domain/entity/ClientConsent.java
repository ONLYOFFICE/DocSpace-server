package com.asc.registration.core.domain.entity;

import com.asc.common.core.domain.entity.Consent;

/**
 * ClientConsent is a wrapper class that encapsulates two aggregate roots: Client and Consent. It
 * represents the relationship between a client and their consent.
 */
public class ClientConsent {
  private final Client client;
  private final Consent consent;

  /**
   * Constructs a ClientConsent with the specified client and consent.
   *
   * @param client the client aggregate root
   * @param consent the consent aggregate root
   */
  public ClientConsent(Client client, Consent consent) {
    this.client = client;
    this.consent = consent;
  }

  /**
   * Returns the client associated with this consent.
   *
   * @return the client
   */
  public Client getClient() {
    return client;
  }

  /**
   * Returns the consent associated with this client.
   *
   * @return the consent
   */
  public Consent getConsent() {
    return consent;
  }
}
