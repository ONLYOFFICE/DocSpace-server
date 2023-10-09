package com.onlyoffice.authorization.api.controllers.advice;

import com.onlyoffice.authorization.api.dto.response.ErrorDTO;
import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

import java.util.concurrent.ExecutionException;

@ControllerAdvice
@Slf4j
public class ExecutionExceptionHandler {
    @ExceptionHandler(ExecutionException.class)
    public ResponseEntity<ErrorDTO> handleExecutionException(ExecutionException ex, HttpServletRequest request) {
        log.error("Exception has occurred: {}", ex.getMessage());
        return new ResponseEntity<ErrorDTO>(ErrorDTO
                .builder()
                .reason("could not perform operation")
                .build(),
                HttpStatus.BAD_REQUEST
        );
    }
}
