/**
 *
 */
package com.asc.authorization.web.server.controllers;

import com.asc.authorization.web.server.utilities.HttpUtils;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.boot.autoconfigure.web.servlet.error.AbstractErrorController;
import org.springframework.boot.web.servlet.error.ErrorAttributes;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;

import java.io.IOException;

/**
 *
 */
@Controller
public class FallbackController extends AbstractErrorController {
    private static final String PATH = "/error";

    public FallbackController(ErrorAttributes errorAttributes) {
        super(errorAttributes);
    }

    /**
     *
     * @param request
     * @param response
     * @throws IOException
     */
    @RequestMapping(PATH)
    public void handleError(HttpServletRequest request, HttpServletResponse response)
            throws IOException {
        response.sendRedirect(String.format("%s://%s/login/?type=oauth2&clientId=error",
                request.getScheme(), HttpUtils.getRequestIP(request)));
    }

    /**
     *
     * @return
     */
    public String getErrorPath() {
        return PATH;
    }
}
