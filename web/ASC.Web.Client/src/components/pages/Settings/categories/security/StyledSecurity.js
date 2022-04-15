import styled from "styled-components";
import ArrowRightIcon from "@appserver/studio/public/images/arrow.right.react.svg";
import commonIconsStyles from "@appserver/components/utils/common-icons-style";
import { Base } from "@appserver/components/themes";

export const StyledArrowRightIcon = styled(ArrowRightIcon)`
  ${commonIconsStyles}
  path {
    fill: ${(props) => props.theme.studio.settings.security.arrowFill};
  }
`;

StyledArrowRightIcon.defaultProps = { theme: Base };

export const MainContainer = styled.div`
  width: 100%;

  hr {
    margin: 24px 0;
    border: none;
    border-top: 1px solid #eceef1;
  }

  .subtitle {
    margin-bottom: 20px;
  }

  .settings_tabs {
    padding-bottom: 16px;
  }

  .page_loader {
    position: fixed;
    left: 50%;
  }
`;

MainContainer.defaultProps = { theme: Base };

export const StyledCategoryWrapper = styled.div`
  display: flex;
  flex-direction: row;
  gap: 4px;
  margin-bottom: 16px;
  align-items: center;
`;

export const StyledTooltip = styled.div`
  .subtitle {
    margin-bottom: 10px;
  }
`;

export const StyledMobileCategoryWrapper = styled.div`
  margin-bottom: 20px;

  .category-item-heading {
    display: flex;
    align-items: center;
    margin-bottom: 5px;
  }

  .category-item-subheader {
    font-size: 13px;
    font-weight: 600;
    margin-bottom: 5px;
  }

  .category-item-description {
    color: ${(props) => props.theme.studio.settings.security.descriptionColor};
    font-size: 12px;
    max-width: 1024px;
  }

  .inherit-title-link {
    margin-right: 7px;
    font-size: 19px;
    font-weight: 600;
  }

  .link-text {
    margin: 0;
  }
`;

StyledMobileCategoryWrapper.defaultProps = { theme: Base };

export const LearnMoreWrapper = styled.div`
  display: none;

  @media (max-width: 600px) {
    display: flex;
    flex-direction: column;
    margin-bottom: 20px;
  }

  .learn-subtitle {
    margin-bottom: 10px;
  }
`;
