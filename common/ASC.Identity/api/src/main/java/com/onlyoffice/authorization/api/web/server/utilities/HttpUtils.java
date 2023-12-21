package com.onlyoffice.authorization.api.web.server.utilities;

import jakarta.servlet.http.HttpServletRequest;

/**
 *
 */
public class HttpUtils {
    private static final String[] IP_HEADERS = {
            "X-Forwarded-For",
            "X-Forwarded-Host",
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

    private HttpUtils() {}

    /**
     *
     * @param request
     * @return
     */
    public static String getRequestIP(HttpServletRequest request) {
        for (var header: IP_HEADERS) {
            var value = request.getHeader(header);
            if (value == null || value.isEmpty())
                continue;
            var parts = value.split("\\s*,\\s*");
            return parts[0];
        }

        return request.getRemoteAddr();
    }

    /**
     *
     * @param request
     * @return
     */
    public static String getClientOS(HttpServletRequest request) {
        var browserDetails = request.getHeader("User-Agent");
        var lowerCaseBrowser = browserDetails.toLowerCase();
        if (lowerCaseBrowser.contains("windows")) {
            return "Windows";
        } else if (lowerCaseBrowser.contains("mac")) {
            return "Mac";
        } else if (lowerCaseBrowser.contains("x11")) {
            return "Unix";
        } else if (lowerCaseBrowser.contains("android")) {
            return "Android";
        } else if (lowerCaseBrowser.contains("iphone")) {
            return "IPhone";
        } else {
            return "Unknown";
        }
    }

    /**
     *
     * @param request
     * @return
     */
    public static String getClientBrowser(HttpServletRequest request) {
        var browserDetails = request.getHeader("User-Agent");
        var user = browserDetails.toLowerCase();
        var browser = "";
        if (user.contains("msie")) {
            var substring = browserDetails.substring(browserDetails.indexOf("MSIE")).split(";")[0];
            browser = substring.split(" ")[0].replace("MSIE", "IE") + " " + substring.split(" ")[1];
        } else if (user.contains("safari") && user.contains("version")) {
            browser = (browserDetails.substring(browserDetails.indexOf("Safari"))
                    .split(" ")[0]).split("/")[0] + " " +
                    (browserDetails.substring(browserDetails.indexOf("Version"))
                            .split(" ")[0]).split("/")[1];
        } else if (user.contains("opr") || user.contains("opera")) {
            if (user.contains("opera"))
                browser = (browserDetails.substring(browserDetails.indexOf("Opera")).split(" ")[0]).split(
                        "/")[0] + "-" + (browserDetails.substring(
                        browserDetails.indexOf("Version")).split(" ")[0]).split("/")[1];
            else if (user.contains("opr"))
                browser = ((browserDetails.substring(browserDetails.indexOf("OPR")).split(" ")[0]).replace("/",
                        " ")).replace(
                        "OPR", "Opera");
        } else if (user.contains("chrome")) {
            browser = (browserDetails.substring(browserDetails.indexOf("Chrome")).split(" ")[0])
                    .replace("/", "-")
                    .replaceAll("\\.[0-9]+", "")
                    .replaceAll("-", " ");
        } else if ((user.indexOf("mozilla/7.0") > -1) || (user.indexOf("netscape6") != -1) || (user.indexOf(
                "mozilla/4.7") != -1) || (user.indexOf("mozilla/4.78") != -1) || (user.indexOf(
                "mozilla/4.08") != -1) || (user.indexOf("mozilla/3") != -1)) {
            browser = "Netscape";
        } else if (user.contains("firefox")) {
            browser = (browserDetails.substring(browserDetails.indexOf("Firefox")).split(" ")[0]).replace("/", " ");
        } else if (user.contains("rv")) {
            browser = "IE";
        } else {
            browser = "Unknown";
        }

        return browser;
    }

    /**
     *
     * @param request
     * @return
     */
    public static String getFullURL(HttpServletRequest request) {
        var requestURL = request.getRequestURL();
        var queryString = request.getQueryString();
        var result = queryString == null ? requestURL.toString() : requestURL.append('?')
                .append(queryString)
                .toString();

        return result;
    }
}
