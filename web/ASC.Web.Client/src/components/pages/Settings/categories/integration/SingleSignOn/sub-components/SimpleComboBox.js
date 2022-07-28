import React from "react";
import { inject, observer } from "mobx-react";

import ComboBox from "@appserver/components/combobox";
import FieldContainer from "@appserver/components/field-container";

import StyledInputWrapper from "../styled-containers/StyledInputWrapper";

const SimpleComboBox = (props) => {
  const {
    labelText,
    name,
    options,
    tabIndex,
    value,
    setComboBox,
    enableSso,
    onLoadXML,
  } = props;

  const currentOption =
    options.find((option) => option.key === value) || options[0];

  const onSelect = () => {
    setComboBox(currentOption, name);
  };

  return (
    <FieldContainer isVertical labelText={labelText}>
      <StyledInputWrapper>
        <ComboBox
          isDisabled={!enableSso || onLoadXML}
          onSelect={onSelect}
          options={options}
          scaled
          scaledOptions
          selectedOption={currentOption}
          showDisabledItems
          tabIndex={tabIndex}
        />
      </StyledInputWrapper>
    </FieldContainer>
  );
};

export default inject(({ ssoStore }) => {
  const { setComboBox, enableSso, onLoadXML } = ssoStore;

  return {
    setComboBox,
    enableSso,
    onLoadXML,
  };
})(observer(SimpleComboBox));
