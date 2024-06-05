package com.asc.authorization.application.rest;

import com.asc.common.utilities.HttpUtils;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import org.springframework.boot.autoconfigure.web.servlet.error.AbstractErrorController;
import org.springframework.boot.web.servlet.error.ErrorAttributes;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;

/** Controller for handling fallback errors and redirecting to the login page. */
@Controller
public class FallbackController extends AbstractErrorController {
  private static final String PATH = "/error";

  /**
   * Constructs a new FallbackController with the given error attributes.
   *
   * @param errorAttributes the error attributes.
   */
  public FallbackController(ErrorAttributes errorAttributes) {
    super(errorAttributes);
  }

  /**
   * Handles errors by redirecting to the login page.
   *
   * @param request the {@link HttpServletRequest} that triggered the error.
   * @param response the {@link HttpServletResponse} to which the redirect is sent.
   * @throws IOException if an input or output exception occurs.
   */
  @RequestMapping(PATH)
  public void handleError(
      @RequestParam(name = "client_id", defaultValue = "error") String clientId,
      HttpServletRequest request,
      HttpServletResponse response)
      throws IOException {
    response.sendRedirect(
        String.format(
            "%s://%s/login?type=oauth2&client_id=%s",
            request.getScheme(), HttpUtils.getFirstRequestIP(request), clientId));
  }

  /**
   * Returns the error path used by this controller.
   *
   * @return the error path.
   */
  public String getErrorPath() {
    return PATH;
  }
}
