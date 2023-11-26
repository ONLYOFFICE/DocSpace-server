package com.onlyoffice.authorization.api.external.validation.annotations;

import com.onlyoffice.authorization.api.external.validation.validators.EqualsAnySupportedAuthenticationMethodValidator;
import jakarta.validation.Constraint;
import jakarta.validation.Payload;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Target({ElementType.METHOD, ElementType.FIELD})
@Retention(RetentionPolicy.RUNTIME)
@Constraint(validatedBy = EqualsAnySupportedAuthenticationMethodValidator.class)
public @interface EqualsAnySupportedAuthenticationMethod {
    String message() default "value has no valid entries";
    Class<?>[] groups() default {};
    Class<? extends Payload>[] payload() default {};
}
