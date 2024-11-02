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

package com.asc.common.utilities;

import jakarta.servlet.http.HttpServletRequest;
import java.util.Arrays;
import java.util.Optional;
import java.util.regex.Pattern;
import org.springframework.context.EnvironmentAware;
import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

/** Utility class for handling HTTP-related operations. */
@Component
public class HttpUtils implements EnvironmentAware {
  private String portalAddress;
  private static final String IP_PATTERN =
      "https?://([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})";
  private static final String DOMAIN_PATTERN = "https?://([a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})";
  private static final String X_FORWARDED_HOST = "X-Forwarded-Host";
  private static final String X_FORWARDED_FOR = "X-Forwarded-For";
  private static final String X_FORWARDED_PROTO = "X-Forwarded-Proto";
  private static final String HOST = "Host";
  private static final String[] IP_HEADERS = {
    "X-Forwarded-Host",
    "X-Forwarded-For",
    "Proxy-Client-IP",
    "WL-Proxy-Client-IP",
    "HTTP_X_FORWARDED_FOR",
    "HTTP_X_FORWARDED",
    "HTTP_X_CLUSTER_CLIENT_IP",
    "HTTP_CLIENT_IP",
    "HTTP_FORWARDED_FOR",
    "HTTP_FORWARDED",
    "HTTP_VIA",
    "REMOTE_ADDR"
  };

  private HttpUtils() {
    // Private constructor to prevent instantiation
  }

  public void setEnvironment(Environment environment) {
    portalAddress = environment.getProperty("APP_URL_PORTAL");
  }

  /**
   * Retrieves the address from the specified header of the request.
   *
   * @param request HttpServletRequest object
   * @param header The header name to retrieve the address from
   * @return An Optional containing the address if found, otherwise an empty Optional
   */
  private Optional<String> getRequestAddress(HttpServletRequest request, String header) {
    if (portalAddress != null && !portalAddress.isBlank())
      return Optional.of(portalAddress);
    var addressHeader = request.getHeader(header);
    var protoHeader = request.getHeader(X_FORWARDED_PROTO);
    if (addressHeader == null
        || addressHeader.isBlank()
        || protoHeader == null
        || protoHeader.isBlank()) return Optional.empty();

    var address =
        Arrays.stream(addressHeader.split(","))
            .map(String::trim)
            .filter(s -> !s.isBlank())
            .findFirst()
            .orElse(null);

    if (address == null || address.isBlank()) {
      return Optional.of(String.format("%s://%s", request.getScheme(), request.getRemoteAddr()));
    }

    var protocol = request.getScheme();
    var protocols = protoHeader.split(",");
    for (String proto : protocols) {
      if ("https".equalsIgnoreCase(proto)) {
        protocol = "https";
        break;
      }
    }

    return Optional.of(String.format("%s://%s", protocol, address));
  }

  /**
   * Retrieves the host address from the 'X-Forwarded-Host' header of the request.
   *
   * @param request HttpServletRequest object
   * @return An Optional containing the host address if found, otherwise an empty Optional
   */
  public Optional<String> getRequestHostAddress(HttpServletRequest request) {
    return getRequestAddress(request, X_FORWARDED_HOST);
  }

  /**
   * Retrieves the client address from the 'X-Forwarded-For' header of the request.
   *
   * @param request HttpServletRequest object
   * @return An Optional containing the client address if found, otherwise an empty Optional
   */
  public Optional<String> getRequestClientAddress(HttpServletRequest request) {
    return getRequestAddress(request, X_FORWARDED_FOR);
  }

  /**
   * Retrieves the domain from the 'Host' header of the request.
   *
   * @param request HttpServletRequest object
   * @return An Optional containing the domain if found, otherwise an empty Optional
   */
  public Optional<String> getRequestDomain(HttpServletRequest request) {
    var hostHeader = request.getHeader(HOST);
    var protoHeader = request.getHeader(X_FORWARDED_PROTO);
    if (hostHeader == null || hostHeader.isBlank() || protoHeader == null || protoHeader.isBlank())
      return Optional.empty();

    var host =
        Arrays.stream(hostHeader.split(","))
            .map(String::trim)
            .filter(s -> !s.isBlank())
            .findFirst()
            .orElse(null);

    if (host == null || host.isBlank()) {
      return Optional.of(String.format("%s://%s", request.getScheme(), request.getRemoteAddr()));
    }

    var protocol = request.getScheme();
    var protocols = protoHeader.split(",");
    for (String proto : protocols) {
      if ("https".equalsIgnoreCase(proto)) {
        protocol = "https";
        break;
      }
    }

    return Optional.of(String.format("%s://%s", protocol, host));
  }

  /**
   * Retrieves the first IP address from the request headers.
   *
   * @param request HttpServletRequest object
   * @return The first IP address found in the request headers, or the remote address if none found
   */
  public String getFirstRequestIP(HttpServletRequest request) {
    for (var header : IP_HEADERS) {
      var value = request.getHeader(header);
      if (value != null && !value.isEmpty()) return value.split("\\s*,\\s*")[0];
    }

    return request.getRemoteAddr();
  }

  /**
   * Determines the client's operating system from the User-Agent header.
   *
   * @param request HttpServletRequest object
   * @return Client's operating system
   */
  public String getClientOS(HttpServletRequest request) {
    var userAgent = request.getHeader("User-Agent");
    if (userAgent == null) return "Unknown";

    var osFamily = "Unknown";
    var osMajor = "";
    var deviceBrand = "";
    var deviceModel = "";

    if (userAgent.toLowerCase().contains("windows")) {
      osFamily = "Windows";
      osMajor = extractMajorVersion(userAgent, "Windows NT");
    } else if (userAgent.toLowerCase().contains("mac os x")) {
      osFamily = "Mac OS X";
      osMajor = extractMajorVersion(userAgent, "Mac OS X");
      deviceBrand = "Apple";
      deviceModel = "Mac";
    } else if (userAgent.toLowerCase().contains("android")) {
      osFamily = "Android";
      osMajor = extractMajorVersion(userAgent, "Android");
    } else if (userAgent.toLowerCase().contains("iphone")) {
      osFamily = "iPhone OS";
      osMajor = extractMajorVersion(userAgent, "iPhone OS");
    } else if (userAgent.toLowerCase().contains("x11")) {
      osFamily = "Unix";
    }

    return String.format("%s %s %s %s", osFamily, osMajor, deviceBrand, deviceModel).trim();
  }

  /**
   * Determines the client's browser from the User-Agent header.
   *
   * @param request HttpServletRequest object
   * @return Client's browser
   */
  public String getClientBrowser(HttpServletRequest request) {
    var browserDetails = request.getHeader("User-Agent");
    var user = browserDetails.toLowerCase();
    var browser = "";
    if (user.contains("msie")) {
      var substring = browserDetails.substring(browserDetails.indexOf("MSIE")).split(";")[0];
      browser = substring.split(" ")[0].replace("MSIE", "IE") + " " + substring.split(" ")[1];
    } else if (user.contains("safari") && user.contains("version")) {
      browser =
          (browserDetails.substring(browserDetails.indexOf("Safari")).split(" ")[0]).split("/")[0]
              + " "
              + (browserDetails.substring(browserDetails.indexOf("Version")).split(" ")[0])
                  .split("/")[1];
    } else if (user.contains("opr") || user.contains("opera")) {
      if (user.contains("opera"))
        browser =
            (browserDetails.substring(browserDetails.indexOf("Opera")).split(" ")[0]).split("/")[0]
                + "-"
                + (browserDetails.substring(browserDetails.indexOf("Version")).split(" ")[0])
                    .split("/")[1];
      else if (user.contains("opr"))
        browser =
            ((browserDetails.substring(browserDetails.indexOf("OPR")).split(" ")[0])
                    .replace("/", " "))
                .replace("OPR", "Opera");
    } else if (user.contains("chrome")) {
      browser =
          (browserDetails.substring(browserDetails.indexOf("Chrome")).split(" ")[0])
              .replace("/", "-")
              .replaceAll("\\.[0-9]+", "")
              .replaceAll("-", " ");
    } else if ((user.contains("mozilla/7.0"))
        || (user.contains("netscape6"))
        || (user.contains("mozilla/4.7"))
        || (user.contains("mozilla/4.78"))
        || (user.contains("mozilla/4.08"))
        || (user.contains("mozilla/3"))) {
      browser = "Netscape";
    } else if (user.contains("firefox")) {
      browser =
          (browserDetails.substring(browserDetails.indexOf("Firefox")).split(" ")[0])
              .replace("/", " ");
    } else if (user.contains("rv")) {
      browser = "IE";
    } else {
      browser = "Unknown";
    }

    return browser;
  }

  /**
   * Constructs the full URL of the current request.
   *
   * @param request HttpServletRequest object
   * @return Full URL of the request
   */
  public String getFullURL(HttpServletRequest request) {
    var requestURL = request.getRequestURL();
    var queryString = request.getQueryString();
    return queryString == null
        ? requestURL.toString()
        : requestURL.append('?').append(queryString).toString();
  }

  /**
   * Extracts the host from the given URL.
   *
   * @param url The URL to extract the host from
   * @return The extracted host if found, otherwise the original URL
   */
  public String extractHostFromUrl(String url) {
    return extractPattern(url, IP_PATTERN)
        .or(() -> extractPattern(url, DOMAIN_PATTERN))
        .orElse(url);
  }

  /**
   * Extracts a pattern from the given input string.
   *
   * @param input The input string
   * @param pattern The pattern to extract
   * @return An Optional containing the extracted pattern if found, otherwise an empty Optional
   */
  private Optional<String> extractPattern(String input, String pattern) {
    var compiledPattern = Pattern.compile(pattern);
    var matcher = compiledPattern.matcher(input);
    if (matcher.find()) return Optional.of(matcher.group(1));
    return Optional.empty();
  }

  /**
   * Extracts OS major version
   *
   * @param userAgent
   * @param identifier
   * @return A string with a major version or an empty string
   */
  private String extractMajorVersion(String userAgent, String identifier) {
    var versionPattern = identifier + " ([\\d.]+)";
    var pattern = Pattern.compile(versionPattern);
    var matcher = pattern.matcher(userAgent);
    if (matcher.find()) return matcher.group(1).split("\\.")[0];
    return "";
  }
}
