// Logs a DocSpace webhook delivery to the console.
//
// Run from go/samples (its own module, pointing at ../generated):
//
//	go run .                          # replay the shared fixture
//	go run . <body> <secret> <sig>
//
// Go has no runtime package yet, so signature checking lives here.
package main

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	openapi "github.com/ONLYOFFICE/docspace-webhooks-sdk-go"
)

const signatureHeader = "x-docspace-signature-256"

// computeSignature mirrors WebhookSender.GetSecretHash: HMAC-SHA256 over the
// raw body, rendered as "sha256=" + UPPERCASE hex.
func computeSignature(body []byte, secret string) string {
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write(body)
	return "sha256=" + strings.ToUpper(hex.EncodeToString(mac.Sum(nil)))
}

// verifySignature compares in constant time, folding case: DocSpace emits
// uppercase hex where GitHub emits lowercase.
func verifySignature(body []byte, secret, signature string) bool {
	if signature == "" {
		return false
	}
	expected := strings.ToLower(computeSignature(body, secret))
	received := strings.ToLower(strings.TrimSpace(signature))
	return hmac.Equal([]byte(expected), []byte(received))
}

type envelope struct {
	Event   openapi.WebhookEventInfo  `json:"event"`
	Webhook openapi.WebhookConfigInfo `json:"webhook"`
	Payload json.RawMessage           `json:"payload"`
}

func entryID(id *openapi.EntryId) string {
	switch {
	case id == nil:
		return "<none>"
	case id.Int32 != nil:
		return fmt.Sprintf("%d", *id.Int32)
	case id.String != nil:
		return *id.String
	}
	return "<none>"
}

func deref[T any](p *T, zero T) T {
	if p == nil {
		return zero
	}
	return *p
}

func time0() time.Time { return time.Time{} }

// modelName is what the sample reports for a trigger: the payload model the
// generated table names, "untyped" for the legacy catch-all, and "unknown" for
// a trigger newer than this SDK.
func modelName(trigger string) string {
	if name, ok := openapi.PayloadModels[trigger]; ok {
		return name
	}
	if openapi.UntypedTriggers[trigger] {
		return "untyped"
	}
	return "unknown"
}

// logPayload parses the payload with the model the generated trigger table
// names and prints a one-line summary. A parse error is returned, never
// skipped: a sample that prints "ok" over a payload its model cannot read hides
// a broken SDK.
func logPayload(trigger string, raw json.RawMessage) error {
	payload, _ := openapi.NewPayload(trigger)
	if payload == nil {
		// The catch-all, or a trigger added on the server after this SDK was
		// generated: acknowledge it and keep the raw payload rather than
		// parsing it with some other model.
		fmt.Println("  no payload model for this trigger; raw payload below")
		return nil
	}
	if err := json.Unmarshal(raw, payload); err != nil {
		return err
	}

	switch p := payload.(type) {
	case *openapi.UserPayload:
		fmt.Printf("  user: %s <%s>\n", deref(p.UserName, ""), deref(p.Email, ""))
	case *openapi.GroupPayload:
		fmt.Printf("  group: %s\n", deref(p.Name, ""))
	case *openapi.FilePayload:
		fmt.Printf("  file: %s  id=%s  parent=%s\n", deref(p.Title, ""), entryID(p.Id), entryID(p.ParentId))
	case *openapi.FolderPayload:
		fmt.Printf("  folder: %s  id=%s  parent=%s\n", deref(p.Title, ""), entryID(p.Id), entryID(p.ParentId))
	case *openapi.RoomPayload:
		fmt.Printf("  room: %s  id=%s  parent=%s\n", deref(p.Title, ""), entryID(p.Id), entryID(p.ParentId))
	case *openapi.FormSubmitPayload:
		original, submitted := "", ""
		if p.OriginalForm != nil {
			original = deref(p.OriginalForm.Title, "")
		}
		if p.SubmittedForm != nil {
			submitted = deref(p.SubmittedForm.Title, "")
		}
		fmt.Printf("  form: %s  submitted as %s\n", original, submitted)
	}
	return nil
}

func main() {
	fixtures := filepath.Join("..", "..", "samples")
	bodyPath := filepath.Join(fixtures, "file.created.json")
	secretPath := filepath.Join(fixtures, "SECRET")
	sigPath := filepath.Join(fixtures, "file.created.sig")

	read := func(p string) string {
		b, err := os.ReadFile(p)
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
		return strings.TrimSpace(string(b))
	}

	secret, signature := read(secretPath), read(sigPath)
	if len(os.Args) > 3 {
		bodyPath, secret, signature = os.Args[1], os.Args[2], os.Args[3]
	}

	// Bytes, not a re-encoded struct: the signature covers what arrived.
	body, err := os.ReadFile(bodyPath)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	if !verifySignature(body, secret, signature) {
		fmt.Fprintln(os.Stderr, "REJECTED: signature does not match")
		os.Exit(1)
	}

	var env envelope
	if err := json.Unmarshal(body, &env); err != nil {
		fmt.Fprintf(os.Stderr, "unparseable body: %v\n", err)
		os.Exit(1)
	}

	trigger := deref(env.Event.Trigger, "")
	if trigger == "" {
		fmt.Fprintln(os.Stderr, "envelope has no event.trigger")
		os.Exit(1)
	}

	fmt.Println(trigger)
	fmt.Println("  signature ok")
	fmt.Printf("  model: %s\n", modelName(trigger))
	fmt.Printf("  event #%d  at %s  by %s\n",
		deref(env.Event.Id, 0),
		deref(env.Event.CreateOn, time0()).Format("2006-01-02T15:04:05Z"),
		deref(env.Event.CreateBy, ""))
	fmt.Printf("  subscription #%d %q\n", deref(env.Webhook.Id, 0), deref(env.Webhook.Name, ""))

	if err := logPayload(trigger, env.Payload); err != nil {
		fmt.Fprintf(os.Stderr, "unparseable %s payload: %v\n", trigger, err)
		os.Exit(1)
	}

	var pretty any
	if err := json.Unmarshal(env.Payload, &pretty); err != nil {
		fmt.Fprintf(os.Stderr, "unparseable payload: %v\n", err)
		os.Exit(1)
	}
	out, err := json.MarshalIndent(pretty, "  ", "  ")
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
	fmt.Println()
	fmt.Println("  " + string(out))
}
