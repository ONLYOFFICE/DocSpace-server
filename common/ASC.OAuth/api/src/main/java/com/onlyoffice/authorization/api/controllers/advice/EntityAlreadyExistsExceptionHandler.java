package com.onlyoffice.authorization.api.controllers.advice;

import com.onlyoffice.authorization.api.dto.response.ErrorDTO;
import com.onlyoffice.authorization.api.exceptions.EntityAlreadyExistsException;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
@Slf4j
public class EntityAlreadyExistsExceptionHandler {
    @ExceptionHandler(EntityAlreadyExistsException.class)
    public ResponseEntity<ErrorDTO> handleEntityAlreadyExists(EntityAlreadyExistsException ex, HttpServletRequest request) {
        log.error("Exception has occurred: {}", ex.getMessage());
        return new ResponseEntity<>(ErrorDTO
                .builder()
                .reason(ex.getMessage())
                .build(),
                HttpStatus.CONFLICT
        );
    }
}
