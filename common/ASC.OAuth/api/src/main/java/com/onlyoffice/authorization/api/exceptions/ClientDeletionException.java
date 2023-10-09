package com.onlyoffice.authorization.api.exceptions;

public class ClientDeletionException extends RuntimeException {
    public ClientDeletionException() {
    }

    public ClientDeletionException(String message) {
        super(message);
    }

    public ClientDeletionException(String message, Throwable cause) {
        super(message, cause);
    }
}
