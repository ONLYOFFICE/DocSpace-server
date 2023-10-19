/**
 *
 */
package com.onlyoffice.authorization.core.exceptions;

/**
 *
 */
public class ClientPermissionException extends RuntimeException {
    public ClientPermissionException() {
    }

    public ClientPermissionException(String message) {
        super(message);
    }

    public ClientPermissionException(String message, Throwable cause) {
        super(message, cause);
    }
}
