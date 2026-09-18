package com.onlyoffice.docspace.webhooks.sdk.model;

import java.util.Map;
import java.util.Objects;

/**
 * Stand-in for the generator's oneOf base class. {@code EntryId} is the only
 * subclass in this contract. Copied into generated/ by ../generate.sh.
 *
 * <p>Its discriminator works by trying each branch's adapter in turn, so the
 * only members the subclass needs are the actual-instance accessors and the
 * schema map.
 */
public abstract class AbstractOpenApiSchema {

    private Object instance;
    private final Boolean isNullable;
    private final String schemaType;

    protected AbstractOpenApiSchema(String schemaType, Boolean isNullable) {
        this.schemaType = schemaType;
        this.isNullable = isNullable;
    }

    /** Branch name to type, e.g. {@code {"Integer": Integer.class}}. */
    public abstract Map<String, Class<?>> getSchemas();

    public Object getActualInstance() {
        return instance;
    }

    public void setActualInstance(Object instance) {
        this.instance = instance;
    }

    public Boolean getIsNullable() {
        return isNullable;
    }

    public String getSchemaType() {
        return schemaType;
    }

    @Override
    public String toString() {
        return String.valueOf(instance);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) {
            return true;
        }
        if (o == null || getClass() != o.getClass()) {
            return false;
        }
        AbstractOpenApiSchema other = (AbstractOpenApiSchema) o;
        return Objects.equals(instance, other.instance)
                && Objects.equals(isNullable, other.isNullable)
                && Objects.equals(schemaType, other.schemaType);
    }

    @Override
    public int hashCode() {
        return Objects.hash(instance, isNullable, schemaType);
    }
}
