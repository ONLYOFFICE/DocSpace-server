/**
 *
 */
package com.onlyoffice.authorization.api.external.controllers.advice;

import com.onlyoffice.authorization.api.core.exceptions.ClientNotFoundException;
import com.onlyoffice.authorization.api.core.transfer.response.ErrorDTO;
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
public class ClientNotFoundExceptionHandler {
    @ExceptionHandler(ClientNotFoundException.class)
    public ResponseEntity<ErrorDTO> handleClientNotFound(ClientNotFoundException ex, HttpServletRequest request) {
        log.error(ex.getMessage());
        return new ResponseEntity<>(ErrorDTO
                .builder()
                .reason(ex.getMessage())
                .build(),
                HttpStatus.BAD_REQUEST
        );
    }
}
