package docspace_webhooks_sdk

// The webhook router: signature check, duplicate check, dispatch to handlers
// registered per trigger. Hand-written; copied into generated/ after every
// regeneration, next to the typed On<Trigger> methods of triggers.go that call
// onTyped.

import (
	"bytes"
	"context"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"reflect"
	"strconv"
	"strings"
	"sync"
)

// SignatureHeader is the header DocSpace sends the body signature in.
const SignatureHeader = "x-docspace-signature-256"

// EventIDHeader is the header DocSpace sends a copy of event.id in. It is not
// signed: use it only to drop a delivery, never to accept one.
const EventIDHeader = "x-docspace-event-id"

// ComputeSignature mirrors WebhookSender.GetSecretHash: HMAC-SHA256 over the
// raw body, rendered as "sha256=" + UPPERCASE hex.
func ComputeSignature(body []byte, secret string) string {
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write(body)
	return "sha256=" + strings.ToUpper(hex.EncodeToString(mac.Sum(nil)))
}

// VerifySignature compares in constant time, folding case: DocSpace emits
// uppercase hex where GitHub emits lowercase. A missing or malformed header is
// a mismatch.
func VerifySignature(body []byte, secret, signature string) bool {
	if signature == "" {
		return false
	}
	expected := strings.ToLower(ComputeSignature(body, secret))
	received := strings.ToLower(strings.TrimSpace(signature))
	return hmac.Equal([]byte(expected), []byte(received))
}

// WebhookOutcome is what WebhookRouter.Handle did with a delivery.
type WebhookOutcome string

const (
	// OutcomeHandled: at least one handler ran, and all of them completed.
	OutcomeHandled WebhookOutcome = "handled"
	// OutcomeDuplicate: the event was processed before; nothing ran.
	OutcomeDuplicate WebhookOutcome = "duplicate"
	// OutcomeUnhandled: no handler is registered for the trigger; the delivery
	// is acknowledged all the same.
	OutcomeUnhandled WebhookOutcome = "unhandled"
	// OutcomeRejected: the signature is missing or does not match.
	OutcomeRejected WebhookOutcome = "rejected"
	// OutcomeInvalid: the body is not a delivery, or a typed trigger arrived
	// without its payload.
	OutcomeInvalid WebhookOutcome = "invalid"
	// OutcomeFailed: a handler (or the event store) returned an error; the
	// remaining handlers did not run.
	OutcomeFailed WebhookOutcome = "failed"
)

// HandleResult is the result of one delivery: what happened and the HTTP status
// to answer DocSpace with. A 2xx acknowledges the delivery; anything else is a
// failed attempt, which DocSpace retries on its schedule.
type HandleResult struct {
	Outcome    WebhookOutcome
	StatusCode int
	// Trigger is empty when the body was not read (duplicate by header,
	// rejected, invalid).
	Trigger string
	// EventID is nil when the event id is not known.
	EventID *int32
	// Error is the message of Err, or why the delivery was refused.
	Error string
	Err   error
}

// WebhookEventStore remembers which events were processed. DocSpace delivers
// at least once: an automatic or manual retry sends the same event.id again,
// and the signature does not prove when a request was sent, so the id is the
// duplicate and replay defence. A real receiver persists the ids for as long as
// an event can still arrive -- the automatic retries span about 18 hours on the
// default schedule, and a manual retry can come at any time later.
//
// An error from either method makes the delivery a failed attempt (500), so
// DocSpace sends it again.
type WebhookEventStore interface {
	// IsProcessed reports whether the event was processed before.
	IsProcessed(ctx context.Context, eventID int32) (bool, error)
	// MarkProcessed records the event as processed; called only after every
	// handler completed.
	MarkProcessed(ctx context.Context, eventID int32) error
}

// InMemoryWebhookEventStore is a WebhookEventStore in process memory: for
// samples and tests, lost on restart.
type InMemoryWebhookEventStore struct {
	ids sync.Map
}

// NewInMemoryWebhookEventStore returns an empty in-memory store.
func NewInMemoryWebhookEventStore() *InMemoryWebhookEventStore {
	return &InMemoryWebhookEventStore{}
}

// IsProcessed implements WebhookEventStore.
func (s *InMemoryWebhookEventStore) IsProcessed(_ context.Context, eventID int32) (bool, error) {
	_, ok := s.ids.Load(eventID)
	return ok, nil
}

// MarkProcessed implements WebhookEventStore.
func (s *InMemoryWebhookEventStore) MarkProcessed(_ context.Context, eventID int32) error {
	s.ids.Store(eventID, struct{}{})
	return nil
}

// AnyWebhook is a delivery as the OnOther handlers get it: the envelope with
// the payload left raw. It is all a receiver has for the untyped catch-all "*"
// and for a trigger added after this SDK was built.
type AnyWebhook struct {
	Trigger string
	Event   WebhookEventInfo
	Webhook WebhookConfigInfo
	// Payload is the payload exactly as received; nil when the body has none.
	Payload json.RawMessage
}

type registration struct {
	invoke func(ctx context.Context, delivery any, hook *AnyWebhook) error
}

// WebhookRouter dispatches verified deliveries to handlers registered per
// trigger (OnFileCreated, ..., or On for a trigger by name) or for anything
// else (OnOther). Any number of handlers can be registered for one trigger;
// each registration returns a function that removes exactly it.
//
// Handle checks the signature, drops a duplicate, reads the body into the
// delivery model of its trigger and calls every handler of that trigger in
// registration order. Answer DocSpace with the result's StatusCode only after
// it returns: a handler that returns an error makes it a failed attempt, and
// DocSpace sends the delivery again, so handlers have to be idempotent.
//
// Registration is safe while Handle runs: a delivery sees the handlers
// registered when it was dispatched.
type WebhookRouter struct {
	secret   string
	unsigned bool
	store    WebhookEventStore

	mu        sync.Mutex
	byTrigger map[string][]*registration
	other     []*registration
}

// NewWebhookRouter returns a router that accepts only deliveries signed with
// secret, the secret key of the subscription. store keeps the processed event
// ids; with nil, duplicates are not detected.
func NewWebhookRouter(secret string, store WebhookEventStore) (*WebhookRouter, error) {
	if secret == "" {
		return nil, errors.New("a secret is required; use NewUnsignedReplayRouter to replay unsigned bodies")
	}
	return &WebhookRouter{secret: secret, store: store, byTrigger: map[string][]*registration{}}, nil
}

// NewUnsignedReplayRouter returns a router that does not check signatures, for
// replaying saved bodies during development only. Never put it behind a
// reachable endpoint: anyone could send it a delivery.
func NewUnsignedReplayRouter(store WebhookEventStore) *WebhookRouter {
	return &WebhookRouter{unsigned: true, store: store, byTrigger: map[string][]*registration{}}
}

// On registers handler for trigger, which has to be delivered as T (On[FileDelivery]
// for "file.created", ...). It returns the function that removes the handler,
// or an error when the trigger has no delivery model or T is not it. The
// generated On<Trigger> methods are the typed shorthand for it.
func On[T any](r *WebhookRouter, trigger string, handler func(context.Context, *T) error) (func(), error) {
	if handler == nil {
		return nil, errors.New("handler is nil")
	}
	model, name := NewDelivery(trigger)
	want := reflect.TypeFor[T]().Name()
	if model == nil {
		return nil, fmt.Errorf("%q is delivered as an untyped body, not %s", trigger, want)
	}
	if _, ok := model.(*T); !ok {
		return nil, fmt.Errorf("%q is delivered as %s, not %s", trigger, name, want)
	}

	reg := &registration{invoke: func(ctx context.Context, delivery any, _ *AnyWebhook) error {
		return handler(ctx, delivery.(*T))
	}}

	r.mu.Lock()
	r.byTrigger = withRegistration(r.byTrigger, trigger, reg)
	r.mu.Unlock()

	return r.remover(func() {
		r.byTrigger = withoutRegistration(r.byTrigger, trigger, reg)
	}), nil
}

// onTyped is what the generated On<Trigger> methods call; their model always
// matches the table, so a mismatch is a broken build and panics.
func onTyped[T any](r *WebhookRouter, trigger string, handler func(context.Context, *T) error) func() {
	remove, err := On(r, trigger, handler)
	if err != nil {
		panic(err)
	}
	return remove
}

// OnOther registers handler for the deliveries no other handler takes:
// triggers without a registration, the untyped catch-all "*", and triggers
// added after this SDK was built. It returns the function that removes it.
func (r *WebhookRouter) OnOther(handler func(context.Context, *AnyWebhook) error) func() {
	if handler == nil {
		panic("docspace_webhooks_sdk: OnOther handler is nil")
	}
	reg := &registration{invoke: func(ctx context.Context, _ any, hook *AnyWebhook) error {
		return handler(ctx, hook)
	}}

	r.mu.Lock()
	r.other = append(r.other[:len(r.other):len(r.other)], reg)
	r.mu.Unlock()

	return r.remover(func() {
		r.other = without(r.other, reg)
	})
}

// remover runs remove under the lock, once however often it is called.
func (r *WebhookRouter) remover(remove func()) func() {
	var once sync.Once
	return func() {
		once.Do(func() {
			r.mu.Lock()
			defer r.mu.Unlock()
			remove()
		})
	}
}

// Handle handles one delivery; see WebhookRouter.
//
// body is the request body exactly as received, signature the
// x-docspace-signature-256 header, and eventIDHeader the x-docspace-event-id
// header or "": an already processed id is dropped before any work. The header
// is not signed, so it is only ever used to drop a delivery, never to accept
// one. ctx is passed on to the handlers and the store.
func (r *WebhookRouter) Handle(ctx context.Context, body []byte, signature, eventIDHeader string) HandleResult {
	if r.store != nil && eventIDHeader != "" {
		if id, err := strconv.ParseInt(strings.TrimSpace(eventIDHeader), 10, 32); err == nil {
			headerID := int32(id)
			done, err := r.store.IsProcessed(ctx, headerID)
			if err != nil {
				return storeFailure("", &headerID, err)
			}
			if done {
				return HandleResult{Outcome: OutcomeDuplicate, StatusCode: 200, EventID: &headerID}
			}
		}
	}

	if !r.unsigned && !VerifySignature(body, r.secret, signature) {
		return HandleResult{Outcome: OutcomeRejected, StatusCode: 401, Error: "signature is missing or does not match"}
	}

	hook, delivery, err := parseBody(body)
	if err != nil {
		return HandleResult{Outcome: OutcomeInvalid, StatusCode: 400, Error: err.Error(), Err: err}
	}

	eventID := hook.Event.Id

	if r.store != nil && eventID != nil {
		done, err := r.store.IsProcessed(ctx, *eventID)
		if err != nil {
			return storeFailure(hook.Trigger, eventID, err)
		}
		if done {
			return HandleResult{Outcome: OutcomeDuplicate, StatusCode: 200, Trigger: hook.Trigger, EventID: eventID}
		}
	}

	handlers := r.handlers(hook.Trigger, delivery)

	for _, h := range handlers {
		if err := invoke(ctx, h, delivery, hook); err != nil {
			return HandleResult{
				Outcome: OutcomeFailed, StatusCode: 500, Trigger: hook.Trigger, EventID: eventID,
				Error: err.Error(), Err: err,
			}
		}
	}

	if r.store != nil && eventID != nil {
		if err := r.store.MarkProcessed(ctx, *eventID); err != nil {
			return storeFailure(hook.Trigger, eventID, err)
		}
	}

	outcome := OutcomeUnhandled
	if len(handlers) > 0 {
		outcome = OutcomeHandled
	}
	return HandleResult{Outcome: outcome, StatusCode: 200, Trigger: hook.Trigger, EventID: eventID}
}

// handlers takes one consistent snapshot: registrations are replaced, never
// changed in place.
func (r *WebhookRouter) handlers(trigger string, delivery any) []*registration {
	r.mu.Lock()
	byTrigger, other := r.byTrigger, r.other
	r.mu.Unlock()

	if delivery != nil {
		if typed := byTrigger[trigger]; len(typed) > 0 {
			return typed
		}
	}
	return other
}

// invoke calls one handler; a panic fails the delivery like an error does,
// instead of taking the receiver down.
func invoke(ctx context.Context, h *registration, delivery any, hook *AnyWebhook) (err error) {
	defer func() {
		if p := recover(); p != nil {
			err = fmt.Errorf("handler panicked: %v", p)
		}
	}()
	return h.invoke(ctx, delivery, hook)
}

func storeFailure(trigger string, eventID *int32, err error) HandleResult {
	return HandleResult{
		Outcome: OutcomeFailed, StatusCode: 500, Trigger: trigger, EventID: eventID,
		Error: "event store: " + err.Error(), Err: err,
	}
}

// parseBody reads the envelope, and for a trigger with a delivery model the
// whole body into that model; the delivery is nil for the untyped "*" and for
// a trigger newer than this SDK.
func parseBody(body []byte) (*AnyWebhook, any, error) {
	var envelope map[string]json.RawMessage
	if err := json.Unmarshal(body, &envelope); err != nil {
		return nil, nil, fmt.Errorf("body is not a webhook envelope: %w", err)
	}
	if envelope == nil {
		return nil, nil, errors.New("body is not a webhook envelope")
	}

	// event.trigger has to be a JSON string: null would unmarshal as "" silently
	var event map[string]json.RawMessage
	var trigger string
	var raw []byte
	if json.Unmarshal(envelope["event"], &event) == nil {
		raw = bytes.TrimSpace(event["trigger"])
	}
	if len(raw) == 0 || raw[0] != '"' || json.Unmarshal(raw, &trigger) != nil {
		return nil, nil, errors.New("envelope has no event.trigger")
	}

	hook := &AnyWebhook{Trigger: trigger, Payload: envelope["payload"]}
	if err := json.Unmarshal(envelope["event"], &hook.Event); err != nil {
		return nil, nil, fmt.Errorf("unparseable event: %w", err)
	}
	if webhook, ok := envelope["webhook"]; ok && !isNull(webhook) {
		if err := json.Unmarshal(webhook, &hook.Webhook); err != nil {
			return nil, nil, fmt.Errorf("unparseable webhook: %w", err)
		}
	}

	delivery, name := NewDelivery(trigger)
	if delivery == nil {
		return hook, nil, nil
	}
	// a typed trigger always carries its payload object
	if p := bytes.TrimSpace(hook.Payload); len(p) == 0 || p[0] != '{' {
		return nil, nil, fmt.Errorf("no payload object for %q", trigger)
	}
	if err := json.Unmarshal(body, delivery); err != nil {
		return nil, nil, fmt.Errorf("unparseable %s: %w", name, err)
	}
	return hook, delivery, nil
}

func isNull(raw json.RawMessage) bool {
	return string(bytes.TrimSpace(raw)) == "null"
}

func withRegistration(source map[string][]*registration, key string, reg *registration) map[string][]*registration {
	copied := make(map[string][]*registration, len(source)+1)
	for k, v := range source {
		copied[k] = v
	}
	list := source[key]
	copied[key] = append(list[:len(list):len(list)], reg)
	return copied
}

func withoutRegistration(source map[string][]*registration, key string, reg *registration) map[string][]*registration {
	copied := make(map[string][]*registration, len(source))
	for k, v := range source {
		copied[k] = v
	}
	if left := without(source[key], reg); len(left) > 0 {
		copied[key] = left
	} else {
		delete(copied, key)
	}
	return copied
}

func without(list []*registration, reg *registration) []*registration {
	left := make([]*registration, 0, len(list))
	for _, r := range list {
		if r != reg {
			left = append(left, r)
		}
	}
	return left
}
