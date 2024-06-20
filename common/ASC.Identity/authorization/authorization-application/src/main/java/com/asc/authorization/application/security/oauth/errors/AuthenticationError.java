package com.asc.authorization.application.security.oauth.errors;

import lombok.Getter;

/** Enum representing various authentication errors. */
@Getter
public enum AuthenticationError {

  /** Error indicating an issue with retrieving ASC data. */
  ASC_RETRIEVAL_ERROR("asc_retrieval_error"),

  /** Error indicating that the authentication method is not supported. */
  AUTHENTICATION_NOT_SUPPORTED_ERROR("authentication_not_supported_error"),

  /** Error indicating that the client is disabled. */
  CLIENT_DISABLED_ERROR("client_disabled_error"),

  /** Error indicating that the client was not found. */
  CLIENT_NOT_FOUND_ERROR("client_not_found_error"),

  /** Error indicating that the client does not have permission. */
  CLIENT_PERMISSION_DENIED_ERROR("client_permission_denied_error"),

  /** Error indicating that the redirect URI is invalid. */
  INVALID_REDIRECT_URI_ERROR("invalid_redirect_uri_error"),

  /** Error indicating that the ASC cookie is missing. */
  MISSING_ASC_COOKIE_ERROR("missing_asc_cookie_error"),

  /** Error indicating that the client ID is missing. */
  MISSING_CLIENT_ID_ERROR("missing_client_id_error"),

  /** Error indicating that something went wrong during the authentication process. */
  SOMETHING_WENT_WRONG_ERROR("something_went_wrong_error");

  /** The error code associated with the authentication error. */
  private final String code;

  /**
   * Constructs an AuthenticationError with the specified error code.
   *
   * @param code the error code
   */
  AuthenticationError(String code) {
    this.code = code;
  }
}
