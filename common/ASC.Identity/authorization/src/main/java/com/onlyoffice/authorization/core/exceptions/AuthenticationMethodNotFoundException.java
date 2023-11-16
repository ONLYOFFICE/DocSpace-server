/**
 *
 */
package com.onlyoffice.authorization.core.exceptions;

/**
 *
 */
public class AuthenticationMethodNotFoundException extends RuntimeException {
    public AuthenticationMethodNotFoundException() {
    }

    public AuthenticationMethodNotFoundException(String message) {
        super(message);
    }

    public AuthenticationMethodNotFoundException(String message, Throwable cause) {
        super(message, cause);
    }

    public AuthenticationMethodNotFoundException(Throwable cause) {
        super(cause);
    }

    public AuthenticationMethodNotFoundException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
