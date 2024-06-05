package com.asc.common.application.client;

import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscResponseWrapper;
import com.asc.common.application.transfer.response.AscSettingsResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
import java.net.URI;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

/**
 * This class is a fallback implementation of the AscApiClient interface. It is used when the
 * primary implementation fails to provide a response. It logs a fallback message and returns an
 * empty response.
 */
@Slf4j
@Component
public class AscApiClientFallback implements AscApiClient {

  /**
   * This method is a fallback for getting the current user profile. It logs a fallback message and
   * returns an empty response.
   *
   * @param cookie The cookie for the request.
   * @return An empty response.
   */
  public AscResponseWrapper<AscPersonResponse> getMe(String cookie) {
    logFallback("Could not fetch current user profile", cookie);
    return AscResponseWrapper.<AscPersonResponse>builder().build();
  }

  /**
   * This method is a fallback for getting a user profile by email. It logs a fallback message and
   * returns an empty response.
   *
   * @param cookie The cookie for the request.
   * @param email The email of the user.
   * @return An empty response.
   */
  public AscResponseWrapper<AscPersonResponse> getProfile(String cookie, String email) {
    logFallback("Could not fetch user profile", email);
    return AscResponseWrapper.<AscPersonResponse>builder().build();
  }

  /**
   * This method is a fallback for getting the current tenant info. It logs a fallback message and
   * returns an empty response.
   *
   * @param cookie The cookie for the request.
   * @return An empty response.
   */
  public AscResponseWrapper<AscTenantResponse> getTenant(String cookie) {
    logFallback("Could not fetch current tenant info", cookie);
    return AscResponseWrapper.<AscTenantResponse>builder().build();
  }

  /**
   * This method is a fallback for getting the current tenant settings. It logs a fallback message
   * and returns an empty response.
   *
   * @param cookie The cookie for the request.
   * @return An empty response.
   */
  public AscResponseWrapper<AscSettingsResponse> getSettings(String cookie) {
    logFallback("Could not fetch current tenant settings", cookie);
    return AscResponseWrapper.<AscSettingsResponse>builder().build();
  }

  /**
   * This method is a fallback for getting the current user profile with a base URI. It logs a
   * fallback message and returns an empty response.
   *
   * @param base The base URI for the request.
   * @param cookie The cookie for the request.
   * @return An empty response.
   */
  public AscResponseWrapper<AscPersonResponse> getMe(URI base, String cookie) {
    logFallback("Could not fetch current user profile", base.toString(), cookie);
    return AscResponseWrapper.<AscPersonResponse>builder().build();
  }

  /**
   * This method is a fallback for getting a user profile by email with a base URI. It logs a
   * fallback message and returns an empty response.
   *
   * @param base The base URI for the request.
   * @param cookie The cookie for the request.
   * @param email The email of the user.
   * @return An empty response.
   */
  public AscResponseWrapper<AscPersonResponse> getProfile(URI base, String cookie, String email) {
    logFallback("Could not fetch user profile", base.toString(), email);
    return AscResponseWrapper.<AscPersonResponse>builder().build();
  }

  /**
   * This method is a fallback for getting the current tenant info with a base URI. It logs a
   * fallback message and returns an empty response.
   *
   * @param base The base URI for the request.
   * @param cookie The cookie for the request.
   * @return An empty response.
   */
  public AscResponseWrapper<AscTenantResponse> getTenant(URI base, String cookie) {
    logFallback("Could not fetch current tenant info", base.toString(), cookie);
    return AscResponseWrapper.<AscTenantResponse>builder().build();
  }

  /**
   * This method is a fallback for getting the current tenant settings with a base URI. It logs a
   * fallback message and returns an empty response.
   *
   * @param base The base URI for the request.
   * @param cookie The cookie for the request.
   * @return An empty response.
   */
  public AscResponseWrapper<AscSettingsResponse> getSettings(URI base, String cookie) {
    logFallback("Could not fetch current tenant settings", base.toString(), cookie);
    return AscResponseWrapper.<AscSettingsResponse>builder().build();
  }

  /**
   * This method logs a fallback message with the given details.
   *
   * @param message The fallback message.
   * @param details The details to include in the log message.
   */
  private void logFallback(String message, String... details) {
    MDC.put("fallback", AscApiClientFallback.class.getName());
    for (int i = 0; i < details.length; i++) {
      MDC.put("detail" + (i + 1), details[i]);
    }
    log.warn(message);
    MDC.clear();
  }
}
