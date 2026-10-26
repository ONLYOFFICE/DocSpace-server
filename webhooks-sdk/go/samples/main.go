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

// payloadKind names the payload model of a trigger, mirroring the
// x-docspace-trigger-payloads map of the contract; "" for a trigger newer than
// this SDK. form.submit has a payload of its own, while form.filled.out and
// form.stopped carry the form file itself.
func payloadKind(trigger string) string {
	switch {
	case trigger == "form.submit":
		return "formSubmit"
	case trigger == "form.filled.out", trigger == "form.stopped", strings.HasPrefix(trigger, "file."):
		return "file"
	case strings.HasPrefix(trigger, "user."):
		return "user"
	case strings.HasPrefix(trigger, "group."):
		return "group"
	case strings.HasPrefix(trigger, "folder."):
		return "folder"
	// An agent is an AI room and arrives in the same shape.
	case strings.HasPrefix(trigger, "room."), strings.HasPrefix(trigger, "agent."):
		return "room"
	}
	return ""
}

// logPayload parses the payload with the model of its trigger and prints a
// one-line summary. A parse error is returned, never skipped: a sample that
// prints "ok" over a payload its model cannot read hides a broken SDK.
func logPayload(trigger string, raw json.RawMessage) error {
	switch payloadKind(trigger) {
	case "user":
		var u openapi.UserPayload
		if err := json.Unmarshal(raw, &u); err != nil {
			return err
		}
		fmt.Printf("  user: %s <%s>\n", deref(u.UserName, ""), deref(u.Email, ""))
	case "group":
		var g openapi.GroupPayload
		if err := json.Unmarshal(raw, &g); err != nil {
			return err
		}
		fmt.Printf("  group: %s\n", deref(g.Name, ""))
	case "file":
		var f openapi.FilePayload
		if err := json.Unmarshal(raw, &f); err != nil {
			return err
		}
		fmt.Printf("  file: %s  id=%s  parent=%s\n", deref(f.Title, ""), entryID(f.Id), entryID(f.ParentId))
	case "folder":
		var f openapi.FolderPayload
		if err := json.Unmarshal(raw, &f); err != nil {
			return err
		}
		fmt.Printf("  folder: %s  id=%s  parent=%s\n", deref(f.Title, ""), entryID(f.Id), entryID(f.ParentId))
	case "room":
		var r openapi.RoomPayload
		if err := json.Unmarshal(raw, &r); err != nil {
			return err
		}
		fmt.Printf("  room: %s  id=%s  parent=%s\n", deref(r.Title, ""), entryID(r.Id), entryID(r.ParentId))
	case "formSubmit":
		var f openapi.FormSubmitPayload
		if err := json.Unmarshal(raw, &f); err != nil {
			return err
		}
		original, submitted := "", ""
		if f.OriginalForm != nil {
			original = deref(f.OriginalForm.Title, "")
		}
		if f.SubmittedForm != nil {
			submitted = deref(f.SubmittedForm.Title, "")
		}
		fmt.Printf("  form: %s  submitted as %s\n", original, submitted)
	default:
		// A trigger family added on the server after this SDK was generated:
		// acknowledge it and keep the raw payload rather than failing.
		fmt.Println("  no payload model for this trigger; raw payload below")
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
	fmt.Println(trigger)
	fmt.Println("  signature ok")
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
