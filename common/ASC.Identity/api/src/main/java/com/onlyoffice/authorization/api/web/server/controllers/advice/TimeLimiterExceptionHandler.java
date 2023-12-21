/**
 *
 */
package com.onlyoffice.authorization.api.web.server.controllers.advice;

import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

import java.util.concurrent.TimeoutException;

/**
 *
 */
@ControllerAdvice
@Slf4j
public class TimeLimiterExceptionHandler {
    /**
     *
     * @param ex
     * @param request
     * @return
     */
    @ExceptionHandler(TimeoutException.class)
    public ResponseEntity handleTimeoutException(TimeoutException ex, HttpServletRequest request) {
        log.error(ex.getMessage());
        return ResponseEntity.status(HttpStatus.REQUEST_TIMEOUT).build();
    }
}
