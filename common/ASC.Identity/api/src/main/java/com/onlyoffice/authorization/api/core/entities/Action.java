package com.onlyoffice.authorization.api.core.entities;

import lombok.Getter;

import java.util.stream.Stream;

/**
 *
 */
@Getter
public enum Action {
    CREATE_CLIENT(9901),
    UPDATE_CLIENT(9902),
    REGENERATE_SECRET(9903),
    DELETE_CLIENT(9904),
    CHANGE_CLIENT_ACTIVATION(9905),
    REVOKE_USER_CLIENT(9906);
    private int code;
    Action(int code) {
        this.code = code;
    }
    public static Action of(int code) {
        return Stream.of(Action.values())
                .filter(p -> p.code == code)
                .findFirst()
                .orElseThrow(IllegalArgumentException::new);
    }
}
