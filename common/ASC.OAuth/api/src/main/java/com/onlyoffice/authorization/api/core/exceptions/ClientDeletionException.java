/**
 *
 */
package com.onlyoffice.authorization.api.core.exceptions;

/**
 *
 */
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
