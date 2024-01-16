package com.asc.authorization.api.extensions.annotations;

import com.asc.authorization.api.extensions.validators.EqualsAnySupportedAuthenticationMethodValidator;
import jakarta.validation.Constraint;
import jakarta.validation.Payload;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 *
 */
@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.METHOD, ElementType.FIELD})
@Constraint(validatedBy = EqualsAnySupportedAuthenticationMethodValidator.class)
public @interface EqualsAnySupportedAuthenticationMethod {
    /**
     *
     * @return
     */
    String message() default "value has no valid entries";

    /**
     *
     * @return
     */
    Class<?>[] groups() default {};

    /**
     *
     * @return
     */
    Class<? extends Payload>[] payload() default {};
}
