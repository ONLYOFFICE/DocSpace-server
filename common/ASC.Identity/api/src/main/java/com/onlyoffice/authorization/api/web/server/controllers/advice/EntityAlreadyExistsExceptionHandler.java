/**
 *
 */
package com.onlyoffice.authorization.api.web.server.controllers.advice;

import com.onlyoffice.authorization.api.core.exceptions.EntityAlreadyExistsException;
import com.onlyoffice.authorization.api.web.server.transfer.response.ErrorDTO;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

/**
 *
 */
@Slf4j
@ControllerAdvice
public class EntityAlreadyExistsExceptionHandler {
    /**
     *
     * @param ex
     * @param request
     * @return
     */
    @ExceptionHandler(EntityAlreadyExistsException.class)
    public ResponseEntity<ErrorDTO> handleEntityAlreadyExists(EntityAlreadyExistsException ex, HttpServletRequest request) {
        log.error(ex.getMessage());
        return new ResponseEntity<>(ErrorDTO
                .builder()
                .reason(ex.getMessage())
                .build(),
                HttpStatus.CONFLICT
        );
    }
}
