module docspace-webhooks-sdk-go-samples

go 1.23

require github.com/ONLYOFFICE/docspace-webhooks-sdk-go v1.0.0

require gopkg.in/validator.v2 v2.0.1 // indirect

// The SDK is the generated/ module next door, not a published release.
replace github.com/ONLYOFFICE/docspace-webhooks-sdk-go => ../generated
