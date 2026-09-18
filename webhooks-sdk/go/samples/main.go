// Logs a DocSpace webhook delivery to the console.
//
//	go run ./samples                          # replay the shared fixture
//	go run ./samples <body> <secret> <sig>
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

	openapi "docspacewebhooks/generated"
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

func main() {
	fixtures := filepath.Join("..", "samples")
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

	switch {
	case strings.HasPrefix(trigger, "user."):
		var u openapi.UserPayload
		if json.Unmarshal(env.Payload, &u) == nil {
			fmt.Printf("  user: %s <%s>\n", deref(u.UserName, ""), deref(u.Email, ""))
		}
	case strings.HasPrefix(trigger, "group."):
		var g openapi.GroupPayload
		if json.Unmarshal(env.Payload, &g) == nil {
			fmt.Printf("  group: %s\n", deref(g.Name, ""))
		}
	default:
		// file.*, folder.*, room.*, agent.*, form.* all arrive as the
		// FileEntry<T> base; fileEntryType is the only discriminator.
		var e openapi.FileEntryPayload
		if json.Unmarshal(env.Payload, &e) == nil {
			kind := "folder"
			if deref(e.FileEntryType, 0) == 2 {
				kind = "file"
			}
			fmt.Printf("  %s: %s  id=%s  parent=%s\n",
				kind, deref(e.Title, ""), entryID(e.Id), entryID(e.ParentId))
		}
	}

	var pretty map[string]any
	_ = json.Unmarshal(env.Payload, &pretty)
	out, _ := json.MarshalIndent(pretty, "  ", "  ")
	fmt.Println()
	fmt.Println("  " + string(out))
}
