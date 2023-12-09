package com.onlyoffice.authorization.api.core.exceptions;

public class DistributedRateLimiterException extends RuntimeException {
    public DistributedRateLimiterException(String message) {
        super(message);
    }
}
