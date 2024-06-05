package com.asc.common.core.domain.value.enums;

/** This enum represents the different grant types for authorization. */
public enum GrantType {

  /** Represents the authorization code grant type. */
  AUTHORIZATION_CODE("authorization_code"),

  /** Represents the refresh token grant type. */
  REFRESH_TOKEN("refresh_token"),

  /** Represents the client credentials grant type. */
  CLIENT_CREDENTIALS("client_credentials");

  private final String type;

  /**
   * Constructs a new GrantType with the specified type.
   *
   * @param type the type of the grant
   */
  GrantType(String type) {
    this.type = type;
  }

  /**
   * Returns the type of the grant.
   *
   * @return the type of the grant
   */
  public String getType() {
    return type;
  }
}
