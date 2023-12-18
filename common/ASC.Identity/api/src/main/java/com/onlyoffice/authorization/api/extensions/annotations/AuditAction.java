package com.onlyoffice.authorization.api.extensions.annotations;

import com.onlyoffice.authorization.api.core.entities.Action;

import java.lang.annotation.*;

@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.METHOD, ElementType.TYPE})
@Documented
public @interface AuditAction {
    Action action();
}
