package com.onlyoffice.authorization.api.external.validation.validators;

import com.onlyoffice.authorization.api.external.validation.annotations.EqualsAnySupportedAuthenticationMethod;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

public class EqualsAnySupportedAuthenticationMethodValidator implements ConstraintValidator<EqualsAnySupportedAuthenticationMethod, String> {
    @Override
    public boolean isValid(String s, ConstraintValidatorContext constraintValidatorContext) {
        return s.equals("none") || s.equals("client_secret_post");
    }
}
