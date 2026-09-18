package docspace_webhooks_sdk

// Minimal stand-ins for the generator's utils.go, which models-only output does
// not emit. Copied into generated/ by ../generate.sh after every regeneration.
//
// Generating the supporting files instead would pull in a whole HTTP API client
// (client.go, configuration.go, response.go, auth) for a package that only ever
// decodes an inbound webhook body.

import (
	"bytes"
	"encoding/json"
	"reflect"
)

// MappedNullable is implemented by every generated model.
type MappedNullable interface {
	ToMap() (map[string]interface{}, error)
}

// newStrictDecoder rejects unknown fields. The oneOf decoder relies on that
// failure to discriminate between branches, so it must stay strict.
func newStrictDecoder(data []byte) *json.Decoder {
	dec := json.NewDecoder(bytes.NewBuffer(data))
	dec.DisallowUnknownFields()
	return dec
}

// IsNil reports whether an interface holds a nil pointer, map, slice or func.
// The generated ToMap methods call it for every optional field.
func IsNil(i interface{}) bool {
	if i == nil {
		return true
	}
	switch reflect.TypeOf(i).Kind() {
	case reflect.Chan, reflect.Func, reflect.Interface,
		reflect.Map, reflect.Ptr, reflect.Slice:
		return reflect.ValueOf(i).IsNil()
	}
	return false
}
