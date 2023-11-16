/**
 *
 */
package com.onlyoffice.authorization.api.core.exceptions;

/**
 *
 */
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
