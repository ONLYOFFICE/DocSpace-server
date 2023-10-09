package com.onlyoffice.authorization.api.exceptions;

public class ClientCreationException extends RuntimeException {
    public ClientCreationException() {
    }

    public ClientCreationException(String message) {
        super(message);
    }

    public ClientCreationException(String message, Throwable cause) {
        super(message, cause);
    }
}
