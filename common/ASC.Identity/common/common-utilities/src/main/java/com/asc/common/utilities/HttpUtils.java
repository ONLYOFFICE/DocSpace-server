package com.asc.common.utilities;

import jakarta.servlet.http.HttpServletRequest;
import java.util.Optional;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class HttpUtils {
  private static final String X_FORWARDED_HOST = "X-Forwarded-Host";
  private static final String X_FORWARDED_FOR = "X-Forwarded-For";
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

  private static Optional<String> getRequestAddress(HttpServletRequest request, String header) {
    String ip = request.getHeader(header);
    if (ip == null || ip.isBlank()) {
      return Optional.empty();
    }
    return Optional.of(String.format("%s://%s", request.getScheme(), ip));
  }

  public static Optional<String> getRequestHostAddress(HttpServletRequest request) {
    return getRequestAddress(request, X_FORWARDED_HOST);
  }

  public static Optional<String> getRequestClientAddress(HttpServletRequest request) {
    return getRequestAddress(request, X_FORWARDED_FOR);
  }

  /**
   * Retrieves the domain name from the Host header.
   *
   * @param request HttpServletRequest object
   * @return Optional containing the domain name from the Host header, or empty if not found
   */
  public static Optional<String> getRequestDomain(HttpServletRequest request) {
    String host = request.getHeader(HOST);
    if (host == null || host.isBlank()) {
      return Optional.empty();
    }
    return Optional.of(String.format("%s://%s", request.getScheme(), host));
  }

  /**
   * Retrieves the first IP address from the request headers.
   *
   * @param request HttpServletRequest object
   * @return First IP address found in the request headers or the remote address if none found
   */
  public static String getFirstRequestIP(HttpServletRequest request) {
    for (String header : IP_HEADERS) {
      String value = request.getHeader(header);
      if (value != null && !value.isEmpty()) {
        return value.split("\\s*,\\s*")[0];
      }
    }
    return request.getRemoteAddr();
  }

  /**
   * Determines the client's operating system and version from the User-Agent header.
   *
   * @param request HttpServletRequest object
   * @return Client's operating system and version
   */
  public static String getClientOS(HttpServletRequest request) {
    String userAgent = request.getHeader("User-Agent");
    String os = "Unknown";
    String osPattern = "";

    if (userAgent == null) {
      return os;
    }

    if (userAgent.toLowerCase().contains("windows")) {
      osPattern = "Windows NT ([\\d.]+)";
    } else if (userAgent.toLowerCase().contains("mac os x")) {
      osPattern = "Mac OS X ([\\d_]+)";
    } else if (userAgent.toLowerCase().contains("android")) {
      osPattern = "Android ([\\d.]+)";
    } else if (userAgent.toLowerCase().contains("iphone")) {
      osPattern = "iPhone OS ([\\d_]+)";
    } else if (userAgent.toLowerCase().contains("x11")) {
      os = "Unix";
    }

    if (!osPattern.isEmpty()) {
      Pattern pattern = Pattern.compile(osPattern);
      Matcher matcher = pattern.matcher(userAgent);
      if (matcher.find()) {
        os = matcher.group().replace('_', '.');
      }
    }

    return os;
  }

  /**
   * Determines the client's browser from the User-Agent header.
   *
   * @param request HttpServletRequest object
   * @return Client's browser
   */
  public static String getClientBrowser(HttpServletRequest request) {
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
   * Retrieves the full URL of the request.
   *
   * @param request HttpServletRequest object
   * @return Full URL including query parameters
   */
  public static String getFullURL(HttpServletRequest request) {
    StringBuffer requestURL = request.getRequestURL();
    String queryString = request.getQueryString();
    return queryString == null
        ? requestURL.toString()
        : requestURL.append('?').append(queryString).toString();
  }

  private static String getBrowserInfo(String userAgent, String browser, String replacement) {
    String substring = userAgent.substring(userAgent.indexOf(browser)).split(";")[0];
    return substring.split(" ")[0].replace(browser, replacement) + " " + substring.split(" ")[1];
  }

  private static String getBrowserVersion(String userAgent, String browser) {
    return userAgent.substring(userAgent.indexOf(browser)).split(" ")[0].split("/")[1];
  }
}
