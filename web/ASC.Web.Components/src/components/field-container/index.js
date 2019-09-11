import React from 'react'
import PropTypes from 'prop-types';
import styled, { css } from 'styled-components';
import { tablet } from '../../utils/device'
import Label from '../label'

const horizontalCss = css`
  display: flex;
  flex-direction: row;
  align-items: start;
  margin: 0 0 16px 0;

  .field-label {
    line-height: 32px;
    margin: 0;
    width: 110px;
    min-width: 110px;
  }
  .field-body {
    flex-grow: 1;
  }
`
const verticalCss = css`
  display: flex;
  flex-direction: column;
  align-items: start;
  margin: 0 0 16px 0;

  .field-label {
    line-height: unset;
    margin: 0 0 4px 0;
    width: 100%;
  }
  .field-body {
    width: 100%;
  }
`

const Container = styled.div`
  ${props => props.vertical ? verticalCss : horizontalCss }

  @media ${tablet} {
    ${verticalCss}
  }
`;

const FieldContainer = React.memo((props) => {
  const {isVertical, className, isRequired, hasError, labelText, children} = props;
  return (
    <Container vertical={isVertical} className={className}>
      <Label isRequired={isRequired} error={hasError} text={labelText} className="field-label"/>
      <div className="field-body">{children}</div>
    </Container>
  );
});

FieldContainer.displayName = 'FieldContainer';

FieldContainer.propTypes = {
  isVertical: PropTypes.bool,
  className: PropTypes.string,
  isRequired: PropTypes.bool,
  hasError: PropTypes.bool,
  labelText: PropTypes.string,
  children: PropTypes.oneOfType([
    PropTypes.arrayOf(PropTypes.node),
    PropTypes.node
  ])
};

export default FieldContainer