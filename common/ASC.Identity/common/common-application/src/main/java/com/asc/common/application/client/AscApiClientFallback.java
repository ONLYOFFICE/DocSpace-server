// (c) Copyright Ascensio System SIA 2009-2024
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
    for (int i = 0; i < details.length; i++) MDC.put("detail" + (i + 1), details[i]);
    log.warn(message);
    MDC.clear();
  }
}
