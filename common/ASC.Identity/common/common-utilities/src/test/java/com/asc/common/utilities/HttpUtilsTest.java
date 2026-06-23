// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.common.utilities;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import jakarta.servlet.http.HttpServletRequest;
import java.lang.reflect.Proxy;
import java.util.Map;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class HttpUtilsTest {
  private HttpUtils httpUtils;

  @BeforeEach
  void setUp() throws Exception {
    var constructor = HttpUtils.class.getDeclaredConstructor();
    constructor.setAccessible(true);
    httpUtils = constructor.newInstance();
  }

  private static HttpServletRequest request(
      String httpMethod,
      Map<String, String> headers,
      String remoteAddr,
      StringBuffer requestURL,
      String queryString) {
    return (HttpServletRequest)
        Proxy.newProxyInstance(
            HttpServletRequest.class.getClassLoader(),
            new Class[] {HttpServletRequest.class},
            (proxy, method, args) -> {
              var name = method.getName();
              switch (name) {
                case "getMethod" -> {
                  return httpMethod;
                }
                case "getHeader" -> {
                  return headers == null ? null : headers.get(args[0]);
                }
                case "getRemoteAddr" -> {
                  return remoteAddr;
                }
                case "getRequestURL" -> {
                  return requestURL;
                }
                case "getQueryString" -> {
                  return queryString;
                }
              }

              var returnType = method.getReturnType();
              if (!returnType.isPrimitive()) return null;
              if (boolean.class.equals(returnType)) return false;
              if (int.class.equals(returnType)) return 0;
              if (long.class.equals(returnType)) return 0L;
              if (double.class.equals(returnType)) return 0D;
              return null;
            });
  }

  @Test
  void givenPublicIp_whenValidatingPublicIp_thenReturnsTrue() {
    assertTrue(httpUtils.isValidPublicIp("8.8.8.8"));
  }

  @Test
  void givenPrivateIp_whenValidatingPublicIp_thenReturnsFalse() {
    assertFalse(httpUtils.isValidPublicIp("192.168.0.1"));
  }

  @Test
  void givenBlankIp_whenValidatingPublicIp_thenReturnsFalse() {
    assertFalse(httpUtils.isValidPublicIp(" "));
  }

  @Test
  void givenValidHttpMethod_whenGettingHttpMethod_thenReturnsMethodName() {
    var request = request("POST", null, null, null, null);
    assertEquals("POST", httpUtils.getHttpMethod(request));
  }

  @Test
  void givenInvalidHttpMethod_whenGettingHttpMethod_thenReturnsProvidedMethod() {
    var request = request("BAD_METHOD", null, null, null, null);
    assertEquals("BAD_METHOD", httpUtils.getHttpMethod(request));
  }

  @Test
  void givenNullOrBlankHttpMethod_whenGettingHttpMethod_thenReturnsGet() {
    assertEquals("GET", httpUtils.getHttpMethod(request(null, null, null, null, null)));
    assertEquals("GET", httpUtils.getHttpMethod(request(" ", null, null, null, null)));
  }

  @Test
  void givenRequestIpHeaderWithMultipleValues_whenGettingFirstRequestIp_thenReturnsFirstIp() {
    var request =
        request(
            null, Map.of("X-Remote-Ip-Address", "192.168.0.1, 10.0.0.1"), "8.8.8.8", null, null);
    assertEquals("192.168.0.1", httpUtils.getFirstRequestIP(request));
  }

  @Test
  void givenNoIpHeaders_whenGettingFirstRequestIp_thenFallsBackToRemoteAddr() {
    var request = request(null, null, "8.8.8.8", null, null);
    assertEquals("8.8.8.8", httpUtils.getFirstRequestIP(request));
  }

  @Test
  void
      givenForwardedHostHeaderWithMultipleValues_whenGettingFirstForwardedHost_thenReturnsFirstHost() {
    var request =
        request(null, Map.of("X-Forwarded-Host", "example.com,other"), "8.8.8.8", null, null);
    assertEquals("example.com", httpUtils.getFirstForwardedHost(request));
  }

  @Test
  void givenQueryStringProvided_whenGettingFullUrl_thenAppendsQueryString() {
    var request = request(null, null, null, new StringBuffer("http://example.com/path"), "a=1");
    assertEquals("http://example.com/path?a=1", httpUtils.getFullURL(request));
  }

  @Test
  void givenQueryStringIsNull_whenGettingFullUrl_thenReturnsBaseUrl() {
    var request = request(null, null, null, new StringBuffer("http://example.com/path"), null);
    assertEquals("http://example.com/path", httpUtils.getFullURL(request));
  }

  @Test
  void givenUrl_whenExtractingHostFromUrl_thenReturnsHost() {
    assertEquals("example.com", httpUtils.extractHostFromUrl("https://example.com/page"));
  }
}
