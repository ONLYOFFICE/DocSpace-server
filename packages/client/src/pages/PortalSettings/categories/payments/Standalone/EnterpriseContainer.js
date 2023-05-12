import React from "react";
import { withRouter } from "react-router";
import { inject, observer } from "mobx-react";
import { Trans } from "react-i18next";

import Text from "@docspace/components/text";
import { ColorTheme, ThemeType } from "@docspace/common/components/ColorTheme";

import { StyledEnterpriseComponent } from "./StyledComponent";
import BenefitsContainer from "./sub-components/BenefitsContainer";
import ButtonContainer from "./sub-components/ButtonContainer";
import TariffTitleContainer from "./sub-components/TariffTitleContainer";

const EnterpriseContainer = (props) => {
  const { salesEmail, t, isSubscriptionExpired, theme } = props;

  return (
    <StyledEnterpriseComponent theme={theme}>
      <Text fontWeight={700} fontSize={"16px"}>
        {t("EnterpriseRenewSubscription")}
      </Text>

      <TariffTitleContainer
        t={t}
        limitedWidth={isSubscriptionExpired}
        isSubscriptionExpired={isSubscriptionExpired}
      />

      {isSubscriptionExpired && <BenefitsContainer t={t} />}
      <Text fontSize="14px" className="payments_renew-subscription">
        {isSubscriptionExpired ? t("BuyLicense") : t("EnterpriseRenewal")}
      </Text>
      <ButtonContainer t={t} />

      <div className="payments_support">
        <Text>
          <Trans i18nKey="EnterprisePersonalRenewal" ns="Payments" t={t}>
            To get your personal renewal terms, contact your dedicated manager
            or write us at
            <ColorTheme
              fontWeight="600"
              target="_blank"
              tag="a"
              href={`mailto:${salesEmail}`}
              themeId={ThemeType.Link}
            >
              {{ email: salesEmail }}
            </ColorTheme>
          </Trans>
        </Text>
      </div>
    </StyledEnterpriseComponent>
  );
};

export default inject(({ auth, payments }) => {
  const { buyUrl, salesEmail } = payments;
  const { settingsStore } = auth;
  const { theme } = settingsStore;
  const isSubscriptionExpired = false;

  return { theme, buyUrl, salesEmail, isSubscriptionExpired };
})(withRouter(observer(EnterpriseContainer)));
