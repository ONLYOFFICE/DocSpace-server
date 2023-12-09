/**
 *
 */
package com.onlyoffice.authorization.api.extensions.annotations;

import com.onlyoffice.authorization.api.extensions.validators.URLCollectionValidator;
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
@Constraint(validatedBy = URLCollectionValidator.class)
public @interface URLCollection {
    String message() default "url collection has invalid entries";
    Class<?>[] groups() default {};
    Class<? extends Payload>[] payload() default {};
}