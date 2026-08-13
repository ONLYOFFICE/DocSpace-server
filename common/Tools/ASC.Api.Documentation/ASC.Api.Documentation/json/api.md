# ONLYOFFICE DocSpace Portal API

The browsable version of this reference, with a request builder and code samples, is published at
<https://api.onlyoffice.com/docspace/api-backend/usage-api/>.

All URIs are relative to *https://yourportal.onlyoffice.com*, where the host is the address of your DocSpace instance.

## Endpoints

| Class | Method | HTTP request | Description |
|------------ | ------------- | ------------- | -------------|
| *AppsApi* | [**get**](#get) | **GET** /api/2.0/apps/{id} | Get a single app |
| *AppsApi* | [**getAll**](#getall) | **GET** /api/2.0/apps | Get all apps |
| *AppsApi* | [**getSettings**](#getsettings) | **GET** /api/2.0/apps/{id}/settings | Get app settings |
| *AppsApi* | [**setEnabled**](#setenabled) | **PUT** /api/2.0/apps/{id}/enabled | Enable or disable an app |
| *AppsApi* | [**setSettings**](#setsettings) | **PUT** /api/2.0/apps/{id}/settings | Save app settings |
| *AuthenticationApi* | [**authenticateMe**](#authenticateme) | **POST** /api/2.0/authentication | Authenticate a user |
| *AuthenticationApi* | [**authenticateMeFromBodyWithCode**](#authenticatemefrombodywithcode) | **POST** /api/2.0/authentication/{code} | Authenticate a user by code |
| *AuthenticationApi* | [**checkConfirm**](#checkconfirm) | **POST** /api/2.0/authentication/confirm | Open confirmation email URL |
| *AuthenticationApi* | [**getIsAuthentificated**](#getisauthentificated) | **GET** /api/2.0/authentication | Check authentication |
| *AuthenticationApi* | [**logout**](#logout) | **POST** /api/2.0/authentication/logout | Log out |
| *AuthenticationApi* | [**saveMobilePhone**](#savemobilephone) | **POST** /api/2.0/authentication/setphone | Set a mobile phone |
| *AuthenticationApi* | [**sendSmsCode**](#sendsmscode) | **POST** /api/2.0/authentication/sendsms | Send SMS code |
| *CapabilitiesApi* | [**getPortalCapabilities**](#getportalcapabilities) | **GET** /api/2.0/capabilities | Get portal capabilities |
| *MigrationApi* | [**cancelMigration**](#cancelmigration) | **POST** /api/2.0/migration/cancel | Cancel migration |
| *MigrationApi* | [**clearMigration**](#clearmigration) | **POST** /api/2.0/migration/clear | Clear migration |
| *MigrationApi* | [**finishMigration**](#finishmigration) | **POST** /api/2.0/migration/finish | Finish migration |
| *MigrationApi* | [**getMigrationLogs**](#getmigrationlogs) | **GET** /api/2.0/migration/logs | Get migration logs |
| *MigrationApi* | [**getMigrationStatus**](#getmigrationstatus) | **GET** /api/2.0/migration/status | Get migration status |
| *MigrationApi* | [**listMigrations**](#listmigrations) | **GET** /api/2.0/migration/list | Get migrations |
| *MigrationApi* | [**startMigration**](#startmigration) | **POST** /api/2.0/migration/migrate | Start migration |
| *MigrationApi* | [**uploadAndInitializeMigration**](#uploadandinitializemigration) | **POST** /api/2.0/migration/init/{migratorName} | Upload and initialize migration |
| *PortalPaymentApi* | [**calculateWalletPayment**](#calculatewalletpayment) | **PUT** /api/2.0/portal/payment/calculatewallet | Calculate the wallet payment amount |
| *PortalPaymentApi* | [**changeTenantWalletServiceState**](#changetenantwalletservicestate) | **POST** /api/2.0/portal/payment/servicestate | Change tenant wallet service state |
| *PortalPaymentApi* | [**createCustomerMonthlyUsageReport**](#createcustomermonthlyusagereport) | **POST** /api/2.0/portal/payment/customer/usage/monthly/report | Start the customer monthly usage report generation |
| *PortalPaymentApi* | [**createCustomerOperationsReport**](#createcustomeroperationsreport) | **POST** /api/2.0/portal/payment/customer/operationsreport | Start the customer operations report generation |
| *PortalPaymentApi* | [**createCustomerServiceUsageReport**](#createcustomerserviceusagereport) | **POST** /api/2.0/portal/payment/customer/usage/report | Start the customer service usage report generation |
| *PortalPaymentApi* | [**getActiveServices**](#getactiveservices) | **GET** /api/2.0/portal/payment/activeservices | Get the active wallet services |
| *PortalPaymentApi* | [**getAiPrices**](#getaiprices) | **GET** /api/2.0/portal/payment/ai-prices | Get AI model prices |
| *PortalPaymentApi* | [**getCheckoutSetupUrl**](#getcheckoutsetupurl) | **GET** /api/2.0/portal/payment/checkoutsetupurl | Get the checkout setup page URL |
| *PortalPaymentApi* | [**getCustomerBalance**](#getcustomerbalance) | **GET** /api/2.0/portal/payment/customer/balance | Get the customer balance |
| *PortalPaymentApi* | [**getCustomerInfo**](#getcustomerinfo) | **GET** /api/2.0/portal/payment/customerinfo | Get the customer information |
| *PortalPaymentApi* | [**getCustomerMonthlyUsage**](#getcustomermonthlyusage) | **GET** /api/2.0/portal/payment/customer/usage/monthly | Get the customer monthly usage |
| *PortalPaymentApi* | [**getCustomerMonthlyUsageReport**](#getcustomermonthlyusagereport) | **GET** /api/2.0/portal/payment/customer/usage/monthly/report | Get the status of the customer monthly usage report generation |
| *PortalPaymentApi* | [**getCustomerOperations**](#getcustomeroperations) | **GET** /api/2.0/portal/payment/customer/operations | Get the customer operations |
| *PortalPaymentApi* | [**getCustomerOperationsReport**](#getcustomeroperationsreport) | **GET** /api/2.0/portal/payment/customer/operationsreport | Get the status of the customer operations report generation |
| *PortalPaymentApi* | [**getCustomerServiceUsage**](#getcustomerserviceusage) | **GET** /api/2.0/portal/payment/customer/usage | Get the customer service usage |
| *PortalPaymentApi* | [**getCustomerServiceUsageReport**](#getcustomerserviceusagereport) | **GET** /api/2.0/portal/payment/customer/usage/report | Get the status of the customer service usage report generation |
| *PortalPaymentApi* | [**getPaymentAccount**](#getpaymentaccount) | **GET** /api/2.0/portal/payment/account | Get the payment account |
| *PortalPaymentApi* | [**getPaymentCurrencies**](#getpaymentcurrencies) | **GET** /api/2.0/portal/payment/currencies | Get currencies |
| *PortalPaymentApi* | [**getPaymentQuotas**](#getpaymentquotas) | **GET** /api/2.0/portal/payment/quotas | Get quotas |
| *PortalPaymentApi* | [**getPaymentUrl**](#getpaymenturl) | **PUT** /api/2.0/portal/payment/url | Get the payment page URL |
| *PortalPaymentApi* | [**getPortalPrices**](#getportalprices) | **GET** /api/2.0/portal/payment/prices | Get prices |
| *PortalPaymentApi* | [**getQuotaPaymentInformation**](#getquotapaymentinformation) | **GET** /api/2.0/portal/payment/quota | Get quota payment information |
| *PortalPaymentApi* | [**getRestrictedAiModels**](#getrestrictedaimodels) | **GET** /api/2.0/portal/payment/ai-model/restrictions | Get restricted AI models |
| *PortalPaymentApi* | [**getSubscriptionBalanceInfo**](#getsubscriptionbalanceinfo) | **GET** /api/2.0/portal/payment/subscription/balance | Get the subscription balance information |
| *PortalPaymentApi* | [**getTenantWalletServiceSettings**](#gettenantwalletservicesettings) | **GET** /api/2.0/portal/payment/servicessettings | Gets the wallet service settings for the tenant. |
| *PortalPaymentApi* | [**getTenantWalletSettings**](#gettenantwalletsettings) | **GET** /api/2.0/portal/payment/topupsettings | Gets the tenant wallet auto top up settings |
| *PortalPaymentApi* | [**getWalletService**](#getwalletservice) | **GET** /api/2.0/portal/payment/walletservice | Get wallet service |
| *PortalPaymentApi* | [**getWalletServices**](#getwalletservices) | **GET** /api/2.0/portal/payment/walletservices | Get wallet services |
| *PortalPaymentApi* | [**moveSubscriptionToWallet**](#movesubscriptiontowallet) | **POST** /api/2.0/portal/payment/subscription/movetowallet | Move the subscription balance to the wallet and purchase admins |
| *PortalPaymentApi* | [**sendPaymentRequest**](#sendpaymentrequest) | **POST** /api/2.0/portal/payment/request | Send a payment request |
| *PortalPaymentApi* | [**setRestrictedAiModels**](#setrestrictedaimodels) | **PUT** /api/2.0/portal/payment/ai-model/restrictions | Set restricted AI models |
| *PortalPaymentApi* | [**setTenantWalletSettings**](#settenantwalletsettings) | **POST** /api/2.0/portal/payment/topupsettings | Set the wallet auto top up settings |
| *PortalPaymentApi* | [**terminateCustomerMonthlyUsageReport**](#terminatecustomermonthlyusagereport) | **DELETE** /api/2.0/portal/payment/customer/usage/monthly/report | Terminate the customer monthly usage report generation |
| *PortalPaymentApi* | [**terminateCustomerOperationsReport**](#terminatecustomeroperationsreport) | **DELETE** /api/2.0/portal/payment/customer/operationsreport | Terminate the customer operations report generation |
| *PortalPaymentApi* | [**terminateCustomerServiceUsageReport**](#terminatecustomerserviceusagereport) | **DELETE** /api/2.0/portal/payment/customer/usage/report | Terminate the customer service usage report generation |
| *PortalPaymentApi* | [**topUpDeposit**](#topupdeposit) | **POST** /api/2.0/portal/payment/deposit | Put money on deposit |
| *PortalPaymentApi* | [**updatePayment**](#updatepayment) | **PUT** /api/2.0/portal/payment/update | Update the payment quantity |
| *PortalPaymentApi* | [**updateWalletPayment**](#updatewalletpayment) | **PUT** /api/2.0/portal/payment/updatewallet | Update the wallet payment quantity |
| *PortalQuotaApi* | [**getPortalQuota**](#getportalquota) | **GET** /api/2.0/portal/quota | Get a portal quota |
| *PortalQuotaApi* | [**getPortalTariff**](#getportaltariff) | **GET** /api/2.0/portal/tariff | Get a portal tariff |
| *PortalQuotaApi* | [**getPortalUsedSpace**](#getportalusedspace) | **GET** /api/2.0/portal/usedspace | Get the portal used space |
| *PortalQuotaApi* | [**getRightQuota**](#getrightquota) | **GET** /api/2.0/portal/quota/right | Get the recommended quota |
| *PortalQuotaApi* | [**getUpcomingPayments**](#getupcomingpayments) | **GET** /api/2.0/portal/tariff/upcoming | Get upcoming payments |
| *PortalSettingsApi* | [**continuePortal**](#continueportal) | **PUT** /api/2.0/portal/continue | Restore a portal |
| *PortalSettingsApi* | [**deletePortal**](#deleteportal) | **DELETE** /api/2.0/portal/delete | Delete a portal |
| *PortalSettingsApi* | [**getPortalInformation**](#getportalinformation) | **GET** /api/2.0/portal | Get a portal |
| *PortalSettingsApi* | [**getPortalPath**](#getportalpath) | **GET** /api/2.0/portal/path | Get a path to the portal |
| *PortalSettingsApi* | [**sendDeleteInstructions**](#senddeleteinstructions) | **POST** /api/2.0/portal/delete | Send removal instructions |
| *PortalSettingsApi* | [**sendSuspendInstructions**](#sendsuspendinstructions) | **POST** /api/2.0/portal/suspend | Send suspension instructions |
| *PortalSettingsApi* | [**suspendPortal**](#suspendportal) | **PUT** /api/2.0/portal/suspend | Deactivate a portal |
| *PortalUsersApi* | [**createInvitationLink**](#createinvitationlink) | **POST** /api/2.0/portal/users/invitationlink | Create an invitation link |
| *PortalUsersApi* | [**deleteInvitationLink**](#deleteinvitationlink) | **DELETE** /api/2.0/portal/users/invitationlink | Deletes an invitation link. |
| *PortalUsersApi* | [**getInvitationLink**](#getinvitationlink) | **GET** /api/2.0/portal/users/invite/{employeeType} | Get an invitation link |
| *PortalUsersApi* | [**getInvitationLinkByEmployeeType**](#getinvitationlinkbyemployeetype) | **GET** /api/2.0/portal/users/invitationlink/{employeeType} | Get an invitation link |
| *PortalUsersApi* | [**getPortalUsersCount**](#getportaluserscount) | **GET** /api/2.0/portal/userscount | Get a number of portal users |
| *PortalUsersApi* | [**getUserById**](#getuserbyid) | **GET** /api/2.0/portal/users/{userID} | Get a user by ID |
| *PortalUsersApi* | [**markGiftMessageAsRead**](#markgiftmessageasread) | **POST** /api/2.0/portal/present/mark | Mark a gift message as read |
| *PortalUsersApi* | [**sendCongratulations**](#sendcongratulations) | **POST** /api/2.0/portal/sendcongratulations | Send congratulations |
| *PortalUsersApi* | [**updateInvitationLink**](#updateinvitationlink) | **PUT** /api/2.0/portal/users/invitationlink | Update an invitation link |
| *SecurityAccessToDevToolsApi* | [**setTenantDevToolsAccessSettings**](#settenantdevtoolsaccesssettings) | **POST** /api/2.0/settings/devtoolsaccess | Set the Developer Tools access settings |
| *SecurityActiveConnectionsApi* | [**getAllActiveConnections**](#getallactiveconnections) | **GET** /api/2.0/security/activeconnections | Get active connections |
| *SecurityActiveConnectionsApi* | [**logOutActiveConnection**](#logoutactiveconnection) | **PUT** /api/2.0/security/activeconnections/logout/{loginEventId} | Log out from the connection |
| *SecurityActiveConnectionsApi* | [**logOutAllActiveConnectionsChangePassword**](#logoutallactiveconnectionschangepassword) | **PUT** /api/2.0/security/activeconnections/logoutallchangepassword | Log out and change password |
| *SecurityActiveConnectionsApi* | [**logOutAllActiveConnectionsForUser**](#logoutallactiveconnectionsforuser) | **PUT** /api/2.0/security/activeconnections/logoutall/{userId} | Log out for the user by ID |
| *SecurityActiveConnectionsApi* | [**logOutAllExceptThisConnection**](#logoutallexceptthisconnection) | **PUT** /api/2.0/security/activeconnections/logoutallexceptthis | Log out from all connections except the current one |
| *SecurityAuditTrailDataApi* | [**createAuditTrailReport**](#createaudittrailreport) | **POST** /api/2.0/security/audit/events/report | Start the audit trail report generation |
| *SecurityAuditTrailDataApi* | [**getAuditEventsByFilter**](#getauditeventsbyfilter) | **GET** /api/2.0/security/audit/events/filter | Get filtered audit trail data |
| *SecurityAuditTrailDataApi* | [**getAuditSettings**](#getauditsettings) | **GET** /api/2.0/security/audit/settings/lifetime | Get the audit trail settings |
| *SecurityAuditTrailDataApi* | [**getAuditTrailMappers**](#getaudittrailmappers) | **GET** /api/2.0/security/audit/mappers | Get audit trail mappers |
| *SecurityAuditTrailDataApi* | [**getAuditTrailReport**](#getaudittrailreport) | **GET** /api/2.0/security/audit/events/report | Get the audit trail report generation status |
| *SecurityAuditTrailDataApi* | [**getAuditTrailTypes**](#getaudittrailtypes) | **GET** /api/2.0/security/audit/types | Get audit trail types |
| *SecurityAuditTrailDataApi* | [**getLastAuditEvents**](#getlastauditevents) | **GET** /api/2.0/security/audit/events/last | Get audit trail data |
| *SecurityAuditTrailDataApi* | [**setAuditSettings**](#setauditsettings) | **POST** /api/2.0/security/audit/settings/lifetime | Set the audit trail settings |
| *SecurityAuditTrailDataApi* | [**terminateAuditTrailReport**](#terminateaudittrailreport) | **DELETE** /api/2.0/security/audit/events/report | Terminate the audit trail report generation |
| *SecurityBannersVisibilityApi* | [**setTenantBannerSettings**](#settenantbannersettings) | **POST** /api/2.0/settings/banner | Set the banners visibility |
| *SecurityCSPApi* | [**configureCsp**](#configurecsp) | **POST** /api/2.0/security/csp | Configure CSP settings |
| *SecurityCSPApi* | [**getCspSettings**](#getcspsettings) | **GET** /api/2.0/security/csp | Get CSP settings |
| *SecurityFirebaseApi* | [**docRegisterPusnNotificationDevice**](#docregisterpusnnotificationdevice) | **POST** /api/2.0/settings/push/docregisterdevice | Save the Documents Firebase device token |
| *SecurityFirebaseApi* | [**subscribeDocumentsPushNotification**](#subscribedocumentspushnotification) | **PUT** /api/2.0/settings/push/docsubscribe | Subscribe to Documents push notification |
| *SecurityLoginHistoryApi* | [**createLoginHistoryReport**](#createloginhistoryreport) | **POST** /api/2.0/security/audit/login/report | Start the login history report generation |
| *SecurityLoginHistoryApi* | [**getLastLoginEvents**](#getlastloginevents) | **GET** /api/2.0/security/audit/login/last | Get login history |
| *SecurityLoginHistoryApi* | [**getLoginEventsByFilter**](#getlogineventsbyfilter) | **GET** /api/2.0/security/audit/login/filter | Get filtered login events |
| *SecurityLoginHistoryApi* | [**getLoginHistoryReport**](#getloginhistoryreport) | **GET** /api/2.0/security/audit/login/report | Get the login history report generation status |
| *SecurityLoginHistoryApi* | [**terminateLoginHistoryReport**](#terminateloginhistoryreport) | **DELETE** /api/2.0/security/audit/login/report | Terminate the login history report generation |
| *SecurityOAuth2Api* | [**generateJwtToken**](#generatejwttoken) | **GET** /api/2.0/security/oauth2/token | Generate JWT token |
| *SecuritySMTPSettingsApi* | [**getSmtpOperationStatus**](#getsmtpoperationstatus) | **GET** /api/2.0/smtpsettings/smtp/test/status | Get the SMTP testing process status |
| *SecuritySMTPSettingsApi* | [**getSmtpSettings**](#getsmtpsettings) | **GET** /api/2.0/smtpsettings/smtp | Get the SMTP settings |
| *SecuritySMTPSettingsApi* | [**resetSmtpSettings**](#resetsmtpsettings) | **DELETE** /api/2.0/smtpsettings/smtp | Reset the SMTP settings |
| *SecuritySMTPSettingsApi* | [**saveSmtpSettings**](#savesmtpsettings) | **POST** /api/2.0/smtpsettings/smtp | Save the SMTP settings |
| *SecuritySMTPSettingsApi* | [**testSmtpSettings**](#testsmtpsettings) | **GET** /api/2.0/smtpsettings/smtp/test | Test the SMTP settings |
| *SettingsAccessToDevToolsApi* | [**getTenantAccessDevToolsSettings**](#gettenantaccessdevtoolssettings) | **GET** /api/2.0/settings/devtoolsaccess | Get the Developer Tools access settings |
| *SettingsAuthorizationApi* | [**getAuthServices**](#getauthservices) | **GET** /api/2.0/settings/authservice | Get the authorization services |
| *SettingsAuthorizationApi* | [**saveAuthKeys**](#saveauthkeys) | **POST** /api/2.0/settings/authservice | Save the authorization keys |
| *SettingsAuthorizationApi* | [**testExternalDatabaseConnection**](#testexternaldatabaseconnection) | **POST** /api/2.0/settings/authservice/externaldb/test | Test external database connection |
| *SettingsBannersVisibilityApi* | [**getTenantBannerSettings**](#gettenantbannersettings) | **GET** /api/2.0/settings/banner | Get the banners visibility |
| *SettingsCommonSettingsApi* | [**closeAdminHelper**](#closeadminhelper) | **PUT** /api/2.0/settings/closeadminhelper | Close the admin helper |
| *SettingsCommonSettingsApi* | [**completeWizard**](#completewizard) | **PUT** /api/2.0/settings/wizard/complete | Complete the Wizard settings |
| *SettingsCommonSettingsApi* | [**configureDeepLink**](#configuredeeplink) | **POST** /api/2.0/settings/deeplink | Configure the deep link settings |
| *SettingsCommonSettingsApi* | [**deletePortalColorTheme**](#deleteportalcolortheme) | **DELETE** /api/2.0/settings/colortheme | Delete a color theme |
| *SettingsCommonSettingsApi* | [**getDeepLinkSettings**](#getdeeplinksettings) | **GET** /api/2.0/settings/deeplink | Get the deep link settings |
| *SettingsCommonSettingsApi* | [**getPaymentSettings**](#getpaymentsettings) | **GET** /api/2.0/settings/payment | Get the payment settings |
| *SettingsCommonSettingsApi* | [**getPortalColorTheme**](#getportalcolortheme) | **GET** /api/2.0/settings/colortheme | Get a color theme |
| *SettingsCommonSettingsApi* | [**getPortalHostname**](#getportalhostname) | **GET** /api/2.0/settings/machine | Get hostname |
| *SettingsCommonSettingsApi* | [**getPortalLogo**](#getportallogo) | **GET** /api/2.0/settings/logo | Get a portal logo |
| *SettingsCommonSettingsApi* | [**getPortalSettings**](#getportalsettings) | **GET** /api/2.0/settings | Get the portal settings |
| *SettingsCommonSettingsApi* | [**getSocketSettings**](#getsocketsettings) | **GET** /api/2.0/settings/socket | Get the socket settings |
| *SettingsCommonSettingsApi* | [**getSupportedCultures**](#getsupportedcultures) | **GET** /api/2.0/settings/cultures | Get supported languages |
| *SettingsCommonSettingsApi* | [**getTenantAiAccessSettings**](#gettenantaiaccesssettings) | **GET** /api/2.0/settings/ai-access | Get the AI access settings for the portal |
| *SettingsCommonSettingsApi* | [**getTenantUserInvitationSettings**](#gettenantuserinvitationsettings) | **GET** /api/2.0/settings/invitationsettings | Get the user invitation settings |
| *SettingsCommonSettingsApi* | [**getTimeZones**](#gettimezones) | **GET** /api/2.0/settings/timezones | Get time zones |
| *SettingsCommonSettingsApi* | [**saveDefaultFolder**](#savedefaultfolder) | **PUT** /api/2.0/settings/defaultfolder | Set the default folder |
| *SettingsCommonSettingsApi* | [**saveDnsSettings**](#savednssettings) | **PUT** /api/2.0/settings/dns | Save the DNS settings |
| *SettingsCommonSettingsApi* | [**saveMailDomainSettings**](#savemaildomainsettings) | **POST** /api/2.0/settings/maildomainsettings | Save the mail domain settings |
| *SettingsCommonSettingsApi* | [**savePortalColorTheme**](#saveportalcolortheme) | **PUT** /api/2.0/settings/colortheme | Save a color theme |
| *SettingsCommonSettingsApi* | [**setTenantAiAccessSettings**](#settenantaiaccesssettings) | **POST** /api/2.0/settings/ai-access | Set the AI access for the portal |
| *SettingsCommonSettingsApi* | [**updateEmailActivationSettings**](#updateemailactivationsettings) | **PUT** /api/2.0/settings/emailactivation | Update the email activation settings |
| *SettingsCommonSettingsApi* | [**updateInvitationSettings**](#updateinvitationsettings) | **PUT** /api/2.0/settings/invitationsettings | Update user invitation settings |
| *SettingsCookiesApi* | [**getCookieSettings**](#getcookiesettings) | **GET** /api/2.0/settings/cookiesettings | Get cookies lifetime |
| *SettingsCookiesApi* | [**updateCookieSettings**](#updatecookiesettings) | **PUT** /api/2.0/settings/cookiesettings | Update cookies lifetime |
| *SettingsDocsCloudApi* | [**calculateDevPack**](#calculatedevpack) | **POST** /api/2.0/settings/docscloud/calculatedevpack | Calculate the DocsCloud subscription switch cost |
| *SettingsDocsCloudApi* | [**checkHealth**](#checkhealth) | **GET** /api/2.0/settings/docscloud/healthcheck | Check the DocsCloud server health |
| *SettingsDocsCloudApi* | [**createTenantQuotaReport**](#createtenantquotareport) | **POST** /api/2.0/settings/docscloud/tenant/quota/report | Start the DocsCloud tenant quota report generation |
| *SettingsDocsCloudApi* | [**getTenant**](#gettenant) | **GET** /api/2.0/settings/docscloud/tenant | Get the DocsCloud tenant |
| *SettingsDocsCloudApi* | [**getTenantConfig**](#gettenantconfig) | **GET** /api/2.0/settings/docscloud/tenant/config | Get the DocsCloud tenant configuration |
| *SettingsDocsCloudApi* | [**getTenantInfo**](#gettenantinfo) | **GET** /api/2.0/settings/docscloud/tenant/info | Get the DocsCloud tenant information |
| *SettingsDocsCloudApi* | [**getTenantQuota**](#gettenantquota) | **GET** /api/2.0/settings/docscloud/tenant/quota | Get the DocsCloud tenant quota |
| *SettingsDocsCloudApi* | [**getTenantQuotaReport**](#gettenantquotareport) | **GET** /api/2.0/settings/docscloud/tenant/quota/report | Get the status of the DocsCloud tenant quota report generation |
| *SettingsDocsCloudApi* | [**getTenantUsage**](#gettenantusage) | **GET** /api/2.0/settings/docscloud/tenant/usage | Get the DocsCloud tenant usage |
| *SettingsDocsCloudApi* | [**startDocsCloudTrial**](#startdocscloudtrial) | **POST** /api/2.0/settings/docscloud/trial | Start the DocsCloud trial |
| *SettingsDocsCloudApi* | [**switchToDevPack**](#switchtodevpack) | **POST** /api/2.0/settings/docscloud/switchtodevpack | Switch the DocsCloud subscription to DocsCloudDevPack |
| *SettingsDocsCloudApi* | [**terminateTenantQuotaReport**](#terminatetenantquotareport) | **DELETE** /api/2.0/settings/docscloud/tenant/quota/report | Terminate the DocsCloud tenant quota report generation |
| *SettingsDocsCloudApi* | [**updateTenantConfig**](#updatetenantconfig) | **PUT** /api/2.0/settings/docscloud/tenant/config | Update the DocsCloud tenant configuration |
| *SettingsEncryptionApi* | [**getStorageEncryptionProgress**](#getstorageencryptionprogress) | **GET** /api/2.0/settings/encryption/progress | Get the storage encryption progress |
| *SettingsEncryptionApi* | [**getStorageEncryptionSettings**](#getstorageencryptionsettings) | **GET** /api/2.0/settings/encryption/settings | Get the storage encryption settings |
| *SettingsEncryptionApi* | [**startStorageEncryption**](#startstorageencryption) | **POST** /api/2.0/settings/encryption/start | Start the storage encryption process |
| *SettingsGreetingSettingsApi* | [**getGreetingSettings**](#getgreetingsettings) | **GET** /api/2.0/settings/greetingsettings | Get greeting settings |
| *SettingsGreetingSettingsApi* | [**getIsDefaultGreetingSettings**](#getisdefaultgreetingsettings) | **GET** /api/2.0/settings/greetingsettings/isdefault | Check the default greeting settings |
| *SettingsGreetingSettingsApi* | [**restoreGreetingSettings**](#restoregreetingsettings) | **POST** /api/2.0/settings/greetingsettings/restore | Restore the greeting settings |
| *SettingsGreetingSettingsApi* | [**saveGreetingSettings**](#savegreetingsettings) | **POST** /api/2.0/settings/greetingsettings | Save the greeting settings |
| *SettingsIPRestrictionsApi* | [**getIpRestrictions**](#getiprestrictions) | **GET** /api/2.0/settings/iprestrictions | Get the IP portal restrictions |
| *SettingsIPRestrictionsApi* | [**readIpRestrictionsSettings**](#readiprestrictionssettings) | **GET** /api/2.0/settings/iprestrictions/settings | Get the IP restriction settings |
| *SettingsIPRestrictionsApi* | [**saveIpRestrictions**](#saveiprestrictions) | **PUT** /api/2.0/settings/iprestrictions | Update the IP restrictions |
| *SettingsIPRestrictionsApi* | [**updateIpRestrictionsSettings**](#updateiprestrictionssettings) | **PUT** /api/2.0/settings/iprestrictions/settings | Update the IP restriction settings |
| *SettingsLicenseApi* | [**acceptLicense**](#acceptlicense) | **POST** /api/2.0/settings/license/accept | Activate a license |
| *SettingsLicenseApi* | [**getIsLicenseRequired**](#getislicenserequired) | **GET** /api/2.0/settings/license/required | Request a license |
| *SettingsLicenseApi* | [**refreshLicense**](#refreshlicense) | **GET** /api/2.0/settings/license/refresh | Refresh the license |
| *SettingsLicenseApi* | [**uploadLicense**](#uploadlicense) | **POST** /api/2.0/settings/license | Upload a license |
| *SettingsLoginSettingsApi* | [**getLoginSettings**](#getloginsettings) | **GET** /api/2.0/settings/security/loginsettings | Get the login settings |
| *SettingsLoginSettingsApi* | [**setDefaultLoginSettings**](#setdefaultloginsettings) | **DELETE** /api/2.0/settings/security/loginsettings | Reset the login settings |
| *SettingsLoginSettingsApi* | [**updateLoginSettings**](#updateloginsettings) | **PUT** /api/2.0/settings/security/loginsettings | Update the login settings |
| *SettingsMessagesApi* | [**enableAdminMessageSettings**](#enableadminmessagesettings) | **POST** /api/2.0/settings/messagesettings | Enable the administrator message settings |
| *SettingsMessagesApi* | [**sendAdminMail**](#sendadminmail) | **POST** /api/2.0/settings/sendadmmail | Send a message to the administrator |
| *SettingsMessagesApi* | [**sendJoinInviteMail**](#sendjoininvitemail) | **POST** /api/2.0/settings/sendjoininvite | Sends an invitation email |
| *SettingsNotificationsApi* | [**getNotificationChannels**](#getnotificationchannels) | **GET** /api/2.0/settings/notification/channels | Get notification channels |
| *SettingsNotificationsApi* | [**getNotificationSettings**](#getnotificationsettings) | **GET** /api/2.0/settings/notification/{type} | Check notification availability |
| *SettingsNotificationsApi* | [**getRoomsNotificationSettings**](#getroomsnotificationsettings) | **GET** /api/2.0/settings/notification/rooms | Get room notification settings |
| *SettingsNotificationsApi* | [**setNotificationSettings**](#setnotificationsettings) | **POST** /api/2.0/settings/notification | Enable notifications |
| *SettingsNotificationsApi* | [**setRoomsNotificationStatus**](#setroomsnotificationstatus) | **POST** /api/2.0/settings/notification/rooms | Set room notification status |
| *SettingsOwnerApi* | [**sendOwnerChangeInstructions**](#sendownerchangeinstructions) | **POST** /api/2.0/settings/owner | Send the owner change instructions |
| *SettingsOwnerApi* | [**updatePortalOwner**](#updateportalowner) | **PUT** /api/2.0/settings/owner | Update the portal owner |
| *SettingsQuotaApi* | [**getUserQuotaSettings**](#getuserquotasettings) | **GET** /api/2.0/settings/userquotasettings | Get the user quota settings |
| *SettingsQuotaApi* | [**saveAiAgentQuotaSettings**](#saveaiagentquotasettings) | **POST** /api/2.0/settings/aiagentquotasettings | Save the AI Agent quota settings |
| *SettingsQuotaApi* | [**saveRoomQuotaSettings**](#saveroomquotasettings) | **POST** /api/2.0/settings/roomquotasettings | Save the room quota settings |
| *SettingsQuotaApi* | [**setTenantQuotaSettings**](#settenantquotasettings) | **PUT** /api/2.0/settings/tenantquotasettings | Save the tenant quota settings |
| *SettingsRebrandingApi* | [**deleteAdditionalWhiteLabelSettings**](#deleteadditionalwhitelabelsettings) | **DELETE** /api/2.0/settings/rebranding/additional | Delete the additional white label settings |
| *SettingsRebrandingApi* | [**deleteCompanyWhiteLabelSettings**](#deletecompanywhitelabelsettings) | **DELETE** /api/2.0/settings/rebranding/company | Delete the company white label settings |
| *SettingsRebrandingApi* | [**getAdditionalWhiteLabelSettings**](#getadditionalwhitelabelsettings) | **GET** /api/2.0/settings/rebranding/additional | Get the additional white label settings |
| *SettingsRebrandingApi* | [**getCompanyWhiteLabelSettings**](#getcompanywhitelabelsettings) | **GET** /api/2.0/settings/rebranding/company | Get the company white label settings |
| *SettingsRebrandingApi* | [**getEnableWhitelabel**](#getenablewhitelabel) | **GET** /api/2.0/settings/enablewhitelabel | Check the white label availability |
| *SettingsRebrandingApi* | [**getIsDefaultWhiteLabelLogoText**](#getisdefaultwhitelabellogotext) | **GET** /api/2.0/settings/whitelabel/logotext/isdefault | Check the default white label logo text |
| *SettingsRebrandingApi* | [**getIsDefaultWhiteLabelLogos**](#getisdefaultwhitelabellogos) | **GET** /api/2.0/settings/whitelabel/logos/isdefault | Check the default white label logos |
| *SettingsRebrandingApi* | [**getLicensorData**](#getlicensordata) | **GET** /api/2.0/settings/companywhitelabel | Get the licensor data |
| *SettingsRebrandingApi* | [**getWhiteLabelLogoText**](#getwhitelabellogotext) | **GET** /api/2.0/settings/whitelabel/logotext | Get the white label logo text |
| *SettingsRebrandingApi* | [**getWhiteLabelLogos**](#getwhitelabellogos) | **GET** /api/2.0/settings/whitelabel/logos | Get the white label logos |
| *SettingsRebrandingApi* | [**restoreWhiteLabelLogoText**](#restorewhitelabellogotext) | **PUT** /api/2.0/settings/whitelabel/logotext/restore | Restore the white label logo text |
| *SettingsRebrandingApi* | [**restoreWhiteLabelLogos**](#restorewhitelabellogos) | **PUT** /api/2.0/settings/whitelabel/logos/restore | Restore the white label logos |
| *SettingsRebrandingApi* | [**saveAdditionalWhiteLabelSettings**](#saveadditionalwhitelabelsettings) | **POST** /api/2.0/settings/rebranding/additional | Save the additional white label settings |
| *SettingsRebrandingApi* | [**saveCompanyWhiteLabelSettings**](#savecompanywhitelabelsettings) | **POST** /api/2.0/settings/rebranding/company | Save the company white label settings |
| *SettingsRebrandingApi* | [**saveWhiteLabelLogoText**](#savewhitelabellogotext) | **POST** /api/2.0/settings/whitelabel/logotext/save | Save the white label logo text settings |
| *SettingsRebrandingApi* | [**saveWhiteLabelSettings**](#savewhitelabelsettings) | **POST** /api/2.0/settings/whitelabel/logos/save | Save the white label logos |
| *SettingsRebrandingApi* | [**saveWhiteLabelSettingsFromFiles**](#savewhitelabelsettingsfromfiles) | **POST** /api/2.0/settings/whitelabel/logos/savefromfiles | Save the white label logos from files |
| *SettingsSSOApi* | [**getDefaultSsoSettingsV2**](#getdefaultssosettingsv2) | **GET** /api/2.0/settings/ssov2/default | Get the default SSO settings |
| *SettingsSSOApi* | [**getSsoSettingsV2**](#getssosettingsv2) | **GET** /api/2.0/settings/ssov2 | Get the SSO settings |
| *SettingsSSOApi* | [**getSsoSettingsV2Constants**](#getssosettingsv2constants) | **GET** /api/2.0/settings/ssov2/constants | Get the SSO settings constants |
| *SettingsSSOApi* | [**resetSsoSettingsV2**](#resetssosettingsv2) | **DELETE** /api/2.0/settings/ssov2 | Reset the SSO settings |
| *SettingsSSOApi* | [**saveSsoSettingsV2**](#savessosettingsv2) | **POST** /api/2.0/settings/ssov2 | Save the SSO settings |
| *SettingsSecurityApi* | [**getEnabledModules**](#getenabledmodules) | **GET** /api/2.0/settings/security/modules | Get the enabled modules |
| *SettingsSecurityApi* | [**getIsProductAdministrator**](#getisproductadministrator) | **GET** /api/2.0/settings/security/administrator | Check a product administrator |
| *SettingsSecurityApi* | [**getPasswordSettings**](#getpasswordsettings) | **GET** /api/2.0/settings/security/password | Get the password settings |
| *SettingsSecurityApi* | [**getProductAdministrators**](#getproductadministrators) | **GET** /api/2.0/settings/security/administrator/{productid} | Get the product administrators |
| *SettingsSecurityApi* | [**getWebItemSecurityInfo**](#getwebitemsecurityinfo) | **GET** /api/2.0/settings/security/{id} | Get the module availability |
| *SettingsSecurityApi* | [**getWebItemSettingsSecurityInfo**](#getwebitemsettingssecurityinfo) | **GET** /api/2.0/settings/security | Get the security settings |
| *SettingsSecurityApi* | [**setAccessToWebItems**](#setaccesstowebitems) | **PUT** /api/2.0/settings/security/access | Set the security settings to modules |
| *SettingsSecurityApi* | [**setProductAdministrator**](#setproductadministrator) | **PUT** /api/2.0/settings/security/administrator | Set a product administrator |
| *SettingsSecurityApi* | [**setWebItemSecurity**](#setwebitemsecurity) | **PUT** /api/2.0/settings/security | Set the module security settings |
| *SettingsSecurityApi* | [**updatePasswordSettings**](#updatepasswordsettings) | **PUT** /api/2.0/settings/security/password | Set the password settings |
| *SettingsStatisticsApi* | [**getSpaceUsageStatistics**](#getspaceusagestatistics) | **GET** /api/2.0/settings/statistics/spaceusage/{id} | Get the space usage statistics |
| *SettingsStorageApi* | [**getAllBackupStorages**](#getallbackupstorages) | **GET** /api/2.0/settings/storage/backup | Get the backup storages |
| *SettingsStorageApi* | [**getAllCdnStorages**](#getallcdnstorages) | **GET** /api/2.0/settings/storage/cdn | Get the CDN storages |
| *SettingsStorageApi* | [**getAllStorages**](#getallstorages) | **GET** /api/2.0/settings/storage | Get storages |
| *SettingsStorageApi* | [**getAmazonS3Regions**](#getamazons3regions) | **GET** /api/2.0/settings/storage/s3/regions | Get Amazon regions |
| *SettingsStorageApi* | [**getStorageProgress**](#getstorageprogress) | **GET** /api/2.0/settings/storage/progress | Get the storage progress |
| *SettingsStorageApi* | [**resetCdnToDefault**](#resetcdntodefault) | **DELETE** /api/2.0/settings/storage/cdn | Reset the CDN storage settings |
| *SettingsStorageApi* | [**resetStorageToDefault**](#resetstoragetodefault) | **DELETE** /api/2.0/settings/storage | Reset the storage settings |
| *SettingsStorageApi* | [**updateCdnStorage**](#updatecdnstorage) | **PUT** /api/2.0/settings/storage/cdn | Update the CDN storage |
| *SettingsStorageApi* | [**updateStorage**](#updatestorage) | **PUT** /api/2.0/settings/storage | Update a storage |
| *SettingsTFASettingsApi* | [**getTfaAppCodes**](#gettfaappcodes) | **GET** /api/2.0/settings/tfaappcodes | Get the TFA codes |
| *SettingsTFASettingsApi* | [**getTfaConfirmData**](#gettfaconfirmdata) | **GET** /api/2.0/settings/tfaapp/confirm | Get TFA confirmation data |
| *SettingsTFASettingsApi* | [**getTfaSettings**](#gettfasettings) | **GET** /api/2.0/settings/tfaapp | Get the TFA settings |
| *SettingsTFASettingsApi* | [**tfaAppGenerateSetupCode**](#tfaappgeneratesetupcode) | **GET** /api/2.0/settings/tfaapp/setup | Generate setup code |
| *SettingsTFASettingsApi* | [**tfaValidateAuthCode**](#tfavalidateauthcode) | **POST** /api/2.0/settings/tfaapp/validate | Validate the TFA code |
| *SettingsTFASettingsApi* | [**unlinkTfaApp**](#unlinktfaapp) | **PUT** /api/2.0/settings/tfaappnewapp | Unlink the TFA application |
| *SettingsTFASettingsApi* | [**updateTfaAppCodes**](#updatetfaappcodes) | **PUT** /api/2.0/settings/tfaappnewcodes | Update the TFA codes |
| *SettingsTFASettingsApi* | [**updateTfaSettings**](#updatetfasettings) | **PUT** /api/2.0/settings/tfaapp | Update the TFA settings |
| *SettingsTFASettingsApi* | [**updateTfaSettingsLink**](#updatetfasettingslink) | **PUT** /api/2.0/settings/tfaappwithlink | Updates TFA settings |
| *SettingsTelegramApi* | [**checkTelegram**](#checktelegram) | **GET** /api/2.0/settings/telegram/check | Check the Telegram connection |
| *SettingsTelegramApi* | [**linkTelegram**](#linktelegram) | **GET** /api/2.0/settings/telegram/link | Get the Telegram link |
| *SettingsTelegramApi* | [**unlinkTelegram**](#unlinktelegram) | **DELETE** /api/2.0/settings/telegram/link | Unlink Telegram |
| *SettingsWebhooksApi* | [**createWebhook**](#createwebhook) | **POST** /api/2.0/settings/webhook | Create a webhook |
| *SettingsWebhooksApi* | [**enableWebhook**](#enablewebhook) | **PUT** /api/2.0/settings/webhook/enable | Enable a webhook |
| *SettingsWebhooksApi* | [**getTenantWebhooks**](#gettenantwebhooks) | **GET** /api/2.0/settings/webhook | Get webhooks |
| *SettingsWebhooksApi* | [**getWebhookTriggers**](#getwebhooktriggers) | **GET** /api/2.0/settings/webhook/triggers | Get webhook triggers |
| *SettingsWebhooksApi* | [**getWebhooksLogs**](#getwebhookslogs) | **GET** /api/2.0/settings/webhooks/log | Get webhook logs |
| *SettingsWebhooksApi* | [**removeWebhook**](#removewebhook) | **DELETE** /api/2.0/settings/webhook/{id} | Remove a webhook |
| *SettingsWebhooksApi* | [**retryWebhook**](#retrywebhook) | **PUT** /api/2.0/settings/webhook/{id}/retry | Retry a webhook |
| *SettingsWebhooksApi* | [**retryWebhooks**](#retrywebhooks) | **PUT** /api/2.0/settings/webhook/retry | Retry webhooks |
| *SettingsWebhooksApi* | [**updateWebhook**](#updatewebhook) | **PUT** /api/2.0/settings/webhook | Update a webhook |
| *SettingsWebpluginsApi* | [**addWebPluginFromFile**](#addwebpluginfromfile) | **POST** /api/2.0/settings/webplugins | Add a web plugin |
| *SettingsWebpluginsApi* | [**deleteWebPlugin**](#deletewebplugin) | **DELETE** /api/2.0/settings/webplugins/{name} | Delete a web plugin |
| *SettingsWebpluginsApi* | [**getWebPlugin**](#getwebplugin) | **GET** /api/2.0/settings/webplugins/{name} | Get a web plugin by name |
| *SettingsWebpluginsApi* | [**getWebPlugins**](#getwebplugins) | **GET** /api/2.0/settings/webplugins | Get web plugins |
| *SettingsWebpluginsApi* | [**updateWebPlugin**](#updatewebplugin) | **PUT** /api/2.0/settings/webplugins/{name} | Update a web plugin |
| *ThirdPartyApi* | [**getThirdPartyCode**](#getthirdpartycode) | **GET** /api/2.0/thirdparty/{provider} | Get the code request |



## AppsApi

### get

> AppWrapper get(id)

`GET /api/2.0/apps/{id}`

Get a single app

Returns a single application by id with the per-tenant enabled state and settings JSON.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **String** | The application identifier. | [required] [example: "ai-room"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Application info | [**AppWrapper**](#model-appwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Application not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AppWrapper**](#model-appwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAll

> AppArrayWrapper getAll()

`GET /api/2.0/apps`

Get all apps

Returns the full list of portal applications declared in configuration, merged with per-tenant overrides  (enabled state and JSON settings).

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of applications | [**AppArrayWrapper**](#model-apparraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AppArrayWrapper**](#model-apparraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSettings

> ObjectWrapper getSettings(id)

`GET /api/2.0/apps/{id}/settings`

Get app settings

Returns the JSON settings document saved for the specified application, or null if no overrides exist.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **String** | The application identifier. | [required] [example: ai-room] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Application settings JSON | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Application not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### setEnabled

> AppWrapper setEnabled(id, SetAppEnabledBody)

`PUT /api/2.0/apps/{id}/enabled`

Enable or disable an app

Toggles the enabled state of the application for the current tenant. Requires portal administrator permissions.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **String** | The application identifier. | [required] [example: ai-room] |
| **SetAppEnabledBody** | body | [**SetAppEnabledBody**](#model-setappenabledbody) | New enabled state. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated application info | [**AppWrapper**](#model-appwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to manage apps | - | - |
| **404** | Application not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AppWrapper**](#model-appwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setSettings

> AppWrapper setSettings(id, SetAppSettingsBody)

`PUT /api/2.0/apps/{id}/settings`

Save app settings

Saves an arbitrary JSON settings document for the specified application for the current tenant.  Requires portal administrator permissions.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **String** | The application identifier. | [required] [example: ai-room] |
| **SetAppSettingsBody** | body | [**SetAppSettingsBody**](#model-setappsettingsbody) | New settings document. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated application info | [**AppWrapper**](#model-appwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Settings is not valid JSON | - | - |
| **403** | You don&#39;t have enough permission to manage apps | - | - |
| **404** | Application not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AppWrapper**](#model-appwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## AuthenticationApi

### authenticateMe

> AuthenticationTokenWrapper authenticateMe(AuthRequestsDto)

`POST /api/2.0/authentication`

Authenticate a user

Authenticates the current user by SMS, authenticator app, or without two-factor authentication.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AuthRequestsDto** | body | [**AuthRequestsDto**](#model-authrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Authentication data | [**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | userName, password or passworHash is empty | - | - |
| **401** | User authentication failed | - | - |
| **404** | The user could not be found | - | - |
| **429** | Too many login attempts. Please try again later | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### authenticateMeFromBodyWithCode

> AuthenticationTokenWrapper authenticateMeFromBodyWithCode(code, AuthWithCodeRequestsDto)

`POST /api/2.0/authentication/{code}`

Authenticate a user by code

Authenticates the current user by SMS or two-factor authentication code.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **code** | path | **String** |  | [required] |
| **AuthWithCodeRequestsDto** | body | [**AuthWithCodeRequestsDto**](#model-authwithcoderequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Authentication data | [**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | userName, password or passworHash is empty | - | - |
| **401** | User authentication failed | - | - |
| **403** | Auth code is not available | - | - |
| **429** | Too many login attempts. Please try again later | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### checkConfirm

> ConfirmWrapper checkConfirm(EmailValidationKeyModel)

`POST /api/2.0/authentication/confirm`

Open confirmation email URL

Opens a confirmation email URL to validate a certain action (employee invitation, portal removal, phone activation, etc.).

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **EmailValidationKeyModel** | body | [**EmailValidationKeyModel**](#model-emailvalidationkeymodel) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Validation result: Ok, Invalid, or Expired | [**ConfirmWrapper**](#model-confirmwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ConfirmWrapper**](#model-confirmwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getIsAuthentificated

> BooleanWrapper getIsAuthentificated()

`GET /api/2.0/authentication`

Check authentication

Checks if the current user is authenticated or not.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the current user is authenticated | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### logout

> StringWrapper logout()

`POST /api/2.0/authentication/logout`

Log out

Logs out of the current user account.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveMobilePhone

> AuthenticationTokenWrapper saveMobilePhone(MobileRequestsDto)

`POST /api/2.0/authentication/setphone`

Set a mobile phone

Sets a mobile phone for the current user.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **MobileRequestsDto** | body | [**MobileRequestsDto**](#model-mobilerequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Authentication data | [**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendSmsCode

> AuthenticationTokenWrapper sendSmsCode(AuthRequestsDto)

`POST /api/2.0/authentication/sendsms`

Send SMS code

Sends SMS with an authentication code.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AuthRequestsDto** | body | [**AuthRequestsDto**](#model-authrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Authentication data | [**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | userName, password or passworHash is empty | - | - |
| **429** | Too many login attempts. Please try again later | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuthenticationTokenWrapper**](#model-authenticationtokenwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## CapabilitiesApi

### getPortalCapabilities

> CapabilitiesWrapper getPortalCapabilities()

`GET /api/2.0/capabilities`

Get portal capabilities

Returns the information about portal capabilities.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Portal capabilities | [**CapabilitiesWrapper**](#model-capabilitieswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CapabilitiesWrapper**](#model-capabilitieswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## MigrationApi

### cancelMigration

> cancelMigration()

`POST /api/2.0/migration/cancel`

Cancel migration

Cancels the migration.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### clearMigration

> clearMigration()

`POST /api/2.0/migration/clear`

Clear migration

Clears the migration.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### finishMigration

> finishMigration(FinishDto)

`POST /api/2.0/migration/finish`

Finish migration

Finishes the migration process.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **FinishDto** | body | [**FinishDto**](#model-finishdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### getMigrationLogs

> getMigrationLogs()

`GET /api/2.0/migration/logs`

Get migration logs

Returns the migration logs.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | No migration is in progress | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### getMigrationStatus

> MigrationStatusWrapper getMigrationStatus()

`GET /api/2.0/migration/status`

Get migration status

Returns the migration status.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**MigrationStatusWrapper**](#model-migrationstatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**MigrationStatusWrapper**](#model-migrationstatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### listMigrations

> STRINGArrayWrapper listMigrations()

`GET /api/2.0/migration/list`

Get migrations

Returns a list of available migrations.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**STRINGArrayWrapper**](#model-stringarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**STRINGArrayWrapper**](#model-stringarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startMigration

> startMigration(MigrationApiInfo)

`POST /api/2.0/migration/migrate`

Start migration

Starts the migration process.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **MigrationApiInfo** | body | [**MigrationApiInfo**](#model-migrationapiinfo) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### uploadAndInitializeMigration

> uploadAndInitializeMigration(migratorName)

`POST /api/2.0/migration/init/{migratorName}`

Upload and initialize migration

Uploads and initializes a migration with a migrator name specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **migratorName** | path | **String** | The migrator name extracted from the route parameters. | [required] [example: "GoogleWorkspace"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

## PortalPaymentApi

### calculateWalletPayment

> PaymentCalculationWrapper calculateWalletPayment(WalletQuantityRequestDto)

`PUT /api/2.0/portal/payment/calculatewallet`

Calculate the wallet payment amount

Calculates an amount of the wallet payment with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **WalletQuantityRequestDto** | body | [**WalletQuantityRequestDto**](#model-walletquantityrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Payment calculation | [**PaymentCalculationWrapper**](#model-paymentcalculationwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**PaymentCalculationWrapper**](#model-paymentcalculationwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeTenantWalletServiceState

> TenantWalletServiceSettingsWrapper changeTenantWalletServiceState(ChangeWalletServiceStateRequestDto)

`POST /api/2.0/portal/payment/servicestate`

Change tenant wallet service state

Changes the state of a wallet service for the current tenant.  Requires permission to edit portal settings and a configured tariff service.  Adds or removes the specified service from the enabled services list based on the enabled flag.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ChangeWalletServiceStateRequestDto** | body | [**ChangeWalletServiceStateRequestDto**](#model-changewalletservicestaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The updated tenant wallet service settings | [**TenantWalletServiceSettingsWrapper**](#model-tenantwalletservicesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantWalletServiceSettingsWrapper**](#model-tenantwalletservicesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createCustomerMonthlyUsageReport

> DocumentBuilderTaskWrapper createCustomerMonthlyUsageReport(CustomerMonthlyUsageReportRequestDto)

`POST /api/2.0/portal/payment/customer/usage/monthly/report`

Start the customer monthly usage report generation

Starts generating a customer monthly usage report as an xlsx file and saves it in Documents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CustomerMonthlyUsageReportRequestDto** | body | [**CustomerMonthlyUsageReportRequestDto**](#model-customermonthlyusagereportrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createCustomerOperationsReport

> DocumentBuilderTaskWrapper createCustomerOperationsReport(CustomerOperationsReportRequestDto)

`POST /api/2.0/portal/payment/customer/operationsreport`

Start the customer operations report generation

Starts generating a customer operations report as an xlsx file and saves it in Documents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CustomerOperationsReportRequestDto** | body | [**CustomerOperationsReportRequestDto**](#model-customeroperationsreportrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer or service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createCustomerServiceUsageReport

> DocumentBuilderTaskWrapper createCustomerServiceUsageReport(CustomerServiceUsageReportRequestDto)

`POST /api/2.0/portal/payment/customer/usage/report`

Start the customer service usage report generation

Starts generating a customer service usage report as an xlsx file and saves it in Documents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CustomerServiceUsageReportRequestDto** | body | [**CustomerServiceUsageReportRequestDto**](#model-customerserviceusagereportrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer or service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getActiveServices

> ActiveServiceArrayWrapper getActiveServices()

`GET /api/2.0/portal/payment/activeservices`

Get the active wallet services

Returns all the active wallet services (quotas) of the current portal: the active additional quotas  from the tariff, plus the services enabled manually via the wallet service settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The list of active wallet services | [**ActiveServiceArrayWrapper**](#model-activeservicearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ActiveServiceArrayWrapper**](#model-activeservicearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAiPrices

> AiPricesResponseWrapper getAiPrices()

`GET /api/2.0/portal/payment/ai-prices`

Get AI model prices

Retrieves the pricing information for AI models including chat, embedding, and web search services.  The prices are returned in the configured currency and normalized per million tokens.  Requires administrator permissions to access.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Prices for AI models | [**AiPricesResponseWrapper**](#model-aipricesresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AiPricesResponseWrapper**](#model-aipricesresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCheckoutSetupUrl

> StringWrapper getCheckoutSetupUrl(BackUrl, SuccessUrl)

`GET /api/2.0/portal/payment/checkoutsetupurl`

Get the checkout setup page URL

Returns the URL to the checkout setup page.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BackUrl** | query | **URI** (uri) | The URL where the user will be redirected after setup cancellation. | [required] [example: "https://example.com/payment/back"] [minLength: 0] [maxLength: 255] |
| **SuccessUrl** | query | **URI** (uri) | The URL where the user will be redirected after successful payment. | [required] [example: "https://example.com/payment/success"] [minLength: 0] [maxLength: 255] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The URL to the checkout setup page | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerBalance

> BalanceWrapper getCustomerBalance(refresh)

`GET /api/2.0/portal/payment/customer/balance`

Get the customer balance

Returns the customer balance from the accounting service.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** | Specifies whether to refresh the payment information cache or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The customer balance | [**BalanceWrapper**](#model-balancewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BalanceWrapper**](#model-balancewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerInfo

> CustomerInfoWrapper getCustomerInfo(refresh)

`GET /api/2.0/portal/payment/customerinfo`

Get the customer information

Returns the customer information.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** | Specifies whether to refresh the payment information cache or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The customer info | [**CustomerInfoWrapper**](#model-customerinfowrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CustomerInfoWrapper**](#model-customerinfowrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerMonthlyUsage

> CustomerMonthlyUsageArrayWrapper getCustomerMonthlyUsage(startDate, endDate)

`GET /api/2.0/portal/payment/customer/usage/monthly`

Get the customer monthly usage

Returns the customer spending aggregated per calendar month from the accounting service.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **startDate** | query | **Date** (date-time) | Start of the period (inclusive). | [optional] [example: "2025-01-01T00:00:00Z"] |
| **endDate** | query | **Date** (date-time) | End of the period (inclusive). | [optional] [example: "2025-12-31T23:59:59Z"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The customer monthly usage | [**CustomerMonthlyUsageArrayWrapper**](#model-customermonthlyusagearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CustomerMonthlyUsageArrayWrapper**](#model-customermonthlyusagearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerMonthlyUsageReport

> DocumentBuilderTaskWrapper getCustomerMonthlyUsageReport()

`GET /api/2.0/portal/payment/customer/usage/monthly/report`

Get the status of the customer monthly usage report generation

Returns the status of generating a customer monthly usage report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerOperations

> ReportWrapper getCustomerOperations(offset, limit, ServiceName, StartDate, EndDate, ParticipantName, Credit, Debit, Type, Status, OrderBy, OrderType)

`GET /api/2.0/portal/payment/customer/operations`

Get the customer operations

Returns the report of customer operations from the accounting service.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **offset** | query | **Integer** (int32) | The number of items to skip for pagination. The default value is 0. | [optional] [example: 0] |
| **limit** | query | **Integer** (int32) | The maximum number of items to return for pagination. The default value is 25. | [optional] [example: 25] |
| **ServiceName** | query | **List** | The service name list. A single string is also accepted for backward compatibility. | [optional] [example: "[backup]"] |
| **StartDate** | query | **Date** (date-time) | The report start date. | [optional] [example: "2024-01-01T00:00:00Z"] |
| **EndDate** | query | **Date** (date-time) | The report end date. | [optional] [example: "2024-01-31T23:59:59Z"] |
| **ParticipantName** | query | **String** | The participant name. | [optional] [example: "My Own Corporation"] |
| **Credit** | query | **Boolean** | Specifies whether to include credit operations in the report. | [optional] [example: true] |
| **Debit** | query | **Boolean** | Specifies whether to include debit operations in the report. | [optional] [example: false] |
| **Type** | query | **OperationType** | The operation type to filter by. | [optional] [example: Any] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19] |
| **Status** | query | **OperationStatus** | The operation status to filter by. | [optional] [example: Any] [enum: 0, 1, 2, 3] |
| **OrderBy** | query | **String** | The field to order by. | [optional] [example: "StartDate"] |
| **OrderType** | query | **OperationOrderType** | Order direction: Ascending or Descending. | [optional] [example: Descending] [enum: 0, 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The customer operations | [**ReportWrapper**](#model-reportwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ReportWrapper**](#model-reportwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerOperationsReport

> DocumentBuilderTaskWrapper getCustomerOperationsReport()

`GET /api/2.0/portal/payment/customer/operationsreport`

Get the status of the customer operations report generation

Returns the status of generating a customer operations report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerServiceUsage

> CustomerServiceUsageReportWrapper getCustomerServiceUsage(ServiceName, ParticipantName, Status, StartDate, EndDate, Metadata, offset, limit, OrderBy, OrderType)

`GET /api/2.0/portal/payment/customer/usage`

Get the customer service usage

Returns the customer usage statistics aggregated per service from the accounting service.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ServiceName** | query | **List** | The service name list. | [optional] [example: [backup]] |
| **ParticipantName** | query | **String** | The participant name. | [optional] [example: My Own Corporation] |
| **Status** | query | **OperationStatus** | The operation status to filter by. | [optional] [example: Completed] [enum: 0, 1, 2, 3] |
| **StartDate** | query | **Date** (date-time) | Start of the period (inclusive). | [optional] [example: 2025-01-01T00:00:00Z] |
| **EndDate** | query | **Date** (date-time) | End of the period (inclusive). | [optional] [example: 2025-12-31T23:59:59Z] |
| **Metadata** | query | **Map** | Metadata key-value pairs to filter by. | [optional] [example: {"key1":"value1","key2":"value2"}] |
| **offset** | query | **Integer** (int32) | The number of items to skip for pagination. The default value is 0. | [optional] [example: 0] |
| **limit** | query | **Integer** (int32) | The maximum number of items to return for pagination. The default value is 25. | [optional] [example: 25] |
| **OrderBy** | query | **String** | The field to order by. | [optional] [example: "ServiceName"] |
| **OrderType** | query | **OperationOrderType** | Order direction: Ascending or Descending. | [optional] [example: Descending] [enum: 0, 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The customer service usage | [**CustomerServiceUsageReportWrapper**](#model-customerserviceusagereportwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CustomerServiceUsageReportWrapper**](#model-customerserviceusagereportwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCustomerServiceUsageReport

> DocumentBuilderTaskWrapper getCustomerServiceUsageReport()

`GET /api/2.0/portal/payment/customer/usage/report`

Get the status of the customer service usage report generation

Returns the status of generating a customer service usage report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPaymentAccount

> StringWrapper getPaymentAccount(backUrl)

`GET /api/2.0/portal/payment/account`

Get the payment account

Returns the URL to the payment account.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **backUrl** | query | **URI** (uri) | The URL where the user will be redirected after payment processing. | [optional] [example: "https://example.com"] [minLength: 0] [maxLength: 255] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The URL to the payment account | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPaymentCurrencies

> CurrenciesArrayWrapper getPaymentCurrencies()

`GET /api/2.0/portal/payment/currencies`

Get currencies

Returns the available portal currencies.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of available portal currencies | [**CurrenciesArrayWrapper**](#model-currenciesarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CurrenciesArrayWrapper**](#model-currenciesarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPaymentQuotas

> QuotaArrayWrapper getPaymentQuotas(wallet, additional)

`GET /api/2.0/portal/payment/quotas`

Get quotas

Returns the available portal quotas.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **wallet** | query | **Boolean** | Specifies whether to return the wallet quotas only. | [optional] [example: true] |
| **additional** | query | **Boolean** | Specifies whether to return additional quotas only. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of available portal quotas | [**QuotaArrayWrapper**](#model-quotaarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**QuotaArrayWrapper**](#model-quotaarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPaymentUrl

> StringWrapper getPaymentUrl(PaymentUrlRequestDto)

`PUT /api/2.0/portal/payment/url`

Get the payment page URL

Returns the URL to the payment page.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **PaymentUrlRequestDto** | body | [**PaymentUrlRequestDto**](#model-paymenturlrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The URL to the payment page | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getPortalPrices

> getPortalPrices_200_response getPortalPrices()

`GET /api/2.0/portal/payment/prices`

Get prices

Returns the available portal prices.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of available portal prices | [**getPortalPrices_200_response**](#model-getportalprices-200-response) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**getPortalPrices_200_response**](#model-getportalprices-200-response)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getQuotaPaymentInformation

> QuotaWrapper getQuotaPaymentInformation(refresh)

`GET /api/2.0/portal/payment/quota`

Get quota payment information

Returns the payment information about the current portal quota.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** | Specifies whether to refresh the payment information cache or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Payment information about the current portal quota | [**QuotaWrapper**](#model-quotawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**QuotaWrapper**](#model-quotawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRestrictedAiModels

> RestrictedModelsResponseWrapper getRestrictedAiModels()

`GET /api/2.0/portal/payment/ai-model/restrictions`

Get restricted AI models

Returns the list of AI chat model IDs that are restricted (disabled) for the current tenant.  Restricted models cannot be used for AI chat conversations by any user within the portal.  Only DocSpace administrators can access this endpoint.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The list of restricted AI model IDs | [**RestrictedModelsResponseWrapper**](#model-restrictedmodelsresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RestrictedModelsResponseWrapper**](#model-restrictedmodelsresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSubscriptionBalanceInfo

> SubscriptionBalanceInfoWrapper getSubscriptionBalanceInfo()

`GET /api/2.0/portal/payment/subscription/balance`

Get the subscription balance information

Returns the information about the current subscription and its unused (prorated) balance.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The subscription balance information | [**SubscriptionBalanceInfoWrapper**](#model-subscriptionbalanceinfowrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **402** | Tariff is not paid | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer or subscription could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SubscriptionBalanceInfoWrapper**](#model-subscriptionbalanceinfowrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantWalletServiceSettings

> TenantWalletServiceSettingsWrapper getTenantWalletServiceSettings()

`GET /api/2.0/portal/payment/servicessettings`

Gets the wallet service settings for the tenant.

Retrieves configuration settings related to the wallet service associated with the current tenant.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The wallet service settings for the tenant | [**TenantWalletServiceSettingsWrapper**](#model-tenantwalletservicesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantWalletServiceSettingsWrapper**](#model-tenantwalletservicesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantWalletSettings

> TenantWalletSettingsWrapper getTenantWalletSettings()

`GET /api/2.0/portal/payment/topupsettings`

Gets the tenant wallet auto top up settings

Returns the wallet auto top up settings for the current tenant.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The wallet auto top up settings | [**TenantWalletSettingsWrapper**](#model-tenantwalletsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantWalletSettingsWrapper**](#model-tenantwalletsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWalletService

> WalletServiceWrapper getWalletService(service)

`GET /api/2.0/portal/payment/walletservice`

Get wallet service

Returns the specified wallet service.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **service** | query | **TenantWalletService** | The wallet service type. | [required] [example: Storage] [enum: -18, -16, -15, -14, -13, -12, -11] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Wallet service | [**WalletServiceWrapper**](#model-walletservicewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WalletServiceWrapper**](#model-walletservicewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWalletServices

> WalletServiceArrayWrapper getWalletServices()

`GET /api/2.0/portal/payment/walletservices`

Get wallet services

Returns the available wallet services.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of available wallet services | [**WalletServiceArrayWrapper**](#model-walletservicearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WalletServiceArrayWrapper**](#model-walletservicearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### moveSubscriptionToWallet

> BooleanWrapper moveSubscriptionToWallet(QuantityRequestDto)

`POST /api/2.0/portal/payment/subscription/movetowallet`

Move the subscription balance to the wallet and purchase admins

Cancels the current subscription, moves its unused balance to the wallet, and purchases the requested number of  admins from the wallet. If the wallet balance is not enough, it is topped up for the missing amount first  (with several attempts, as the balance may be consumed concurrently).

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **QuantityRequestDto** | body | [**QuantityRequestDto**](#model-quantityrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **402** | Tariff is not paid | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer or subscription could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendPaymentRequest

> sendPaymentRequest(SalesRequestsDto)

`POST /api/2.0/portal/payment/request`

Send a payment request

Sends a request for the portal payment.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SalesRequestsDto** | body | [**SalesRequestsDto**](#model-salesrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email or message text is empty | - | - |
| **403** | No permissions to perform this action | - | - |
| **429** | Request limit is exceeded | - | `Retry-After` |
| **401** | Unauthorized | - | - |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### setRestrictedAiModels

> RestrictedModelsResponseWrapper setRestrictedAiModels(SetRestrictedAiModelsRequestDto)

`PUT /api/2.0/portal/payment/ai-model/restrictions`

Set restricted AI models

Overwrites the entire set of restricted AI model IDs for the current tenant.  The request body must contain the complete desired set — to add a restriction, include the new model alongside existing ones;  to remove one, omit it. An empty set lifts all restrictions. Only portal administrators can perform this action.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SetRestrictedAiModelsRequestDto** | body | [**SetRestrictedAiModelsRequestDto**](#model-setrestrictedaimodelsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The updated list of restricted AI model IDs | [**RestrictedModelsResponseWrapper**](#model-restrictedmodelsresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RestrictedModelsResponseWrapper**](#model-restrictedmodelsresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setTenantWalletSettings

> TenantWalletSettingsWrapper setTenantWalletSettings(TenantWalletSettingsWrapper)

`POST /api/2.0/portal/payment/topupsettings`

Set the wallet auto top up settings

Updates the wallet auto top up settings for the current tenant.  Requires the tariff service to be configured and the user to be authorized as a payer.  Returns null if the tariff service is not configured or customer information/balance cannot be retrieved.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantWalletSettingsWrapper** | body | [**TenantWalletSettingsWrapper**](#model-tenantwalletsettingswrapper) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The wallet auto top up settings | [**TenantWalletSettingsWrapper**](#model-tenantwalletsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantWalletSettingsWrapper**](#model-tenantwalletsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateCustomerMonthlyUsageReport

> terminateCustomerMonthlyUsageReport()

`DELETE /api/2.0/portal/payment/customer/usage/monthly/report`

Terminate the customer monthly usage report generation

Terminates generating a customer monthly usage report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### terminateCustomerOperationsReport

> terminateCustomerOperationsReport()

`DELETE /api/2.0/portal/payment/customer/operationsreport`

Terminate the customer operations report generation

Terminates generating a customer operations report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### terminateCustomerServiceUsageReport

> terminateCustomerServiceUsageReport()

`DELETE /api/2.0/portal/payment/customer/usage/report`

Terminate the customer service usage report generation

Terminates generating a customer service usage report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### topUpDeposit

> BooleanWrapper topUpDeposit(TopUpDepositRequestDto)

`POST /api/2.0/portal/payment/deposit`

Put money on deposit

Returns the result of putting money on deposit.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TopUpDepositRequestDto** | body | [**TopUpDepositRequestDto**](#model-topupdepositrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updatePayment

> BooleanWrapper updatePayment(QuantityRequestDto)

`PUT /api/2.0/portal/payment/update`

Update the payment quantity

Updates the payment quantity with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **QuantityRequestDto** | body | [**QuantityRequestDto**](#model-quantityrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateWalletPayment

> BooleanWrapper updateWalletPayment(WalletQuantityRequestDto)

`PUT /api/2.0/portal/payment/updatewallet`

Update the wallet payment quantity

Updates the wallet payment quantity with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **WalletQuantityRequestDto** | body | [**WalletQuantityRequestDto**](#model-walletquantityrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **402** | Payment required | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PortalQuotaApi

### getPortalQuota

> TenantQuotaWrapper getPortalQuota()

`GET /api/2.0/portal/quota`

Get a portal quota

Returns the current portal quota.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Current portal quota | [**TenantQuotaWrapper**](#model-tenantquotawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantQuotaWrapper**](#model-tenantquotawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalTariff

> TariffWrapper getPortalTariff(refresh)

`GET /api/2.0/portal/tariff`

Get a portal tariff

Returns the current portal tariff.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** | The value indicating whether the current portal tariff information should be refreshed. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Current portal tariff | [**TariffWrapper**](#model-tariffwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TariffWrapper**](#model-tariffwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalUsedSpace

> DoubleWrapper getPortalUsedSpace()

`GET /api/2.0/portal/usedspace`

Get the portal used space

Returns the used space of the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Used portal space | [**DoubleWrapper**](#model-doublewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DoubleWrapper**](#model-doublewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRightQuota

> TenantQuotaWrapper getRightQuota()

`GET /api/2.0/portal/quota/right`

Get the recommended quota

Returns the recommended quota for the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Recommended portal quota | [**TenantQuotaWrapper**](#model-tenantquotawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantQuotaWrapper**](#model-tenantquotawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUpcomingPayments

> UpcomingPaymentArrayWrapper getUpcomingPayments(refresh)

`GET /api/2.0/portal/tariff/upcoming`

Get upcoming payments

Returns the list of upcoming payments based on the active quotas of the current portal tariff.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** | The value indicating whether the current portal tariff information should be refreshed. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of upcoming payments | [**UpcomingPaymentArrayWrapper**](#model-upcomingpaymentarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UpcomingPaymentArrayWrapper**](#model-upcomingpaymentarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## PortalSettingsApi

### continuePortal

> continuePortal()

`PUT /api/2.0/portal/continue`

Restore a portal

Restores the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### deletePortal

> StringWrapper deletePortal()

`DELETE /api/2.0/portal/delete`

Delete a portal

Deletes the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | URL to the feedback form about removing a portal | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalInformation

> TenantWrapper getPortalInformation()

`GET /api/2.0/portal`

Get a portal

Returns the current portal information.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Current portal information | [**TenantWrapper**](#model-tenantwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantWrapper**](#model-tenantwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalPath

> ObjectWrapper getPortalPath(virtualPath)

`GET /api/2.0/portal/path`

Get a path to the portal

Returns the full absolute path to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **virtualPath** | query | **String** | The virtual path for the portal resource access. | [optional] [example: "/portal/documents"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Portal path | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### sendDeleteInstructions

> sendDeleteInstructions()

`POST /api/2.0/portal/delete`

Send removal instructions

Sends the instructions to remove the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### sendSuspendInstructions

> sendSuspendInstructions()

`POST /api/2.0/portal/suspend`

Send suspension instructions

Sends the instructions to suspend the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### suspendPortal

> suspendPortal()

`PUT /api/2.0/portal/suspend`

Deactivate a portal

Deactivates the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

## PortalUsersApi

### createInvitationLink

> InvitationLinkWrapper createInvitationLink(InvitationLinkCreateRequestDto)

`POST /api/2.0/portal/users/invitationlink`

Create an invitation link

Returns an invitation link for joining the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **InvitationLinkCreateRequestDto** | body | [**InvitationLinkCreateRequestDto**](#model-invitationlinkcreaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Invitation link | [**InvitationLinkWrapper**](#model-invitationlinkwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**InvitationLinkWrapper**](#model-invitationlinkwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteInvitationLink

> StringWrapper deleteInvitationLink(InvitationLinkDeleteRequestDto)

`DELETE /api/2.0/portal/users/invitationlink`

Deletes an invitation link.

Ensures that the current user has permission to delete the specified invitation link.  Throws security or not-found exceptions if required conditions are not met.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **InvitationLinkDeleteRequestDto** | body | [**InvitationLinkDeleteRequestDto**](#model-invitationlinkdeleterequestdto) | The data transfer object containing the details of the invitation link to be deleted. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Invitation link | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getInvitationLink

> StringWrapper getInvitationLink(employeeType)

`GET /api/2.0/portal/users/invite/{employeeType}`

Get an invitation link

Returns an invitation link for joining the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **employeeType** | path | **EmployeeType** | The type of employee role for the invitation link (DocSpaceAdmin, RoomAdmin or User). | [required] [example: 1] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Invitation link | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getInvitationLinkByEmployeeType

> InvitationLinkWrapper getInvitationLinkByEmployeeType(employeeType)

`GET /api/2.0/portal/users/invitationlink/{employeeType}`

Get an invitation link

Returns an invitation link for joining the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **employeeType** | path | **EmployeeType** | The type of employee role for the invitation link (DocSpaceAdmin, RoomAdmin or User). | [required] [example: 1] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Invitation link | [**InvitationLinkWrapper**](#model-invitationlinkwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**InvitationLinkWrapper**](#model-invitationlinkwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalUsersCount

> Int64Wrapper getPortalUsersCount()

`GET /api/2.0/portal/userscount`

Get a number of portal users

Returns a number of portal users.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Number of portal users | [**Int64Wrapper**](#model-int64wrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**Int64Wrapper**](#model-int64wrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUserById

> UserInfoWrapper getUserById(userID)

`GET /api/2.0/portal/users/{userID}`

Get a user by ID

Returns a user with the ID specified in the request from the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userID** | path | **UUID** (uuid) | The user ID extracted from the route parameters. | [required] [example: "00000000-0000-0000-0000-000000000000"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | User information | [**UserInfoWrapper**](#model-userinfowrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | The user could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UserInfoWrapper**](#model-userinfowrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### markGiftMessageAsRead

> markGiftMessageAsRead()

`POST /api/2.0/portal/present/mark`

Mark a gift message as read

Marks a gift message as read.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### sendCongratulations

> sendCongratulations(Userid, Key)

`POST /api/2.0/portal/sendcongratulations`

Send congratulations

Sends congratulations to the user after registering a portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Userid** | query | **UUID** (uuid) | The user ID to receive the congratulatory message. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **Key** | query | **String** | The template identifier or email configuration key. | [required] [example: "birthday"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### updateInvitationLink

> InvitationLinkWrapper updateInvitationLink(InvitationLinkUpdateRequestDto)

`PUT /api/2.0/portal/users/invitationlink`

Update an invitation link

Returns an invitation link for joining the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **InvitationLinkUpdateRequestDto** | body | [**InvitationLinkUpdateRequestDto**](#model-invitationlinkupdaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Invitation link | [**InvitationLinkWrapper**](#model-invitationlinkwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**InvitationLinkWrapper**](#model-invitationlinkwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SecurityAccessToDevToolsApi

### setTenantDevToolsAccessSettings

> TenantDevToolsAccessSettingsWrapper setTenantDevToolsAccessSettings(TenantDevToolsAccessSettingsDto)

`POST /api/2.0/settings/devtoolsaccess`

Set the Developer Tools access settings

Sets the Developer Tools access settings for the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantDevToolsAccessSettingsDto** | body | [**TenantDevToolsAccessSettingsDto**](#model-tenantdevtoolsaccesssettingsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Developer Tools access settings | [**TenantDevToolsAccessSettingsWrapper**](#model-tenantdevtoolsaccesssettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantDevToolsAccessSettingsWrapper**](#model-tenantdevtoolsaccesssettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SecurityActiveConnectionsApi

### getAllActiveConnections

> ActiveConnectionsWrapper getAllActiveConnections()

`GET /api/2.0/security/activeconnections`

Get active connections

Returns all the active connections to the portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Active portal connections | [**ActiveConnectionsWrapper**](#model-activeconnectionswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ActiveConnectionsWrapper**](#model-activeconnectionswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### logOutActiveConnection

> BooleanWrapper logOutActiveConnection(loginEventId)

`PUT /api/2.0/security/activeconnections/logout/{loginEventId}`

Log out from the connection

Logs out from the connection with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **loginEventId** | path | **Integer** (int32) | The ID of the specific login event. | [required] [example: 12345] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### logOutAllActiveConnectionsChangePassword

> StringWrapper logOutAllActiveConnectionsChangePassword()

`PUT /api/2.0/security/activeconnections/logoutallchangepassword`

Log out and change password

Logs out from all the active connections for the current user and changes their password.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | URL to the confirmation message for changing a password | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### logOutAllActiveConnectionsForUser

> logOutAllActiveConnectionsForUser(userId)

`PUT /api/2.0/security/activeconnections/logoutall/{userId}`

Log out for the user by ID

Logs out from all the active connections for the user with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userId** | path | **UUID** (uuid) | The user ID extracted from the route parameters. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### logOutAllExceptThisConnection

> StringWrapper logOutAllExceptThisConnection()

`PUT /api/2.0/security/activeconnections/logoutallexceptthis`

Log out from all connections except the current one

Logs out from all the active connections except the current connection.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Current user name | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SecurityAuditTrailDataApi

### createAuditTrailReport

> DocumentBuilderTaskWrapper createAuditTrailReport(format)

`POST /api/2.0/security/audit/events/report`

Start the audit trail report generation

Starts generating the audit trail report (XLSX by default, or CSV) and saves it to My documents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **format** | query | **AuditReportFormat** | The output file format of the report. Defaults to XLSX. | [optional] [example: Xlsx] [enum: 0, 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | You don&#39;t have enough permission to create | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAuditEventsByFilter

> AuditEventArrayWrapper getAuditEventsByFilter(userId, moduleType, actionType, action, entryType, target, from, to, count, startIndex)

`GET /api/2.0/security/audit/events/filter`

Get filtered audit trail data

Returns a list of the audit events by the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userId** | query | **UUID** (uuid) | The ID of the user who triggered the audit event. | [optional] [example: "00000000-0000-0000-0000-000000000001"] |
| **moduleType** | query | **LocationType** | The location where the audit event occurred. | [optional] [example: Files] [enum: 0, 1, 2, 3, 27, 29, 30, 31] |
| **actionType** | query | **ActionType** | The type of action performed in the audit event (e.g., Create, Update, Delete). | [optional] [example: Create] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19] |
| **action** | query | **MessageAction** | The specific action that occurred within the audit event. | [optional] [example: FileCreated] [enum: 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 4000, 4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008, 4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037, 5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 5012, 5013, 5014, 5015, 5016, 5017, 5018, 5019, 5020, 5021, 5022, 5023, 5024, 5025, 5026, 5027, 5028, 5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038, 5039, 5040, 5041, 5042, 5043, 5044, 5045, 5046, 5047, 5048, 5049, 5050, 5053, 5054, 5055, 5056, 5057, 5058, 5059, 5060, 5061, 5062, 5063, 5064, 5065, 5066, 5068, 5069, 5070, 5071, 5072, 5073, 5074, 5075, 5076, 5077, 5078, 5079, 5080, 5081, 5082, 5083, 5084, 5085, 5086, 5087, 5088, 5089, 5090, 5091, 5092, 5093, 5094, 5095, 5096, 5097, 5098, 5099, 5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5112, 5113, 5114, 5115, 5116, 5117, 5118, 5119, 5120, 5121, 5122, 5123, 5124, 5125, 5126, 5127, 5128, 5129, 5130, 5131, 5132, 5133, 5150, 5151, 5152, 5153, 5154, 5155, 5156, 5157, 5158, 5159, 5160, 5201, 5202, 5203, 5204, 5205, 5206, 5501, 5502, 5503, 6000, 6001, 6002, 6003, 6004, 6005, 6006, 6007, 6008, 6009, 6010, 6011, 6012, 6013, 6014, 6015, 6016, 6017, 6018, 6019, 6020, 6021, 6022, 6023, 6024, 6025, 6026, 6027, 6028, 6029, 6030, 6031, 6032, 6033, 6034, 6035, 6036, 6037, 6038, 6039, 6040, 6041, 6042, 6043, 6044, 6045, 6046, 6047, 6048, 6049, 6050, 6051, 6052, 6053, 6054, 6055, 6056, 6057, 6058, 6059, 6060, 6061, 6062, 6063, 6064, 6065, 6066, 6067, 6068, 6069, 6070, 6071, 6072, 6073, 6074, 6075, 6076, 6077, 6078, 6079, 6080, 6081, 6082, 6083, 6084, 6085, 6086, 6087, 6088, 6089, 6090, 6091, 6092, 6093, 6094, 6095, 6096, 6097, 6098, 6099, 6100, 6101, 6102, 7000, 7001, 7002, 7003, 7004, 9901, 9902, 9903, 9904, 9905, 9906, 9907, 9908, 9909, -1] |
| **entryType** | query | **EntryType** | The type of audit entry (e.g., Folder, User, File). | [optional] [example: File] [enum: 0, 1, 2, 23, 24, 25, 26, 27] |
| **target** | query | **String** | The target object affected by the audit event (e.g., document ID, user account). | [optional] [example: "document.docx"] |
| **from** | query | **ApiDateTime** | The starting date and time for filtering audit events. | [optional] [example: 2024-01-01T00:00:00Z] |
| **to** | query | **ApiDateTime** | The ending date and time for filtering audit events. | [optional] [example: 2024-01-31T23:59:59Z] |
| **count** | query | **Integer** (int32) | The maximum number of audit event records to retrieve. | [optional] [example: 100] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The index of the first audit event record to retrieve in a paged query. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of filtered audit trail data | [**AuditEventArrayWrapper**](#model-auditeventarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuditEventArrayWrapper**](#model-auditeventarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAuditSettings

> TenantAuditSettingsWrapper getAuditSettings()

`GET /api/2.0/security/audit/settings/lifetime`

Get the audit trail settings

Returns the audit trail settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Audit settings | [**TenantAuditSettingsWrapper**](#model-tenantauditsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantAuditSettingsWrapper**](#model-tenantauditsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAuditTrailMappers

> ObjectWrapper getAuditTrailMappers(productType, moduleType)

`GET /api/2.0/security/audit/mappers`

Get audit trail mappers

Returns the mappers for the audit trail types.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **productType** | query | **ProductType** | The type of product related to the audit trail. | [optional] [example: Documents] [enum: 2, 3, 7, 8] |
| **moduleType** | query | **LocationType** | The location associated with the audit trail. | [optional] [example: Files] [enum: 0, 1, 2, 3, 27, 29, 30, 31] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Audit trail mappers | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAuditTrailReport

> DocumentBuilderTaskWrapper getAuditTrailReport()

`GET /api/2.0/security/audit/events/report`

Get the audit trail report generation status

Returns the status of generating the audit trail report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAuditTrailTypes

> ObjectWrapper getAuditTrailTypes()

`GET /api/2.0/security/audit/types`

Get audit trail types

Returns all the available audit trail types.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Audit trail types | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getLastAuditEvents

> AuditEventArrayWrapper getLastAuditEvents()

`GET /api/2.0/security/audit/events/last`

Get audit trail data

Returns a list of the latest changes (creation, modification, deletion, etc.) made by users to the entities on the portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of audit trail data | [**AuditEventArrayWrapper**](#model-auditeventarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuditEventArrayWrapper**](#model-auditeventarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### setAuditSettings

> TenantAuditSettingsWrapper setAuditSettings(TenantAuditSettingsWrapper)

`POST /api/2.0/security/audit/settings/lifetime`

Set the audit trail settings

Sets the audit trail settings for the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantAuditSettingsWrapper** | body | [**TenantAuditSettingsWrapper**](#model-tenantauditsettingswrapper) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Audit trail settings | [**TenantAuditSettingsWrapper**](#model-tenantauditsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Exception in LoginHistoryLifeTime or AuditTrailLifeTime | - | - |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantAuditSettingsWrapper**](#model-tenantauditsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateAuditTrailReport

> terminateAuditTrailReport()

`DELETE /api/2.0/security/audit/events/report`

Terminate the audit trail report generation

Terminates generating the audit trail report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

## SecurityBannersVisibilityApi

### setTenantBannerSettings

> TenantBannerSettingsWrapper setTenantBannerSettings(TenantBannerSettingsDto)

`POST /api/2.0/settings/banner`

Set the banners visibility

Sets the visibility settings of the promotional banners in the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantBannerSettingsDto** | body | [**TenantBannerSettingsDto**](#model-tenantbannersettingsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Promotional banners visibility settings | [**TenantBannerSettingsWrapper**](#model-tenantbannersettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantBannerSettingsWrapper**](#model-tenantbannersettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SecurityCSPApi

### configureCsp

> CspWrapper configureCsp(CspRequestsDto)

`POST /api/2.0/security/csp`

Configure CSP settings

Configures the CSP (Content Security Policy) settings for the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CspRequestsDto** | body | [**CspRequestsDto**](#model-csprequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**CspWrapper**](#model-cspwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Exception in Domains | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CspWrapper**](#model-cspwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getCspSettings

> CspWrapper getCspSettings()

`GET /api/2.0/security/csp`

Get CSP settings

Returns the CSP (Content Security Policy) settings for the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**CspWrapper**](#model-cspwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CspWrapper**](#model-cspwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SecurityFirebaseApi

### docRegisterPusnNotificationDevice

> FireBaseUserWrapper docRegisterPusnNotificationDevice(FirebaseRequestsDto)

`POST /api/2.0/settings/push/docregisterdevice`

Save the Documents Firebase device token

Saves the Firebase device token specified in the request for the Documents application.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **FirebaseRequestsDto** | body | [**FirebaseRequestsDto**](#model-firebaserequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | FireBase user | [**FireBaseUserWrapper**](#model-firebaseuserwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FireBaseUserWrapper**](#model-firebaseuserwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### subscribeDocumentsPushNotification

> FireBaseUserWrapper subscribeDocumentsPushNotification(FirebaseRequestsDto)

`PUT /api/2.0/settings/push/docsubscribe`

Subscribe to Documents push notification

Subscribes to the Documents push notification.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **FirebaseRequestsDto** | body | [**FirebaseRequestsDto**](#model-firebaserequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | FireBase user | [**FireBaseUserWrapper**](#model-firebaseuserwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FireBaseUserWrapper**](#model-firebaseuserwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SecurityLoginHistoryApi

### createLoginHistoryReport

> DocumentBuilderTaskWrapper createLoginHistoryReport(format)

`POST /api/2.0/security/audit/login/report`

Start the login history report generation

Starts generating the login history report (XLSX by default, or CSV) and saves it to My documents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **format** | query | **AuditReportFormat** | The output file format of the report. Defaults to XLSX. | [optional] [example: Xlsx] [enum: 0, 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getLastLoginEvents

> LoginEventArrayWrapper getLastLoginEvents()

`GET /api/2.0/security/audit/login/last`

Get login history

Returns all the latest user login activity, including successful logins and error logs.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of login events | [**LoginEventArrayWrapper**](#model-logineventarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**LoginEventArrayWrapper**](#model-logineventarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getLoginEventsByFilter

> LoginEventArrayWrapper getLoginEventsByFilter(userId, action, from, to, count, startIndex)

`GET /api/2.0/security/audit/login/filter`

Get filtered login events

Returns a list of the login events by the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userId** | query | **UUID** (uuid) | The ID of the user whose login events are being queried. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **action** | query | **MessageAction** | The login-related action to filter events by. | [optional] [example: FileCreated] [enum: 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 4000, 4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008, 4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037, 5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 5012, 5013, 5014, 5015, 5016, 5017, 5018, 5019, 5020, 5021, 5022, 5023, 5024, 5025, 5026, 5027, 5028, 5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038, 5039, 5040, 5041, 5042, 5043, 5044, 5045, 5046, 5047, 5048, 5049, 5050, 5053, 5054, 5055, 5056, 5057, 5058, 5059, 5060, 5061, 5062, 5063, 5064, 5065, 5066, 5068, 5069, 5070, 5071, 5072, 5073, 5074, 5075, 5076, 5077, 5078, 5079, 5080, 5081, 5082, 5083, 5084, 5085, 5086, 5087, 5088, 5089, 5090, 5091, 5092, 5093, 5094, 5095, 5096, 5097, 5098, 5099, 5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5112, 5113, 5114, 5115, 5116, 5117, 5118, 5119, 5120, 5121, 5122, 5123, 5124, 5125, 5126, 5127, 5128, 5129, 5130, 5131, 5132, 5133, 5150, 5151, 5152, 5153, 5154, 5155, 5156, 5157, 5158, 5159, 5160, 5201, 5202, 5203, 5204, 5205, 5206, 5501, 5502, 5503, 6000, 6001, 6002, 6003, 6004, 6005, 6006, 6007, 6008, 6009, 6010, 6011, 6012, 6013, 6014, 6015, 6016, 6017, 6018, 6019, 6020, 6021, 6022, 6023, 6024, 6025, 6026, 6027, 6028, 6029, 6030, 6031, 6032, 6033, 6034, 6035, 6036, 6037, 6038, 6039, 6040, 6041, 6042, 6043, 6044, 6045, 6046, 6047, 6048, 6049, 6050, 6051, 6052, 6053, 6054, 6055, 6056, 6057, 6058, 6059, 6060, 6061, 6062, 6063, 6064, 6065, 6066, 6067, 6068, 6069, 6070, 6071, 6072, 6073, 6074, 6075, 6076, 6077, 6078, 6079, 6080, 6081, 6082, 6083, 6084, 6085, 6086, 6087, 6088, 6089, 6090, 6091, 6092, 6093, 6094, 6095, 6096, 6097, 6098, 6099, 6100, 6101, 6102, 7000, 7001, 7002, 7003, 7004, 9901, 9902, 9903, 9904, 9905, 9906, 9907, 9908, 9909, -1] |
| **from** | query | **ApiDateTime** | The starting date and time for filtering login events. | [optional] [example: 2024-01-15T10:30:00Z] |
| **to** | query | **ApiDateTime** | The ending date and time for filtering login events. | [optional] [example: 2024-01-15T10:30:00Z] |
| **count** | query | **Integer** (int32) | The number of login events to retrieve in the query. | [optional] [example: 1] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for fetching a subset of login events from the query results. | [optional] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of filtered login events | [**LoginEventArrayWrapper**](#model-logineventarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**LoginEventArrayWrapper**](#model-logineventarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getLoginHistoryReport

> DocumentBuilderTaskWrapper getLoginHistoryReport()

`GET /api/2.0/security/audit/login/report`

Get the login history report generation status

Returns the status of generating the login history report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### terminateLoginHistoryReport

> terminateLoginHistoryReport()

`DELETE /api/2.0/security/audit/login/report`

Terminate the login history report generation

Terminates generating the login history report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

## SecurityOAuth2Api

### generateJwtToken

> StringWrapper generateJwtToken()

`GET /api/2.0/security/oauth2/token`

Generate JWT token

Generates a JWT token for communication between login (client) and identity services.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Jwt Token | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SecuritySMTPSettingsApi

### getSmtpOperationStatus

> SmtpOperationStatusRequestsWrapper getSmtpOperationStatus()

`GET /api/2.0/smtpsettings/smtp/test/status`

Get the SMTP testing process status

Returns the status of the SMTP testing process.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | SMTP operation status | [**SmtpOperationStatusRequestsWrapper**](#model-smtpoperationstatusrequestswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SmtpOperationStatusRequestsWrapper**](#model-smtpoperationstatusrequestswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSmtpSettings

> SmtpSettingsWrapper getSmtpSettings()

`GET /api/2.0/smtpsettings/smtp`

Get the SMTP settings

Returns the current portal SMTP settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | SMTP settings | [**SmtpSettingsWrapper**](#model-smtpsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SmtpSettingsWrapper**](#model-smtpsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### resetSmtpSettings

> SmtpSettingsWrapper resetSmtpSettings()

`DELETE /api/2.0/smtpsettings/smtp`

Reset the SMTP settings

Resets the SMTP settings of the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Default SMTP settings | [**SmtpSettingsWrapper**](#model-smtpsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SmtpSettingsWrapper**](#model-smtpsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveSmtpSettings

> SmtpSettingsWrapper saveSmtpSettings(SmtpSettingsDto)

`POST /api/2.0/smtpsettings/smtp`

Save the SMTP settings

Saves the SMTP settings for the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SmtpSettingsDto** | body | [**SmtpSettingsDto**](#model-smtpsettingsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | SMTP settings | [**SmtpSettingsWrapper**](#model-smtpsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SmtpSettingsWrapper**](#model-smtpsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### testSmtpSettings

> SmtpOperationStatusRequestsWrapper testSmtpSettings()

`GET /api/2.0/smtpsettings/smtp/test`

Test the SMTP settings

Tests the SMTP settings for the current portal (sends test message to the user email).

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | SMTP operation status | [**SmtpOperationStatusRequestsWrapper**](#model-smtpoperationstatusrequestswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SmtpOperationStatusRequestsWrapper**](#model-smtpoperationstatusrequestswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SettingsAccessToDevToolsApi

### getTenantAccessDevToolsSettings

> TenantDevToolsAccessSettingsWrapper getTenantAccessDevToolsSettings()

`GET /api/2.0/settings/devtoolsaccess`

Get the Developer Tools access settings

Returns the Developer Tools access settings for the portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Developer Tools access settings | [**TenantDevToolsAccessSettingsWrapper**](#model-tenantdevtoolsaccesssettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantDevToolsAccessSettingsWrapper**](#model-tenantdevtoolsaccesssettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SettingsAuthorizationApi

### getAuthServices

> AuthServiceRequestsArrayWrapper getAuthServices()

`GET /api/2.0/settings/authservice`

Get the authorization services

Returns the authorization services.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Authorization services | [**AuthServiceRequestsArrayWrapper**](#model-authservicerequestsarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AuthServiceRequestsArrayWrapper**](#model-authservicerequestsarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveAuthKeys

> BooleanWrapper saveAuthKeys(AuthServiceRequestsDto)

`POST /api/2.0/settings/authservice`

Save the authorization keys

Saves the authorization keys.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AuthServiceRequestsDto** | body | [**AuthServiceRequestsDto**](#model-authservicerequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the authorization keys are changed | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Bad keys | - | - |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### testExternalDatabaseConnection

> ConnectionTestResultWrapper testExternalDatabaseConnection(ExternalDatabaseSettings)

`POST /api/2.0/settings/authservice/externaldb/test`

Test external database connection

Tests an external database connection with the provided settings without saving them.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ExternalDatabaseSettings** | body | [**ExternalDatabaseSettings**](#model-externaldatabasesettings) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Connection test result with Success flag and optional Error message | [**ConnectionTestResultWrapper**](#model-connectiontestresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ConnectionTestResultWrapper**](#model-connectiontestresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsBannersVisibilityApi

### getTenantBannerSettings

> TenantBannerSettingsWrapper getTenantBannerSettings()

`GET /api/2.0/settings/banner`

Get the banners visibility

Returns the visibility settings of the promotional banners in the portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Promotional banners visibility settings | [**TenantBannerSettingsWrapper**](#model-tenantbannersettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantBannerSettingsWrapper**](#model-tenantbannersettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SettingsCommonSettingsApi

### closeAdminHelper

> closeAdminHelper()

`PUT /api/2.0/settings/closeadminhelper`

Close the admin helper

Closes the administrator helper notification.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | Not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### completeWizard

> WizardSettingsWrapper completeWizard(WizardRequestsDto)

`PUT /api/2.0/settings/wizard/complete`

Complete the Wizard settings

Completes the Wizard settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **WizardRequestsDto** | body | [**WizardRequestsDto**](#model-wizardrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Wizard settings | [**WizardSettingsWrapper**](#model-wizardsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email address/The password is empty | - | - |
| **402** | You must enter a license key or license key is not correct or license expired or user quota does not match the license | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WizardSettingsWrapper**](#model-wizardsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### configureDeepLink

> TenantDeepLinkSettingsWrapper configureDeepLink(DeepLinkConfigurationRequestsDto)

`POST /api/2.0/settings/deeplink`

Configure the deep link settings

Saves the deep link configuration settings for the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DeepLinkConfigurationRequestsDto** | body | [**DeepLinkConfigurationRequestsDto**](#model-deeplinkconfigurationrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Deep link configuration updated | [**TenantDeepLinkSettingsWrapper**](#model-tenantdeeplinksettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid deep link configuration | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantDeepLinkSettingsWrapper**](#model-tenantdeeplinksettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deletePortalColorTheme

> CustomColorThemesSettingsWrapper deletePortalColorTheme(id)

`DELETE /api/2.0/settings/colortheme`

Delete a color theme

Deletes the portal color theme with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | query | **Integer** (int32) | The ID of the portal theme to delete. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Portal theme settings: custom color theme settings, selected or not, limit | [**CustomColorThemesSettingsWrapper**](#model-customcolorthemessettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CustomColorThemesSettingsWrapper**](#model-customcolorthemessettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getDeepLinkSettings

> TenantDeepLinkSettingsWrapper getDeepLinkSettings()

`GET /api/2.0/settings/deeplink`

Get the deep link settings

Returns the deep link settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**TenantDeepLinkSettingsWrapper**](#model-tenantdeeplinksettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantDeepLinkSettingsWrapper**](#model-tenantdeeplinksettingswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPaymentSettings

> PaymentSettingsWrapper getPaymentSettings()

`GET /api/2.0/settings/payment`

Get the payment settings

Returns the portal payment settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Payment settings: sales email, feedback and support URL, link to pay for a portal, Standalone or not, current license, maximum quota quantity | [**PaymentSettingsWrapper**](#model-paymentsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**PaymentSettingsWrapper**](#model-paymentsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalColorTheme

> CustomColorThemesSettingsWrapper getPortalColorTheme()

`GET /api/2.0/settings/colortheme`

Get a color theme

Returns the portal color theme.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Settings of the portal themes | [**CustomColorThemesSettingsWrapper**](#model-customcolorthemessettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CustomColorThemesSettingsWrapper**](#model-customcolorthemessettingswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalHostname

> ObjectWrapper getPortalHostname()

`GET /api/2.0/settings/machine`

Get hostname

Returns the portal hostname.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Portal hostname | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalLogo

> StringWrapper getPortalLogo()

`GET /api/2.0/settings/logo`

Get a portal logo

Returns the portal logo image URL.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Portal logo image URL | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPortalSettings

> SettingsWrapper getPortalSettings(withpassword)

`GET /api/2.0/settings`

Get the portal settings

Returns a list of all the available portal settings with the current values for each parameter.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **withpassword** | query | **Boolean** | Specifies whether to include the password hashing configuration in the response. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Settings | [**SettingsWrapper**](#model-settingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SettingsWrapper**](#model-settingswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSocketSettings

> ObjectWrapper getSocketSettings()

`GET /api/2.0/settings/socket`

Get the socket settings

Returns the socket settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Socket settings: hub URL | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSupportedCultures

> STRINGArrayWrapper getSupportedCultures()

`GET /api/2.0/settings/cultures`

Get supported languages

Returns a list of all the available portal languages in the format of a two-letter or four-letter language code (e.g. de, en-US, etc.).

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of all the available portal languages | [**STRINGArrayWrapper**](#model-stringarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**STRINGArrayWrapper**](#model-stringarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantAiAccessSettings

> TenantAiAccessSettingsWrapper getTenantAiAccessSettings()

`GET /api/2.0/settings/ai-access`

Get the AI access settings for the portal

Returns the current portal-level AI access settings that control whether all AI functionality  (chat, agents, vectorization) is available for the portal. AI is enabled by default.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | AI access settings | [**TenantAiAccessSettingsWrapper**](#model-tenantaiaccesssettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantAiAccessSettingsWrapper**](#model-tenantaiaccesssettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantUserInvitationSettings

> TenantUserInvitationSettingsWrapper getTenantUserInvitationSettings()

`GET /api/2.0/settings/invitationsettings`

Get the user invitation settings

Returns the portal user invitation settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | portal user invitation settings | [**TenantUserInvitationSettingsWrapper**](#model-tenantuserinvitationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantUserInvitationSettingsWrapper**](#model-tenantuserinvitationsettingswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTimeZones

> TimezonesRequestsArrayWrapper getTimeZones()

`GET /api/2.0/settings/timezones`

Get time zones

Returns a list of all the available portal time zones.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of all the available time zones with their IDs and display names | [**TimezonesRequestsArrayWrapper**](#model-timezonesrequestsarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TimezonesRequestsArrayWrapper**](#model-timezonesrequestsarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveDefaultFolder

> StudioDefaultPageSettingsWrapper saveDefaultFolder(DefaultProductRequestDto)

`PUT /api/2.0/settings/defaultfolder`

Set the default folder

Sets the default folder.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DefaultProductRequestDto** | body | [**DefaultProductRequestDto**](#model-defaultproductrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about saving settings successfully | [**StudioDefaultPageSettingsWrapper**](#model-studiodefaultpagesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StudioDefaultPageSettingsWrapper**](#model-studiodefaultpagesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveDnsSettings

> StringWrapper saveDnsSettings(DnsSettingsRequestsDto)

`PUT /api/2.0/settings/dns`

Save the DNS settings

Saves the DNS settings specified in the request to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DnsSettingsRequestsDto** | body | [**DnsSettingsRequestsDto**](#model-dnssettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about changing DNS | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid domain name/incorrect length of doman name | - | - |
| **402** | Your pricing plan does not support this option | - | - |
| **405** | Method not allowed | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveMailDomainSettings

> StringWrapper saveMailDomainSettings(MailDomainSettingsRequestsDto)

`POST /api/2.0/settings/maildomainsettings`

Save the mail domain settings

Saves the mail domain settings specified in the request to the portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **MailDomainSettingsRequestsDto** | body | [**MailDomainSettingsRequestsDto**](#model-maildomainsettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about the result of saving the mail domain settings | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### savePortalColorTheme

> CustomColorThemesSettingsWrapper savePortalColorTheme(CustomColorThemesSettingsRequestsDto)

`PUT /api/2.0/settings/colortheme`

Save a color theme

Saves the portal color theme specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CustomColorThemesSettingsRequestsDto** | body | [**CustomColorThemesSettingsRequestsDto**](#model-customcolorthemessettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Portal theme settings | [**CustomColorThemesSettingsWrapper**](#model-customcolorthemessettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CustomColorThemesSettingsWrapper**](#model-customcolorthemessettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setTenantAiAccessSettings

> TenantAiAccessSettingsWrapper setTenantAiAccessSettings(TenantAiAccessSettingsDto)

`POST /api/2.0/settings/ai-access`

Set the AI access for the portal

Updates the portal-level AI access settings. When AI is disabled, all AI features are turned off:  the AI Agents folder is hidden from root folder listings, AI status checks immediately return disabled,  and AI chat endpoints become inaccessible. Only users with the DocSpaceAdmin role  (EditPortalSettings permission) can change this setting.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantAiAccessSettingsDto** | body | [**TenantAiAccessSettingsDto**](#model-tenantaiaccesssettingsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated AI access settings | [**TenantAiAccessSettingsWrapper**](#model-tenantaiaccesssettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to change the AI access settings | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantAiAccessSettingsWrapper**](#model-tenantaiaccesssettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateEmailActivationSettings

> EmailActivationSettingsWrapper updateEmailActivationSettings(EmailActivationSettings)

`PUT /api/2.0/settings/emailactivation`

Update the email activation settings

Updates the email activation settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **EmailActivationSettings** | body | [**EmailActivationSettings**](#model-emailactivationsettings) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated email activation settings | [**EmailActivationSettingsWrapper**](#model-emailactivationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmailActivationSettingsWrapper**](#model-emailactivationsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateInvitationSettings

> TenantUserInvitationSettingsWrapper updateInvitationSettings(TenantUserInvitationSettingsRequestDto)

`PUT /api/2.0/settings/invitationsettings`

Update user invitation settings

Updates the portal user invitation settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantUserInvitationSettingsRequestDto** | body | [**TenantUserInvitationSettingsRequestDto**](#model-tenantuserinvitationsettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated user invitation settings | [**TenantUserInvitationSettingsWrapper**](#model-tenantuserinvitationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantUserInvitationSettingsWrapper**](#model-tenantuserinvitationsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsCookiesApi

### getCookieSettings

> CookieSettingsWrapper getCookieSettings()

`GET /api/2.0/settings/cookiesettings`

Get cookies lifetime

Returns the cookies lifetime value in minutes.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Lifetime value in minutes | [**CookieSettingsWrapper**](#model-cookiesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CookieSettingsWrapper**](#model-cookiesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateCookieSettings

> StringWrapper updateCookieSettings(CookieSettingsRequestsDto)

`PUT /api/2.0/settings/cookiesettings`

Update cookies lifetime

Updates the cookies lifetime value in minutes.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CookieSettingsRequestsDto** | body | [**CookieSettingsRequestsDto**](#model-cookiesettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about the result of saving new settings | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsDocsCloudApi

### calculateDevPack

> PaymentCalculationWrapper calculateDevPack(DocsCloudDevPackRequestDto)

`POST /api/2.0/settings/docscloud/calculatedevpack`

Calculate the DocsCloud subscription switch cost

Calculates the top-up cost of switching the current DocsCloud subscription to DocsCloudDevPack,  without making any changes. The quantity is taken from the currently purchased DocsCloud quota.  Only the portal payer can perform this action.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DocsCloudDevPackRequestDto** | body | [**DocsCloudDevPackRequestDto**](#model-docsclouddevpackrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Payment calculation | [**PaymentCalculationWrapper**](#model-paymentcalculationwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **402** | Tariff is not paid | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer or service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**PaymentCalculationWrapper**](#model-paymentcalculationwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### checkHealth

> checkHealth()

`GET /api/2.0/settings/docscloud/healthcheck`

Check the DocsCloud server health

Checks whether the DocsCloud server is reachable.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | DocsCloud server is reachable | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### createTenantQuotaReport

> DocumentBuilderTaskWrapper createTenantQuotaReport()

`POST /api/2.0/settings/docscloud/tenant/quota/report`

Start the DocsCloud tenant quota report generation

Starts generating the DocsCloud user quota report as an xlsx file and saves it in My Documents.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenant

> DocsCloudTenantWrapper getTenant(refresh)

`GET /api/2.0/settings/docscloud/tenant`

Get the DocsCloud tenant

Returns the DocsCloud tenant of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** |  | [optional] [default to false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | DocsCloud tenant | [**DocsCloudTenantWrapper**](#model-docscloudtenantwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocsCloudTenantWrapper**](#model-docscloudtenantwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantConfig

> DocsCloudConfigWrapper getTenantConfig(refresh)

`GET /api/2.0/settings/docscloud/tenant/config`

Get the DocsCloud tenant configuration

Returns the DocsCloud tenant configuration of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** |  | [optional] [default to false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | DocsCloud tenant configuration | [**DocsCloudConfigWrapper**](#model-docscloudconfigwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocsCloudConfigWrapper**](#model-docscloudconfigwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantInfo

> DocsCloudTenantInfoWrapper getTenantInfo(refresh)

`GET /api/2.0/settings/docscloud/tenant/info`

Get the DocsCloud tenant information

Returns the DocsCloud license and server information with usage statistics of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** |  | [optional] [default to false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | DocsCloud tenant information | [**DocsCloudTenantInfoWrapper**](#model-docscloudtenantinfowrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocsCloudTenantInfoWrapper**](#model-docscloudtenantinfowrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantQuota

> DocsCloudQuotaWrapper getTenantQuota(refresh)

`GET /api/2.0/settings/docscloud/tenant/quota`

Get the DocsCloud tenant quota

Returns the DocsCloud user quota (active users) of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** |  | [optional] [default to false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | DocsCloud user quota | [**DocsCloudQuotaWrapper**](#model-docscloudquotawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocsCloudQuotaWrapper**](#model-docscloudquotawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantQuotaReport

> DocumentBuilderTaskWrapper getTenantQuotaReport()

`GET /api/2.0/settings/docscloud/tenant/quota/report`

Get the status of the DocsCloud tenant quota report generation

Returns the status of generating the DocsCloud user quota report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTenantUsage

> DocsCloudUsageWrapper getTenantUsage(refresh)

`GET /api/2.0/settings/docscloud/tenant/usage`

Get the DocsCloud tenant usage

Returns the DocsCloud usage statistics of the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **refresh** | query | **Boolean** |  | [optional] [default to false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | DocsCloud tenant usage statistics | [**DocsCloudUsageWrapper**](#model-docscloudusagewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocsCloudUsageWrapper**](#model-docscloudusagewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startDocsCloudTrial

> BooleanWrapper startDocsCloudTrial()

`POST /api/2.0/settings/docscloud/trial`

Start the DocsCloud trial

Starts the DocsCloud trial.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Quota is already set | - | - |
| **402** | Tariff is not paid | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Quota could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### switchToDevPack

> BooleanWrapper switchToDevPack(DocsCloudDevPackRequestDto)

`POST /api/2.0/settings/docscloud/switchtodevpack`

Switch the DocsCloud subscription to DocsCloudDevPack

Switches the current DocsCloud subscription to DocsCloudDevPack: charges the price difference  from the wallet and transfers the subscription (with its license) to the target product.  The quantity is taken from the currently purchased DocsCloud quota.  Only the portal payer can perform this action.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DocsCloudDevPackRequestDto** | body | [**DocsCloudDevPackRequestDto**](#model-docsclouddevpackrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid request parameters | - | - |
| **402** | Tariff is not paid | - | - |
| **403** | No permissions to perform this action | - | - |
| **404** | Customer or service could not be found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateTenantQuotaReport

> terminateTenantQuotaReport()

`DELETE /api/2.0/settings/docscloud/tenant/quota/report`

Terminate the DocsCloud tenant quota report generation

Terminates generating the DocsCloud user quota report.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### updateTenantConfig

> DocsCloudConfigWrapper updateTenantConfig(DocsCloudConfig)

`PUT /api/2.0/settings/docscloud/tenant/config`

Update the DocsCloud tenant configuration

Updates the DocsCloud tenant configuration of the current portal with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DocsCloudConfig** | body | [**DocsCloudConfig**](#model-docscloudconfig) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated DocsCloud tenant configuration | [**DocsCloudConfigWrapper**](#model-docscloudconfigwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocsCloudConfigWrapper**](#model-docscloudconfigwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsEncryptionApi

### getStorageEncryptionProgress

> DoubleNullableWrapper getStorageEncryptionProgress()

`GET /api/2.0/settings/encryption/progress`

Get the storage encryption progress

Returns the storage encryption progress.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Storage encryption progress | [**DoubleNullableWrapper**](#model-doublenullablewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | Method not allowed | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DoubleNullableWrapper**](#model-doublenullablewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getStorageEncryptionSettings

> EncryptionSettingsWrapper getStorageEncryptionSettings()

`GET /api/2.0/settings/encryption/settings`

Get the storage encryption settings

Returns the storage encryption settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Storage encryption settings | [**EncryptionSettingsWrapper**](#model-encryptionsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **405** | Method not allowed | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionSettingsWrapper**](#model-encryptionsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startStorageEncryption

> BooleanWrapper startStorageEncryption(StorageEncryptionRequestsDto)

`POST /api/2.0/settings/encryption/start`

Start the storage encryption process

Starts the storage encryption process.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **StorageEncryptionRequestsDto** | body | [**StorageEncryptionRequestsDto**](#model-storageencryptionrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | No permissions to perform this action | - | - |
| **405** | Method not allowed | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsGreetingSettingsApi

### getGreetingSettings

> ObjectWrapper getGreetingSettings()

`GET /api/2.0/settings/greetingsettings`

Get greeting settings

Returns the greeting settings for the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Greeting settings: tenant name | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getIsDefaultGreetingSettings

> BooleanWrapper getIsDefaultGreetingSettings()

`GET /api/2.0/settings/greetingsettings/isdefault`

Check the default greeting settings

Checks if the greeting settings of the current portal are set to default or not.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the greeting settings of the current portal are set to default | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### restoreGreetingSettings

> StringWrapper restoreGreetingSettings()

`POST /api/2.0/settings/greetingsettings/restore`

Restore the greeting settings

Restores the current portal greeting settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Greeting settings: tenant name | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveGreetingSettings

> StringWrapper saveGreetingSettings(GreetingSettingsRequestsDto)

`POST /api/2.0/settings/greetingsettings`

Save the greeting settings

Saves the greeting settings specified in the request to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **GreetingSettingsRequestsDto** | body | [**GreetingSettingsRequestsDto**](#model-greetingsettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about saving greeting settings successfully | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsIPRestrictionsApi

### getIpRestrictions

> IPRestrictionArrayWrapper getIpRestrictions()

`GET /api/2.0/settings/iprestrictions`

Get the IP portal restrictions

Returns the IP portal restrictions.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of IP restrictions parameters | [**IPRestrictionArrayWrapper**](#model-iprestrictionarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**IPRestrictionArrayWrapper**](#model-iprestrictionarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### readIpRestrictionsSettings

> IPRestrictionsSettingsWrapper readIpRestrictionsSettings()

`GET /api/2.0/settings/iprestrictions/settings`

Get the IP restriction settings

Returns the IP restriction settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | IP restriction settings | [**IPRestrictionsSettingsWrapper**](#model-iprestrictionssettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**IPRestrictionsSettingsWrapper**](#model-iprestrictionssettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveIpRestrictions

> IpRestrictionsWrapper saveIpRestrictions(IpRestrictionsDto)

`PUT /api/2.0/settings/iprestrictions`

Update the IP restrictions

Updates the IP restrictions with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IpRestrictionsDto** | body | [**IpRestrictionsDto**](#model-iprestrictionsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated IP restriction settings | [**IpRestrictionsWrapper**](#model-iprestrictionswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**IpRestrictionsWrapper**](#model-iprestrictionswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateIpRestrictionsSettings

> IpRestrictionsWrapper updateIpRestrictionsSettings(IpRestrictionsDto)

`PUT /api/2.0/settings/iprestrictions/settings`

Update the IP restriction settings

Updates the IP restriction settings with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IpRestrictionsDto** | body | [**IpRestrictionsDto**](#model-iprestrictionsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated IP restriction settings | [**IpRestrictionsWrapper**](#model-iprestrictionswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**IpRestrictionsWrapper**](#model-iprestrictionswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsLicenseApi

### acceptLicense

> StringWrapper acceptLicense()

`POST /api/2.0/settings/license/accept`

Activate a license

Activates a license for the portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about the result of activating license | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getIsLicenseRequired

> BooleanWrapper getIsLicenseRequired()

`GET /api/2.0/settings/license/required`

Request a license

Requests a portal license if necessary.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the license is required | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### refreshLicense

> BooleanWrapper refreshLicense()

`GET /api/2.0/settings/license/refresh`

Refresh the license

Refreshes the portal license.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### uploadLicense

> StringWrapper uploadLicense(Files)

`POST /api/2.0/settings/license`

Upload a license

Uploads a portal license specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Files** | form | **List** (binary) | The list of license files to be uploaded. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | License | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | The uploaded file could not be found | - | - |
| **403** | Portal Access | - | - |
| **405** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

## SettingsLoginSettingsApi

### getLoginSettings

> LoginSettingsWrapper getLoginSettings()

`GET /api/2.0/settings/security/loginsettings`

Get the login settings

Returns the portal login settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Login settings | [**LoginSettingsWrapper**](#model-loginsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**LoginSettingsWrapper**](#model-loginsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### setDefaultLoginSettings

> LoginSettingsWrapper setDefaultLoginSettings()

`DELETE /api/2.0/settings/security/loginsettings`

Reset the login settings

Resets the portal login settings to default.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Login settings | [**LoginSettingsWrapper**](#model-loginsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**LoginSettingsWrapper**](#model-loginsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateLoginSettings

> LoginSettingsWrapper updateLoginSettings(LoginSettingsRequestDto)

`PUT /api/2.0/settings/security/loginsettings`

Update the login settings

Updates the login settings with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **LoginSettingsRequestDto** | body | [**LoginSettingsRequestDto**](#model-loginsettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated login settings | [**LoginSettingsWrapper**](#model-loginsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**LoginSettingsWrapper**](#model-loginsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsMessagesApi

### enableAdminMessageSettings

> StringWrapper enableAdminMessageSettings(TurnOnAdminMessageSettingsRequestDto)

`POST /api/2.0/settings/messagesettings`

Enable the administrator message settings

Displays the contact form on the Sign In page, allowing users to send a message to the DocSpace administrator in case they encounter any issues while accessing DocSpace.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TurnOnAdminMessageSettingsRequestDto** | body | [**TurnOnAdminMessageSettingsRequestDto**](#model-turnonadminmessagesettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about the result of saving new settings | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendAdminMail

> StringWrapper sendAdminMail(AdminMessageSettingsRequestsDto)

`POST /api/2.0/settings/sendadmmail`

Send a message to the administrator

Sends a message to the administrator email when unauthorized users encounter issues accessing DocSpace.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AdminMessageSettingsRequestsDto** | body | [**AdminMessageSettingsRequestsDto**](#model-adminmessagesettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about the result of sending a message | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email or message text is empty | - | - |
| **429** | Request limit is exceeded | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendJoinInviteMail

> StringWrapper sendJoinInviteMail(AdminMessageBaseSettingsRequestsDto)

`POST /api/2.0/settings/sendjoininvite`

Sends an invitation email

Sends an invitation email with a link to the DocSpace.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AdminMessageBaseSettingsRequestsDto** | body | [**AdminMessageBaseSettingsRequestsDto**](#model-adminmessagebasesettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about sending a link to confirm joining the DocSpace | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect email or email already exists | - | - |
| **403** | No permissions to perform this action | - | - |
| **429** | Request limit is exceeded | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsNotificationsApi

### getNotificationChannels

> NotificationChannelStatusWrapper getNotificationChannels()

`GET /api/2.0/settings/notification/channels`

Get notification channels

Returns a list of notification channels.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Notification settings | [**NotificationChannelStatusWrapper**](#model-notificationchannelstatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NotificationChannelStatusWrapper**](#model-notificationchannelstatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getNotificationSettings

> NotificationSettingsWrapper getNotificationSettings(type)

`GET /api/2.0/settings/notification/{type}`

Check notification availability

Checks if the notification type specified in the request is enabled or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **type** | path | **NotificationType** | The type of notification to query, specified in the route. | [required] [example: 0] [enum: 0, 1, 2, 3] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Notification settings | [**NotificationSettingsWrapper**](#model-notificationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NotificationSettingsWrapper**](#model-notificationsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomsNotificationSettings

> RoomsNotificationSettingsWrapper getRoomsNotificationSettings()

`GET /api/2.0/settings/notification/rooms`

Get room notification settings

Returns a list of rooms with the disabled notifications.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room notification settings | [**RoomsNotificationSettingsWrapper**](#model-roomsnotificationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomsNotificationSettingsWrapper**](#model-roomsnotificationsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### setNotificationSettings

> NotificationSettingsWrapper setNotificationSettings(NotificationSettingsRequestsDto)

`POST /api/2.0/settings/notification`

Enable notifications

Enables the notification type specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **NotificationSettingsRequestsDto** | body | [**NotificationSettingsRequestsDto**](#model-notificationsettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Notification settings | [**NotificationSettingsWrapper**](#model-notificationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NotificationSettingsWrapper**](#model-notificationsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setRoomsNotificationStatus

> RoomsNotificationSettingsWrapper setRoomsNotificationStatus(RoomsNotificationsSettingsRequestDto)

`POST /api/2.0/settings/notification/rooms`

Set room notification status

Sets a notification status for a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **RoomsNotificationsSettingsRequestDto** | body | [**RoomsNotificationsSettingsRequestDto**](#model-roomsnotificationssettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room notification settings | [**RoomsNotificationSettingsWrapper**](#model-roomsnotificationsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomsNotificationSettingsWrapper**](#model-roomsnotificationsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsOwnerApi

### sendOwnerChangeInstructions

> OwnerChangeInstructionsWrapper sendOwnerChangeInstructions(OwnerIdSettingsRequestDto)

`POST /api/2.0/settings/owner`

Send the owner change instructions

Sends the instructions to change the DocSpace owner.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **OwnerIdSettingsRequestDto** | body | [**OwnerIdSettingsRequestDto**](#model-owneridsettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Message about changing the portal owner | [**OwnerChangeInstructionsWrapper**](#model-ownerchangeinstructionswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Owner&#39;s email is not activated | - | - |
| **403** | Collaborator can not be an owner | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**OwnerChangeInstructionsWrapper**](#model-ownerchangeinstructionswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updatePortalOwner

> updatePortalOwner(OwnerIdSettingsRequestDto)

`PUT /api/2.0/settings/owner`

Update the portal owner

Updates the current portal owner with a new one specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **OwnerIdSettingsRequestDto** | body | [**OwnerIdSettingsRequestDto**](#model-owneridsettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | The user could not be found | - | - |
| **409** |  | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

## SettingsQuotaApi

### getUserQuotaSettings

> TenantUserQuotaSettingsWrapper getUserQuotaSettings()

`GET /api/2.0/settings/userquotasettings`

Get the user quota settings

Returns the user quota settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**TenantUserQuotaSettingsWrapper**](#model-tenantuserquotasettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantUserQuotaSettingsWrapper**](#model-tenantuserquotasettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveAiAgentQuotaSettings

> TenantAiAgentQuotaSettingsWrapper saveAiAgentQuotaSettings(QuotaSettingsRequestsDto)

`POST /api/2.0/settings/aiagentquotasettings`

Save the AI Agent quota settings

Saves the AI Agent quota settings specified in the request to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **QuotaSettingsRequestsDto** | body | [**QuotaSettingsRequestsDto**](#model-quotasettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Tenant AI Agent quota settings | [**TenantAiAgentQuotaSettingsWrapper**](#model-tenantaiagentquotasettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantAiAgentQuotaSettingsWrapper**](#model-tenantaiagentquotasettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveRoomQuotaSettings

> TenantRoomQuotaSettingsWrapper saveRoomQuotaSettings(QuotaSettingsRequestsDto)

`POST /api/2.0/settings/roomquotasettings`

Save the room quota settings

Saves the room quota settings specified in the request to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **QuotaSettingsRequestsDto** | body | [**QuotaSettingsRequestsDto**](#model-quotasettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Tenant room quota settings | [**TenantRoomQuotaSettingsWrapper**](#model-tenantroomquotasettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantRoomQuotaSettingsWrapper**](#model-tenantroomquotasettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setTenantQuotaSettings

> TenantQuotaSettingsWrapper setTenantQuotaSettings(TenantQuotaSettingsRequestsDto)

`PUT /api/2.0/settings/tenantquotasettings`

Save the tenant quota settings

Saves the tenant quota settings specified in the request to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TenantQuotaSettingsRequestsDto** | body | [**TenantQuotaSettingsRequestsDto**](#model-tenantquotasettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Tenant quota settings | [**TenantQuotaSettingsWrapper**](#model-tenantquotasettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **405** | Not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TenantQuotaSettingsWrapper**](#model-tenantquotasettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsRebrandingApi

### deleteAdditionalWhiteLabelSettings

> AdditionalWhiteLabelSettingsWrapper deleteAdditionalWhiteLabelSettings()

`DELETE /api/2.0/settings/rebranding/additional`

Delete the additional white label settings

Deletes the additional white label settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Default additional white label settings | [**AdditionalWhiteLabelSettingsWrapper**](#model-additionalwhitelabelsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AdditionalWhiteLabelSettingsWrapper**](#model-additionalwhitelabelsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteCompanyWhiteLabelSettings

> CompanyWhiteLabelSettingsWrapper deleteCompanyWhiteLabelSettings()

`DELETE /api/2.0/settings/rebranding/company`

Delete the company white label settings

Deletes the company white label settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Default company white label settings | [**CompanyWhiteLabelSettingsWrapper**](#model-companywhitelabelsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CompanyWhiteLabelSettingsWrapper**](#model-companywhitelabelsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAdditionalWhiteLabelSettings

> AdditionalWhiteLabelSettingsWrapper getAdditionalWhiteLabelSettings()

`GET /api/2.0/settings/rebranding/additional`

Get the additional white label settings

Returns the additional white label settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Additional white label settings | [**AdditionalWhiteLabelSettingsWrapper**](#model-additionalwhitelabelsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AdditionalWhiteLabelSettingsWrapper**](#model-additionalwhitelabelsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCompanyWhiteLabelSettings

> CompanyWhiteLabelSettingsWrapper getCompanyWhiteLabelSettings()

`GET /api/2.0/settings/rebranding/company`

Get the company white label settings

Returns the company white label settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Company white label settings | [**CompanyWhiteLabelSettingsWrapper**](#model-companywhitelabelsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CompanyWhiteLabelSettingsWrapper**](#model-companywhitelabelsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getEnableWhitelabel

> BooleanWrapper getEnableWhitelabel()

`GET /api/2.0/settings/enablewhitelabel`

Check the white label availability

Checks if the white label is enabled or not.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the white label is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getIsDefaultWhiteLabelLogoText

> IsDefaultWhiteLabelLogosWrapper getIsDefaultWhiteLabelLogoText(IsDark, IsDefault)

`GET /api/2.0/settings/whitelabel/logotext/isdefault`

Check the default white label logo text

Specifies if the white label logo text is default or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Request properties of white label logos | [**IsDefaultWhiteLabelLogosWrapper**](#model-isdefaultwhitelabellogoswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**IsDefaultWhiteLabelLogosWrapper**](#model-isdefaultwhitelabellogoswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getIsDefaultWhiteLabelLogos

> IsDefaultWhiteLabelLogosArrayWrapper getIsDefaultWhiteLabelLogos(IsDark, IsDefault)

`GET /api/2.0/settings/whitelabel/logos/isdefault`

Check the default white label logos

Specifies if the white label logos are default or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Request properties of white label logos | [**IsDefaultWhiteLabelLogosArrayWrapper**](#model-isdefaultwhitelabellogosarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**IsDefaultWhiteLabelLogosArrayWrapper**](#model-isdefaultwhitelabellogosarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getLicensorData

> CompanyWhiteLabelSettingsArrayWrapper getLicensorData()

`GET /api/2.0/settings/companywhitelabel`

Get the licensor data

Returns the licensor data.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of company white label settings | [**CompanyWhiteLabelSettingsArrayWrapper**](#model-companywhitelabelsettingsarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CompanyWhiteLabelSettingsArrayWrapper**](#model-companywhitelabelsettingsarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWhiteLabelLogoText

> StringWrapper getWhiteLabelLogoText(IsDark, IsDefault)

`GET /api/2.0/settings/whitelabel/logotext`

Get the white label logo text

Returns the white label logo text.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Logo text | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWhiteLabelLogos

> WhiteLabelItemArrayWrapper getWhiteLabelLogos(IsDark, IsDefault)

`GET /api/2.0/settings/whitelabel/logos`

Get the white label logos

Returns the white label logos.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | White label logos | [**WhiteLabelItemArrayWrapper**](#model-whitelabelitemarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WhiteLabelItemArrayWrapper**](#model-whitelabelitemarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### restoreWhiteLabelLogoText

> BooleanWrapper restoreWhiteLabelLogoText(IsDark, IsDefault)

`PUT /api/2.0/settings/whitelabel/logotext/restore`

Restore the white label logo text

Restores the white label logo text.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### restoreWhiteLabelLogos

> BooleanWrapper restoreWhiteLabelLogos(IsDark, IsDefault)

`PUT /api/2.0/settings/whitelabel/logos/restore`

Restore the white label logos

Restores the white label logos.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveAdditionalWhiteLabelSettings

> BooleanWrapper saveAdditionalWhiteLabelSettings(AdditionalWhiteLabelSettingsWrapper)

`POST /api/2.0/settings/rebranding/additional`

Save the additional white label settings

Saves the additional white label settings specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AdditionalWhiteLabelSettingsWrapper** | body | [**AdditionalWhiteLabelSettingsWrapper**](#model-additionalwhitelabelsettingswrapper) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Settings is empty | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveCompanyWhiteLabelSettings

> BooleanWrapper saveCompanyWhiteLabelSettings(CompanyWhiteLabelSettingsWrapper)

`POST /api/2.0/settings/rebranding/company`

Save the company white label settings

Saves the company white label settings specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CompanyWhiteLabelSettingsWrapper** | body | [**CompanyWhiteLabelSettingsWrapper**](#model-companywhitelabelsettingswrapper) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Argument is empty or invalid | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveWhiteLabelLogoText

> BooleanWrapper saveWhiteLabelLogoText(IsDark, IsDefault, WhiteLabelRequestsDto)

`POST /api/2.0/settings/whitelabel/logotext/save`

Save the white label logo text settings

Saves the white label logo text specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |
| **WhiteLabelRequestsDto** | body | [**WhiteLabelRequestsDto**](#model-whitelabelrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is sucessful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveWhiteLabelSettings

> BooleanWrapper saveWhiteLabelSettings(IsDark, IsDefault, WhiteLabelRequestsDto)

`POST /api/2.0/settings/whitelabel/logos/save`

Save the white label logos

Saves the white label logos specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |
| **WhiteLabelRequestsDto** | body | [**WhiteLabelRequestsDto**](#model-whitelabelrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is sucessful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveWhiteLabelSettingsFromFiles

> BooleanWrapper saveWhiteLabelSettingsFromFiles(IsDark, IsDefault)

`POST /api/2.0/settings/whitelabel/logos/savefromfiles`

Save the white label logos from files

Saves the white label logos from files.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **IsDark** | query | **Boolean** | Specifies if the white label logo is for the dark theme or not. | [optional] [example: true] |
| **IsDefault** | query | **Boolean** | Specifies if the logo is for a default tenant or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is sucessful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **409** | No input files | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SettingsSSOApi

### getDefaultSsoSettingsV2

> SsoSettingsV2Wrapper getDefaultSsoSettingsV2()

`GET /api/2.0/settings/ssov2/default`

Get the default SSO settings

Returns the default portal SSO settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Default SSO settings | [**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSsoSettingsV2

> SsoSettingsV2Wrapper getSsoSettingsV2()

`GET /api/2.0/settings/ssov2`

Get the SSO settings

Returns the current portal SSO settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | SSO settings | [**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSsoSettingsV2Constants

> ObjectWrapper getSsoSettingsV2Constants()

`GET /api/2.0/settings/ssov2/constants`

Get the SSO settings constants

Returns the SSO settings constants.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The SSO settings constants: SSO name ID format type, SSO binding type, SSO signing algorithm type, SSO SP certificate action type, SSO IDP certificate action type | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### resetSsoSettingsV2

> SsoSettingsV2Wrapper resetSsoSettingsV2()

`DELETE /api/2.0/settings/ssov2`

Reset the SSO settings

Resets the SSO settings of the current portal.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Default SSO settings | [**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveSsoSettingsV2

> SsoSettingsV2Wrapper saveSsoSettingsV2(SsoSettingsRequestsDto)

`POST /api/2.0/settings/ssov2`

Save the SSO settings

Saves the SSO settings for the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SsoSettingsRequestsDto** | body | [**SsoSettingsRequestsDto**](#model-ssosettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | SSO settings | [**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Settings could not be null | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SsoSettingsV2Wrapper**](#model-ssosettingsv2wrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsSecurityApi

### getEnabledModules

> ObjectWrapper getEnabledModules()

`GET /api/2.0/settings/security/modules`

Get the enabled modules

Returns a list of all the enabled modules.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of enabled modules | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getIsProductAdministrator

> ProductAdministratorWrapper getIsProductAdministrator(productid, userid)

`GET /api/2.0/settings/security/administrator`

Check a product administrator

Checks if the selected user is an administrator of a product with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **productid** | query | **UUID** (uuid) | The ID of the product extracted from the query parameters. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **userid** | query | **UUID** (uuid) | The user ID extracted from the query parameters. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Object with the user security information: product ID, user ID, administrator or not | [**ProductAdministratorWrapper**](#model-productadministratorwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ProductAdministratorWrapper**](#model-productadministratorwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPasswordSettings

> PasswordSettingsWrapper getPasswordSettings()

`GET /api/2.0/settings/security/password`

Get the password settings

Returns the portal password settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Password settings | [**PasswordSettingsWrapper**](#model-passwordsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**PasswordSettingsWrapper**](#model-passwordsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getProductAdministrators

> EmployeeArrayWrapper getProductAdministrators(productid)

`GET /api/2.0/settings/security/administrator/{productid}`

Get the product administrators

Returns a list of all the administrators of a product with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **productid** | path | **UUID** (uuid) | The ID of the product extracted from the route parameters. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of product administrators with the following parameters | [**EmployeeArrayWrapper**](#model-employeearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EmployeeArrayWrapper**](#model-employeearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWebItemSecurityInfo

> BooleanWrapper getWebItemSecurityInfo(id)

`GET /api/2.0/settings/security/{id}`

Get the module availability

Returns the availability of the module with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The ID extracted from the route parameters. | [required] [example: "1"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true - module is enabled, false - module is disabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWebItemSettingsSecurityInfo

> SecurityArrayWrapper getWebItemSettingsSecurityInfo(ids)

`GET /api/2.0/settings/security`

Get the security settings

Returns the security settings for the modules specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ids** | query | **List** | The list of module identifiers for which to retrieve the security settings. | [optional] [example: ["item1","item2"]] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Security settings | [**SecurityArrayWrapper**](#model-securityarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SecurityArrayWrapper**](#model-securityarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### setAccessToWebItems

> SecurityArrayWrapper setAccessToWebItems(WebItemsSecurityRequestsDto)

`PUT /api/2.0/settings/security/access`

Set the security settings to modules

Sets the security settings to the modules with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **WebItemsSecurityRequestsDto** | body | [**WebItemsSecurityRequestsDto**](#model-webitemssecurityrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Security settings | [**SecurityArrayWrapper**](#model-securityarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Security settings are disabled for an open portal | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SecurityArrayWrapper**](#model-securityarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setProductAdministrator

> ProductAdministratorWrapper setProductAdministrator(SecurityRequestsDto)

`PUT /api/2.0/settings/security/administrator`

Set a product administrator

Sets the selected user as an administrator of a product with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SecurityRequestsDto** | body | [**SecurityRequestsDto**](#model-securityrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Object with the user security information: product ID, user ID, administrator or not | [**ProductAdministratorWrapper**](#model-productadministratorwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **402** | Your pricing plan does not support this option | - | - |
| **403** | Only portal owner can set user as administrator | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ProductAdministratorWrapper**](#model-productadministratorwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setWebItemSecurity

> SecurityArrayWrapper setWebItemSecurity(WebItemSecurityRequestsDto)

`PUT /api/2.0/settings/security`

Set the module security settings

Sets the security settings to the module with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **WebItemSecurityRequestsDto** | body | [**WebItemSecurityRequestsDto**](#model-webitemsecurityrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Security settings | [**SecurityArrayWrapper**](#model-securityarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Security settings are disabled for an open portal | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SecurityArrayWrapper**](#model-securityarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updatePasswordSettings

> PasswordSettingsWrapper updatePasswordSettings(PasswordSettingsRequestsDto)

`PUT /api/2.0/settings/security/password`

Set the password settings

Sets the portal password settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **PasswordSettingsRequestsDto** | body | [**PasswordSettingsRequestsDto**](#model-passwordsettingsrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Password settings | [**PasswordSettingsWrapper**](#model-passwordsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | MinLength | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**PasswordSettingsWrapper**](#model-passwordsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsStatisticsApi

### getSpaceUsageStatistics

> UsageSpaceStatItemArrayWrapper getSpaceUsageStatistics(id)

`GET /api/2.0/settings/statistics/spaceusage/{id}`

Get the space usage statistics

Returns the space usage statistics for the module with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The ID extracted from the route parameters. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Module space usage statistics | [**UsageSpaceStatItemArrayWrapper**](#model-usagespacestatitemarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UsageSpaceStatItemArrayWrapper**](#model-usagespacestatitemarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SettingsStorageApi

### getAllBackupStorages

> StorageArrayWrapper getAllBackupStorages(Dump)

`GET /api/2.0/settings/storage/backup`

Get the backup storages

Returns a list of all the backup storages.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Dump** | query | **Boolean** | Indicates whether the operation should perform a dump of backup storage data.  This property is used as a parameter in backup-related API requests to specify  if additional details or data dumping is required during the process. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of the backup storages with the following parameters | [**StorageArrayWrapper**](#model-storagearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StorageArrayWrapper**](#model-storagearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAllCdnStorages

> StorageArrayWrapper getAllCdnStorages()

`GET /api/2.0/settings/storage/cdn`

Get the CDN storages

Returns a list of all the CDN storages.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of the CDN storages with the following parameters | [**StorageArrayWrapper**](#model-storagearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StorageArrayWrapper**](#model-storagearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAllStorages

> StorageArrayWrapper getAllStorages()

`GET /api/2.0/settings/storage`

Get storages

Returns a list of all the portal storages.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of storages with the following parameters | [**StorageArrayWrapper**](#model-storagearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StorageArrayWrapper**](#model-storagearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAmazonS3Regions

> ObjectWrapper getAmazonS3Regions()

`GET /api/2.0/settings/storage/s3/regions`

Get Amazon regions

Returns a list of all Amazon regions.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of the Amazon regions | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getStorageProgress

> DoubleWrapper getStorageProgress()

`GET /api/2.0/settings/storage/progress`

Get the storage progress

Returns the storage progress.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Storage progress | [**DoubleWrapper**](#model-doublewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DoubleWrapper**](#model-doublewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### resetCdnToDefault

> resetCdnToDefault()

`DELETE /api/2.0/settings/storage/cdn`

Reset the CDN storage settings

Resets the CDN storage settings to the default parameters.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### resetStorageToDefault

> resetStorageToDefault()

`DELETE /api/2.0/settings/storage`

Reset the storage settings

Resets the storage settings to the default parameters.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### updateCdnStorage

> CdnStorageSettingsWrapper updateCdnStorage(StorageRequestsDto)

`PUT /api/2.0/settings/storage/cdn`

Update the CDN storage

Updates the CDN storage with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **StorageRequestsDto** | body | [**StorageRequestsDto**](#model-storagerequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated CDN storage | [**CdnStorageSettingsWrapper**](#model-cdnstoragesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Module | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CdnStorageSettingsWrapper**](#model-cdnstoragesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateStorage

> StorageSettingsWrapper updateStorage(StorageRequestsDto)

`PUT /api/2.0/settings/storage`

Update a storage

Updates a storage with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **StorageRequestsDto** | body | [**StorageRequestsDto**](#model-storagerequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated storage settings | [**StorageSettingsWrapper**](#model-storagesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Module | - | - |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StorageSettingsWrapper**](#model-storagesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsTFASettingsApi

### getTfaAppCodes

> TfaAppCodeArrayWrapper getTfaAppCodes()

`GET /api/2.0/settings/tfaappcodes`

Get the TFA codes

Returns the two-factor authentication application codes.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of TFA application codes | [**TfaAppCodeArrayWrapper**](#model-tfaappcodearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | TFA application settings are not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TfaAppCodeArrayWrapper**](#model-tfaappcodearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTfaConfirmData

> TfaConfirmDataWrapper getTfaConfirmData()

`GET /api/2.0/settings/tfaapp/confirm`

Get TFA confirmation data

Returns the confirmation data for authorization via SMS or TFA application.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | TFA confirmation data | [**TfaConfirmDataWrapper**](#model-tfaconfirmdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TfaConfirmDataWrapper**](#model-tfaconfirmdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTfaSettings

> TfaSettingsArrayWrapper getTfaSettings()

`GET /api/2.0/settings/tfaapp`

Get the TFA settings

Returns the current two-factor authentication settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | TFA settings | [**TfaSettingsArrayWrapper**](#model-tfasettingsarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TfaSettingsArrayWrapper**](#model-tfasettingsarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### tfaAppGenerateSetupCode

> SetupCodeWrapper tfaAppGenerateSetupCode()

`GET /api/2.0/settings/tfaapp/setup`

Generate setup code

Generates the setup TFA code for the current user.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Setup code | [**SetupCodeWrapper**](#model-setupcodewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | TFA application settings are not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**SetupCodeWrapper**](#model-setupcodewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### tfaValidateAuthCode

> BooleanWrapper tfaValidateAuthCode(TfaValidateRequestsDto)

`POST /api/2.0/settings/tfaapp/validate`

Validate the TFA code

Validates the two-factor authentication code specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TfaValidateRequestsDto** | body | [**TfaValidateRequestsDto**](#model-tfavalidaterequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | True if the code is valid | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### unlinkTfaApp

> StringWrapper unlinkTfaApp(TfaRequestsDto)

`PUT /api/2.0/settings/tfaappnewapp`

Unlink the TFA application

Unlinks the current two-factor authentication application from the user account specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TfaRequestsDto** | body | [**TfaRequestsDto**](#model-tfarequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Login URL | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **405** | TFA application settings are not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateTfaAppCodes

> TfaAppCodeArrayWrapper updateTfaAppCodes()

`PUT /api/2.0/settings/tfaappnewcodes`

Update the TFA codes

Requests the new backup codes for the two-factor authentication application.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New backup codes | [**TfaAppCodeArrayWrapper**](#model-tfaappcodearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | TFA application settings are not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TfaAppCodeArrayWrapper**](#model-tfaappcodearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateTfaSettings

> BooleanWrapper updateTfaSettings(TfaRequestsDto)

`PUT /api/2.0/settings/tfaapp`

Update the TFA settings

Updates the two-factor authentication settings with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TfaRequestsDto** | body | [**TfaRequestsDto**](#model-tfarequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | True if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **405** | SMS settings are not available/TFA application settings are not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateTfaSettingsLink

> StringWrapper updateTfaSettingsLink(TfaRequestsDto)

`PUT /api/2.0/settings/tfaappwithlink`

Updates TFA settings

Updates TFA settings and returns the confirmation URL for authorization via SMS or TFA application.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TfaRequestsDto** | body | [**TfaRequestsDto**](#model-tfarequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | TFA confirmation URL | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **405** | SMS settings are not available/TFA application settings are not available | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsTelegramApi

### checkTelegram

> TelegramStatusWrapper checkTelegram()

`GET /api/2.0/settings/telegram/check`

Check the Telegram connection

Checks if the current user is connected to the Telegram Bot or not.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Status if user is linked or not | [**TelegramStatusWrapper**](#model-telegramstatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**TelegramStatusWrapper**](#model-telegramstatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### linkTelegram

> StringWrapper linkTelegram()

`GET /api/2.0/settings/telegram/link`

Get the Telegram link

Returns a link that will connect the Telegram Bot to your account.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | A link to connect Telegram account | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### unlinkTelegram

> BooleanWrapper unlinkTelegram()

`DELETE /api/2.0/settings/telegram/link`

Unlink Telegram

Unlinks the Telegram Bot from your account.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | True if success | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

## SettingsWebhooksApi

### createWebhook

> WebhooksConfigWrapper createWebhook(CreateWebhooksConfigRequestsDto)

`POST /api/2.0/settings/webhook`

Create a webhook

Creates a new tenant webhook with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateWebhooksConfigRequestsDto** | body | [**CreateWebhooksConfigRequestsDto**](#model-createwebhooksconfigrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Tenant webhook with its config parameters | [**WebhooksConfigWrapper**](#model-webhooksconfigwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid or empty parameters | - | - |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksConfigWrapper**](#model-webhooksconfigwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### enableWebhook

> WebhooksConfigWrapper enableWebhook(UpdateWebhooksConfigRequestsDto)

`PUT /api/2.0/settings/webhook/enable`

Enable a webhook

Enables or disables a tenant webhook with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateWebhooksConfigRequestsDto** | body | [**UpdateWebhooksConfigRequestsDto**](#model-updatewebhooksconfigrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Enable or disable tenant webhook | [**WebhooksConfigWrapper**](#model-webhooksconfigwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid or empty parameters | - | - |
| **403** | Access denied | - | - |
| **404** | Item not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksConfigWrapper**](#model-webhooksconfigwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getTenantWebhooks

> WebhooksConfigWithStatusArrayWrapper getTenantWebhooks()

`GET /api/2.0/settings/webhook`

Get webhooks

Returns a list of the tenant webhooks.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of tenant webhooks with their config parameters | [**WebhooksConfigWithStatusArrayWrapper**](#model-webhooksconfigwithstatusarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksConfigWithStatusArrayWrapper**](#model-webhooksconfigwithstatusarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWebhookTriggers

> WebhookTriggerArrayWrapper getWebhookTriggers()

`GET /api/2.0/settings/webhook/triggers`

Get webhook triggers

Returns a list of triggers for a webhook with their availability for the current user.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of triggers with availability for the current user | [**WebhookTriggerArrayWrapper**](#model-webhooktriggerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhookTriggerArrayWrapper**](#model-webhooktriggerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWebhooksLogs

> WebhooksLogArrayWrapper getWebhooksLogs(deliveryFrom, deliveryTo, hookUri, configId, eventId, groupStatus, userId, trigger, count, startIndex)

`GET /api/2.0/settings/webhooks/log`

Get webhook logs

Returns the logs of the webhook activities.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **deliveryFrom** | query | **Date** (date-time) | The delivery start time for filtering webhook logs. | [optional] [example: "2024-01-15T10:30:00Z"] |
| **deliveryTo** | query | **Date** (date-time) | The delivery end time for filtering webhook logs. | [optional] [example: 2024-01-15T10:30:00Z] |
| **hookUri** | query | **String** | The destination URL where webhooks are delivered. | [optional] [example: "https://example.com/webhook"] |
| **configId** | query | **Integer** (int32) | The webhook configuration identifier. | [optional] [example: 1] |
| **eventId** | query | **Integer** (int32) | The unique identifier of the event that triggered the webhook. | [optional] [example: 1] |
| **groupStatus** | query | **WebhookGroupStatus** | The status of the webhook delivery group. | [optional] [example: NotSent] [enum: 0, 1, 2, 4, 8, 16] |
| **userId** | query | **UUID** (uuid) | The identifier of the user associated with the webhook event. | [optional] [example: "{}"] |
| **trigger** | query | **WebhookTrigger** | The type of event that triggered the webhook. | [optional] [example: 0] [enum: 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824] |
| **count** | query | **Integer** (int32) | The maximum number of webhook log records to return in the query response. | [optional] [example: 1] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | Specifies the starting index for retrieving webhook logs.  Used for pagination in the webhook delivery log queries. | [optional] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Logs of the webhook activities | [**WebhooksLogArrayWrapper**](#model-webhookslogarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksLogArrayWrapper**](#model-webhookslogarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### removeWebhook

> WebhooksConfigWrapper removeWebhook(id)

`DELETE /api/2.0/settings/webhook/{id}`

Remove a webhook

Removes a tenant webhook with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The ID extracted from the route parameters. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Tenant webhook with its config parameters | [**WebhooksConfigWrapper**](#model-webhooksconfigwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **404** | Item not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksConfigWrapper**](#model-webhooksconfigwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### retryWebhook

> WebhooksLogWrapper retryWebhook(id)

`PUT /api/2.0/settings/webhook/{id}/retry`

Retry a webhook

Retries a webhook with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The ID extracted from the route parameters. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Logs of the webhook activities | [**WebhooksLogWrapper**](#model-webhookslogwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Id incorrect | - | - |
| **403** | Access denied | - | - |
| **404** | Item not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksLogWrapper**](#model-webhookslogwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### retryWebhooks

> WebhooksLogArrayWrapper retryWebhooks(WebhookRetryRequestsDto)

`PUT /api/2.0/settings/webhook/retry`

Retry webhooks

Retries all the webhooks with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **WebhookRetryRequestsDto** | body | [**WebhookRetryRequestsDto**](#model-webhookretryrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Logs of the webhook activities | [**WebhooksLogArrayWrapper**](#model-webhookslogarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Access denied | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksLogArrayWrapper**](#model-webhookslogarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateWebhook

> WebhooksConfigWrapper updateWebhook(UpdateWebhooksConfigRequestsDto)

`PUT /api/2.0/settings/webhook`

Update a webhook

Updates a tenant webhook with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateWebhooksConfigRequestsDto** | body | [**UpdateWebhooksConfigRequestsDto**](#model-updatewebhooksconfigrequestsdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated tenant webhook with its config parameters | [**WebhooksConfigWrapper**](#model-webhooksconfigwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid or empty parameters | - | - |
| **403** | Access denied | - | - |
| **404** | Item not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebhooksConfigWrapper**](#model-webhooksconfigwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## SettingsWebpluginsApi

### addWebPluginFromFile

> WebPluginWrapper addWebPluginFromFile(system)

`POST /api/2.0/settings/webplugins`

Add a web plugin

Adds a web plugin from a file to the current portal.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **system** | query | **Boolean** | Specifies whether to load the system plugins or not. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Web plugin | [**WebPluginWrapper**](#model-webpluginwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | bad request | - | - |
| **403** | Plugins disabled | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebPluginWrapper**](#model-webpluginwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteWebPlugin

> deleteWebPlugin(name)

`DELETE /api/2.0/settings/webplugins/{name}`

Delete a web plugin

Deletes a web plugin by the name specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **name** | path | **String** | The web plugin name. | [required] [example: example-plugin] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Plugins disabled | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### getWebPlugin

> WebPluginWrapper getWebPlugin(name)

`GET /api/2.0/settings/webplugins/{name}`

Get a web plugin by name

Returns a web plugin by the name specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **name** | path | **String** | The web plugin name. | [required] [example: "example-plugin"] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Web plugin | [**WebPluginWrapper**](#model-webpluginwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Plugins disabled | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebPluginWrapper**](#model-webpluginwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getWebPlugins

> WebPluginArrayWrapper getWebPlugins(enabled)

`GET /api/2.0/settings/webplugins`

Get web plugins

Returns the portal web plugins.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **enabled** | query | **Boolean** | The optional filter for the plugin enabled state. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Web plugin | [**WebPluginArrayWrapper**](#model-webpluginarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Plugins disabled | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**WebPluginArrayWrapper**](#model-webpluginarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateWebPlugin

> updateWebPlugin(name, WebPluginRequests)

`PUT /api/2.0/settings/webplugins/{name}`

Update a web plugin

Updates a web plugin with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **name** | path | **String** | The web plugin name. | [required] [example: example-plugin] |
| **WebPluginRequests** | body | [**WebPluginRequests**](#model-webpluginrequests) | The configuration settings for the web plugin instance. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | Plugins disabled | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

## ThirdPartyApi

### getThirdPartyCode

> ObjectWrapper getThirdPartyCode(provider)

`GET /api/2.0/thirdparty/{provider}`

Get the code request

Returns a request to get the confirmation code from URL.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **provider** | path | **LoginProvider** | The identity provider used for authentication. | [required] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Code request | [**ObjectWrapper**](#model-objectwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectWrapper**](#model-objectwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json


## Models


### Model ActionType
[0 - None, 1 - Create, 2 - Update, 3 - Delete, 4 - Link, 5 - Unlink, 6 - Attach, 7 - Detach, 8 - Send, 9 - Import, 10 - Export, 11 - Update access, 12 - Download, 13 - Upload, 14 - Copy, 15 - Move, 16 - Reassigns, 17 - Follow, 18 - Unfollow, 19 - Logout]


### Model ActiveConnectionsDto
The active connections parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **loginEvent** | **Integer** (int32) | The login event. | [required] [example: 1] |
| **items** | [**List**](#model-activeconnectionsitemdto) | The list of active connection items. | [optional] [example: [{id=conn1, ip=192.168.1.1}]] [nullable] |


### Model ActiveConnectionsItemDto
The active connection item parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The active connection ID. | [required] [example: 1] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [required] [example: 1] |
| **userId** | **UUID** (uuid) | The user ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **mobile** | **Boolean** | Specifies if the active connection has a mobile phone or not. | [optional] [example: true] |
| **ip** | **String** | The IP address of the active connection. | [optional] [example: 192.0.2.1] [nullable] |
| **country** | **String** | The active connection country. | [optional] [example: United States] [nullable] |
| **city** | **String** | The active connection city. | [optional] [example: New York] [nullable] |
| **browser** | **String** | The active connection browser. | [optional] [example: Chrome 120.0] [nullable] |
| **platform** | **String** | The active connection platform. | [optional] [example: Windows] [nullable] |
| **date** | [**ApiDateTime**](#model-apidatetime) | The API date and time parameters. | [optional] |
| **page** | **String** | The active connection page. | [optional] [example: /rooms/shared] [nullable] |


### Model ActiveConnectionsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ActiveConnectionsDto**](#model-activeconnectionsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ActiveServiceArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-activeservicedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ActiveServiceDto
Represents an active wallet service (quota) of the current portal.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **service** | **String** | The name of the service. | [optional] [example: disk-storage] [nullable] |
| **serviceUnit** | **String** | The unit of measurement for the service. | [optional] [example: GB] [nullable] |
| **subscription** | **Boolean** | Indicates whether the service is subscription-based. | [optional] [example: true] |
| **title** | **String** | The title of the service. | [optional] [example: Additional disk storage] [nullable] |
| **limit** | **Integer** (int32) | The service limit. Populated only for the subscription-based services. | [optional] [example: 500] [nullable] |
| **used** | **Integer** (int32) | The current service usage. Populated only for the subscription-based services. | [optional] [example: 320] [nullable] |


### Model AdditionalWhiteLabelSettings
The additional white label settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **startDocsEnabled** | **Boolean** | Specifies if the sample documents are displayed or hidden. | [optional] [example: true] |
| **helpCenterEnabled** | **Boolean** | Specifies if the Help Center link is available or not. | [optional] [example: true] |
| **feedbackAndSupportEnabled** | **Boolean** | Specifies if the Feedback &amp; Support link is available or not. | [optional] [example: true] |
| **userForumEnabled** | **Boolean** | Specifies if the user forum is available or not. | [optional] [example: true] |
| **videoGuidesEnabled** | **Boolean** | Specifies if the Video Guides link is available or not. | [optional] [example: true] |
| **licenseAgreementsEnabled** | **Boolean** | Specifies if the License Agreements link is available or not. | [optional] [example: true] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model AdditionalWhiteLabelSettingsWrapper
The additional white label settings wrapper.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **settings** | [**AdditionalWhiteLabelSettings**](#model-additionalwhitelabelsettings) | The additional white label settings. | [optional] |


### Model AdminMessageBaseSettingsRequestsDto
The request parameters for the administrator message configuration.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address used for sending administrator messages. | [required] [example: admin@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **culture** | **String** | The locale identifier for message localization. | [optional] [example: en-US] [nullable] |


### Model AdminMessageSettingsRequestsDto
The request parameters for configuring the administrator message content.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **message** | **String** | The content of the administrator message to be sent. | [required] [example: Hello, this is a test message from the administrator.] [minLength: 0] [maxLength: 255] [nullable] |
| **email** | **String** (email) | Email | [required] [example: user@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **culture** | **String** | Culture | [optional] [example: en-US] [nullable] |
| **recaptchaType** | [**RecaptchaType**](#model-recaptchatype) | The type of CAPTCHA validation used. | [optional] [enum: 0, 1, 2, 3] |
| **recaptchaResponse** | **String** | The user&#39;s response to the CAPTCHA challenge. | [optional] [example: 03AGdBq24PBCbwiDRaS...] [nullable] |


### Model AiChatModelPricing

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** |  | [required] [nullable] |
| **alias** | **String** |  | [optional] [nullable] |
| **ownedBy** | **String** |  | [optional] [nullable] |
| **provider** | **String** |  | [optional] [nullable] |
| **link** | **String** |  | [optional] [nullable] |
| **price** | [**AiChatPrice**](#model-aichatprice) |  | [required] |


### Model AiChatPrice

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **prompt** | **Double** (double) |  | [optional] |
| **completion** | **Double** (double) |  | [optional] |


### Model AiEmbeddingModelPricing

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** |  | [required] [nullable] |
| **alias** | **String** |  | [optional] [nullable] |
| **ownedBy** | **String** |  | [optional] [nullable] |
| **provider** | **String** |  | [optional] [nullable] |
| **link** | **String** |  | [optional] [nullable] |
| **price** | [**AiEmbeddingPrice**](#model-aiembeddingprice) |  | [required] |


### Model AiEmbeddingPrice

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **prompt** | **Double** (double) |  | [optional] |


### Model AiImageModelPricing

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** |  | [required] [nullable] |
| **alias** | **String** |  | [optional] [nullable] |
| **ownedBy** | **String** |  | [optional] [nullable] |
| **provider** | **String** |  | [optional] [nullable] |
| **link** | **String** |  | [optional] [nullable] |
| **price** | [**AiImagePrice**](#model-aiimageprice) |  | [required] |


### Model AiImagePrice

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **prompt** | **Double** (double) |  | [optional] |
| **image** | **Double** (double) |  | [optional] |


### Model AiPricesResponse

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **chat** | [**List**](#model-aichatmodelpricing) |  | [required] [nullable] |
| **embedding** | [**List**](#model-aiembeddingmodelpricing) |  | [required] [nullable] |
| **image** | [**List**](#model-aiimagemodelpricing) |  | [required] [nullable] |
| **search** | [**List**](#model-aiwebsearchpricing) |  | [required] [nullable] |
| **currency** | [**CurrencyInfo**](#model-currencyinfo) |  | [required] |


### Model AiPricesResponseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**AiPricesResponse**](#model-aipricesresponse) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AiWebSearchPricing

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** |  | [optional] [nullable] |
| **provider** | **String** |  | [optional] [nullable] |
| **price** | **Double** (double) |  | [optional] |
| **link** | **String** |  | [optional] [nullable] |


### Model ApiDateTime
The API date and time parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **utcTime** | **Date** (date-time) | The time in UTC format. | [optional] [example: 2018-01-01T00:00:00.0000000Z] |
| **timeZoneOffset** | **String** (date-span) | The time zone offset. | [optional] [example: 00:00:00] |


### Model AppArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-appdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AppDto
The portal application information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The application identifier (stable slug). The client maps this to its title, description and icon. | [optional] [example: ai-rooms] [nullable] |
| **enabled** | **Boolean** | Whether the application is enabled for the current tenant. | [optional] [example: true] |
| **settings** | [**AppDto_settings**](#model-appdtosettings) |  | [optional] |


### Model AppDto.settings
Application-specific settings as a JSON document, or null if no overrides exist.


### Model AppWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**AppDto**](#model-appdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AuditEventArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-auditeventdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AuditEventDto
The audit event parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The audit event ID. | [optional] [example: 1] |
| **date** | [**ApiDateTime**](#model-apidatetime) | The API date and time parameters. | [optional] |
| **user** | **String** | The name of the user who triggered the audit event. | [optional] [example: John Doe] [nullable] |
| **userId** | **UUID** (uuid) | The ID of the user who triggered the audit event. | [optional] [example: 00000000-0000-0000-0000-000000000001] |
| **action** | **String** | The audit event action. | [optional] [example: User logged in] [nullable] |
| **actionId** | [**MessageAction**](#model-messageaction) | The event action ID. | [optional] [enum: 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 4000, 4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008, 4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037, 5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 5012, 5013, 5014, 5015, 5016, 5017, 5018, 5019, 5020, 5021, 5022, 5023, 5024, 5025, 5026, 5027, 5028, 5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038, 5039, 5040, 5041, 5042, 5043, 5044, 5045, 5046, 5047, 5048, 5049, 5050, 5053, 5054, 5055, 5056, 5057, 5058, 5059, 5060, 5061, 5062, 5063, 5064, 5065, 5066, 5068, 5069, 5070, 5071, 5072, 5073, 5074, 5075, 5076, 5077, 5078, 5079, 5080, 5081, 5082, 5083, 5084, 5085, 5086, 5087, 5088, 5089, 5090, 5091, 5092, 5093, 5094, 5095, 5096, 5097, 5098, 5099, 5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5112, 5113, 5114, 5115, 5116, 5117, 5118, 5119, 5120, 5121, 5122, 5123, 5124, 5125, 5126, 5127, 5128, 5129, 5130, 5131, 5132, 5133, 5150, 5151, 5152, 5153, 5154, 5155, 5156, 5157, 5158, 5159, 5160, 5201, 5202, 5203, 5204, 5205, 5206, 5501, 5502, 5503, 6000, 6001, 6002, 6003, 6004, 6005, 6006, 6007, 6008, 6009, 6010, 6011, 6012, 6013, 6014, 6015, 6016, 6017, 6018, 6019, 6020, 6021, 6022, 6023, 6024, 6025, 6026, 6027, 6028, 6029, 6030, 6031, 6032, 6033, 6034, 6035, 6036, 6037, 6038, 6039, 6040, 6041, 6042, 6043, 6044, 6045, 6046, 6047, 6048, 6049, 6050, 6051, 6052, 6053, 6054, 6055, 6056, 6057, 6058, 6059, 6060, 6061, 6062, 6063, 6064, 6065, 6066, 6067, 6068, 6069, 6070, 6071, 6072, 6073, 6074, 6075, 6076, 6077, 6078, 6079, 6080, 6081, 6082, 6083, 6084, 6085, 6086, 6087, 6088, 6089, 6090, 6091, 6092, 6093, 6094, 6095, 6096, 6097, 6098, 6099, 6100, 6101, 6102, 7000, 7001, 7002, 7003, 7004, 9901, 9902, 9903, 9904, 9905, 9906, 9907, 9908, 9909, -1] |
| **ip** | **String** | The audit event IP. | [optional] [example: 192.0.2.1] [nullable] |
| **country** | **String** | The audit event country. | [optional] [example: United States] [nullable] |
| **city** | **String** | The audit event city. | [optional] [example: New York] [nullable] |
| **browser** | **String** | The audit event browser. | [optional] [example: Chrome 120.0] [nullable] |
| **platform** | **String** | The audit event platform. | [optional] [example: Windows] [nullable] |
| **page** | **String** | The audit event page. | [optional] [example: /rooms/shared] [nullable] |
| **actionType** | [**ActionType**](#model-actiontype) | The type of action performed in the audit event (e.g., Create, Update, Delete). | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19] |
| **product** | [**ProductType**](#model-producttype) | The type of product related to the audit event. | [optional] [enum: 2, 3, 7, 8] |
| **location** | [**LocationType**](#model-locationtype) | The location where the audit event occurred. | [optional] [enum: 0, 1, 2, 3, 27, 29, 30, 31] |
| **target** | **List** | The list of target objects affected by the audit event (e.g., document ID, user account). | [optional] [example: [item1, item2]] [nullable] |
| **entries** | [**List**](#model-entrytype) | The list of audit entry types (e.g., Folder, User, File). | [optional] [example: [File, Folder]] [nullable] |
| **context** | **String** | The audit event context. | [optional] [example: Security settings updated] [nullable] |


### Model AuditReportFormat
[]


### Model AuthKey
The authorization key parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The authorization key name. | [required] [example: Auth-Key] [nullable] |
| **value** | **String** | The authorization key value. | [required] [example: abc123xyz456] [minLength: 0] [maxLength: 4000] [nullable] |
| **title** | **String** | The authorization key title. | [optional] [example: API key] [nullable] |
| **type** | **String** | The field type: text, password, select, toggle. | [optional] [nullable] |
| **options** | **List** | The list of options for select type fields. | [optional] [nullable] |
| **dependsOn** | **String** | The name of another key this field depends on for visibility. | [optional] [nullable] |
| **dependsOnValue** | **String** | The value of ASC.Web.Studio.UserControls.Management.AuthKey.DependsOn key that makes this field visible. | [optional] [nullable] |


### Model AuthRequestsDto
The parameters required for the user authentication requests.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userName** | **String** | The username or email used for authentication. | [optional] [example: user@example.com] [nullable] |
| **password** | **String** | The password in plain text for user authentication. | [optional] [example: SecurePassword123!] [nullable] |
| **passwordHash** | **String** | The hashed password for secure verification. | [optional] [example: 5f4dcc3b5aa765d61d8327deb882cf99] [nullable] |
| **provider** | **String** | The type of authentication provider (e.g., internal, Google, Azure). | [optional] [example: google] [nullable] |
| **accessToken** | **String** | The access token used for authentication with external providers. | [optional] [example: ya29.a0AfH6SMBx...] [nullable] |
| **serializedProfile** | **String** | The serialized user profile data, if applicable. | [optional] [example: {"name":"John Doe","email":"john@example.com"}] [nullable] |
| **codeOAuth** | **String** | The authorization code used for obtaining OAuth tokens. | [optional] [example: 4/0AY0e-g7...] [nullable] |
| **session** | **Boolean** | Specifies whether the authentication is session-based. | [optional] [example: true] |
| **confirmData** | [**ConfirmData**](#model-confirmdata) | The additional confirmation data required for authentication. | [optional] |
| **recaptchaType** | [**RecaptchaType**](#model-recaptchatype) | The type of CAPTCHA validation used. | [optional] [enum: 0, 1, 2, 3] |
| **recaptchaResponse** | **String** | The user&#39;s response to the CAPTCHA challenge. | [optional] [example: 03AGdBq25...] [nullable] |
| **culture** | **String** | The culture code for localization during authentication. | [optional] [example: en-US] [nullable] |


### Model AuthServiceRequestsArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-authservicerequestsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AuthServiceRequestsDto
The request parameters for handling the authorization service.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The name of the authorization service. | [optional] [example: google] [nullable] |
| **title** | **String** | The user-friendly display title of the authorization service. | [optional] [example: Google] [nullable] |
| **description** | **String** | The brief description of the authorization service. | [optional] [example: Google OAuth authentication] [nullable] |
| **instruction** | **String** | The detailed instructions for configuring or using the authorization service. | [optional] [example: Configure your Google OAuth credentials] [nullable] |
| **canSet** | **Boolean** | Specifies whether the authorization service can be configured by the user. | [optional] [example: true] |
| **paid** | **Boolean** | Specifies whether the authorization service is paid or not. | [optional] [example: false] |
| **props** | [**List**](#model-authkey) | The collection of authorization keys associated with the authorization service. | [optional] [example: [{name=key, value=value}]] [nullable] |


### Model AuthWithCodeRequestsDto
The parameters required for the user two-factor authentication requests.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userName** | **String** | The username or email used for authentication. | [optional] |
| **password** | **String** | The password in plain text for user authentication. | [optional] |
| **passwordHash** | **String** | The hashed password for secure verification. | [optional] |
| **provider** | **String** | The type of authentication provider (e.g., internal, Google, Azure). | [optional] |
| **accessToken** | **String** | The access token used for authentication with external providers. | [optional] |
| **serializedProfile** | **String** | The serialized user profile data, if applicable. | [optional] |
| **codeOAuth** | **String** | The authorization code used for obtaining OAuth tokens. | [optional] |
| **session** | **Boolean** | Specifies whether the authentication is session-based. | [optional] |
| **confirmData** | [**ConfirmData**](#model-confirmdata) | The additional confirmation data required for authentication. | [optional] |
| **recaptchaType** | [**RecaptchaType**](#model-recaptchatype) | The type of CAPTCHA validation used. | [optional] [enum: 0, 1, 2, 3] |
| **recaptchaResponse** | **String** | The user&#39;s response to the CAPTCHA challenge. | [optional] |
| **culture** | **String** | The culture code for localization during authentication. | [optional] |
| **code** | **String** | The code for two-factor authentication. | [optional] [nullable] |


### Model AuthenticationTokenDto
The authentication token parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **token** | **String** | The authentication token. | [optional] [example: abcde12345] [nullable] |
| **expires** | **Date** (date-time) | The token expiration time. | [optional] [example: 2024-01-15T10:30:00Z] |
| **sms** | **Boolean** | Specifies if the authentication code is sent by SMS or not. | [optional] [example: true] |
| **phoneNoise** | **String** | The phone number. | [optional] [example: +1***1234] [nullable] |
| **tfa** | **Boolean** | Specifies if the two-factor application is used or not. | [optional] [example: true] |
| **tfaKey** | **String** | The two-factor authentication key. | [optional] [example: JBSWY3DPEHPK3PXP] [nullable] |
| **confirmUrl** | **URI** (uri) | The confirmation email URL. | [optional] [example: https://example.com/confirm?token=abc123] [nullable] |


### Model AuthenticationTokenWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**AuthenticationTokenDto**](#model-authenticationtokendto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Balance
Represents a balance with an account number and a list of sub-accounts.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **accountNumber** | **Integer** (int32) | The account number. | [optional] [example: 12345] |
| **subAccountNumber** | **Integer** (int32) | The sub-account number. | [optional] [example: 12345] |
| **accountName** | **String** | The account name. | [optional] [example: account name] [nullable] |
| **accountCurrency** | **String** | The account currency. | [optional] [example: "USD"] [nullable] |
| **subAccounts** | [**List**](#model-subaccount) | A list of sub-accounts. | [optional] [example: [{currency=USD, amount=1500.75}]] [nullable] |
| **lastCredit** | [**TransactionInfo**](#model-transactioninfo) | The most recent credit transaction applied to the account. | [optional] |


### Model BalanceWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**Balance**](#model-balance) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BooleanWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Boolean** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CapabilitiesDto
The capabilities parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ldapEnabled** | **Boolean** | Specifies if the LDAP settings are enabled or not. | [required] [example: false] |
| **ldapDomain** | **String** | The LDAP domain. | [optional] [example: example.com] [nullable] |
| **providers** | **List** | The list of providers. | [required] [example: [google, facebook, microsoft]] [nullable] |
| **ssoLabel** | **String** | The SP login label. | [required] [example: Enterprise SSO] [nullable] |
| **oauthEnabled** | **Boolean** | Specifies if OAuth is enabled or not. | [required] [example: true] |
| **ssoUrl** | **URI** (uri) | The SSO URL. If this parameter is empty, then the SSO settings are disabled. | [required] [example: https://sso.example.com/login] [nullable] |
| **identityServerEnabled** | **Boolean** | Specifies if an identity server is enabled or not. | [required] [example: false] |


### Model CapabilitiesWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CapabilitiesDto**](#model-capabilitiesdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CdnStorageSettings

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **module** | **String** |  | [optional] [nullable] |
| **props** | **null** |  | [optional] |
| **lastModified** | **Date** (date-time) |  | [optional] |


### Model CdnStorageSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CdnStorageSettings**](#model-cdnstoragesettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ChangeWalletServiceStateRequestDto
The request parameters for changing the tenant wallet service state.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **service** | [**TenantWalletService**](#model-tenantwalletservice) | The wallet service type. | [optional] [enum: -18, -16, -15, -14, -13, -12, -11] |
| **enabled** | **Boolean** | Specifies whether the wallet service is enabled. | [optional] [example: true] |


### Model CompanyWhiteLabelSettings
The company white label settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **companyName** | **String** | The company name. | [optional] [example: ONLYOFFICE] [minLength: 0] [maxLength: 255] [nullable] |
| **site** | **URI** (uri) | The company site. | [optional] [example: https://www.onlyoffice.com] [minLength: 0] [maxLength: 255] [nullable] |
| **email** | **String** (email) | The company email address. | [optional] [example: support@onlyoffice.com] [minLength: 0] [maxLength: 255] [nullable] |
| **address** | **String** | The company address. | [optional] [example: Lubanas st. 125a-25] [minLength: 0] [maxLength: 255] [nullable] |
| **phone** | **String** (tel) | The company phone number. | [optional] [example: +7 843 2271372] [minLength: 0] [maxLength: 255] [nullable] |
| **IsLicensor** | **Boolean** | Specifies if a company is a licensor or not. | [optional] [example: true] |
| **hideAbout** | **Boolean** | Specifies if the About page is visible or not | [optional] [example: false] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model CompanyWhiteLabelSettingsArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-companywhitelabelsettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CompanyWhiteLabelSettingsWrapper
The company white label settings wrapper.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **settings** | [**CompanyWhiteLabelSettings**](#model-companywhitelabelsettings) | The company white label settings. | [optional] |


### Model ConfirmData
The additional confirmation data required for authentication.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address to confirm the user&#39;s identity. | [optional] [example: user@example.com] [nullable] |
| **first** | **Boolean** | Specifies whether this is the first access to the user&#39;s account. | [optional] [example: true] [nullable] |
| **key** | **String** | The unique confirmation key for validating user identity. | [optional] [example: abc123def456] [nullable] |


### Model ConfirmDto
The confirmation parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **result** | [**ValidationResult**](#model-validationresult) | The confirmation result. | [required] [enum: 0, 1, 2, 3, 4, 5, 6] |
| **roomId** | **String** | The confirmation room ID. | [optional] [example: 1] [nullable] |
| **title** | **String** | The confirmation title. | [optional] [example: Conference Room] [nullable] |
| **email** | **String** | The confirmation email. | [optional] [example: user@example.com] [nullable] |
| **isAgent** | **Boolean** | The confirmation is agent. | [optional] [example: true] |


### Model ConfirmType
[EmpInvite - Emp invite, LinkInvite - Link invite, PortalSuspend - Portal suspend, PortalContinue - Portal continue, PortalRemove - Portal remove, DnsChange - Dns change, PortalOwnerChange - Portal owner change, Activation - Activation, EmailChange - Email change, EmailActivation - Email activation, PasswordChange - Password change, ProfileRemove - Profile remove, PhoneActivation - Phone activation, PhoneAuth - Phone auth, Auth - Auth, TfaActivation - Tfa activation, TfaAuth - Tfa auth, Wizard - Wizard, GuestShareLink - Guest share link]


### Model ConfirmWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ConfirmDto**](#model-confirmdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ConnectionTestResult

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **success** | **Boolean** |  | [optional] |
| **error** | **String** |  | [optional] [nullable] |


### Model ConnectionTestResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ConnectionTestResult**](#model-connectiontestresult) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CookieSettingsDto
The cookie settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **lifeTime** | **Integer** (int32) | The cookie lifetime value in minutes. | [required] [example: 1440] |
| **enabled** | **Boolean** | Specifies if the cookie settings are enabled or not. | [required] [example: true] |


### Model CookieSettingsRequestsDto
The request parameters for managing cookie settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **lifeTime** | **Integer** (int32) | The cookie lifetime in minutes. | [optional] [example: 525600] |
| **enabled** | **Boolean** | Specifies whether the cookie settings are enabled or disabled. | [optional] [example: true] |


### Model CookieSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CookieSettingsDto**](#model-cookiesettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CreateWebhooksConfigRequestsDto
The request parameters for creating the webhook configuration.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The human-readable name of the webhook configuration. | [required] [example: Production Webhook] [minLength: 0] [maxLength: 50] |
| **uri** | **String** | The destination URL where the webhook events will be sent. | [required] [example: https://example.com/webhook] [minLength: 1] |
| **secretKey** | **String** | The webhook secret key used to sign the webhook payloads for the security verification. | [optional] [example: my-secret-key-123] [minLength: 0] [maxLength: 50] [nullable] |
| **enabled** | **Boolean** | Specifies whether the webhook configuration is active or not. | [optional] [example: true] |
| **ssl** | **Boolean** | Specifies whether the SSL certificate verification is required or not. | [optional] [example: true] |
| **triggers** | [**WebhookTrigger**](#model-webhooktrigger) | The webhook trigger type. | [optional] [enum: 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824] |
| **targetId** | **String** | Target ID | [optional] [example: 00000000-0000-0000-0000-000000000001] [minLength: 0] [maxLength: 255] [nullable] |


### Model CspDto
The CSP (Content Security Policy) parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **domains** | **List** | The list of CSP domains. | [required] [example: [https://example.com, https://cdn.example.com]] [nullable] |
| **header** | **String** | The CSP header. | [required] [example: default-src 'self'; script-src 'self' https://example.com] [nullable] |


### Model CspRequestsDto
The request parameters for configuring the Content Security Policy (CSP) settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **domains** | **List** | The collection of allowed domains in the Content Security Policy (CSP). | [optional] [example: [example.com, trusted-site.com]] [nullable] |


### Model CspWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CspDto**](#model-cspdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CultureSpecificExternalResource
The external resource parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **domain** | **String** | The external resource domain. | [optional] [example: example.com] [nullable] |
| **entries** | **null** | The external resource entries. | [optional] [example: {welcomeMessage=Welcome, logoutButton=Log out}] |


### Model CultureSpecificExternalResources
The external resources settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **api** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the product API. | [optional] |
| **common** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the common product information. | [optional] |
| **forum** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the forum. | [optional] |
| **helpcenter** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the Help Center. | [optional] |
| **integrations** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the product integrations. | [optional] |
| **site** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the product website. | [optional] |
| **socialNetworks** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the product social nerworks. | [optional] |
| **support** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the product support. | [optional] |
| **videoguides** | [**CultureSpecificExternalResource**](#model-culturespecificexternalresource) | The link to the video guides. | [optional] |


### Model CurrenciesArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-currenciesdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CurrenciesDto
The currencies parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **isoCountryCode** | **String** | The ISO country code. | [optional] [example: US] [nullable] |
| **isoCurrencySymbol** | **String** | The ISO currency symbol. | [optional] [example: USD] [nullable] |
| **currencyNativeName** | **String** | The currency native name. | [optional] [example: Example Name] [nullable] |


### Model CurrencyAmount

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **currency** | **String** | The three-character ISO 4217 currency symbol. | [optional] |
| **amount** | **Double** (double) | The amount in the specified currency. | [optional] |


### Model CurrencyCode

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **currency** | **String** | The three-character ISO 4217 currency symbol. | [optional] [example: "USD"] [nullable] |


### Model CurrencyInfo

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **code** | **String** |  | [required] [nullable] |
| **symbol** | **String** |  | [required] [nullable] |


### Model CurrentLicenseInfo
The current license information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **trial** | **Boolean** | Specifies whether the license is trial or not. | [required] [example: false] |
| **dueDate** | **Date** (date-time) | The date when the license expires. | [required] [example: 2025-06-15T10:30:00.0000000Z] |


### Model CustomColorThemesSettingsColorItem
The custom color theme color parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **accent** | **String** | The accent color. | [optional] [example: #4781D1] [nullable] |
| **buttons** | **String** | The button color. | [optional] [example: #5299E0] [nullable] |


### Model CustomColorThemesSettingsDto
The custom color themes settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **themes** | [**List**](#model-customcolorthemessettingsitem) | The list of the custom color themes. | [optional] [example: [{id=1, name=Custom Theme}]] [nullable] |
| **selected** | **Integer** (int32) | Specifies whether the custom color theme is selected. | [optional] [example: 1] |
| **limit** | **Integer** (int32) | The maximum number of the custom color themes. | [optional] [example: 1] |


### Model CustomColorThemesSettingsItem
The custom color theme settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The custom color theme ID. | [optional] [example: 1] |
| **name** | **String** | The custom color theme name. | [optional] [example: blue] [nullable] |
| **main** | [**CustomColorThemesSettingsColorItem**](#model-customcolorthemessettingscoloritem) | The custom color theme main colors. | [optional] |
| **text** | [**CustomColorThemesSettingsColorItem**](#model-customcolorthemessettingscoloritem) | The custom color theme text colors. | [optional] |


### Model CustomColorThemesSettingsRequestsDto
The request parameters for managing the portal theme settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **theme** | [**CustomColorThemesSettingsItem**](#model-customcolorthemessettingsitem) | The custom color theme settings. | [optional] |
| **selected** | **Integer** (int32) | Specifies the optional value indicating the selected custom color theme. | [optional] [example: 1] [nullable] |


### Model CustomColorThemesSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CustomColorThemesSettingsDto**](#model-customcolorthemessettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CustomerInfoDto
The customer information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **portalId** | **String** | The portal ID. | [optional] [example: portal-001] [nullable] |
| **paymentMethodStatus** | [**PaymentMethodStatus**](#model-paymentmethodstatus) | The payment method status. | [optional] [enum: 0, 1, 2] |
| **email** | **String** | The customer email address. | [optional] [example: user@example.com] [nullable] |
| **payer** | [**EmployeeDto**](#model-employeedto) | The user parameters. | [optional] |


### Model CustomerInfoWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CustomerInfoDto**](#model-customerinfodto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CustomerMonthlyUsageArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-customermonthlyusagedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CustomerMonthlyUsageDto
Aggregated customer spending for a single calendar month.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **year** | **Integer** (int32) | The calendar year. | [optional] [example: 2025] |
| **month** | **Integer** (int32) | The calendar month (1-12). | [optional] [example: 1] |
| **currency** | **String** | The three-character ISO 4217 currency symbol of the amounts. | [optional] [example: USD] [nullable] |
| **totalAmount** | **Double** (double) | The total amount charged across all services in this month. | [optional] [example: 199.98] |
| **operationCount** | **Integer** (int32) | The number of individual purchase operations in this month. | [optional] [example: 3] |


### Model CustomerMonthlyUsageReportRequestDto
The request parameters for generating a customer monthly usage report.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **startDate** | **Date** (date-time) | The report start date. | [optional] [example: 2025-01-01T00:00:00Z] [nullable] |
| **endDate** | **Date** (date-time) | The report end date. | [optional] [example: 2025-12-31T23:59:59Z] [nullable] |


### Model CustomerOperationsReportRequestDto
The request parameters for generating a report on client operations.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **serviceName** | **List** | The service name list. A single string is also accepted for backward compatibility. | [optional] [example: [backup]] [nullable] |
| **startDate** | **Date** (date-time) | The report start date. | [optional] [example: 2024-01-01T00:00:00Z] [nullable] |
| **endDate** | **Date** (date-time) | The report end date. | [optional] [example: 2024-01-31T23:59:59Z] [nullable] |
| **participantName** | **String** | The participant name. | [optional] [example: My Own Corporation] [nullable] |
| **credit** | **Boolean** | Specifies whether to include credit operations in the report. | [optional] [example: true] [nullable] |
| **debit** | **Boolean** | Specifies whether to include debit operations in the report. | [optional] [example: false] [nullable] |
| **type** | [**OperationType**](#model-operationtype) | The operation type to filter by. | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19] |
| **status** | [**OperationStatus**](#model-operationstatus) | The operation status to filter by. | [optional] [enum: 0, 1, 2, 3] |
| **orderBy** | **String** | The field to order by. | [optional] [example: StartDate] [nullable] |
| **orderType** | [**OperationOrderType**](#model-operationordertype) | Order direction: Ascending or Descending. | [optional] [enum: 0, 1] |


### Model CustomerServiceUsageDto
Aggregated customer usage statistics for a service over a period.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **service** | **String** | The name of the service. | [optional] [example: disk-storage] [nullable] |
| **title** | **String** | The title of the service. | [optional] [example: Additional disk storage] [nullable] |
| **serviceUnit** | **String** | The unit of measurement for the service. | [optional] [example: GB] [nullable] |
| **currency** | **String** | The three-character ISO 4217 currency symbol of the amounts. | [optional] [example: USD] [nullable] |
| **totalQuantity** | **Integer** (int32) | The total number of units consumed. | [optional] [example: 100] |
| **totalAmount** | **Double** (double) | The total amount charged for the service. | [optional] [example: 14] |
| **operationCount** | **Integer** (int32) | The number of individual purchase operations. | [optional] [example: 1] |
| **price** | **Double** (double) | The price of the service. | [optional] [example: 0.14] |
| **subscription** | **Boolean** | Indicates whether the service is subscription-based. | [optional] [example: true] |


### Model CustomerServiceUsageReportDto
Represents a paged report of customer service usage statistics.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **collection** | [**List**](#model-customerserviceusagedto) | A collection of service usage statistics. | [optional] [example: [{service=backup, totalAmount=49.99}]] [nullable] |
| **offset** | **Integer** (int32) | The report data offset. | [optional] [example: 0] |
| **limit** | **Integer** (int32) | The report data limit. | [optional] [example: 25] |
| **totalQuantity** | **Long** (int64) | The total quantity of records in the report. | [optional] [example: 1] |
| **totalPage** | **Integer** (int32) | The total number of pages in the report. | [optional] [example: 1] |
| **currentPage** | **Integer** (int32) | The current page number of the report. | [optional] [example: 1] |


### Model CustomerServiceUsageReportRequestDto
The request parameters for generating a customer service usage report.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **serviceName** | **List** | The service name list. A single string is also accepted for backward compatibility. | [optional] [example: [backup]] [nullable] |
| **startDate** | **Date** (date-time) | The report start date. | [optional] [example: 2024-01-01T00:00:00Z] [nullable] |
| **endDate** | **Date** (date-time) | The report end date. | [optional] [example: 2024-01-31T23:59:59Z] [nullable] |
| **participantName** | **String** | The participant name. | [optional] [example: My Own Corporation] [nullable] |
| **status** | [**OperationStatus**](#model-operationstatus) | The operation status to filter by. | [optional] [enum: 0, 1, 2, 3] |
| **metadata** | **null** | Metadata key-value pairs to filter by. | [optional] [example: {key1=value1, key2=value2}] |
| **orderBy** | **String** | The field to order by. | [optional] [example: ServiceName] [nullable] |
| **orderType** | [**OperationOrderType**](#model-operationordertype) | Order direction: Ascending or Descending. | [optional] [enum: 0, 1] |


### Model CustomerServiceUsageReportWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CustomerServiceUsageReportDto**](#model-customerserviceusagereportdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DbTenant
The database tenant parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **name** | **String** | The tenant name. | [optional] [example: Tenant] [maxLength: 255] [nullable] |
| **alias** | **String** | The tenant alias. | [optional] [example: tenant] [maxLength: 100] [nullable] |
| **mappedDomain** | **String** | Mapped domain | [optional] [example: tenant.example.com] [maxLength: 100] [nullable] |
| **version** | **Integer** (int32) | The tenant version. | [optional] [example: 5] |
| **version\_Changed** | **Date** (date-time) | The Version_changed field. | [optional] [example: 2025-01-01T10:00:00Z] [nullable] |
| **versionChanged** | **Date** (date-time) | The date and time when the version was changed. | [optional] [example: 2025-01-01T10:00:00Z] |
| **language** | **String** | The tenant language. | [optional] [example: en-US] [maxLength: 10] [nullable] |
| **timeZone** | **String** | The tenant time zone. | [optional] [example: UTC] [maxLength: 50] [nullable] |
| **trustedDomainsRaw** | **String** | The tenant trusted domains raw. | [optional] [example: tenant.exapmle.com, example.com] [maxLength: 1024] [nullable] |
| **trustedDomainsEnabled** | [**TenantTrustedDomainsType**](#model-tenanttrusteddomainstype) | The type of the tenant trusted domains. | [optional] [enum: 0, 1, 2] |
| **status** | [**TenantStatus**](#model-tenantstatus) | The tenant status. | [optional] [enum: 0, 1, 2, 3, 4, 5, 6] |
| **statusChanged** | **Date** (date-time) | The date and time when the tenant status was changed. | [optional] [example: 2025-01-01T12:00:00Z] [nullable] |
| **statusChangedHack** | **Date** (date-time) | The hacked date and time when the tenant status was changed. | [optional] [example: 2025-01-01T12:00:00Z] |
| **creationDateTime** | **Date** (date-time) | The tenant creation date. | [optional] [example: 2025-01-01T12:00:00Z] |
| **ownerId** | **UUID** (uuid) | The tenant owner ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **paymentId** | **String** | The tenant payment ID. | [optional] [example: pay_1234567890] [maxLength: 38] [nullable] |
| **industry** | [**TenantIndustry**](#model-tenantindustry) | The tenant industry. | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16] |
| **lastModified** | **Date** (date-time) | The date and time when the tenant was last modified. | [optional] [example: 2025-02-01T08:30:00Z] |
| **calls** | **Boolean** | Specifies if the calls are available for the current tenant or not. | [optional] [example: true] |
| **partner** | [**DbTenantPartner**](#model-dbtenantpartner) | The database tenant partner parameters. | [optional] |


### Model DbTenantPartner
The database tenant partner parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **partnerId** | **String** | The partner ID. | [optional] [example: partner_123] [maxLength: 36] [nullable] |
| **affiliateId** | **String** | The affiliate ID. | [optional] [example: artifact_123] [maxLength: 50] [nullable] |
| **campaign** | **String** | The tenant partner campaign. | [optional] [example: campaigh] [maxLength: 50] [nullable] |


### Model DeepLinkConfigurationRequestsDto
The request parameters for managing the deep link configuration.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **deepLinkSettings** | [**TenantDeepLinkSettings**](#model-tenantdeeplinksettings) | The deep link settings. | [optional] |


### Model DeepLinkDto
The deep link parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **androidPackageName** | **String** | The Android package name. | [required] [example: com.example.docspace] [nullable] |
| **url** | **String** | The deep link URL. | [required] [example: https://example.com/deeplink] [nullable] |
| **iosPackageId** | **String** | The deep link IOS package ID. | [required] [example: com.example.docspace] [nullable] |


### Model DeepLinkHandlingMode
[0 - Provide choice, 1 - Web, 2 - App]


### Model DefaultProductRequestDto
The request parameters for setting the default product configuration.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **defaultFolderType** | [**FolderType**](#model-foldertype) | The folder type. | [required] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |


### Model DistributedTaskStatus
[0 - Created, 1 - Running, 2 - Completed, 3 - Canceled, 4 - Failted]


### Model DnsSettingsRequestsDto
The request parameters for managing the DNS (Domain Name System) settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **dnsName** | **String** | The DNS (Domain Name System) configuration name. | [optional] [example: example.com] [nullable] |
| **enable** | **Boolean** | Specifies whether the DNS settings are enabled. | [optional] [example: true] |


### Model DocsCloudConfig
Represents the configuration of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **tenantName** | **String** | The tenant name. | [optional] [example: My Portal] [nullable] |
| **security** | [**DocsCloudSecurityConfig**](#model-docscloudsecurityconfig) | The security configuration. | [optional] |
| **server** | [**DocsCloudServerConfig**](#model-docscloudserverconfig) | The server configuration. | [optional] |
| **wopi** | [**DocsCloudWopiConfig**](#model-docscloudwopiconfig) | The WOPI configuration. | [optional] |
| **ipFilter** | [**DocsCloudIpFilterConfig**](#model-docscloudipfilterconfig) | The IP filter configuration. | [optional] |


### Model DocsCloudConfigWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocsCloudConfig**](#model-docscloudconfig) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocsCloudDevPackRequestDto
The request parameters for switch the DocsCloud subscription to DocsCloudDevPack.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **quantity** | **Integer** (int32) | The number of users for DocsCloudDevPack subscription. | [optional] [example: 10] [min: 1] [max: 2147483647] |


### Model DocsCloudIpFilterConfig
Represents the IP filter configuration of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **rules** | [**List**](#model-docscloudipfilterrule) | The IP filter rules. | [optional] [example: [{address=127.0.0.1, allowed=true}]] [nullable] |


### Model DocsCloudIpFilterRule
Represents the IP filter rule of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **address** | **String** | The IP address. | [optional] [example: 127.0.0.1] [nullable] |
| **allowed** | **Boolean** | Whether the IP address is allowed. | [optional] [example: true] |


### Model DocsCloudLicenseInfo
Represents the license information of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **valid** | **Date** (date-time) | The date and time until which the license is valid. | [optional] [example: 2024-01-15T10:30:00Z] |
| **trial** | **Boolean** | Whether the license is a trial. | [optional] [example: false] |
| **buildDate** | **Date** (date-time) | The license build date. | [optional] [example: 2024-01-15T10:30:00Z] |


### Model DocsCloudPayment
Represents the payment information of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **cartId** | **String** | The cart ID. | [optional] [example: CartId] [nullable] |
| **productId** | **Integer** (int32) | The product ID. | [optional] [example: 12345] |
| **status** | **Integer** (int32) | The payment status. | [optional] [example: 1] |
| **intervalUnit** | **Integer** (int32) | The interval unit. | [optional] [example: 1] |
| **isYear** | **Boolean** | Whether the payment interval is yearly. | [optional] [example: false] |
| **isPrepaid** | **Boolean** | Whether the payment is prepaid. | [optional] [example: false] |
| **quantity** | **Integer** (int32) | The quantity. | [optional] [example: 10] |
| **currency** | **String** | The three-character ISO 4217 currency symbol of the payment. | [optional] [example: USD] [nullable] |


### Model DocsCloudQuota
Represents the current user quota of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **users** | [**List**](#model-docscloudquotauser) | The editor users. | [optional] [example: [{userid=00000000-0000-0000-0000-000000000000, expire=2024-01-15T10:30:00Z}]] [nullable] |
| **usersView** | [**List**](#model-docscloudquotauser) | The viewer users. | [optional] [example: [{userid=00000000-0000-0000-0000-000000000000, expire=2024-01-15T10:30:00Z}]] [nullable] |


### Model DocsCloudQuotaUser
Represents a single user entry of a DocsCloud quota.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userId** | **String** | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **expire** | **String** | The expiration date of the user. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |


### Model DocsCloudQuotaWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocsCloudQuota**](#model-docscloudquota) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocsCloudSecurityConfig
Represents the security configuration of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **secret** | **String** | The security secret. | [optional] [example: abc123] [nullable] |
| **header** | **String** | The security header name. | [optional] [example: Authorization] [nullable] |


### Model DocsCloudServerConfig
Represents the server configuration of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **isAnonymousSupport** | **Boolean** | Whether anonymous access is supported. | [optional] [example: false] |
| **fileSizeLimit** | **Long** (int64) | The maximum file size in bytes. | [optional] [example: 104857600] [min: 0] [max: 209715200] |


### Model DocsCloudServerInfo
Represents the DocsCloud server information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **version** | **String** | The server version. | [optional] [example: 8.0.0] [nullable] |
| **packageType** | **String** | The server package type (Open Source, Enterprise Edition or Developer Edition). | [optional] [example: Enterprise Edition] [nullable] |
| **date** | **Date** (date-time) | The server build date. | [optional] [example: 2024-01-15T10:30:00Z] |


### Model DocsCloudStats
Represents the usage statistics of a DocsCloud tenant for the current period.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **periodDay** | **Integer** (int32) | The length of the statistics period in days. | [optional] [example: 30] |
| **editor** | [**DocsCloudUserStats**](#model-docsclouduserstats) | The statistics for editor users. | [optional] |
| **viewer** | [**DocsCloudUserStats**](#model-docsclouduserstats) | The statistics for viewer users. | [optional] |


### Model DocsCloudTenant
Represents a DocsCloud tenant of a portal.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **dedicatedResourceExId** | **Integer** (int32) | The external ID of the dedicated resource the tenant is hosted on. | [optional] [example: 12345] |
| **alias** | **String** | The tenant alias. | [optional] [example: my-portal] [nullable] |
| **name** | **String** | The tenant name. | [optional] [example: My Portal] [nullable] |
| **modifiedDate** | **Date** (date-time) | The date and time when the tenant was last modified. | [optional] [example: 2024-01-15T10:30:00Z] |
| **customerId** | **String** | The customer ID. | [optional] [example: CustomerId] [nullable] |
| **customerName** | **String** | The customer name. | [optional] [example: CustomerName] [nullable] |
| **endDate** | **Date** (date-time) | The date and time when the tenant subscription ends. | [optional] [example: 2024-01-15T10:30:00Z] |
| **resourceType** | **Integer** (int32) | The resource type. | [optional] [example: 1] |
| **isActive** | **Boolean** | Whether the tenant is active (the end date is in the future). | [optional] [example: false] |
| **address** | **String** | The tenant address. | [optional] [example: https://my-portal.onlyoffice.com] [nullable] |
| **payment** | [**DocsCloudPayment**](#model-docscloudpayment) | The tenant payment information. | [optional] |


### Model DocsCloudTenantInfo
Represents the license and server information of a DocsCloud tenant, with usage statistics for the current period.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **license** | [**DocsCloudLicenseInfo**](#model-docscloudlicenseinfo) | The license information. | [optional] |
| **server** | [**DocsCloudServerInfo**](#model-docscloudserverinfo) | The DocsCloud server information. | [optional] |
| **usersLimit** | [**DocsCloudUsersLimit**](#model-docsclouduserslimit) | The user limits of the license. | [optional] |
| **stats** | [**DocsCloudStats**](#model-docscloudstats) | The usage statistics for the current period. | [optional] |


### Model DocsCloudTenantInfoWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocsCloudTenantInfo**](#model-docscloudtenantinfo) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocsCloudTenantWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocsCloudTenant**](#model-docscloudtenant) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocsCloudUsage
Represents the usage statistics of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **since** | **Date** (date-time) | The date and time the usage statistics are counted from. | [optional] [example: 2024-01-15T10:30:00Z] |
| **activeCount** | **Integer** (int32) | The number of active users. | [optional] [example: 10] |


### Model DocsCloudUsageWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocsCloudUsage**](#model-docscloudusage) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocsCloudUserStats
Represents the usage statistics of a single DocsCloud user category (editor or viewer).

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **active** | **Integer** (int32) | The number of active users. | [optional] [example: 10] |
| **internal** | **Integer** (int32) | The number of internal users. | [optional] [example: 8] |
| **external** | **Integer** (int32) | The number of external users. | [optional] [example: 2] |
| **remaining** | **Integer** (int32) | The number of remaining users before the limit is reached. | [optional] [example: 90] |
| **criticalRemaining** | **Boolean** | Whether the number of remaining users is critically low. | [optional] [example: false] |


### Model DocsCloudUsersLimit
Represents the user limits of a DocsCloud license.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **edit** | **Integer** (int32) | The maximum number of users who can edit documents. | [optional] [example: 100] |
| **view** | **Integer** (int32) | The maximum number of users who can view documents. | [optional] [example: 100] |


### Model DocsCloudWopiConfig
Represents the WOPI configuration of a DocsCloud tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enable** | **Boolean** | Whether WOPI is enabled. | [optional] [example: false] |


### Model DocumentBuilderTaskDto
The Document Builder task parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The Document Builder task ID. | [required] [example: task-123-456] [nullable] |
| **error** | **String** | The error message occurred during the document building process. | [required] [example: Build failed] [nullable] |
| **percentage** | **Integer** (int32) | The progress percentage of the document building process. | [required] [example: 75] |
| **isCompleted** | **Boolean** | Specifies whether the document building process is completed or not. | [required] [example: false] |
| **status** | [**DistributedTaskStatus**](#model-distributedtaskstatus) | The status of the document building process. | [required] [enum: 0, 1, 2, 3, 4] |
| **resultFileId** | **null** | The result file ID. | [required] [example: 123] |
| **resultFileName** | **String** | The result file name. | [required] [example: result.docx] [nullable] |
| **resultFileUrl** | **String** | The result file URL. | [required] [example: http://localhost/files/result.docx] [nullable] |


### Model DocumentBuilderTaskWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocumentBuilderTaskDto**](#model-documentbuildertaskdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DoubleNullableWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **BigDecimal** |  | [optional] [nullable] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DoubleWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **BigDecimal** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EmailActivationSettings
The email activation settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **show** | **Boolean** | Specifies whether the email activation settings are shown or hidden. | [optional] [example: true] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model EmailActivationSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**EmailActivationSettings**](#model-emailactivationsettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EmailValidationKeyModel
The confirmation email parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** | The email validation key. | [optional] [example: abcdef123456] [nullable] |
| **emplType** | [**EmployeeType**](#model-employeetype) | The user type. | [optional] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **email** | **String** (email) | The email address. | [optional] [example: user@example.com] [nullable] |
| **encEmail** | **String** | The encrypted email address. | [optional] [example: user%40example.com] [nullable] |
| **uiD** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **type** | [**ConfirmType**](#model-confirmtype) | The confirmation email type. | [optional] [enum: EmpInvite, LinkInvite, PortalSuspend, PortalContinue, PortalRemove, DnsChange, PortalOwnerChange, Activation, EmailChange, EmailActivation, PasswordChange, ProfileRemove, PhoneActivation, PhoneAuth, Auth, TfaActivation, TfaAuth, Wizard, GuestShareLink] |
| **first** | **String** | Specifies whether it is the first time account access or not. | [optional] [example: false] [nullable] |
| **roomId** | **String** | The room ID. | [optional] [example: 1] [nullable] |


### Model EmployeeActivationStatus
[0 - Not activated, 1 - Activated, 2 - Pending, 4 - Auto generated]


### Model EmployeeArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-employeedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EmployeeDto
The user parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **displayName** | **String** | The HTML-encoded user&#39;s display name formatted according to the default format for the current culture. | [optional] [example: Mike Zanyatski] [nullable] |
| **avatar** | **String** | The user avatar. | [optional] [example: https://example.com/avatar.jpg] [nullable] |
| **avatarOriginal** | **String** | The user original size avatar. | [optional] [example: https://example.com/avatar_original.jpg] [nullable] |
| **avatarMax** | **String** | The user maximum size avatar. | [optional] [example: https://example.com/avatar_max.jpg] [nullable] |
| **avatarMedium** | **String** | The user medium size avatar. | [optional] [example: https://example.com/avatar_medium.jpg] [nullable] |
| **avatarSmall** | **String** | The user small size avatar. | [optional] [example: https://example.com/avatar_small.jpg] [nullable] |
| **profileUrl** | **String** | The user profile URL. | [optional] [example: https://example.com/profile/user123] [nullable] |
| **hasAvatar** | **Boolean** | Specifies if the user has an avatar or not. | [optional] [example: true] |
| **isAnonim** | **Boolean** | Specifies if the user is anonymous or not. | [optional] [example: false] |


### Model EmployeeStatus
[1 - Active, 2 - Terminated, 4 - Pending, 5 - Default, 7 - All]


### Model EmployeeType
[All - All, RoomAdmin - Room admin, Guest - Guest, DocSpaceAdmin - DocSpace admin, User - User]


### Model EncryprtionStatus
[0 - Decrypted, 1 - Encryption started, 2 - Encrypted, 3 - Decryption started]


### Model EncryptionSettings
The encryption settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **password** | **String** | The encryption password. | [optional] [example: password] [nullable] |
| **status** | [**EncryprtionStatus**](#model-encryprtionstatus) | The encryption status. | [optional] [enum: 0, 1, 2, 3] |
| **notifyUsers** | **Boolean** | Specifies if the users will be notified about the encryption operation or not. | [optional] [example: true] |


### Model EncryptionSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**EncryptionSettings**](#model-encryptionsettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EntryType
[0 - None, 1 - File, 2 - Folder, 23 - User, 24 - Group, 25 - Room, 26 - Tag, 27 - Agent]


### Model ExternalDatabaseSettings

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **databaseType** | **String** |  | [optional] [nullable] |
| **databaseTypeEnum** | [**ExternalDatabaseType**](#model-externaldatabasetype) |  | [optional] [enum: 0, 1] |
| **dbHost** | **String** |  | [optional] [nullable] |
| **dbPort** | **Integer** (int32) |  | [optional] |
| **dbName** | **String** |  | [optional] [nullable] |
| **dbUser** | **String** |  | [optional] [nullable] |
| **dbPassword** | **String** |  | [optional] [nullable] |
| **dbSsl** | **Boolean** |  | [optional] |
| **sqliteFilePath** | **String** |  | [optional] [nullable] |


### Model ExternalDatabaseType
[]


### Model FeatureUsedDto
The used space parameters of the tenant quota feature.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **value** | **null** | The used space value. | [required] [example: {}] |
| **title** | **String** | The used space title. | [optional] [example: 50 GB used] [nullable] |


### Model FinishDto
The parameters for terminating a process or operation.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **isSendWelcomeEmail** | **Boolean** | Specifies whether to send a welcome email or not. | [required] [example: true] |


### Model FireBaseUser
The Firebase user parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The Firebase user ID. | [optional] [example: 1] |
| **userId** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **firebaseDeviceToken** | **String** | The Firebase device token. | [optional] [example: token123] [maxLength: 255] [nullable] |
| **application** | **String** | The Firebase application. | [optional] [example: web] [maxLength: 20] [nullable] |
| **isSubscribed** | **Boolean** | Specifies if the user is subscribed to the push notifications or not. | [optional] [example: true] [nullable] |
| **tenant** | [**DbTenant**](#model-dbtenant) | The database tenant parameters. | [optional] |


### Model FireBaseUserWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FireBaseUser**](#model-firebaseuser) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FirebaseDto
The Firebase parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **apiKey** | **String** | The Firebase API key. | [required] [example: AIzaSyDxK9L3j4H8mN2pQ5rS6tU7vW8xY9zA1bC] [nullable] |
| **authDomain** | **String** | The Firebase authentication domain. | [required] [example: myapp-12345.firebaseapp.com] [nullable] |
| **projectId** | **String** | The Firebase project ID. | [required] [example: myapp-12345] [nullable] |
| **storageBucket** | **String** | The Firebase storage bucket. | [required] [example: myapp-12345.appspot.com] [nullable] |
| **messagingSenderId** | **String** | The Firebase messaging sender ID. | [required] [example: 123456789012] [nullable] |
| **appId** | **String** | The Firebase application ID. | [required] [example: 1:123456789012:web:a1b2c3d4e5f6g7h8] [nullable] |
| **measurementId** | **String** | The Firebase measurement ID. | [required] [example: G-ABCD123456] [nullable] |
| **databaseURL** | **String** | The Firebase database URL. | [required] [example: https://myapp-12345.firebaseio.com] [nullable] |


### Model FirebaseRequestsDto
The Firebase-related request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **firebaseDeviceToken** | **String** | The Firebase device token. | [optional] [example: dGhpc2lzYXRva2Vu...] [nullable] |
| **isSubscribed** | **Boolean** | Specifies whether the user is subscribed to the push notifications or not. | [optional] [example: true] |


### Model FolderType
[0 - Default, 1 - Coomon, 2 - Bunch, 3 - Trash, 5 - User, 6 - Share, 8 - Projects, 10 - Favourites, 11 - Recent, 12 - Templates, 13 - Privacy, 14 - Virtual rooms, 15 - Filling forms room, 16 - Editing room, 19 - Custom room, 20 - Archive, 21 - Thirdparty backup, 22 - Public room, 25 - Ready form folder, 26 - In process form folder, 27 - Form filling folder done, 28 - Form filling folder in progress, 29 - Virtual Data Room, 30 - Room templates folder, 31 - AI Room, 32 - Knowledge, 33 - Result storage, 34 - AI Agents, 35 - Default Templates, 36 - Forms]


### Model FormGalleryDto
The form gallery parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **path** | **String** | The form gallery path. | [required] [example: /forms/templates] [nullable] |
| **domain** | **String** | The form gallery domain. | [required] [example: https://forms.example.com] [nullable] |
| **ext** | **String** | The form gallery extension. | [required] [example: .docxf] [nullable] |
| **uploadPath** | **String** | The form gallery upload path. | [required] [example: /forms/upload] [nullable] |
| **uploadDomain** | **String** | The form gallery upload domain. | [required] [example: https://upload.forms.example.com] [nullable] |
| **uploadExt** | **String** | The form gallery upload extension. | [required] [example: .docxf] [nullable] |
| **uploadDashboard** | **String** | The form gallery upload dashboard. | [required] [example: /dashboard/forms] [nullable] |


### Model GreetingSettingsRequestsDto
The request parameters for managing the greeting settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The title of the tenant greeting settings. | [required] [example: Welcome to Our Portal] [minLength: 0] [maxLength: 255] [nullable] |


### Model GroupSummaryDto
The group summary parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **name** | **String** | The group name. | [required] [example: Group Name] [nullable] |
| **manager** | **String** | The group manager. | [optional] [example: Jake.Zazhitski] [nullable] |
| **isSystem** | **Boolean** | Indicates whether the group is a system group. | [optional] [example: false] [nullable] |


### Model IMagickGeometry

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **aspectRatio** | **Boolean** |  | [optional] |
| **fillArea** | **Boolean** |  | [optional] |
| **greater** | **Boolean** |  | [optional] |
| **height** | **Integer** (int32) |  | [optional] |
| **ignoreAspectRatio** | **Boolean** |  | [optional] |
| **isPercentage** | **Boolean** |  | [optional] |
| **less** | **Boolean** |  | [optional] |
| **limitPixels** | **Boolean** |  | [optional] |
| **width** | **Integer** (int32) |  | [optional] |
| **x** | **Integer** (int32) |  | [optional] |
| **y** | **Integer** (int32) |  | [optional] |


### Model IPRestriction

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ip** | **String** |  | [required] |
| **forAdmin** | **Boolean** |  | [optional] |
| **id** | **Integer** (int32) |  | [optional] |
| **tenantId** | **Integer** (int32) |  | [optional] |


### Model IPRestrictionArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-iprestriction) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model IPRestrictionsSettings

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enable** | **Boolean** |  | [optional] |
| **lastModified** | **Date** (date-time) |  | [optional] |


### Model IPRestrictionsSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**IPRestrictionsSettings**](#model-iprestrictionssettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ImportableApiEntity

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **shouldImport** | **Boolean** |  | [optional] |


### Model Int64Wrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Integer** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model InvitationLinkCreateRequestDto
The request parameters for creating an invitation link.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **employeeType** | [**EmployeeType**](#model-employeetype) | The user type. | [required] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **expiration** | **Date** (date-time) | The expiration date of the invitation link. | [optional] [example: 2025-06-15T10:30:00.0000000Z] [nullable] |
| **maxUseCount** | **Integer** (int32) | The maximum number of times the invitation link can be used. | [optional] [example: 1] [min: 1] [max: 1000] [nullable] |


### Model InvitationLinkDeleteRequestDto
The request parameters for deleting an invitation link.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The ID of the invitation link. | [required] [example: 00000000-0000-0000-0000-000000000000] |


### Model InvitationLinkDto
The invitation link parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The ID of the invitation link. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **employeeType** | [**EmployeeType**](#model-employeetype) | The user type. | [required] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **expiration** | [**ApiDateTime**](#model-apidatetime) | The API date and time parameters. | [optional] |
| **isExpired** | **Boolean** | Indicates whether the invitation link has expired. | [optional] [example: true] |
| **maxUseCount** | **Integer** (int32) | The maximum number of times the invitation link can be used. | [optional] [example: 1] [nullable] |
| **currentUseCount** | **Integer** (int32) | The current number of times the invitation link has been used. | [optional] [example: 1] |
| **url** | **String** | The URL of the invitation link. | [optional] [example: https://example.com] [nullable] |


### Model InvitationLinkUpdateRequestDto
The request parameters for updating an invitation link.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The ID of the invitation link. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **expiration** | **Date** (date-time) | The expiration date of the invitation link. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |
| **maxUseCount** | **Integer** (int32) | The maximum number of times the invitation link can be used. | [optional] [example: 1] [min: 1] [max: 1000] [nullable] |


### Model InvitationLinkWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**InvitationLinkDto**](#model-invitationlinkdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model IpRestrictionBase

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ip** | **String** |  | [required] [nullable] |
| **forAdmin** | **Boolean** |  | [optional] |


### Model IpRestrictionsDto
The parameters for configuring new IP restriction settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ipRestrictions** | [**List**](#model-iprestrictionbase) | The list of IP restriction addresses. | [required] [example: [{ip=192.0.2.1, forAdmin=false}]] [nullable] |
| **enable** | **Boolean** | Specifies whether to enable IP restrictions or not. | [optional] [example: true] [nullable] |


### Model IpRestrictionsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**IpRestrictionsDto**](#model-iprestrictionsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model IsDefaultWhiteLabelLogosArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-isdefaultwhitelabellogosdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model IsDefaultWhiteLabelLogosDto
The default white label logo parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The white label logo name. | [required] [example: logo_light] [nullable] |
| **default** | **Boolean** | Specifies if the white label logo is default or not. | [required] [example: true] |


### Model IsDefaultWhiteLabelLogosWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**IsDefaultWhiteLabelLogosDto**](#model-isdefaultwhitelabellogosdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ItemKeyValuePairStringBoolean

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** |  | [optional] [nullable] |
| **value** | **Boolean** |  | [optional] |


### Model ItemKeyValuePairStringLogoRequestsDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** |  | [optional] [nullable] |
| **value** | [**LogoRequestsDto**](#model-logorequestsdto) | The request parameters for the theme-specific logo configurations. | [optional] |


### Model ItemKeyValuePairStringString

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** |  | [optional] [nullable] |
| **value** | **String** |  | [optional] [nullable] |


### Model LocationType
[0 - None, 1 - Files, 2 - Folders, 3 - Documents settings, 27 - Rooms, 29 - Settings, 30 - Contacts, 31 - Agents]


### Model LoginEventArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-logineventdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model LoginEventDto
The login event parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The login event ID. | [optional] [example: 1] |
| **date** | [**ApiDateTime**](#model-apidatetime) | The API date and time parameters. | [optional] |
| **user** | **String** | The user name of the login event. | [optional] [example: John Doe] [nullable] |
| **userId** | **UUID** (uuid) | The user ID of the login event. | [optional] [example: {}] |
| **login** | **String** | The user login of the login event. | [optional] [example: user@example.com] [nullable] |
| **action** | **String** | The login event action. | [optional] [example: User logged in] [nullable] |
| **actionId** | [**MessageAction**](#model-messageaction) | The event action ID. | [optional] [enum: 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 4000, 4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008, 4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037, 5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 5012, 5013, 5014, 5015, 5016, 5017, 5018, 5019, 5020, 5021, 5022, 5023, 5024, 5025, 5026, 5027, 5028, 5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038, 5039, 5040, 5041, 5042, 5043, 5044, 5045, 5046, 5047, 5048, 5049, 5050, 5053, 5054, 5055, 5056, 5057, 5058, 5059, 5060, 5061, 5062, 5063, 5064, 5065, 5066, 5068, 5069, 5070, 5071, 5072, 5073, 5074, 5075, 5076, 5077, 5078, 5079, 5080, 5081, 5082, 5083, 5084, 5085, 5086, 5087, 5088, 5089, 5090, 5091, 5092, 5093, 5094, 5095, 5096, 5097, 5098, 5099, 5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5112, 5113, 5114, 5115, 5116, 5117, 5118, 5119, 5120, 5121, 5122, 5123, 5124, 5125, 5126, 5127, 5128, 5129, 5130, 5131, 5132, 5133, 5150, 5151, 5152, 5153, 5154, 5155, 5156, 5157, 5158, 5159, 5160, 5201, 5202, 5203, 5204, 5205, 5206, 5501, 5502, 5503, 6000, 6001, 6002, 6003, 6004, 6005, 6006, 6007, 6008, 6009, 6010, 6011, 6012, 6013, 6014, 6015, 6016, 6017, 6018, 6019, 6020, 6021, 6022, 6023, 6024, 6025, 6026, 6027, 6028, 6029, 6030, 6031, 6032, 6033, 6034, 6035, 6036, 6037, 6038, 6039, 6040, 6041, 6042, 6043, 6044, 6045, 6046, 6047, 6048, 6049, 6050, 6051, 6052, 6053, 6054, 6055, 6056, 6057, 6058, 6059, 6060, 6061, 6062, 6063, 6064, 6065, 6066, 6067, 6068, 6069, 6070, 6071, 6072, 6073, 6074, 6075, 6076, 6077, 6078, 6079, 6080, 6081, 6082, 6083, 6084, 6085, 6086, 6087, 6088, 6089, 6090, 6091, 6092, 6093, 6094, 6095, 6096, 6097, 6098, 6099, 6100, 6101, 6102, 7000, 7001, 7002, 7003, 7004, 9901, 9902, 9903, 9904, 9905, 9906, 9907, 9908, 9909, -1] |
| **ip** | **String** | The login event IP. | [optional] [example: 192.0.2.1] [nullable] |
| **country** | **String** | The login event country. | [optional] [example: United States] [nullable] |
| **city** | **String** | The login event city. | [optional] [example: New York] [nullable] |
| **browser** | **String** | The login event browser. | [optional] [example: Chrome 120.0] [nullable] |
| **platform** | **String** | The login event platform. | [optional] [example: Windows] [nullable] |
| **page** | **String** | The login event page. | [optional] [example: /login] [nullable] |


### Model LoginProvider
[0 - Facebook, 1 - Google, 2 - Dropbox, 3 - Docusign, 4 - Box, 5 - OneDrive, 6 - GosUslugi, 7 - LinkedIn, 8 - MailRu, 9 - VK, 10 - Wordpress, 11 - Yahoo, 12 - Yandex, 13 - Github, 14 - Nextcloud]


### Model LoginSettingsDto
The login settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **attemptCount** | **Integer** (int32) | The maximum number of consecutive failed login attempts allowed before triggering account suspension. | [required] [example: 5] |
| **blockTime** | **Integer** (int32) | The duration (in minutes) for which an account remains suspended after exceeding maximum login attempts. | [required] [example: 15] |
| **checkPeriod** | **Integer** (int32) | The maximum time (in seconds) allowed for server to process and respond to login requests. | [required] [example: 60] |
| **isDefault** | **Boolean** | Specifies whether the login settings are default or not. | [required] [example: false] |


### Model LoginSettingsRequestDto
The request parameters for configuring login security and performance settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **attemptCount** | **Integer** (int32) | The maximum number of consecutive failed login attempts allowed before triggering account suspension. | [optional] [example: 1] [min: 1] [max: 9999] |
| **blockTime** | **Integer** (int32) | The duration (in minutes) for which an account remains suspended after exceeding maximum login attempts. | [optional] [example: 1] [min: 1] [max: 9999] |
| **checkPeriod** | **Integer** (int32) | The maximum time (in seconds) allowed for server to process and respond to login requests. | [optional] [example: 1] [min: 1] [max: 9999] |


### Model LoginSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**LoginSettingsDto**](#model-loginsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model LogoRequestsDto
The request parameters for the theme-specific logo configurations.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **light** | **String** | The URL or base64-encoded image data for the light theme logo. | [optional] [example: data:image/png;base64,iVBORw0KGgoAAAANS...] [nullable] |
| **dark** | **String** | The URL or base64-encoded image data for the dark theme logo. | [optional] [example: data:image/png;base64,iVBORw0KGgoAAAANS...] [nullable] |


### Model MailDomainSettingsRequestsDto
The request parameters for configuring trusted mail domains and visitor invitation settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | [**TenantTrustedDomainsType**](#model-tenanttrusteddomainstype) | The type of the tenant trusted domains. | [required] [enum: 0, 1, 2] |
| **domains** | **List** | The list of authorized email domains that are considered trusted. | [required] [example: [example.com, company.com]] [nullable] |
| **inviteUsersAsVisitors** | **Boolean** | Specifies the default permission level for the invited users (visitors or not). | [required] [example: false] |


### Model MessageAction
[1000 - Login success, 1001 - Login success via social account, 1002 - Login fail invalid combination, 1003 - Login fail social account not found, 1004 - Login fail disabled profile, 1005 - Login fail, 1006 - Logout, 1007 - Login success via sms, 1008 - Login fail via sms, 1009 - Login fail ip security, 1010 - Login success via api, 1011 - Login success via social app, 1012 - Login success via api sms, 1013 - Login fail via api, 1014 - Login fail via api sms, 1015 - Login success via SSO, 1016 - Session started, 1017 - Session completed, 1018 - Login fail via SSO, 1019 - Login success via api social account, 1020 - Login fail via api social account, 1021 - Login succes via tfa app, 1022 - Login fail via Tfa app, 1023 - Login fail brute force, 1024 - Login success via api tfa, 1025 - Login fail via api tfa, 1026 - Login fail recaptcha, 1027 - Authorization link activated, 1028 - Login success via OAuth 2.0, 1029 - Login success via login and password, 4000 - User created, 4001 - Guest created, 4002 - User created via invite, 4003 - Guest created via invite, 4004 - User activated, 4005 - Guest activated, 4006 - User updated, 4007 - User updated language, 4008 - User added avatar, 4009 - User deleted avatar, 4010 - User updated avatar thumbnails, 4011 - User linked social account, 4012 - User unlinked social account, 4013 - User sent activation instructions, 4014 - User sent email change instructions, 4015 - User sent password change instructions, 4016 - User sent delete instructions, 4017 - User updated password, 4018 - User deleted, 4019 - Users updated type, 4020 - Users updated status, 4021 - Users sent activation instructions, 4022 - Users deleted, 4023 - Sent invite instructions, 4024 - User imported, 4025 - Guest imported, 4026 - Group created, 4027 - Group updated, 4028 - Group deleted, 4029 - User updated mobile number, 4030 - User data reassigns, 4031 - User data removing, 4032 - User connected tfa app, 4033 - User disconnected tfa app, 4034 - User logout active connections, 4035 - User logout active connection, 4036 - User logout active connections for user, 4037 - Send join invite, 5000 - File created, 5001 - File renamed, 5002 - File updated, 5003 - File created version, 5004 - File deleted version, 5005 - File updated revision comment, 5006 - File locked, 5007 - File unlocked, 5008 - File updated access, 5009 - File downloaded, 5010 - File downloaded as, 5011 - File uploaded, 5012 - File imported, 5013 - File copied, 5014 - File copied with overwriting, 5015 - File moved, 5016 - File moved with overwriting, 5017 - File moved to trash, 5018 - File deleted, 5019 - Folder created, 5020 - Folder renamed, 5021 - Folder updated access, 5022 - Folder copied, 5023 - Folder copied with overwriting, 5024 - Folder moved, 5025 - Folder moved with overwriting, 5026 - Folder moved to trash, 5027 - Folder deleted, 5028 - ThirdParty created, 5029 - ThirdParty updated, 5030 - ThirdParty deleted, 5031 - Documents ThirdParty settings updated, 5032 - Documents overwriting settings updated, 5033 - Documents uploading formats settings updated, 5034 - User file updated, 5035 - File converted, 5036 - File send access link, 5037 - Document service location setting, 5038 - Authorization keys setting, 5039 - Full text search setting, 5040 - Start transfer setting, 5041 - Backup started, 5042 - License key uploaded, 5043 - File change owner, 5044 - File restore version, 5045 - Document send to sign, 5046 - Document sign complete, 5047 - User updated email, 5048 - Documents store forcesave, 5049 - Documents forcesave, 5050 - Start storage encryption, 5053 - Start storage decryption, 5054 - File opened for change, 5055 - File marked as favorite, 5056 - File removed from favorite, 5057 - Folder downloaded, 5058 - File removed from list, 5059 - Folder removed from list, 5060 - File external link access updated, 5061 - Trash emptied, 5062 - File revision downloaded, 5063 - File marked as read, 5064 - File readed, 5065 - Folder marked as read, 5066 - Folder updated access for, 5068 - File updated access for, 5069 - Documents external share settings updated, 5070 - Room created, 5071 - Room renamed, 5072 - Room archived, 5073 - Room unarchived, 5074 - Room deleted, 5075 - Room update access for user, 5076 - Tag created, 5077 - Tags deleted, 5078 - Added room tags, 5079 - Deleted room tags, 5080 - Room logo created, 5081 - Room logo deleted, 5082 - Room invitation link updated, 5083 - Documents keep new file name settings updated, 5084 - Room remove user, 5085 - Room create user, 5086 - Room invitation link created, 5087 - Room invitation link deleted, 5088 - Room external link created, 5089 - Room external link updated, 5090 - Room external link deleted, 5091 - File external link created, 5092 - File external link updated, 5093 - File external link deleted, 5094 - Room group added, 5095 - Room update access for group, 5096 - Room group remove, 5097 - Room external link revoked, 5098 - Room external link renamed, 5099 - File uploaded with overwriting, 5100 - Room copied, 5101 - Documents display file extension updated, 5102 - Room color changed, 5103 - Room cover changed, 5104 - Room indexing changed, 5105 - Room deny download changed, 5106 - Room index export saved, 5107 - Folder index changed, 5108 - Folder index reordered, 5109 - Room deny download enabled, 5110 - Room deny download disabled, 5111 - File index changed, 5112 - Room watermark set, 5113 - Room watermark disabled, 5114 - Room index export saved, 5115 - Room indexing disabled, 5116 - Room life time set, 5117 - Room life time disabled, 5118 - Room invite resend, 5119 - File version deleted, 5120 - File custom filter enabled, 5121 - File custom filter disabled, 5122 - Folder external link created, 5123 - Folder external link updated, 5124 - Folder external link deleted, 5125 - Backup completed, 5126 - Backup failed, 5127 - Scheduled backup started, 5128 - Scheduled backup completed, 5129 - Scheduled backup failed, 5130 - Scheduled backup deleted, 5131 - Backup cancelled, 5132 - Restore started, 5133 - Restore cancelled, 5150 - Form started to fill, 5151 - Form partially filled, 5152 - Form completely filled, 5153 - Form stopped, 5154 - AI agent created, 5155 - AI agent renamed, 5156 - AI agent deleted, 5157 - MCP server added to AI agent, 5158 - MCP server deleted from AI agent, 5159 - Room change owner, 5160 - Documents default templates settings updated, 5201 - File saved, user quota exceeded, 5202 - File not saved due to user quota exceeded, 5203 - File saved, room quota exceeded, 5204 - File not saved due to room quota exceeded, 5205 - File saved, tenant quota exceeded, 5206 - File not saved due to tenant quota exceeded, 5501 - Ldap enabled, 5502 - Ldap disabled, 5503 - LDAP synchronization completed, 6000 - Language settings updated, 6001 - Time zone settings updated, 6002 - Dns settings updated, 6003 - Trusted mail domain settings updated, 6004 - Password strength settings updated, 6005 - Two factor authentication settings updated, 6006 - Administrator message settings updated, 6007 - Default start page settings updated, 6008 - Products list updated, 6009 - Administrator added, 6010 - Administrator opened full access, 6011 - Administrator deleted, 6012 - Users opened product access, 6013 - Groups opened product access, 6014 - Product access opened, 6015 - Product access restricted, 6016 - Product added administrator, 6017 - Product deleted administrator, 6018 - Greeting settings updated, 6019 - Team template changed, 6020 - Color theme changed, 6021 - Owner sent change owner instructions, 6022 - Owner updated, 6023 - Owner sent portal deactivation instructions, 6024 - Owner sent portal delete instructions, 6025 - Portal deactivated, 6026 - Portal deleted, 6027 - Login history report downloaded, 6028 - Audit trail report downloaded, 6029 - SSO enabled, 6030 - SSO disabled, 6031 - Portal access settings updated, 6032 - Cookie settings updated, 6033 - Mail service settings updated, 6034 - Custom navigation settings updated, 6035 - Audit settings updated, 6036 - Two factor authentication disabled, 6037 - Two factor authentication enabled by sms, 6038 - Two factor authentication enabled by tfa app, 6039 - Portal renamed, 6040 - Quota per room changed, 6041 - Quota per room disabled, 6042 - Quota per user changed, 6043 - Quota per user disabled, 6044 - Quota per portal changed, 6045 - Quota per portal disabled, 6046 - Form submit, 6047 - Form opened for filling, 6048 - Custom quota per room default, 6049 - Custom quota per room changed, 6050 - Custom quota per room disabled, 6051 - Custom quota per user default, 6052 - Custom quota per user changed, 6053 - Custom quota per user disabled, 6054 - DevTools access settings changed, 6055 - Webhook created, 6056 - Webhook updated, 6057 - Webhook deleted, 6058 - Created api key, 6059 - Update api key, 6060 - Deleted User api key, 6061 - Customer wallet topped up, 6062 - Customer operation performed, 6063 - Customer operations report downloaded, 6064 - Customer wallet top up settings updated, 6065 - Customer subscription updated, 6066 - Promotional banners visibility settings changed, 6067 - Customer wallet services settings updated, 6068 - Quota per AI agent changed, 6069 - Quota per AI agent disabled, 6070 - Custom quota per AI agent default, 6071 - Custom quota per AI agent changed, 6072 - Custom quota per AI agent disabled, 6073 - AI provider created, 6074 - AI provider updated, 6075 - AI provider deleted, 6076 - MCP server created, 6077 - MCP server updated, 6078 - MCP server enabled, 6079 - MCP server disabled, 6080 - MCP server deleted, 6081 - WebSearch settings configured, 6082 - WebSearch settings reset, 6083 - Vectorization settings configured, 6084 - Vectorization settings reset, 6085 - Webplugin uploaded, 6086 - Webplugin updated, 6087 - Webplugin deleted, 6088 - Whitelabel settings logo text updated, 6089 - Whitelabel settings logos updated, 6090 - Whitelabel company settings updated, 6091 - Whitelabel additional settings updated, 6092 - Whitelabel mail settings updated, 6093 - Invitation settings updated, 6094 - IP restrictions settings updated, 6095 - Login settings updated, 6096 - AI default provider set, 6097 - AI access enabled, 6098 - AI access disabled, 6099 - User AI settings updated, 6100 - Subscription balance moved to wallet, 6101 - Docs Cloud config updated, 6102 - Docs Cloud quota report downloaded, 7000 - Contact admin mail sent, 7001 - Room invite link used, 7002 - User created and added to room, 7003 - Guest created and added to room, 7004 - Contact sales mail sent, 9901 - Create client, 9902 - Update client, 9903 - Regenerate secret, 9904 - Delete client, 9905 - Change client activation, 9906 - Change client visibility, 9907 - Revoke user client, 9908 - Generate authorization code token, 9909 - Generate personal access token, -1 - None]


### Model MigratingApiFiles

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **foldersCount** | **Integer** (int32) |  | [optional] |
| **filesCount** | **Integer** (int32) |  | [optional] |
| **bytesTotal** | **Long** (int64) |  | [optional] |


### Model MigratingApiGroup

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **shouldImport** | **Boolean** |  | [optional] |
| **groupName** | **String** |  | [optional] [nullable] |
| **moduleName** | **String** |  | [optional] [nullable] |
| **userUidList** | **List** |  | [optional] [nullable] |


### Model MigratingApiUser

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **shouldImport** | **Boolean** |  | [optional] |
| **key** | **String** |  | [optional] [nullable] |
| **email** | **String** (email) |  | [optional] [nullable] |
| **displayName** | **String** |  | [optional] [nullable] |
| **firstName** | **String** |  | [optional] [nullable] |
| **lastName** | **String** |  | [optional] [nullable] |
| **userType** | [**EmployeeType**](#model-employeetype) | The user type. | [optional] [enum: All, RoomAdmin, Guest, DocSpaceAdmin, User] |
| **migratingFiles** | [**MigratingApiFiles**](#model-migratingapifiles) |  | [optional] |


### Model MigrationApiInfo

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **migratorName** | **String** |  | [optional] [nullable] |
| **operation** | **String** |  | [optional] [nullable] |
| **failedArchives** | **List** |  | [optional] [nullable] |
| **users** | [**List**](#model-migratingapiuser) |  | [optional] [nullable] |
| **withoutEmailUsers** | [**List**](#model-migratingapiuser) |  | [optional] [nullable] |
| **existUsers** | [**List**](#model-migratingapiuser) |  | [optional] [nullable] |
| **groups** | [**List**](#model-migratingapigroup) |  | [optional] [nullable] |
| **importPersonalFiles** | **Boolean** |  | [optional] |
| **importSharedFiles** | **Boolean** |  | [optional] |
| **importSharedFolders** | **Boolean** |  | [optional] |
| **importCommonFiles** | **Boolean** |  | [optional] |
| **importProjectFiles** | **Boolean** |  | [optional] |
| **importGroups** | **Boolean** |  | [optional] |
| **successedUsers** | **Integer** (int32) |  | [optional] |
| **failedUsers** | **Integer** (int32) |  | [optional] |
| **files** | **List** |  | [optional] [nullable] |
| **errors** | **List** |  | [optional] [nullable] |


### Model MigrationStatusDto
The migration status parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **progress** | **Double** (double) | The migration progress. | [optional] [example: 99.99] |
| **error** | **String** | The migration error. | [optional] [example: Connection failed] [nullable] |
| **parseResult** | [**MigrationApiInfo**](#model-migrationapiinfo) | The migration API information. | [optional] |
| **isCompleted** | **Boolean** | Specifies whether the migration is completed or not. | [optional] [example: true] |


### Model MigrationStatusWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**MigrationStatusDto**](#model-migrationstatusdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model MobilePhoneActivationStatus
[0 - Not activated, 1 - Activated]


### Model MobileRequestsDto
The parameters required for the mobile phone verification.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **mobilePhone** | **String** | The user&#39;s mobile phone number. | [optional] [example: +1234567890] [nullable] |


### Model NotificationChannelDto
The notification channel information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The notification channel name. | [required] [example: Email] [nullable] |
| **isEnabled** | **Boolean** | Specifies whether the notification channel is enabled. | [required] [example: true] |


### Model NotificationChannelStatusDto
The notification channel settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **channels** | [**List**](#model-notificationchanneldto) | The list of notification channels. | [optional] [example: [{name=email, isActive=true}]] [nullable] |


### Model NotificationChannelStatusWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**NotificationChannelStatusDto**](#model-notificationchannelstatusdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model NotificationSettingsDto
The notification settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | [**NotificationType**](#model-notificationtype) | The notification type. | [optional] [enum: 0, 1, 2, 3] |
| **isEnabled** | **Boolean** | Specifies if the notification type is enabled or not. | [optional] [example: true] |


### Model NotificationSettingsRequestsDto
The request parameters for configuring notification settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | [**NotificationType**](#model-notificationtype) | The notification type. | [required] [enum: 0, 1, 2, 3] |
| **isEnabled** | **Boolean** | Specifies if the specified notification type is enabled or not. | [optional] [example: true] |


### Model NotificationSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**NotificationSettingsDto**](#model-notificationsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model NotificationType
[0 - Badges, 1 - Rooms activity, 2 - Daily feed, 3 - Usefull tips]


### Model ObjectWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Object** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model OperationDto
Represents an operation.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **date** | [**ApiDateTime**](#model-apidatetime) | The API date and time parameters. | [optional] |
| **service** | **String** | The service related to the operation. | [optional] [example: Storage] [nullable] |
| **description** | **String** | The brief operation description. | [optional] [example: Storage quota increase] [nullable] |
| **details** | **String** | The detailed information about the operation. | [optional] [example: Increased storage from 50GB to 100GB] [nullable] |
| **serviceUnit** | **String** | The service unit. | [optional] [example: GB] [nullable] |
| **quantity** | **Integer** (int32) | The quantity of the service used. | [optional] [example: 1] |
| **currency** | **String** | The three-character ISO 4217 currency symbol of the operation. | [optional] [example: USD] [nullable] |
| **credit** | **Double** (double) | The credit amount of the operation. | [optional] [example: 99.99] |
| **debit** | **Double** (double) | The debit amount of the operation. | [optional] [example: 99.99] |
| **participantName** | **String** | The participant original name. | [optional] [example: Example Name] [nullable] |
| **participantDisplayName** | **String** | The participant display name. | [optional] [example: Example Name] [nullable] |
| **agentId** | **String** | AI Agent id. | [optional] [example: 123] [nullable] |
| **agentTitle** | **String** | AI Agent name. | [optional] [example: My AI Agent] [nullable] |
| **type** | [**OperationType**](#model-operationtype) | The operation type | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19] |


### Model OperationOrderType
[0 - Descending, 1 - Ascending]


### Model OperationStatus
[0 - Pending, 1 - Completed, 2 - Rejected, 3 - Canceled]


### Model OperationType
[0 - Unknown, 1 - ServicePayment, 2 - PackagePayment, 3 - AiServicePayment, 4 - Deposit, 5 - ReceiveProviderInvoice, 6 - ProcessProviderInvoice, 7 - WriteOffServiceProfit, 8 - Profit, 9 - PartnerAccrual, 10 - ProviderPayment, 11 - PartnerPayment, 12 - Refund, 13 - BankDeposit, 14 - BankWithdrawal, 15 - GoodwillCredit, 16 - WriteOffProfit, 17 - WriteOffDifferenceCurrency, 18 - AiDebit, 19 - AiCredit]


### Model OwnerChangeInstructionsDto
The owner change instructions parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **status** | **Integer** (int32) | The owner change instructions status. | [optional] [example: 1] |
| **message** | **String** | The owner change instructions message. | [optional] [example: Ownership transferred successfully] [nullable] |


### Model OwnerChangeInstructionsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**OwnerChangeInstructionsDto**](#model-ownerchangeinstructionsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model OwnerIdSettingsRequestDto
The request parameters for managing the owner-specific settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ownerId** | **UUID** (uuid) | The ID of the owner whose settings are being managed. | [required] [example: 00000000-0000-0000-0000-000000000001] |


### Model PasswordHasher

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **size** | **Integer** (int32) |  | [optional] |
| **iterations** | **Integer** (int32) |  | [optional] |
| **salt** | **String** |  | [optional] [nullable] |


### Model PasswordSettingsDto
The password settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **minLength** | **Integer** (int32) | The minimum number of characters required for valid passwords. | [required] [example: 8] |
| **upperCase** | **Boolean** | Specifies whether the password should contain the uppercase letters or not. | [required] [example: true] |
| **digits** | **Boolean** | Specifies whether the password should contain the digits or not. | [required] [example: true] |
| **specSymbols** | **Boolean** | Specifies whether the password should contain the special symbols or not. | [required] [example: false] |
| **allowedCharactersRegexStr** | **String** | The allowed password characters in the regex string format. | [required] [example: ^[a-zA-Z0-9!@#$%^&*()]+$] [nullable] |
| **digitsRegexStr** | **String** | The password digits in the regex string format. | [required] [example: (?=.*\\d)] [nullable] |
| **upperCaseRegexStr** | **String** | The password uppercase letters in the regex string format. | [required] [example: (?=.*[A-Z])] [nullable] |
| **specSymbolsRegexStr** | **String** | The passaword special symbols in the regex string format. | [required] [example: (?=.*[!@#$%^&*()])] [nullable] |


### Model PasswordSettingsRequestsDto
The request parameters for configuring the password complexity requirements.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **minLength** | **Integer** (int32) | The minimum number of characters required for valid passwords. | [required] [example: 8] |
| **upperCase** | **Boolean** | Specifies whether the password should contain the uppercase letters or not. | [optional] [example: true] |
| **digits** | **Boolean** | Specifies whether the password should contain the digits or not. | [optional] [example: true] |
| **specSymbols** | **Boolean** | Specifies whether the password should contain the special symbols or not. | [optional] [example: true] |


### Model PasswordSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**PasswordSettingsDto**](#model-passwordsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model PaymentCalculation
The parameters of the calculated payment amount.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **operationId** | **Long** (int64) | The operation unique identifier. | [optional] [example: 123456789] |
| **amount** | **Double** (double) | The calculated payment amount. | [optional] [example: 10.0] |
| **currency** | **String** | The three-character ISO 4217 currency symbol used for the payment calculation. | [optional] [example: USD] [nullable] |
| **quantity** | **Integer** (int32) | The quantity associated with the payment calculation. | [optional] [example: 1] |


### Model PaymentCalculationWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**PaymentCalculation**](#model-paymentcalculation) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model PaymentMethodStatus
[0 - None, 1 - Set, 2 - Expired]


### Model PaymentSettingsDto
The payment settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **salesEmail** | **String** | The email address for sales inquiries and support. | [required] [example: sales@example.com] [nullable] |
| **feedbackAndSupportUrl** | **String** | The URL for accessing the feedback and support resources. | [optional] [example: https://example.com] [nullable] |
| **buyUrl** | **String** | The URL for purchasing or upgrading the product. | [required] [example: https://example.com/buy] [nullable] |
| **standalone** | **Boolean** | Indicates whether the system is running in standalone mode. | [required] [example: false] |
| **currentLicense** | [**CurrentLicenseInfo**](#model-currentlicenseinfo) | The current license information. | [required] |
| **max** | **Integer** (int32) | The maximum quota quantity. | [required] [example: 1] |


### Model PaymentSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**PaymentSettingsDto**](#model-paymentsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model PaymentUrlRequestDto
The request parameters for the payment URL configuration with quantity information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **backUrl** | **URI** (uri) | The URL where the user will be redirected after payment cancellation. | [required] [example: https://example.com/payment/back] [minLength: 0] [maxLength: 255] |
| **successUrl** | **URI** (uri) | The URL where the user will be redirected after successful payment. | [required] [example: https://example.com/payment/success] [minLength: 0] [maxLength: 255] |
| **quantity** | **Map** (int32) | The payment quantity. | [required] [example: {admin=1}] |


### Model PluginsDto
The plugins parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies if the plugins are enabled or not. | [optional] [example: true] |
| **upload** | **Boolean** | Specifies if the plugins can be uploaded or not. | [optional] [example: true] |
| **delete** | **Boolean** | Specifies if the plugins can be deleted or not. | [optional] [example: true] |


### Model PriceDto
The price parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **value** | **Double** (double) | The price value. | [optional] [example: 99.99] [nullable] |
| **currencySymbol** | **String** | The currency symbol. | [optional] [example: $] [nullable] |
| **isoCurrencySymbol** | **String** | The three-character ISO 4217 currency symbol. | [optional] [example: USD] [nullable] |


### Model ProductAdministratorDto
The product administrator parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **productId** | **UUID** (uuid) | The product ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **userId** | **UUID** (uuid) | The user unique identifier. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **administrator** | **Boolean** | Indicates whether the user has administrator privileges for the product. | [required] [example: true] |


### Model ProductAdministratorWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ProductAdministratorDto**](#model-productadministratordto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ProductQuantityType
[0 - Set, 1 - Add, 2 - Sub, 3 - Renew]


### Model ProductType
[2 - Documents, 3 - Login, 7 - Settings, 8 - Contacts]


### Model QuantityRequestDto
The request parameters for specifying payment quantity.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **quantity** | **Map** (int32) | The mapping of item identifiers to their respective quantities in the payment. | [required] [example: {admin=1}] |


### Model Quota
The quota parameters.  &lt;example&gt;  {    id: 1,    quantity: 50,    wallet: false,    additional: false,    dueDate: 2026-03-31T00:00:00Z,    nextQuantity: 100,    state: Active  }  &lt;/example&gt;

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The quota ID. | [optional] |
| **quantity** | **Integer** (int32) | The quota quantity. | [optional] [example: 50] |
| **wallet** | **Boolean** | The quota applies to the wallet or not | [optional] [example: false] |
| **dueDate** | **Date** (date-time) | The quota due date. | [optional] [example: 2026-03-31T00:00:00Z] [nullable] |
| **nextQuantity** | **Integer** (int32) | The quota next quantity. | [optional] [example: 100] [nullable] |
| **additional** | **Boolean** | Indicates whether the quota is primary or additional. | [optional] [example: false] |
| **nextQuota** | **Integer** (int32) | The quota ID to switch to at the next period. | [optional] [nullable] |
| **state** | [**QuotaState**](#model-quotastate) | The quota state. | [optional] [enum: 0, 1] |


### Model QuotaArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-quotadto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model QuotaDto
The quota information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The quota ID. | [required] [example: 1] |
| **title** | **String** | The quota title. | [optional] [example: Basic Plan] [nullable] |
| **price** | [**PriceDto**](#model-pricedto) | The price parameters. | [required] |
| **nonProfit** | **Boolean** | Specifies if the quota is nonprofit or not. | [required] [example: false] |
| **free** | **Boolean** | Specifies if the quota is free or not. | [required] [example: true] |
| **trial** | **Boolean** | Specifies if the quota is trial or not. | [required] [example: false] |
| **features** | [**List**](#model-tenantquotafeaturedto) | The list of tenant quota features. | [required] [example: [{id=00000000-0000-0000-0000-000000000001, title=Premium Storage}]] [nullable] |
| **usersQuota** | [**TenantEntityQuotaSettings**](#model-tenantentityquotasettings) | The tenant entity quota settings. | [optional] |
| **roomsQuota** | [**TenantEntityQuotaSettings**](#model-tenantentityquotasettings) | The tenant entity quota settings. | [optional] |
| **aiAgentsQuota** | [**TenantEntityQuotaSettings**](#model-tenantentityquotasettings) | The tenant entity quota settings. | [optional] |
| **tenantCustomQuota** | [**TenantQuotaSettings**](#model-tenantquotasettings) | The tenant quota settings. | [optional] |
| **dueDate** | **Date** (date-time) | The due date. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |


### Model QuotaSettingsRequestsDto
The request parameters for managing the user storage quota configurations.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enableQuota** | **Boolean** | Specifies whether the storage quota restrictions are enabled. | [optional] [example: true] |
| **defaultQuota** | [**QuotaSettingsRequestsDto_defaultQuota**](#model-quotasettingsrequestsdtodefaultquota) |  | [required] |


### Model QuotaSettingsRequestsDto.defaultQuota
The default storage quota value applied to new users.


### Model QuotaState
[0 - Active, 1 - Overdue]


### Model QuotaWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**QuotaDto**](#model-quotadto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RecaptchaType
[0 - Default, 1 - AndroidV2, 2 - iOSV2, 3 - hCaptcha]


### Model RegStatus
[]


### Model ReportDto
Represents a report containing a collection of operations.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **collection** | [**List**](#model-operationdto) | A collection of operations. | [optional] [example: [{id=op1, type=payment}]] [nullable] |
| **offset** | **Integer** (int32) | The report data offset. | [optional] [example: 1] |
| **limit** | **Integer** (int32) | The report data limit. | [optional] [example: 1] |
| **totalQuantity** | **Long** (int64) | The total quantity of operations in the report. | [optional] [example: 1] |
| **totalPage** | **Integer** (int32) | The total number of pages in the report. | [optional] [example: 1] |
| **currentPage** | **Integer** (int32) | The current page number of the report. | [optional] [example: 1] |


### Model ReportWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ReportDto**](#model-reportdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RestrictedModelsResponse

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **models** | **List** |  | [required] [nullable] |


### Model RestrictedModelsResponseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**RestrictedModelsResponse**](#model-restrictedmodelsresponse) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomsNotificationSettingsDto
The rooms notification settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **disabledRooms** | **List** | The list of rooms with the disabled notifications. | [optional] [example: [1, 2, 3]] [nullable] |


### Model RoomsNotificationSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**RoomsNotificationSettingsDto**](#model-roomsnotificationsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomsNotificationsSettingsRequestDto
The request parameters for configuring notification settings for the chat or collaboration rooms.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomsId** | **null** | The target room identifier. | [optional] [example: {}] |
| **mute** | **Boolean** | Specifies whether the notifications will be delivered to the specified room or not. | [optional] [example: true] |


### Model STRINGArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **List** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SalesRequestsDto
The request parameters for handling sales and payment inquiries in the portal.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userName** | **String** | The name of the user submitting the sales request. | [required] [example: John Doe] [minLength: 1] [maxLength: 255] |
| **email** | **String** | The contact email address for the sales inquiry. | [required] [example: user@example.com] [minLength: 1] [maxLength: 64] |
| **message** | **String** | The details of the sales inquiry or payment request. | [required] [example: I would like to inquire about pricing] [minLength: 1] [maxLength: 255] |


### Model SecurityArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-securitydto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SecurityDto
The security information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **webItemId** | **String** | The module ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **users** | [**List**](#model-employeedto) | The list of users with the access to the module. | [optional] [example: [{displayName=John Doe}]] [nullable] |
| **groups** | [**List**](#model-groupsummarydto) | The list of groups with the access to the module. | [optional] [example: [{id=00000000-0000-0000-0000-000000000000, name=Administrators}]] [nullable] |
| **enabled** | **Boolean** | Specifies if the security settings are enabled or not. | [optional] [example: true] |
| **isSubItem** | **Boolean** | Specifies if the module is a subitem or not. | [optional] [example: true] |


### Model SecurityRequestsDto
The request parameters for managing user security and access permissions.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **productId** | **UUID** (uuid) | The product ID for which permissions are being set. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **userId** | **UUID** (uuid) | The ID of the user whose permissions are being configured. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **administrator** | **Boolean** | Specifies whether the user has administrative privileges. | [optional] [example: true] |


### Model SetAppEnabledBody
Request body for toggling an application enabled state.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Whether the application should be enabled. | [optional] |


### Model SetAppSettingsBody
Request body for saving application-specific settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **settings** | [**SetAppSettingsBody_settings**](#model-setappsettingsbodysettings) |  | [optional] |


### Model SetAppSettingsBody.settings
Arbitrary JSON document with application-specific settings.


### Model SetRestrictedAiModelsRequestDto
The request parameters for setting restricted AI models.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **models** | **Set** | The set of restricted AI model IDs. | [required] [example: [model1, model2]] |


### Model SettingsDto
The settings information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **timezone** | **String** | The time zone. | [optional] [example: UTC] [nullable] |
| **trustedDomains** | **List** | The list of the trusted domains. | [optional] [example: [mydomain.com, mydomain1.com]] [nullable] |
| **trustedDomainsType** | [**TenantTrustedDomainsType**](#model-tenanttrusteddomainstype) | The type of the tenant trusted domains. | [optional] [enum: 0, 1, 2] |
| **culture** | **String** | The language. | [required] [example: en-US] [nullable] |
| **utcOffset** | **String** (date-span) | The UTC offset in the TimeSpan format. | [optional] [example: -08:30:00] |
| **utcHoursOffset** | **Double** (double) | The UTC offset in hours. | [optional] [example: -8.5] |
| **greetingSettings** | **String** | The greeting settings. | [optional] [example: Web Office Applications] [nullable] |
| **ownerId** | **UUID** (uuid) | The owner ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **nameSchemaId** | **String** | The team template ID. | [optional] [example: default] [nullable] |
| **enabledJoin** | **Boolean** | Specifies if a user can join the portal or not. | [optional] [example: true] [nullable] |
| **enableAdmMess** | **Boolean** | Specifies if a user can send a message to the administrator when accessing the DocSpace portal or not. | [optional] [example: true] [nullable] |
| **thirdpartyEnable** | **Boolean** | Specifies if a user can connect third-party providers to the portal or not. | [optional] [example: true] [nullable] |
| **docSpace** | **Boolean** | Specifies if this portal is a DocSpace portal or not. | [optional] [example: true] |
| **standalone** | **Boolean** | Indicates whether the system is running in standalone mode. | [optional] [example: true] |
| **isAmi** | **Boolean** | Specifies if this portal is the AMI instance or not. | [optional] [example: true] |
| **baseDomain** | **String** | The base domain. | [required] [example: example.com] [nullable] |
| **wizardToken** | **String** | The wizard token. | [optional] [example: dGhpc2lzYXRva2Vu...] [nullable] |
| **passwordHash** | [**PasswordHasher**](#model-passwordhasher) | The password hash. | [optional] |
| **firebase** | [**FirebaseDto**](#model-firebasedto) | The Firebase parameters. | [optional] |
| **version** | **String** | The portal version. | [optional] [example: 12.5.0] [nullable] |
| **recaptchaType** | [**RecaptchaType**](#model-recaptchatype) | The type of CAPTCHA validation used. | [optional] [enum: 0, 1, 2, 3] |
| **recaptchaPublicKey** | **String** | The ReCAPTCHA public key. | [optional] [example: abc123def456] [nullable] |
| **debugInfo** | **Boolean** | Specifies if the debug information will be sent or not. | [optional] [example: true] |
| **socketUrl** | **String** | The socket URL. | [optional] [example: https://example.com] [nullable] |
| **tenantStatus** | [**TenantStatus**](#model-tenantstatus) | The tenant status. | [optional] [enum: 0, 1, 2, 3, 4, 5, 6] |
| **tenantAlias** | **String** | The tenant alias. | [optional] [example: mycompany] [nullable] |
| **displayAbout** | **Boolean** | Specifies whether to display the About portal section. | [optional] [example: true] |
| **domainValidator** | [**TenantDomainValidator**](#model-tenantdomainvalidator) | The domain validator. | [optional] |
| **zendeskKey** | **String** | The Zendesk key. | [optional] [example: abc123def456] [nullable] |
| **tagManagerId** | **String** | The tag manager ID. | [optional] [example: GTM-XXXXXX] [nullable] |
| **cookieSettingsEnabled** | **Boolean** | Specifies whether the cookie settings are enabled. | [required] [example: true] |
| **limitedAccessSpace** | **Boolean** | Specifies whether the access to the space management is limited or not. | [optional] [example: true] |
| **limitedAccessDevToolsForUsers** | **Boolean** | Specifies whether the access to the Developer Tools is limited for users or not. | [optional] [example: true] |
| **displayBanners** | **Boolean** | Specifies whether to display the promotional banners. | [optional] [example: true] |
| **aiEnabled** | **Boolean** | Specifies whether AI functionality (chat, agents, vectorization) is enabled for the current tenant.  When &#x60;false&#x60;, all AI features are disabled and the AI Agents folder is hidden. | [optional] [example: true] |
| **walletLowBalance** | **Boolean** | Specifies whether the tenant wallet balance is currently below the low-balance threshold. Only returned to portal administrators. | [optional] [example: false] [nullable] |
| **userNameRegex** | **String** | The user name validation regex. | [optional] [example: ^[a-zA-Z0-9_]{3,20}$] [nullable] |
| **invitationLimit** | **Integer** (int32) | The maximum number of invitations to the portal. | [optional] [example: 10] [nullable] |
| **plugins** | [**PluginsDto**](#model-pluginsdto) | The plugins settings. | [optional] |
| **deepLink** | [**DeepLinkDto**](#model-deeplinkdto) | The deep link settings. | [required] |
| **formGallery** | [**FormGalleryDto**](#model-formgallerydto) | The form gallery settings. | [optional] |
| **maxImageUploadSize** | **Long** (int64) | The maximum image upload size. | [optional] [example: 10485760] |
| **logoText** | **String** | The white label logo text. | [optional] [example: Company Name] [nullable] |
| **externalResources** | [**CultureSpecificExternalResources**](#model-culturespecificexternalresources) | The external resources settings. | [optional] |
| **defaultFolderType** | [**FolderType**](#model-foldertype) | The folder type. | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **externalDbEnabled** | **Boolean** | Specifies if an external database is connected for storing form results. | [optional] [example: true] |


### Model SettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**SettingsDto**](#model-settingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SetupCode

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **account** | **String** |  | [optional] [nullable] |
| **manualEntryKey** | **String** |  | [optional] [nullable] |
| **qrCodeSetupImageUrl** | **String** |  | [optional] [nullable] |


### Model SetupCodeWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**SetupCode**](#model-setupcode) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SmtpOperationStatusRequestsDto
The request parameters for tracking SMTP (Simple Mail Transfer Protocol) operation status.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **completed** | **Boolean** | Specifies whether the SMTP operation has finished processing. | [optional] [example: true] |
| **id** | **String** | The unique identifier for tracking the SMTP operation. | [optional] [example: smtp-op-123] [nullable] |
| **error** | **String** | The error message if the SMTP operation encountered issues. | [optional] [example: SMTP connection failed.] [nullable] |
| **status** | **String** | The current state of the SMTP operation. | [optional] [example: Completed] [nullable] |
| **percents** | **Integer** (int32) | The progress indicator showing completion percentage of the operation. | [optional] [example: 1] |


### Model SmtpOperationStatusRequestsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**SmtpOperationStatusRequestsDto**](#model-smtpoperationstatusrequestsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SmtpSettingsDto
The SMTP settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **host** | **String** | The SMTP host. | [optional] [example: mail.example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **port** | **Integer** (int32) | The SMTP port. | [optional] [example: 25] [min: 1] [max: 65535] [nullable] |
| **senderAddress** | **String** | The sender address. | [optional] [example: notify@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **senderDisplayName** | **String** | The sender display name. | [optional] [example: Postman] [minLength: 0] [maxLength: 255] [nullable] |
| **credentialsUserName** | **String** | The credentials username. | [optional] [example: notify@example.com] [minLength: 0] [maxLength: 255] [nullable] |
| **credentialsUserPassword** | **String** | The credentials user password. | [optional] [example: example value] [nullable] |
| **enableSSL** | **Boolean** | Specifies whether the SSL is enabled or not. | [optional] [example: true] |
| **enableAuth** | **Boolean** | Specifies whether the authentication is enabled or not. | [optional] [example: true] |
| **useNtlm** | **Boolean** | Specifies whether to use NTLM or not. | [optional] [example: true] |
| **isDefaultSettings** | **Boolean** | Specifies if the current settings are default or not. | [optional] [example: true] |


### Model SmtpSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**SmtpSettingsDto**](#model-smtpsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SsoCertificate
The SSO certificate parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **selfSigned** | **Boolean** | Specifies if a certificate is self-signed or not. | [optional] [example: false] |
| **crt** | **String** | The CRT certificate file. | [optional] [example: crt file] [nullable] |
| **key** | **String** | The certificate key. | [optional] [example: key] [nullable] |
| **action** | **String** | The certificate action. | [optional] [example: validate] [nullable] |
| **domainName** | **String** | The certificate domain name. | [optional] [example: example.com] [nullable] |
| **startDate** | **Date** (date-time) | The certificate start date. | [optional] [example: 2024-01-01T00:00:00Z] |
| **expiredDate** | **Date** (date-time) | The certificate expiration date. | [optional] [example: 2024-01-01T00:00:00Z] |


### Model SsoFieldMapping
The SSO field mapping.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **firstName** | **String** | The first name. | [optional] [example: givenName] [nullable] |
| **lastName** | **String** | The last name. | [optional] [example: sn] [nullable] |
| **email** | **String** (email) | The email address. | [optional] [example: sn@example.com] [nullable] |
| **title** | **String** | The title. | [optional] [example: SN] [nullable] |
| **location** | **String** | The location. | [optional] [example: Location] [nullable] |
| **phone** | **String** | The phone number. | [optional] [example: +14155552671] [nullable] |


### Model SsoIdpCertificateAdvanced
The IdP advanced certificate parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **verifyAlgorithm** | **String** | The certificate verification algorithm. | [optional] [example: rsa-sha256] [nullable] |
| **verifyAuthResponsesSign** | **Boolean** | Specifies if the signatures of the SAML authentication responses sent to SP will be verified or not. | [optional] [example: true] |
| **verifyLogoutRequestsSign** | **Boolean** | Specifies if the signatures of the SAML logout requests sent to SP will be verified or not. | [optional] [example: true] |
| **verifyLogoutResponsesSign** | **Boolean** | Specifies if the signatures of the SAML logout responses sent to SP will be verified or not. | [optional] [example: true] |
| **decryptAlgorithm** | **String** | The certificate decryption algorithm. | [optional] [example: aes256-cbc] [nullable] |
| **decryptAssertions** | **Boolean** | Specifies if the assertions will be decrypted or not. | [optional] [example: true] |


### Model SsoIdpSettings
The SSO IdP settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **entityId** | **String** | The entity ID. | [optional] [example: https://idp.company.com/saml] [nullable] |
| **ssoUrl** | **String** | The SSO URL. | [optional] [example: https://idp.example.com/sso] [nullable] |
| **ssoBinding** | **String** | The SSO binding. | [optional] [example: urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect] [nullable] |
| **sloUrl** | **String** | The SLO URL. | [optional] [example: https://idp.example.com/slo] [nullable] |
| **sloBinding** | **String** | The SLO binding. | [optional] [example: urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect] [nullable] |
| **nameIdFormat** | **String** | The name ID format. | [optional] [example: urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress] [nullable] |


### Model SsoSettingsRequestsDto
The request parameters for the Single Sign-On (SSO) configuration settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **serializeSettings** | **String** | The JSON-serialized SSO configuration settings. | [required] [example: {"enableSso":true,"idpSettings":{"entityId":"https://idp.example.com"}}] [nullable] |


### Model SsoSettingsV2
The SSO portal settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |
| **enableSso** | **Boolean** | Specifies if the SSO settings are enabled or not. | [optional] [example: false] [nullable] |
| **idpSettings** | [**SsoIdpSettings**](#model-ssoidpsettings) | The SSO IdP settings. | [optional] |
| **idpCertificates** | [**List**](#model-ssocertificate) | The list of the IdP certificates. | [optional] [example: [{crt=base64-cert-data, key=base64-key-data}]] [nullable] |
| **idpCertificateAdvanced** | [**SsoIdpCertificateAdvanced**](#model-ssoidpcertificateadvanced) | The IdP advanced certificate. | [optional] |
| **spLoginLabel** | **String** | The SP login label. | [optional] [example: Single Sign-on] [nullable] |
| **spCertificates** | [**List**](#model-ssocertificate) | The list of the SP certificates. | [optional] [example: [{crt=base64-cert-data, key=base64-key-data}]] [nullable] |
| **spCertificateAdvanced** | [**SsoSpCertificateAdvanced**](#model-ssospcertificateadvanced) | The SP advanced certificate. | [optional] |
| **fieldMapping** | [**SsoFieldMapping**](#model-ssofieldmapping) | The SSO field mapping. | [optional] |
| **hideAuthPage** | **Boolean** | Specifies if the authentication page will be hidden or not. | [optional] [example: false] |
| **usersType** | **Integer** (int32) | The user type. | [optional] [example: 1] |
| **disableEmailVerification** | **Boolean** | Specifies if the email verification is disabled or not. | [optional] [example: false] |


### Model SsoSettingsV2Wrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**SsoSettingsV2**](#model-ssosettingsv2) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SsoSpCertificateAdvanced
The SP advanced certificate parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **signingAlgorithm** | **String** | The certificate signing algorithm. | [optional] [example: rsa-sha256] [nullable] |
| **signAuthRequests** | **Boolean** | Specifies if SP will sign the SAML authentication requests sent to IdP or not. | [optional] [example: true] |
| **signLogoutRequests** | **Boolean** | Specifies if SP will sign the SAML logout requests sent to IdP or not. | [optional] [example: true] |
| **signLogoutResponses** | **Boolean** | Specifies if SP will sign the SAML logout responses sent to IdP or not. | [optional] [example: true] |
| **encryptAlgorithm** | **String** | The certificate encryption algorithm. | [optional] [example: aes256-cbc] [nullable] |
| **decryptAlgorithm** | **String** | The certificate decryption algorithm. | [optional] [example: aes256-cbc] [nullable] |
| **encryptAssertions** | **Boolean** | Specifies if the assertions will be encrypted or not. | [optional] [example: true] |


### Model StorageArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-storagedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model StorageDto
The storage information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The storage ID. | [required] [example: storage_001] [nullable] |
| **title** | **String** | The storage title. | [required] [example: Main Storage] [nullable] |
| **properties** | [**List**](#model-authkey) | The list of storage authentication keys. | [optional] [example: [{name=ApiKey, value=12345}]] [nullable] |
| **current** | **Boolean** | Specifies if this is the current portal storage or not. | [required] [example: true] |
| **isSet** | **Boolean** | Specifies if this storage can be set or not. | [required] [example: true] |


### Model StorageEncryptionRequestsDto
The request parameters for managing storage encryption operations and notifications.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **notifyUsers** | **Boolean** | Specifies whether the users receive notifications about the storage encryption operations. | [optional] [example: true] |


### Model StorageRequestsDto
The request parameters for configuring the storage module settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **module** | **String** | The name for the storage module to be configured. | [required] [example: default] [nullable] |
| **props** | [**List**](#model-itemkeyvaluepairstringstring) | The list of configuration key-value pairs for the storage module. | [optional] [example: [item1, item2]] [nullable] |


### Model StorageSettings

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **module** | **String** |  | [optional] [nullable] |
| **props** | **null** |  | [optional] |
| **lastModified** | **Date** (date-time) |  | [optional] |


### Model StorageSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**StorageSettings**](#model-storagesettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model StringWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **String** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model StudioDefaultPageSettings

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **defaultFolderType** | [**FolderType**](#model-foldertype) | Specifies the type of the default folder associated with the settings. | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model StudioDefaultPageSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**StudioDefaultPageSettings**](#model-studiodefaultpagesettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SubAccount
Represents a sub-account with a specific currency and balance.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **currency** | **String** | The three-character ISO 4217 currency symbol. | [optional] [example: "USD"] [nullable] |
| **amount** | **Double** (double) | The amount in the specified currency. | [optional] [example: 1500.75] |


### Model SubscriptionBalanceInfo
The information about the current subscription and its unused balance.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **totalCost** | **Double** (double) | The total cost of the current billing period (the sum across all subscription items). | [optional] [example: 120.0] |
| **currency** | **String** | The three-character ISO 4217 currency symbol of the subscription. | [optional] [example: USD] [nullable] |
| **periodStart** | **Date** (date-time) | The start of the current billing period. | [optional] [example: 2026-06-01T00:00:00Z] |
| **periodEnd** | **Date** (date-time) | The end of the current billing period. | [optional] [example: 2026-07-01T00:00:00Z] |
| **periodUsedUntil** | **Date** (date-time) | The boundary of the used part of the period (the moment of the request). | [optional] [example: 2026-06-23T14:35:00Z] |
| **daysElapsed** | **Integer** (int32) | The number of days elapsed since the start of the period (inclusive). | [optional] [example: 23] |
| **remainingBalance** | **Double** (double) | The unused balance of the subscription, in the subscription currency. | [optional] [example: 87.74] |
| **remainingBalanceInWalletCurrency** | **Double** (double) | The unused balance of the subscription, converted to the wallet currency. | [optional] [example: 87.74] |
| **walletCurrency** | **String** | The three-character ISO 4217 currency symbol of the wallet. | [optional] [example: USD] [nullable] |


### Model SubscriptionBalanceInfoWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**SubscriptionBalanceInfo**](#model-subscriptionbalanceinfo) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Tariff
The tariff parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The tariff ID. | [optional] [example: 1] |
| **state** | [**TariffState**](#model-tariffstate) | The tariff state. | [optional] [enum: 0, 1, 2, 3] |
| **dueDate** | **Date** (date-time) | The tariff due date. | [required] [example: 2026-03-31T00:00:00Z] |
| **delayDueDate** | **Date** (date-time) | The tariff delay due date. | [optional] [example: 2026-04-07T00:00:00Z] |
| **licenseDate** | **Date** (date-time) | The tariff license date. | [optional] [example: 2026-03-01T00:00:00Z] |
| **customerId** | **String** | The tariff customer ID. | [optional] [example: cus_123] [nullable] |
| **quotas** | [**List**](#model-quota) | The list of tariff quotas. | [required] [example: {quotas=[{id=1, quantity=50, wallet=false}]}] [nullable] |
| **overdueQuotas** | [**List**](#model-quota) | The list of overdue tariff quotas. | [optional] [example: []] [nullable] |


### Model TariffState
[0 - Trial, 1 - Paid, 2 - Delay, 3 - Not paid]


### Model TariffWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**Tariff**](#model-tariff) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TelegramStatusDto
The Telegram connection status parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **status** | [**RegStatus**](#model-regstatus) | The registration Telegram status. | [required] [enum: 0, 1, 2] |
| **username** | **String** | The Telegram username. | [optional] [example: john_doe] [nullable] |


### Model TelegramStatusWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TelegramStatusDto**](#model-telegramstatusdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantAiAccessSettings
The tenant-level settings for enabling or disabling all AI functionality in DocSpace.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies whether AI functionality is enabled for the tenant.  When set to &#x60;false&#x60;, all AI features (chat, agents, vectorization) are disabled tenant-wide. | [optional] [example: true] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantAiAccessSettingsDto
The request parameters for managing the tenant-level AI access settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies whether AI functionality is enabled for the tenant.  Set to &#x60;true&#x60; to enable all AI features or &#x60;false&#x60; to disable them tenant-wide. | [optional] [example: false] |


### Model TenantAiAccessSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantAiAccessSettings**](#model-tenantaiaccesssettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantAiAgentQuotaSettings
The AI agent quota settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enableQuota** | **Boolean** | Specifies if the quota is enabled for the tenant entity or not. | [optional] |
| **defaultQuota** | **Long** (int64) | The default quota of the tenant entity. | [optional] |
| **lastRecalculateDate** | **Date** (date-time) | The date of the last quota recalculation. | [optional] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] |


### Model TenantAiAgentQuotaSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantAiAgentQuotaSettings**](#model-tenantaiagentquotasettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantAuditSettings
The tenant audit settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **loginHistoryLifeTime** | **Integer** (int32) | The login history lifetime. | [optional] [example: 180] |
| **auditTrailLifeTime** | **Integer** (int32) | The audit trail lifetime. | [optional] [example: 180] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantAuditSettingsWrapper
The tenant audit settings wrapper.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **settings** | [**TenantAuditSettings**](#model-tenantauditsettings) | The tenant audit settings parameters. | [optional] |


### Model TenantBannerSettings
The visibility settings of the promotional banners.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **hidden** | **Boolean** | The banners visibility flag. | [optional] [example: false] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantBannerSettingsDto
The request parameters for managing the visibility settings of the promotional banners for the current tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **hidden** | **Boolean** | The banners visibility flag. | [optional] [example: true] |


### Model TenantBannerSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantBannerSettings**](#model-tenantbannersettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantDeepLinkSettings
The deep link settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **handlingMode** | [**DeepLinkHandlingMode**](#model-deeplinkhandlingmode) | The deep link handling mode. | [optional] [enum: 0, 1, 2] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantDeepLinkSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantDeepLinkSettings**](#model-tenantdeeplinksettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantDevToolsAccessSettings
The Developer Tools access settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **limitedAccessForUsers** | **Boolean** | Specifies if the Developer Tools access are limited for users or not. | [optional] [example: false] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantDevToolsAccessSettingsDto
The request parameters for managing the Developer Tools access settings for the current tenant.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **limitedAccessForUsers** | **Boolean** | Determines if users have restricted access to the Developer Tools. | [optional] [example: false] |


### Model TenantDevToolsAccessSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantDevToolsAccessSettings**](#model-tenantdevtoolsaccesssettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantDomainValidator
The domain validator.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **regex** | **String** | The regex string to validate a domain. | [optional] [example: ^[a-z0-9]([a-z0-9-]){1,61}[a-z0-9]$] [nullable] |
| **minLength** | **Integer** (int32) | The minimum length of the valid domain. | [optional] [example: 6] |
| **maxLength** | **Integer** (int32) | The maximum length of the valid domain. | [optional] [example: 63] |


### Model TenantDto
The tenant parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **affiliateId** | **String** | The affiliate ID. | [optional] [example: AFF12345] [nullable] |
| **tenantAlias** | **String** | The tenant alias. | [optional] [example: my-company] [nullable] |
| **calls** | **Boolean** | Specifies if the calls are available for this tenant or not. | [optional] [example: true] |
| **campaign** | **String** | The tenant campaign. | [optional] [example: WINTER2024] [nullable] |
| **creationDateTime** | **Date** (date-time) | The tenant creation date and time. | [optional] [example: 2024-01-15T10:30:00Z] |
| **hostedRegion** | **String** | The hosted region. | [optional] [example: EU] [nullable] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **industry** | [**TenantIndustry**](#model-tenantindustry) | The tenant industry. | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16] |
| **language** | **String** | The tenant language. | [optional] [example: en-US] [nullable] |
| **lastModified** | **Date** (date-time) | The date and time when the tenant was last modified. | [optional] [example: 2024-02-10T14:20:00Z] |
| **mappedDomain** | **String** | The tenant mapped domain. | [optional] [example: mycompany.example.com] [nullable] |
| **name** | **String** | The tenant name. | [optional] [example: My Company] [nullable] |
| **ownerId** | **UUID** (uuid) | The tenant owner ID. | [optional] [example: 00000000-0000-0000-0000-000000000001] |
| **paymentId** | **String** | The tenant payment ID. | [optional] [example: PAY123456789] [nullable] |
| **spam** | **Boolean** | Specifies if the ONLYOFFICE newsletter is allowed or not. | [optional] [example: false] |
| **status** | [**TenantStatus**](#model-tenantstatus) | The tenant status. | [optional] [enum: 0, 1, 2, 3, 4, 5, 6] |
| **statusChangeDate** | **Date** (date-time) | The date and time when the tenant status was changed. | [optional] [example: 2024-01-15T10:30:00Z] |
| **timeZone** | **String** | The tenant time zone. | [optional] [example: America/New_York] [nullable] |
| **trustedDomains** | **List** | The list of tenant trusted domains. | [optional] [example: [example.com, trusted.com]] [nullable] |
| **trustedDomainsRaw** | **String** | The tenant trusted domains in the string format. | [optional] [example: example.com,trusted.com] [nullable] |
| **trustedDomainsType** | [**TenantTrustedDomainsType**](#model-tenanttrusteddomainstype) | The type of the tenant trusted domains. | [optional] [enum: 0, 1, 2] |
| **version** | **Integer** (int32) | The tenant version | [optional] [example: 2] |
| **versionChanged** | **Date** (date-time) | The date and time when the tenant version was changed. | [optional] [example: 2024-02-01T09:00:00Z] |
| **region** | **String** | The tenant AWS region. | [optional] [example: us-east-1] [nullable] |


### Model TenantEntityQuotaSettings
The tenant entity quota settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enableQuota** | **Boolean** | Specifies if the quota is enabled for the tenant entity or not. | [optional] [example: true] |
| **defaultQuota** | **Long** (int64) | The default quota of the tenant entity. | [optional] [example: 1000] |
| **lastRecalculateDate** | **Date** (date-time) | The date of the last quota recalculation. | [optional] [example: 2024-01-01T00:00:00Z] [nullable] |


### Model TenantIndustry
[0 - Other, 1 - Accounting, 2 - Advertising marketing PR, 3 - Banking, 4 - Consulting, 5 - Design, 6 - Education, 7 - Environment, 8 - Financial services, 9 - Health care, 10 - IT, 11 - Legal, 12 - Manufacturing, 13 - Public sector, 14 - Publishing, 15 - Retail sales, 16 - Telecommunications]


### Model TenantQuota
The current tenant quota.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **name** | **String** | The tenant name. | [optional] [example: Default] [nullable] |
| **price** | **Double** (double) | The tenant price. | [optional] [example: 10.0] |
| **priceCurrencySymbol** | **String** | The tenant price currency symbol. | [optional] [example: $] [nullable] |
| **priceISOCurrencySymbol** | **String** | The tenant price three-character ISO 4217 currency symbol. | [optional] [example: USD] [nullable] |
| **productId** | **String** | The tenant product ID. | [optional] [example: 64] [nullable] |
| **serviceName** | **String** | The service name. | [optional] [example: backup] [nullable] |
| **serviceGroup** | **String** | The service group. | [optional] [example: services] [nullable] |
| **visible** | **Boolean** | Specifies if the tenant quota is visible or not. | [optional] [example: true] |
| **wallet** | **Boolean** | Specifies if the tenant quota applies to the wallet or not | [optional] [example: true] |
| **additional** | **Boolean** | Specifies if the tenant quota is primary or additional. | [optional] [example: false] |
| **dueDate** | **Date** (date-time) | The quota due date. | [optional] [example: 2021-01-01T00:00:00] [nullable] |
| **features** | **String** | The tenant quota features. | [optional] [example: audit,ldap,sso] [nullable] |
| **maxFileSize** | **Long** (int64) | The tenant maximum file size. | [optional] [example: 25000000] |
| **maxTotalSize** | **Long** (int64) | The tenant maximum total size. | [optional] [example: 25000000000] |
| **countUser** | **Integer** (int32) | The number of portal users. | [optional] [example: 100] |
| **countRoomAdmin** | **Integer** (int32) | The number of portal room administrators. | [optional] [example: 10] |
| **usersInRoom** | **Integer** (int32) | The number of room users. | [optional] [example: 50] |
| **countRoom** | **Integer** (int32) | The number of rooms. | [optional] [example: 500] |
| **nonProfit** | **Boolean** | Specifies if the tenant quota is nonprofit or not. | [optional] [example: false] |
| **trial** | **Boolean** | Specifies if the tenant quota is trial or not. | [optional] [example: false] |
| **free** | **Boolean** | Specifies if the tenant quota is free or not. | [optional] [example: false] |
| **update** | **Boolean** | Specifies if the tenant quota is updated or not. | [optional] [example: false] |
| **audit** | **Boolean** | Specifies if the audit trail is available or not. | [optional] [example: true] |
| **docsEdition** | **Boolean** | Specifies if ONLYOFFICE Docs is included in the tenant quota or not. | [optional] [example: true] |
| **ldap** | **Boolean** | Specifies if the LDAP settings are available or not. | [optional] [example: true] |
| **sso** | **Boolean** | Specifies if the SSO settings are available or not. | [optional] [example: true] |
| **statistic** | **Boolean** | Specifies if the statistics settings are available or not. | [optional] [example: true] |
| **branding** | **Boolean** | Specifies if the branding settings are available or not. | [optional] [example: true] |
| **customization** | **Boolean** | Specifies if the customization settings are available or not. | [optional] [example: true] |
| **lifetime** | **Boolean** | Specifies if the license has the lifetime settings or not. | [optional] [example: false] |
| **automationApi** | **Boolean** | Specifies if the Automation API is available or not. | [optional] [example: true] |
| **custom** | **Boolean** | Specifies if the custom domain URL is available or not. | [optional] [example: false] |
| **restore** | **Boolean** | Specifies if the restore is enabled or not. | [optional] [example: true] |
| **oauth** | **Boolean** | Specifies if Oauth is available or not. | [optional] [example: true] |
| **contentSearch** | **Boolean** | Specifies if the content search is available or not. | [optional] [example: true] |
| **thirdParty** | **Boolean** | Specifies if the third-party accounts linking is available or not. | [optional] [example: true] |
| **year** | **Boolean** | Specifies if the tenant quota is yearly subscription or not. | [optional] [example: true] |
| **countFreeBackup** | **Integer** (int32) | The number of free backups within a month. | [optional] [example: 1] |
| **backup** | **Boolean** | Specifies if the backup enabled as a wallet service or not. | [optional] [example: true] |
| **countAIAgent** | **Integer** (int32) | The number of AI agents. | [optional] [example: 5] |
| **aiTools** | **Boolean** | Specifies if the AI tools enabled as a wallet service or not. | [optional] [example: true] |
| **aiSearch** | **Boolean** | Specifies if the AI search enabled as a wallet service or not. | [optional] [example: true] |
| **docsCloud** | **Integer** (int32) | The number of DocsCloud users. | [optional] [example: true] |
| **docsCloudDevPack** | **Boolean** | Specifies if the DocsCloudDevPack enabled or not. | [optional] [example: true] |
| **docsCloudTrial** | **Boolean** | Specifies if the DocsCloudTrial enabled or not. | [optional] [example: true] |


### Model TenantQuotaFeatureDto
The tenant quota feature parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The ID of the tenant quota feature. | [optional] [example: 00000000-0000-0000-0000-000000000001] [nullable] |
| **title** | **String** | The title of the tenant quota feature. | [optional] [example: Premium Storage] [nullable] |
| **image** | **String** | The image URL of the tenant quota feature. | [optional] [example: /images/premium-storage.png] [nullable] |
| **value** | **null** | The value of the tenant quota feature. | [optional] [example: {}] |
| **type** | **String** | The type of the tenant quota feature. | [optional] [example: Storage] [nullable] |
| **used** | [**FeatureUsedDto**](#model-featureuseddto) | The used space parameters of the tenant quota feature. | [optional] |
| **priceTitle** | **String** | The price title of the tenant quota feature. | [optional] [example: $9.99/month] [nullable] |


### Model TenantQuotaSettings
The tenant quota settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enableQuota** | **Boolean** | Specifies if the tenant quota is enabled or not. | [optional] [example: true] |
| **quota** | **Long** (int64) | The tenant quota. | [optional] [example: 10737418240] |
| **lastRecalculateDate** | **Date** (date-time) | The date of the last tenant quota recalculation. | [optional] [example: 1990-01-01T00:00:00Z] [nullable] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantQuotaSettingsRequestsDto
The request parameters for managing the tenant storage quota settings in a multi-tenant system.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **tenantId** | **Integer** (int32) | The ID of the tenant whose quota is being configured. | [required] [example: 1] |
| **quota** | **Long** (int64) | The storage quota limit in bytes allocated to the tenant. | [optional] [example: 1048576] |


### Model TenantQuotaSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantQuotaSettings**](#model-tenantquotasettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantQuotaWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantQuota**](#model-tenantquota) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantRoomQuotaSettings
The room quota settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enableQuota** | **Boolean** | Specifies if the quota is enabled for the tenant entity or not. | [optional] |
| **defaultQuota** | **Long** (int64) | The default quota of the tenant entity. | [optional] |
| **lastRecalculateDate** | **Date** (date-time) | The date of the last quota recalculation. | [optional] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] |


### Model TenantRoomQuotaSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantRoomQuotaSettings**](#model-tenantroomquotasettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantStatus
[0 - Active, 1 - Suspended, 2 - Remove pending, 3 - Transfering, 4 - Restoring, 5 - Migrating, 6 - Encryption]


### Model TenantTrustedDomainsType
[0 - None, 1 - Custom, 2 - All]


### Model TenantUserInvitationSettingsDto
The user invitation settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **allowInvitingMembers** | **Boolean** | Specifies whether to allow inviting new DocSpace members through the Contacts section. | [required] [example: true] |
| **allowInvitingGuests** | **Boolean** | Specifies whether to allow all DocSpace members to invite external guests to the rooms. | [required] [example: false] |


### Model TenantUserInvitationSettingsRequestDto
The request parameters for updating the user invitation settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **allowInvitingMembers** | **Boolean** | Specifies whether to allow inviting new DocSpace members through the Contacts section. | [optional] [example: true] |
| **allowInvitingGuests** | **Boolean** | Specifies whether to allow all DocSpace members to invite external guests to the rooms. | [optional] [example: false] |


### Model TenantUserInvitationSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantUserInvitationSettingsDto**](#model-tenantuserinvitationsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantUserQuotaSettings
The user quota settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enableQuota** | **Boolean** | Specifies if the quota is enabled for the tenant entity or not. | [optional] |
| **defaultQuota** | **Long** (int64) | The default quota of the tenant entity. | [optional] |
| **lastRecalculateDate** | **Date** (date-time) | The date of the last quota recalculation. | [optional] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] |


### Model TenantUserQuotaSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantUserQuotaSettings**](#model-tenantuserquotasettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantWalletService
[-18 - AISearch, -16 - DocsCloudDevPack, -15 - DocsCloud, -14 - Admin, -13 - AITools, -12 - Backup, -11 - Storage]


### Model TenantWalletServiceSettings
The wallet services settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabledServices** | **List** | The list of the enabled wallet services. | [optional] [example: [-11, -12]] [enum: -18, -16, -15, -14, -13, -12, -11] [nullable] |
| **lastModified** | **Date** (date-time) | The date and time when the wallet services settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantWalletServiceSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantWalletServiceSettings**](#model-tenantwalletservicesettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TenantWalletSettings
The tenant wallet settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies whether automatic top-up for the tenant wallet is enabled. | [optional] [example: true] |
| **minBalance** | **Integer** (int32) | The minimum wallet balance at which automatic top-up will be triggered. Must be between 5 and 1000. | [optional] [example: 10] [min: 5] [max: 1000] |
| **upToBalance** | **Integer** (int32) | The maximum wallet balance at which automatic top-up will be triggered. Must be between 6 and 5000. | [optional] [example: 100] [min: 6] [max: 5000] |
| **currency** | **String** | The three-character ISO 4217 currency symbol. | [optional] [example: USD] [nullable] |
| **lowBalanceThreshold** | **Integer** (int32) | The wallet balance below which a low-balance notification is sent. Set internally, not user-configurable. | [optional] [example: 1] |
| **lowBalanceNotified** | **Boolean** | Specifies whether a low-balance notification has already been sent for the current dip below ASC.Core.Tenants.TenantWalletSettings.LowBalanceThreshold. | [optional] [example: false] |
| **lastModified** | **Date** (date-time) | The date and time when the tenant wallet settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model TenantWalletSettingsWrapper
The wrapper for the tenant wallet settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **settings** | [**TenantWalletSettings**](#model-tenantwalletsettings) | The tenant wallet settings. | [optional] |


### Model TenantWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TenantDto**](#model-tenantdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TfaAppCodeArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-tfaappcodedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TfaAppCodeDto
The TFA app code.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **isUsed** | **Boolean** | The TFA app code usage status. | [optional] [example: true] |
| **code** | **String** | The TFA app code. | [optional] [example: 123456] [nullable] |


### Model TfaConfirmDataDto
The TFA confirmation data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **url** | **String** | The confirmation URL. | [optional] [example: https://example.com/confirm?type=TfaAuth&key=abc123] [nullable] |
| **cookieName** | **String** | The confirmation cookie name. | [optional] [example: asc_confirm_key_TfaAuth] [nullable] |
| **cookieValue** | **String** | The confirmation cookie value. | [optional] [example: 1234567890.abcdef] [nullable] |


### Model TfaConfirmDataWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**TfaConfirmDataDto**](#model-tfaconfirmdatadto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TfaRequestsDto
The request parameters for configuring the Two-Factor Authentication (TFA) settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | [**TfaRequestsDtoType**](#model-tfarequestsdtotype) | The two-factor authentication type. | [optional] [enum: 0, 1, 2] |
| **id** | **UUID** (uuid) | The ID of the user for whom the TFA settings are being configured. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **trustedIps** | **List** | The list of IP addresses that bypass TFA verification. | [optional] [example: [item1, item2]] [nullable] |
| **mandatoryUsers** | **List** (uuid) | The list of user IDs for whom TFA is mandatory. | [optional] [example: [00000000-0000-0000-0000-000000000000]] [nullable] |
| **mandatoryGroups** | **List** (uuid) | The list group IDs whose members must use TFA. | [optional] [example: [00000000-0000-0000-0000-000000000000]] [nullable] |


### Model TfaRequestsDtoType
[0 - None, 1 - Sms, 2 - App]


### Model TfaSettingsArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-tfasettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TfaSettingsDto
The parameters representing the Two-Factor Authentication (TFA) configuration settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The ID of the TFA configuration. | [required] [example: tfa-default] [nullable] |
| **title** | **String** | The display name or description of the TFA configuration. | [required] [example: Default TFA policy] [nullable] |
| **enabled** | **Boolean** | Indicates whether the TFA configuration is currently active. | [required] [example: true] |
| **available** | **Boolean** | Indicates whether the TFA configuration can be used. | [required] [example: true] |
| **trustedIps** | **List** | The list of IP addresses that are exempt from TFA requirements. | [optional] [example: [item1, item2]] [nullable] |
| **mandatoryUsers** | **List** (uuid) | The list of user IDs that are required to use TFA. | [optional] [example: [00000000-0000-0000-0000-000000000000]] [nullable] |
| **mandatoryGroups** | **List** (uuid) | The list of group IDs whose members are required to use TFA. | [optional] [example: [00000000-0000-0000-0000-000000000000]] [nullable] |


### Model TfaValidateRequestsDto
The request parameters for validating the two-factor authentication codes.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **code** | **String** | The verification code provided by the user. | [required] [example: 123456] [nullable] |
| **session** | **Boolean** | Specifies whether the authentication is session-based. | [optional] [example: true] |


### Model TimezonesRequestsArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-timezonesrequestsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model TimezonesRequestsDto
The request parameters for managing the time zone configurations.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The IANA time zone identifier. | [required] [example: America/New_York] [nullable] |
| **displayName** | **String** | The user-friendly name for the time zone. | [required] [example: (UTC-05:00) Eastern Time (US and Canada)] [nullable] |


### Model TopUpDepositRequestDto
The request parameters for putting money on deposit.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **amount** | **Integer** (int32) | The amount of money for the operation. | [optional] [example: 1] [min: 1] [max: 999999] |
| **currency** | **String** | The three-character ISO 4217 currency symbol. | [optional] [example: USD] [minLength: 0] [maxLength: 3] [nullable] |


### Model TransactionInfo
Represents information about the transaction applied to an account.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **currency** | **String** | The three-character ISO 4217 currency symbol. | [optional] |
| **amount** | **Double** (double) | The amount in the specified currency. | [optional] |
| **date** | **Date** (date-time) | The date and time when the credit transaction occurred. | [optional] |


### Model TurnOnAdminMessageSettingsRequestDto
The request parameters for enabling or disabling administrator messaging system.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **turnOn** | **Boolean** | The global switch for the administrator messaging functionality. | [optional] [example: true] |


### Model UpcomingPaymentArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-upcomingpaymentdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model UpcomingPaymentDto
The upcoming payment parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The quota ID. | [optional] [example: -11] |
| **name** | **String** | The quota name. | [optional] [example: storage] [nullable] |
| **title** | **String** | The quota title. | [optional] [example: Business plan] [nullable] |
| **unitOfMeasure** | **String** | The quota unit of measure. | [optional] [example: admins] [nullable] |
| **quantity** | **Integer** (int32) | The quantity that will be charged (the next quantity if set, otherwise the current quantity). | [optional] [example: 100] |
| **wallet** | **Boolean** | The quota applies to the wallet or not. | [optional] [example: true] |
| **dueDate** | [**ApiDateTime**](#model-apidatetime) | The API date and time parameters. | [optional] |
| **amount** | **Double** (double) | The amount that will be charged (unit price multiplied by the quantity). | [optional] [example: 14] |
| **currency** | **String** | The three-character ISO 4217 currency symbol of the amount. | [optional] [example: USD] [nullable] |


### Model UpdateWebhooksConfigRequestsDto
The request parameters for updating the webhook configuration.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The human-readable name of the webhook configuration. | [required] [minLength: 0] [maxLength: 50] |
| **uri** | **String** | The destination URL where the webhook events will be sent. | [required] [minLength: 1] |
| **secretKey** | **String** | The webhook secret key used to sign the webhook payloads for the security verification. | [optional] [minLength: 0] [maxLength: 50] |
| **enabled** | **Boolean** | Specifies whether the webhook configuration is active or not. | [optional] |
| **ssl** | **Boolean** | Specifies whether the SSL certificate verification is required or not. | [optional] |
| **triggers** | [**WebhookTrigger**](#model-webhooktrigger) | The webhook trigger type. | [optional] [enum: 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824] |
| **targetId** | **String** | Target ID | [optional] [minLength: 0] [maxLength: 255] |
| **id** | **Integer** (int32) | The webhook configuration ID. | [required] |


### Model UsageSpaceStatItemArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-usagespacestatitemdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model UsageSpaceStatItemDto
The parameters of the usage space statistics item.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The item name. | [optional] [example: Item name] [nullable] |
| **icon** | **String** | The item icon path. | [optional] [example: Item icon path] [nullable] |
| **disabled** | **Boolean** | Specifies if the item is disabled or not. | [optional] [example: true] |
| **size** | **String** | The item used space. | [optional] [example: 0 Byte] [nullable] |
| **url** | **String** | The item URL. | [optional] [example: Item url] [nullable] |


### Model UserInfo
The user information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **firstName** | **String** | The user&#39;s first name. | [optional] [example: John] [nullable] |
| **lastName** | **String** | The user&#39;s last name. | [optional] [example: Doe] [nullable] |
| **userName** | **String** | The user username. | [optional] [example: johndoe] [nullable] |
| **birthDate** | **Date** (date-time) | The user birthday. | [optional] [example: 1990-01-01T00:00:00Z] [nullable] |
| **sex** | **Boolean** | The user sex (male or female). | [optional] [example: true] [nullable] |
| **status** | [**EmployeeStatus**](#model-employeestatus) | The user status. | [optional] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | [**EmployeeActivationStatus**](#model-employeeactivationstatus) | The user activation status. | [optional] [enum: 0, 1, 2, 4] |
| **terminatedDate** | **Date** (date-time) | The date and time when the user account was terminated. | [optional] [example: 2025-12-31T23:59:59Z] [nullable] |
| **title** | **String** | The user title. | [optional] [example: Manager] [nullable] |
| **workFromDate** | **Date** (date-time) | The user registration date. | [optional] [example: 2020-01-15T00:00:00Z] [nullable] |
| **email** | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [nullable] |
| **contacts** | **String** | The list of user contacts in the string format. | [optional] [example: skype:johndoe\|telegram:@johndoe] [nullable] |
| **contactsList** | **List** | The list of user contacts. | [optional] [example: [skype:johndoe, telegram:@johndoe]] [nullable] |
| **location** | **String** | The user location. | [optional] [example: New York, USA] [nullable] |
| **notes** | **String** | The user notes. | [optional] [example: Additional information about the user] [nullable] |
| **removed** | **Boolean** | Specifies if the user account was removed or not. | [optional] [example: false] |
| **lastModified** | **Date** (date-time) | The date and time when the user account was last modified. | [optional] [example: 2025-02-08T10:30:00Z] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **isActive** | **Boolean** | Specifies if the user is active or not. | [optional] [example: true] |
| **cultureName** | **String** | The user culture code. | [optional] [example: en-US] [nullable] |
| **mobilePhone** | **String** | The user mobile phone. | [optional] [example: +1234567890] [nullable] |
| **mobilePhoneActivationStatus** | [**MobilePhoneActivationStatus**](#model-mobilephoneactivationstatus) | The user mobile phone activation status. | [optional] [enum: 0, 1] |
| **sid** | **String** | The LDAP user identifier. | [optional] [example: S-1-5-21-3623811015-3361044348-30300820-1013] [nullable] |
| **ldapQouta** | **Long** (int64) | The LDAP user quota attribute. | [optional] [example: 1073741824] |
| **ssoNameId** | **String** | The SSO SAML user identifier. | [optional] [example: johndoe@example.com] [nullable] |
| **ssoSessionId** | **String** | The SSO SAML user session identifier. | [optional] [example: _1a2b3c4d5e6f7g8h9i0j] [nullable] |
| **createDate** | **Date** (date-time) | The date and time when the user account was created. | [optional] [example: 2020-01-15T00:00:00Z] |
| **createdBy** | **UUID** (uuid) | The ID of the user who created the current user account. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **spam** | **Boolean** | Specifies if tips, updates and offers are allowed to be sent to the user or not. | [optional] [example: false] [nullable] |
| **checkActivation** | **Boolean** | Indicates whether the activation status of the employee or recipient is unchecked or inactive.  Depending on the context, this property evaluates the activation or eligibility status accordingly. | [optional] [example: false] |


### Model UserInfoWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**UserInfo**](#model-userinfo) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ValidationResult
[0 - Ok, 1 - Invalid, 2 - Expired, 3 - Tariff limit, 4 - User existed, 5 - User excluded, 6 - Quota failed]


### Model WalletQuantityRequestDto
The request parameters for specifying wallet payment quantity.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **quantity** | **Map** (int32) | The mapping of item identifiers to their respective quantities in the payment. | [required] [example: {admin=1}] |
| **productQuantityType** | [**ProductQuantityType**](#model-productquantitytype) | The type of action performed on a product&#39;s quantity. | [optional] [enum: 0, 1, 2, 3] |


### Model WalletServiceArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-walletservicedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WalletServiceDto
The wallet service information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The quota ID. | [required] |
| **title** | **String** | The quota title. | [optional] |
| **price** | [**PriceDto**](#model-pricedto) | The price parameters. | [required] |
| **nonProfit** | **Boolean** | Specifies if the quota is nonprofit or not. | [required] |
| **free** | **Boolean** | Specifies if the quota is free or not. | [required] |
| **trial** | **Boolean** | Specifies if the quota is trial or not. | [required] |
| **features** | [**List**](#model-tenantquotafeaturedto) | The list of tenant quota features. | [required] |
| **usersQuota** | [**TenantEntityQuotaSettings**](#model-tenantentityquotasettings) | The tenant entity quota settings. | [optional] |
| **roomsQuota** | [**TenantEntityQuotaSettings**](#model-tenantentityquotasettings) | The tenant entity quota settings. | [optional] |
| **aiAgentsQuota** | [**TenantEntityQuotaSettings**](#model-tenantentityquotasettings) | The tenant entity quota settings. | [optional] |
| **tenantCustomQuota** | [**TenantQuotaSettings**](#model-tenantquotasettings) | The tenant quota settings. | [optional] |
| **dueDate** | **Date** (date-time) | The due date. | [optional] |
| **innerServices** | [**List**](#model-walletservicedto) | The list of inner services. | [optional] [nullable] |
| **serviceName** | **String** | The service name. | [optional] [nullable] |


### Model WalletServiceWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**WalletServiceDto**](#model-walletservicedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebItemSecurityRequestsDto
The request parameters for configuring security settings of a single web module.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The module ID. | [required] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **enabled** | **Boolean** | Controls whether the security restrictions are enforced for this module. | [optional] [example: true] |
| **subjects** | **List** (uuid) | The collection of user and group identifiers granted access to the module. | [optional] [example: [00000000-0000-0000-0000-000000000000]] [nullable] |


### Model WebItemsSecurityRequestsDto
The request parameters for configuring security settings across multiple web modules.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **items** | [**List**](#model-itemkeyvaluepairstringboolean) | The list of module security configurations. | [optional] [example: [item1, item2]] [nullable] |


### Model WebPluginArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-webplugindto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebPluginDto
The web plugin information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The web plugin name. | [required] [example: Example Plugin] [nullable] |
| **version** | **String** | The web plugin version. | [required] [example: 1.0.0] [nullable] |
| **minDocSpaceVersion** | **String** | The minimum version of DocSpace with which the plugin is guaranteed to work. | [optional] [example: 12.0.0] [nullable] |
| **description** | **String** | The web plugin description. | [required] [example: A plugin that provides additional functionality] [nullable] |
| **license** | **String** | The web plugin license. | [required] [example: MIT] [nullable] |
| **author** | **String** | The web plugin author. | [required] [example: ONLYOFFICE] [nullable] |
| **homePage** | **String** | The web plugin home page URL. | [required] [example: https://example.com] [nullable] |
| **pluginName** | **String** | The name by which the web plugin is registered in the window object. | [required] [example: examplePlugin] [nullable] |
| **scopes** | **String** | The web plugin scopes. | [required] [example: Files,Rooms] [nullable] |
| **image** | **String** | The web plugin image. | [required] [example: https://example.com/image.png] [nullable] |
| **createBy** | [**EmployeeDto**](#model-employeedto) | The user parameters. | [required] |
| **createOn** | **Date** (date-time) | The date and time when the web plugin was created. | [required] [example: 2024-01-15T10:30:00Z] |
| **enabled** | **Boolean** | Specifies if the web plugin is enabled or not. | [required] [example: true] |
| **system** | **Boolean** | Specifies if the web plugin is system or not. | [required] [example: false] |
| **url** | **String** | The web plugin URL. | [required] [example: https://example.com/plugin.js] [nullable] |
| **cssUrl** | **String** | The web plugin css URL. | [required] [example: https://example.com/plugin.css] [nullable] |
| **settings** | **String** | The web plugin settings. | [required] [example: {}] [nullable] |
| **nameLocale** | **null** | The web plugin localized name. | [optional] [example: {}] |
| **descriptionLocale** | **null** | The web plugin localized description. | [optional] [example: {}] |
| **runtime** | **String** | The web plugin loading method | [optional] [example: module] [nullable] |


### Model WebPluginRequests
The configuration settings for the web plugin instance.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Controls whether the web plugin is active and operational. | [optional] [example: true] |
| **settings** | **String** | The JSON-formatted configuration settings for the web plugin. | [required] [example: {"theme":"dark","autoSave":true}] [minLength: 0] [maxLength: 255] [nullable] |


### Model WebPluginWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**WebPluginDto**](#model-webplugindto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebhookGroupStatus
[0 - None, 1 - Not sent, 2 - Status2xx, 4 - Status3xx, 8 - Status4xx, 16 - Status5xx]


### Model WebhookRetryRequestsDto
The parameters for requesting the webhook delivery retries.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ids** | **List** (int32) | The list of webhook delivery IDs to retry. | [optional] [example: [1, 2, 3]] [nullable] |


### Model WebhookTrigger
[0 - *, 1 - user.created, 2 - user.invited, 4 - user.updated, 8 - user.deleted, 16 - group.created, 32 - group.updated, 64 - group.deleted, 128 - file.created, 256 - file.uploaded, 512 - file.updated, 1024 - file.trashed, 2048 - file.deleted, 4096 - file.restored, 8192 - file.copied, 16384 - file.moved, 32768 - folder.created, 65536 - folder.updated, 131072 - folder.trashed, 262144 - folder.deleted, 524288 - folder.restored, 1048576 - folder.copied, 2097152 - folder.moved, 4194304 - room.created, 8388608 - room.updated, 16777216 - room.archived, 33554432 - room.deleted, 67108864 - room.restored, 134217728 - room.copied, 268435456 - form.submit, 536870912 - form.filled.out, 1073741824 - form.stopped]


### Model WebhookTriggerArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-webhooktriggerdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebhookTriggerDto
The webhook trigger with its availability for the current user.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The trigger name. | [optional] [example: file.created] [nullable] |
| **id** | **Long** (int64) | The trigger bit value. | [optional] [example: 128] |
| **available** | **Boolean** | Specifies whether this trigger is available for the current user&#39;s role. | [optional] [example: true] |


### Model WebhooksConfigDto
The webhook configuration parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The webhook ID. | [required] [example: 1] |
| **name** | **String** | The webhook name. | [optional] [example: John] [nullable] |
| **uri** | **String** | The webhook URI. | [optional] [example: https://example.com] [nullable] |
| **enabled** | **Boolean** | Specifies if the webhooks are enabled or not. | [optional] [example: true] |
| **ssl** | **Boolean** | The webhook SSL verification (enabled or not). | [optional] [example: true] |
| **triggers** | [**WebhookTrigger**](#model-webhooktrigger) | The webhook trigger type. | [optional] [enum: 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824] |
| **targetId** | **String** | The webhook target ID. | [optional] [example: 00000000-0000-0000-0000-000000000001] [nullable] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) | The user parameters. | [optional] |
| **createdOn** | **Date** (date-time) | The date and time when the webhook was created. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |
| **modifiedBy** | [**EmployeeDto**](#model-employeedto) | The user parameters. | [optional] |
| **modifiedOn** | **Date** (date-time) | The date and time when the webhook was modified. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |
| **lastFailureOn** | **Date** (date-time) | The date and time of the webhook last failure. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |
| **lastFailureContent** | **String** | The webhook last failure content. | [optional] [example: example value] [nullable] |
| **lastSuccessOn** | **Date** (date-time) | The date and time of the webhook last success. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |


### Model WebhooksConfigWithStatusArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-webhooksconfigwithstatusdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebhooksConfigWithStatusDto
The webhook configuration with its status.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **configs** | [**WebhooksConfigDto**](#model-webhooksconfigdto) | The webhook configuration. | [optional] |
| **status** | **Integer** (int32) | The webhook status. | [optional] [example: 1] |


### Model WebhooksConfigWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**WebhooksConfigDto**](#model-webhooksconfigdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebhooksLogArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-webhookslogdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WebhooksLogDto
The webhook log parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The webhook log ID. | [required] [example: 1] |
| **configName** | **String** | The webhook configuration name. | [optional] [example: Example Name] [nullable] |
| **trigger** | [**WebhookTrigger**](#model-webhooktrigger) | The webhook trigger type. | [optional] [enum: 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824] |
| **creationTime** | **Date** (date-time) | The webhook creation time. | [optional] [example: 2024-01-15T10:30:00Z] |
| **method** | **String** | The webhook method. | [optional] [example: example value] [nullable] |
| **route** | **String** | The webhook route. | [optional] [example: example value] [nullable] |
| **requestHeaders** | **String** | The webhook request headers. | [optional] [example: example value] [nullable] |
| **requestPayload** | **String** | The webhook request payload. | [optional] [example: example value] [nullable] |
| **responseHeaders** | **String** | The webhook response headers. | [optional] [example: example value] [nullable] |
| **responsePayload** | **String** | The webhook response payload. | [optional] [example: example value] [nullable] |
| **status** | **Integer** (int32) | The webhook status. | [optional] [example: 1] |
| **delivery** | **Date** (date-time) | The webhook delivery time. | [optional] [example: 2024-01-15T10:30:00Z] [nullable] |


### Model WebhooksLogWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**WebhooksLogDto**](#model-webhookslogdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WhiteLabelItemArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-whitelabelitemdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model WhiteLabelItemDto
The white label item parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | [**WhiteLabelLogoType**](#model-whitelabellogotype) | The white label logo type. | [optional] [enum: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16] |
| **name** | **String** | The white label file name. | [optional] [example: Example Name] [nullable] |
| **size** | [**IMagickGeometry**](#model-imagickgeometry) | The white label file size. | [optional] |
| **path** | [**WhiteLabelItemPathDto**](#model-whitelabelitempathdto) | The white label file path. | [optional] |


### Model WhiteLabelItemPathDto
The white label item path parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **light** | **String** | The path to the light theme logo. | [optional] [example: /images/logo-light.png] [nullable] |
| **dark** | **String** | The path to the dark theme logo. | [optional] [example: /images/logo-dark.png] [nullable] |


### Model WhiteLabelLogoType
[1 - Light small, 2 - Login page, 3 - Favicon, 4 - Docs editor, 5 - Docs editor embed, 6 - Left menu, 7 - About page, 8 - Notification, 9 - Spreadsheet editor, 10 - Spreadsheet editor embed, 11 - Presentation editor, 12 - Presentation editor embed, 13 - Pdf editor, 14 - Pdf editor embed, 15 - Diagram editor, 16 - Diagram editor embed]


### Model WhiteLabelRequestsDto
The request parameters for configuring the white label branding settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **logoText** | **String** | The text to display alongside or in place of the logo. | [optional] [example: Company Name] [minLength: 0] [maxLength: 40] [nullable] |
| **logo** | [**List**](#model-itemkeyvaluepairstringlogorequestsdto) | The white label tenant IDs with their logos (light or dark). | [optional] [example: [item1, item2]] [nullable] |


### Model WizardRequestsDto
The request parameters for initial configuration of the setup wizard.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The user&#39;s email address for authentication and communication. | [required] [example: user@example.com] [nullable] |
| **passwordHash** | **String** | The hashed representation of the user&#39;s password. | [required] [example: 2DYmIoA/aYKEksFocEf6uw==] [nullable] |
| **lng** | **String** | The user&#39;s preferred interface language code. | [optional] [example: en-US] [nullable] |
| **timeZone** | **String** | The user&#39;s time zone identifier. | [optional] [example: UTC] [nullable] |
| **amiId** | **String** | The Amazon Machine Image (AMI) identifier. | [optional] [example: 00000000-0000-0000-0000-000000000001] [nullable] |
| **subscribeFromSite** | **Boolean** | Specifies whether the user opted in for site communications. | [optional] [example: true] |


### Model WizardSettings
The Wizard settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **completed** | **Boolean** | Specifies if the Wizard settings are completed or not | [optional] [example: true] |
| **lastModified** | **Date** (date-time) | The timestamp indicating when the settings were last modified. | [optional] [example: 1990-01-01T00:00:00Z] |


### Model WizardSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**WizardSettings**](#model-wizardsettings) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model getPortalPrices 200 response

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Map** (double) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-getportalprices-200-responselinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model getPortalPrices 200 response.links item

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **href** | **String** | URL of the link | [optional] |
| **action** | **String** | Action associated with the link | [optional] |


## Authorization


### asc_auth_key
- **Type**: API key
- **API key parameter name**: asc_auth_key
- **Location**: 


### Basic

- **Type**: HTTP basic authentication


### Bearer

- **Type**: HTTP Bearer Token authentication (JWT)


### ApiKeyBearer
- **Type**: API key
- **API key parameter name**: ApiKeyBearer
- **Location**: HTTP header


### OAuth2

- **Type**: OAuth
- **Flow**: accessCode
- **Authorization URL**: 
- **Scopes**: 
  - read: Read access to protected resources
  - write: Write access to protected resources


### OpenId


### cookieAuth
- **Type**: API key
- **API key parameter name**: asc_auth_key
- **Location**: 


### bearerAuth

- **Type**: HTTP Bearer Token authentication


### x-signature
- **Type**: API key
- **API key parameter name**: x-signature
- **Location**: 

