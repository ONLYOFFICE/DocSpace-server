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
