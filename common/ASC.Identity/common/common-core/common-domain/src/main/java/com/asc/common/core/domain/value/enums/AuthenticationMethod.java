package com.asc.common.core.domain.value.enums;

/** Represents an enumeration of authentication methods used in the domain layer. */
public enum AuthenticationMethod {

  /** Default authentication method using client secret post. */
  DEFAULT_AUTHENTICATION("client_secret_post"),

  /** PKCE authentication method using none. */
  PKCE_AUTHENTICATION("none");

  private final String method;

  AuthenticationMethod(String method) {
    this.method = method;
  }

  /**
   * Returns the method associated with this authentication method.
   *
   * @return The method associated with this authentication method.
   */
  public String getMethod() {
    return method;
  }

  /**
   * Returns the authentication method associated with the given method.
   *
   * @param method The method to search for.
   * @return The authentication method associated with the given method.
   * @throws IllegalArgumentException If no authentication method is found with the given method.
   */
  public static AuthenticationMethod fromMethod(String method) {
    for (AuthenticationMethod authenticationMethod : values()) {
      if (authenticationMethod.getMethod().equals(method)) {
        return authenticationMethod;
      }
    }

    throw new IllegalArgumentException("No enum constant for method: " + method);
  }
}
