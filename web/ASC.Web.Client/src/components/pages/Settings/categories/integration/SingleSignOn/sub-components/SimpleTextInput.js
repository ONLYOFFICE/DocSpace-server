import React from "react";
import { inject, observer } from "mobx-react";

import TextInput from "@appserver/components/text-input";

import StyledInputWrapper from "../styled-containers/StyledInputWrapper";

const SimpleTextInput = (props) => {
  const {
    hasError,
    isDisabled,
    maxWidth,
    name,
    placeholder,
    tabIndex,
    value,
    enableSso,
    setInput,
    onLoadXML,
    setError,
    hideError,
  } = props;

  const onFocus = (e) => {
    hideError(e.target.name);
  };

  const onBlur = (e) => {
    const field = e.target.name;
    const value = e.target.value;

    setError(field, value);
  };

  return (
    <StyledInputWrapper maxWidth={maxWidth}>
      <TextInput
        className="field-input"
        hasError={hasError}
        isDisabled={isDisabled ?? (!enableSso || onLoadXML)}
        name={name}
        onBlur={onBlur}
        onFocus={onFocus}
        onChange={setInput}
        placeholder={placeholder}
        scale
        tabIndex={tabIndex}
        value={value}
      />
    </StyledInputWrapper>
  );
};

export default inject(({ ssoStore }) => {
  const { enableSso, setInput, onLoadXML, setError, hideError } = ssoStore;

  return {
    enableSso,
    setInput,
    onLoadXML,
    setError,
    hideError,
  };
})(observer(SimpleTextInput));
