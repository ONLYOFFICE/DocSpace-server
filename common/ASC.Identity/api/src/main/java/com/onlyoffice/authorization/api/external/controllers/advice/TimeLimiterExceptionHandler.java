/**
 *
 */
package com.onlyoffice.authorization.api.external.controllers.advice;

import com.onlyoffice.authorization.api.core.transfer.response.ErrorDTO;
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
    @ExceptionHandler(TimeoutException.class)
    public ResponseEntity<ErrorDTO> handleTimeoutException(TimeoutException ex, HttpServletRequest request) {
        log.error(ex.getMessage());
        return new ResponseEntity<ErrorDTO>(ErrorDTO
                .builder()
                .reason("request timeout")
                .build(),
                HttpStatus.REQUEST_TIMEOUT
        );
    }
}
