package com.onlyoffice.authorization.api.controllers.advice;

import com.onlyoffice.authorization.api.dto.response.ErrorDTO;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

import java.util.concurrent.TimeoutException;

@ControllerAdvice
@Slf4j
public class TimeLimiterExceptionHandler {
    @ExceptionHandler(TimeoutException.class)
    public ResponseEntity<ErrorDTO> handleTimeoutException(TimeoutException ex, HttpServletRequest request) {
        log.warn("Request to path '{}' is blocked due to time-limiting. {}",
                request.getRequestURI(), ex.getMessage());
        return new ResponseEntity<ErrorDTO>(ErrorDTO
                .builder()
                .reason("request timeout")
                .build(),
                HttpStatus.REQUEST_TIMEOUT
        );
    }
}
