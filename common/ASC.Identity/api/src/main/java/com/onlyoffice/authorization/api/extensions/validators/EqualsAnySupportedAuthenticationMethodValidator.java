package com.onlyoffice.authorization.api.extensions.validators;

import com.onlyoffice.authorization.api.extensions.annotations.EqualsAnySupportedAuthenticationMethod;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import lombok.extern.slf4j.Slf4j;

/**
 *
 */
@Slf4j
public class EqualsAnySupportedAuthenticationMethodValidator implements ConstraintValidator<EqualsAnySupportedAuthenticationMethod, String> {
    private final String PKCE = "none";
    private final String CLIENT_SECRET = "client_secret_post";

    /**
     *
     * @param s
     * @param constraintValidatorContext
     * @return
     */
    public boolean isValid(String s, ConstraintValidatorContext constraintValidatorContext) {
        log.debug("Validating authentication method", s);
        return s.equals(PKCE) || s.equals(CLIENT_SECRET);
    }
}