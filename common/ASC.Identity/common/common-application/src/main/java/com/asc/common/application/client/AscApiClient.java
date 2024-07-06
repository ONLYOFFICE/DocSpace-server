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
import org.springframework.cache.annotation.Cacheable;
import org.springframework.cloud.openfeign.FeignClient;
import org.springframework.context.annotation.Primary;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RequestParam;

/**
 * Feign client for ASC API.
 *
 * <p>This client is configured to work with both localhost (development environment) and Kubernetes
 * service names (test and production environments).
 *
 * <p>Example application.yml configuration:
 *
 * <pre>
 * spring:
 *   cloud:
 *     kubernetes:
 *       discovery:
 *         enabled: true
 *   application:
 *     name: your-application-name
 *
 * feign:
 *   client:
 *     config:
 *       default:
 *         connectTimeout: 5000
 *         readTimeout: 5000
 *
 * # Define your Kubernetes service name
 * asc-service:
 *   url: http://asc-service
 * </pre>
 *
 * Example application-local.yml (for local development):
 *
 * <pre>
 * asc-service:
 *   url: http://localhost:8080  # Assuming the local instance is running on port 8080
 * </pre>
 */
@Primary
@FeignClient(
    name = "asc-service",
    url = "${asc-service.url}",
    fallback = AscApiClientFallback.class)
public interface AscApiClient {
  // Kubernetes service calls
  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/people/@self")
  @Cacheable(
      cacheNames = {"ascMe"},
      key = "#cookie",
      unless = "#result == null")
  AscResponseWrapper<AscPersonResponse> getMe(@RequestHeader("Cookie") String cookie);

  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/people/email")
  @Cacheable(
      cacheNames = {"ascEmail"},
      key = "#email",
      unless = "#result == null")
  AscResponseWrapper<AscPersonResponse> getProfile(
      @RequestHeader("Cookie") String cookie, @RequestParam("email") String email);

  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/portal")
  @Cacheable(
      cacheNames = {"ascTenant"},
      key = "#cookie",
      unless = "#result == null")
  AscResponseWrapper<AscTenantResponse> getTenant(@RequestHeader("Cookie") String cookie);

  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/settings")
  @Cacheable(
      cacheNames = {"ascSettings"},
      key = "#cookie",
      unless = "#result == null")
  AscResponseWrapper<AscSettingsResponse> getSettings(@RequestHeader("Cookie") String cookie);

  // Dynamic URI calls
  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/people/@self")
  @Cacheable(
      cacheNames = {"ascMe"},
      key = "#cookie",
      unless = "#result == null")
  AscResponseWrapper<AscPersonResponse> getMe(URI base, @RequestHeader("Cookie") String cookie);

  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/people/email")
  @Cacheable(
      cacheNames = {"ascEmail"},
      key = "#email",
      unless = "#result == null")
  AscResponseWrapper<AscPersonResponse> getProfile(
      URI base, @RequestHeader("Cookie") String cookie, @RequestParam("email") String email);

  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/portal")
  @Cacheable(
      cacheNames = {"ascTenant"},
      key = "#cookie",
      unless = "#result == null")
  AscResponseWrapper<AscTenantResponse> getTenant(URI base, @RequestHeader("Cookie") String cookie);

  @RequestMapping(method = RequestMethod.GET, value = "${web.api}/settings")
  @Cacheable(
      cacheNames = {"ascSettings"},
      key = "#cookie",
      unless = "#result == null")
  AscResponseWrapper<AscSettingsResponse> getSettings(
      URI base, @RequestHeader("Cookie") String cookie);
}
