// Logs a DocSpace webhook delivery to the console.
//
// Run from go/samples (its own module, pointing at ../generated):
//
//	go run .                          # replay the shared fixture
//	go run . <body> <secret> <sig> [<x-docspace-event-id>]
package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"

	openapi "github.com/ONLYOFFICE/docspace-webhooks-sdk-go"
)

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

func printHeader(trigger string, event *openapi.WebhookEventInfo, webhook *openapi.WebhookConfigInfo, delivery string) {
	if event == nil {
		event = &openapi.WebhookEventInfo{}
	}
	if webhook == nil {
		webhook = &openapi.WebhookConfigInfo{}
	}
	fmt.Println(trigger)
	fmt.Println("  signature ok")
	fmt.Printf("  model: %s\n", modelName(trigger))
	fmt.Printf("  delivery: %s\n", delivery)
	fmt.Printf("  event #%d  at %s  by %s\n",
		deref(event.Id, 0),
		deref(event.CreateOn, time.Time{}).Format("2006-01-02T15:04:05Z"),
		deref(event.CreateBy, ""))
	fmt.Printf("  subscription #%d %q\n", deref(webhook.Id, 0), deref(webhook.Name, ""))
}

// printSummary prints a one-line summary of a typed payload.
func printSummary(payload any) {
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
}

// printPayload prints the payload as received, indented.
func printPayload(raw json.RawMessage) error {
	if len(raw) == 0 {
		raw = json.RawMessage("null")
	}
	var out bytes.Buffer
	if err := json.Indent(&out, raw, "  ", "  "); err != nil {
		return err
	}
	fmt.Println()
	fmt.Println("  " + out.String())
	return nil
}

// rawPayload is the payload of the body being handled, as received: the typed
// handlers print it rather than their model re-encoded.
var rawPayload json.RawMessage

// logDelivery prints any delivery model: they all share Event, Payload and
// Webhook, with Payload typed per model.
func logDelivery(trigger, name string, delivery any) error {
	var (
		event   *openapi.WebhookEventInfo
		webhook *openapi.WebhookConfigInfo
		payload any
	)
	switch d := delivery.(type) {
	case *openapi.UserDelivery:
		event, webhook, payload = d.Event, d.Webhook, d.Payload
	case *openapi.GroupDelivery:
		event, webhook, payload = d.Event, d.Webhook, d.Payload
	case *openapi.FileDelivery:
		event, webhook, payload = d.Event, d.Webhook, d.Payload
	case *openapi.FolderDelivery:
		event, webhook, payload = d.Event, d.Webhook, d.Payload
	case *openapi.RoomDelivery:
		event, webhook, payload = d.Event, d.Webhook, d.Payload
	case *openapi.FormSubmitDelivery:
		event, webhook, payload = d.Event, d.Webhook, d.Payload
	default:
		return fmt.Errorf("no case for %s", name)
	}
	printHeader(trigger, event, webhook, name)
	printSummary(payload)
	return printPayload(rawPayload)
}

// register adds one handler for trigger, delivered as T.
func register[T any](router *openapi.WebhookRouter, trigger string) error {
	_, err := openapi.On(router, trigger, func(_ context.Context, d *T) error {
		_, name := openapi.NewDelivery(trigger)
		return logDelivery(trigger, name, d)
	})
	return err
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

	var secret, signature, eventIDHeader string
	if len(os.Args) > 3 {
		bodyPath, secret, signature = os.Args[1], os.Args[2], os.Args[3]
		if len(os.Args) > 4 {
			eventIDHeader = os.Args[4]
		}
	} else {
		secret, signature = read(secretPath), read(sigPath)
	}

	// The router this sample hands the delivery to. It checks the signature,
	// drops an event it has processed before (DocSpace delivers at least once,
	// and a retry carries the same event.id), reads the body into the delivery
	// model of its trigger and calls the handlers. The store lives in memory for
	// the sake of the sample; a real receiver persists the ids for as long as an
	// event can still arrive -- the automatic retries span about 18 hours on the
	// default schedule, and a manual retry can come at any time later.
	router, err := openapi.NewWebhookRouter(secret, openapi.NewInMemoryWebhookEventStore())
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	// One handler for every trigger with a typed body, from the generated
	// table. A receiver normally registers only the triggers it cares about.
	triggers := make([]string, 0, len(openapi.DeliveryModels))
	for trigger := range openapi.DeliveryModels {
		triggers = append(triggers, trigger)
	}
	sort.Strings(triggers)
	for _, trigger := range triggers {
		var err error
		switch _, name := openapi.NewDelivery(trigger); name {
		case "UserDelivery":
			err = register[openapi.UserDelivery](router, trigger)
		case "GroupDelivery":
			err = register[openapi.GroupDelivery](router, trigger)
		case "FileDelivery":
			err = register[openapi.FileDelivery](router, trigger)
		case "FolderDelivery":
			err = register[openapi.FolderDelivery](router, trigger)
		case "RoomDelivery":
			err = register[openapi.RoomDelivery](router, trigger)
		case "FormSubmitDelivery":
			err = register[openapi.FormSubmitDelivery](router, trigger)
		default:
			err = fmt.Errorf("%s: no case for %s", trigger, name)
		}
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
	}

	// A handler for one trigger: its argument is a *FileDelivery, typed by the
	// method's name.
	router.OnFileCreated(func(_ context.Context, d *openapi.FileDelivery) error {
		if d.Payload != nil {
			fmt.Printf("  OnFileCreated: %q in folder %s\n", deref(d.Payload.Title, ""), entryID(d.Payload.ParentId))
		}
		return nil
	})

	// Whatever has no handler: the catch-all "*", and triggers newer than this SDK.
	router.OnOther(func(_ context.Context, hook *openapi.AnyWebhook) error {
		printHeader(hook.Trigger, &hook.Event, &hook.Webhook, "none")
		fmt.Println("  no payload model for this trigger; raw payload below")
		return printPayload(hook.Payload)
	})

	// Bytes, not a re-encoded struct: the signature covers what arrived.
	body, err := os.ReadFile(bodyPath)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
	var envelope struct {
		Payload json.RawMessage `json:"payload"`
	}
	_ = json.Unmarshal(body, &envelope) // the router reports a broken body
	rawPayload = envelope.Payload

	// In an HTTP handler, answer with result.StatusCode once this returns: a
	// 2xx acknowledges the delivery, anything else makes DocSpace send it again.
	result := router.Handle(context.Background(), body, signature, eventIDHeader)

	switch result.Outcome {
	case openapi.OutcomeRejected:
		fmt.Fprintln(os.Stderr, "REJECTED: signature does not match")
	case openapi.OutcomeInvalid:
		fmt.Fprintf(os.Stderr, "unparseable body: %s\n", result.Error)
	case openapi.OutcomeFailed:
		fmt.Fprintf(os.Stderr, "a handler failed: %s\n", result.Error)
	case openapi.OutcomeDuplicate:
		fmt.Printf("duplicate of event #%d, already processed; skipped\n", deref(result.EventID, 0))
	}

	if result.StatusCode < 200 || result.StatusCode >= 300 {
		os.Exit(1)
	}
}
