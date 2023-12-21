package com.onlyoffice.authorization.api.core.exceptions;

/**
 *
 */
public class DistributedRateLimiterException extends RuntimeException {
    public DistributedRateLimiterException() {
    }

    public DistributedRateLimiterException(String message) {
        super(message);
    }

    public DistributedRateLimiterException(String message, Throwable cause) {
        super(message, cause);
    }

    public DistributedRateLimiterException(Throwable cause) {
        super(cause);
    }

    public DistributedRateLimiterException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
