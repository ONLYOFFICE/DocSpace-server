package com.onlyoffice.authorization.api.external.controllers.advice;

import com.onlyoffice.authorization.api.core.transfer.response.ErrorDTO;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
@Slf4j
public class ValidationExceptionHandler {
    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<ErrorDTO> handleValidationException(MethodArgumentNotValidException e) {
        log.error(e.getMessage());
        return new ResponseEntity<>(ErrorDTO
                .builder()
                .reason(e.getMessage())
                .build(), HttpStatus.BAD_REQUEST);
    }
}
