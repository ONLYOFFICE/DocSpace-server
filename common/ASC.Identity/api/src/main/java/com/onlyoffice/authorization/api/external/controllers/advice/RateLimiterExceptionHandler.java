/**
 *
 */
package com.onlyoffice.authorization.api.external.controllers.advice;

import com.onlyoffice.authorization.api.core.transfer.response.ErrorDTO;
import io.github.resilience4j.ratelimiter.RequestNotPermitted;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 *
 */
@ControllerAdvice
@Slf4j
public class RateLimiterExceptionHandler {
    @ExceptionHandler(RequestNotPermitted.class)
    public ResponseEntity<ErrorDTO> handleRequestNotPermitted(RequestNotPermitted ex, HttpServletRequest request) {
        log.error(ex.getMessage());
        return new ResponseEntity<ErrorDTO>(ErrorDTO
                .builder()
                .reason("too many requests")
                .build(),
                HttpStatus.TOO_MANY_REQUESTS
        );
    }
}
