package com.onlyoffice.authorization.api.core.exceptions;

public class AuthorizationsDeletionException extends RuntimeException {
    public AuthorizationsDeletionException() {
    }

    public AuthorizationsDeletionException(String message) {
        super(message);
    }

    public AuthorizationsDeletionException(String message, Throwable cause) {
        super(message, cause);
    }
}
