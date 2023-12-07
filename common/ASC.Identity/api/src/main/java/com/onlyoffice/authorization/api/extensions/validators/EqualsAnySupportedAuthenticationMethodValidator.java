package com.onlyoffice.authorization.api.extensions.validators;

import com.onlyoffice.authorization.api.extensions.annotations.EqualsAnySupportedAuthenticationMethod;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

public class EqualsAnySupportedAuthenticationMethodValidator implements ConstraintValidator<EqualsAnySupportedAuthenticationMethod, String> {
    private final String PKCE = "none";
    private final String CLIENT_SECRET = "client_secret_post";
    @Override
    public boolean isValid(String s, ConstraintValidatorContext constraintValidatorContext) {
        return s.equals(PKCE) || s.equals(CLIENT_SECRET);
    }
}
