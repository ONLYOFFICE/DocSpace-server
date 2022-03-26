import styled from "styled-components";

export const StyledPage = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  margin: 56px auto 0 auto;
  max-width: 960px;

  @media (max-width: 768px) {
    padding: 0 16px;
  }

  @media (max-width: 414px) {
    margin-top: 72px;
  }
`;

export const StyledHeader = styled.div`
  text-align: left;

  .title {
    margin-bottom: 24px;
  }

  .subtitle {
    margin-bottom: 32px;
  }
`;

export const StyledBody = styled.div`
  width: 320px;

  @media (max-width: 768px) {
    width: 100%;
  }
  @media (max-width: 375px) {
    width: 100%;
  }

  .form-field {
    height: 48px;
  }

  .password-field-wrapper {
    width: 100%;
  }

  .confirm-button {
    width: 100%;
    margin-top: 8px;
  }

  .password-change-form {
    margin-top: 32px;
    margin-bottom: 16px;
  }

  .password-change-title {
    margin-bottom: 8px;
  }

  .subtitle-delete {
    margin-bottom: 8px;
  }

  .info-delete {
    margin-bottom: 24px;
  }
`;
