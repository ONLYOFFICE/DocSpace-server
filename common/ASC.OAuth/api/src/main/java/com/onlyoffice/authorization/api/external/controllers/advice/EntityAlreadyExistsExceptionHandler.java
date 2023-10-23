/**
 *
 */
package com.onlyoffice.authorization.api.external.controllers.advice;

import com.onlyoffice.authorization.api.core.exceptions.EntityAlreadyExistsException;
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
public class EntityAlreadyExistsExceptionHandler {
    @ExceptionHandler(EntityAlreadyExistsException.class)
    public ResponseEntity<ErrorDTO> handleEntityAlreadyExists(EntityAlreadyExistsException ex, HttpServletRequest request) {
        return new ResponseEntity<>(ErrorDTO
                .builder()
                .reason(ex.getMessage())
                .build(),
                HttpStatus.CONFLICT
        );
    }
}
