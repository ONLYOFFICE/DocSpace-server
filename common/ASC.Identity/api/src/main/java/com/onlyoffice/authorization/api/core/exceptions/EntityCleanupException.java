package com.onlyoffice.authorization.api.core.exceptions;

public class EntityCleanupException extends RuntimeException {
    public EntityCleanupException() {
    }

    public EntityCleanupException(String message) {
        super(message);
    }

    public EntityCleanupException(String message, Throwable cause) {
        super(message, cause);
    }

    public EntityCleanupException(Throwable cause) {
        super(cause);
    }

    public EntityCleanupException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
