package com.asc.authorization.api.extensions.annotations;

import com.asc.authorization.api.core.entities.Action;

import java.lang.annotation.*;

/**
 *
 */
@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.METHOD, ElementType.TYPE})
@Documented
public @interface AuditAction {
    /**
     *
     * @return
     */
    Action action();
}
