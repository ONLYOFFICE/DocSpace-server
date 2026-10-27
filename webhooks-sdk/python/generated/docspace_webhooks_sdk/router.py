"""
Dispatches verified DocSpace webhook deliveries to handlers registered per trigger.

Hand-written: lives in python/shim/ and is copied into the generated package by
the generate-webhook-sdk post-step, so it survives regeneration.

    router = WebhookRouter(secret, store=InMemoryEventStore())

    @router.on("file.created")
    def created(delivery: FileDelivery, trigger: str) -> None:
        ...

    result = router.handle(body, headers.get("x-docspace-signature-256"),
                           headers.get("x-docspace-event-id"))
    # answer DocSpace with result.status_code
"""

from __future__ import annotations

import enum
import hashlib
import hmac
import json
import threading
from dataclasses import dataclass
from typing import Any, Callable, Dict, Iterable, List, Optional, Protocol, Sequence, Set, TypeVar, Union

from docspace_webhooks_sdk.models.webhook_config_info import WebhookConfigInfo
from docspace_webhooks_sdk.models.webhook_event_info import WebhookEventInfo
from docspace_webhooks_sdk.triggers import delivery_type

__all__ = [
    "WebhookOutcome",
    "HandleResult",
    "AnyWebhook",
    "EventStore",
    "InMemoryEventStore",
    "WebhookRouter",
    "verify_signature",
]


class WebhookOutcome(str, enum.Enum):
    """What `WebhookRouter.handle` did with a delivery."""

    #: At least one handler ran, and all of them completed.
    HANDLED = "handled"
    #: The event was processed before; nothing ran.
    DUPLICATE = "duplicate"
    #: No handler is registered for the trigger; the delivery is acknowledged all the same.
    UNHANDLED = "unhandled"
    #: The signature is missing or does not match.
    REJECTED = "rejected"
    #: The body is not a delivery, or a typed trigger arrived without its payload.
    INVALID = "invalid"
    #: A handler raised; the remaining ones did not run.
    FAILED = "failed"


@dataclass(frozen=True)
class HandleResult:
    """
    The result of one delivery: what happened and the HTTP status to answer
    DocSpace with. A 2xx acknowledges the delivery; anything else is a failed
    attempt, which DocSpace retries on its schedule.
    """

    outcome: WebhookOutcome
    status_code: int
    trigger: Optional[str] = None
    event_id: Optional[int] = None
    error: Optional[str] = None
    cause: Optional[BaseException] = None


@dataclass(frozen=True)
class AnyWebhook:
    """
    A delivery no typed handler takes: the untyped catch-all ``*``, a trigger
    added after this SDK was built, or a typed trigger nobody registered for.
    ``payload`` is the raw JSON value, not narrowed to a model.
    """

    trigger: str
    event: Optional[WebhookEventInfo]
    webhook: Optional[WebhookConfigInfo]
    payload: Any
    #: The envelope as received.
    raw: Dict[str, Any]


class EventStore(Protocol):
    """
    Remembers which events were processed. DocSpace delivers at least once: an
    automatic or manual retry sends the same ``event.id`` again, and the signature
    does not prove when a request was sent, so the id is the duplicate and replay
    defence. A real receiver persists the ids for as long as an event can still
    arrive -- the automatic retries span about 18 hours on the default schedule,
    and a manual retry can come at any time later.
    """

    def is_processed(self, event_id: int) -> bool: ...

    def mark_processed(self, event_id: int) -> None:
        """Called only after every handler completed."""
        ...


class InMemoryEventStore:
    """An `EventStore` in process memory: for samples and tests, lost on restart."""

    def __init__(self) -> None:
        self._ids: Set[int] = set()
        self._lock = threading.Lock()

    def is_processed(self, event_id: int) -> bool:
        with self._lock:
            return event_id in self._ids

    def mark_processed(self, event_id: int) -> None:
        with self._lock:
            self._ids.add(event_id)


def verify_signature(body: bytes, secret: str, signature: Optional[str]) -> bool:
    """
    Constant-time, case-insensitive check of ``x-docspace-signature-256`` over
    the RAW body. DocSpace emits uppercase hex where GitHub emits lowercase, so
    a receiver ported from GitHub's docs fails unless the comparison folds case.
    """
    if not isinstance(signature, str) or not signature:
        return False
    expected = "sha256=" + hmac.new(secret.encode("utf-8"), body, hashlib.sha256).hexdigest()
    return hmac.compare_digest(expected.encode("ascii"),
                               signature.strip().lower().encode("utf-8"))


DeliveryHandler = Callable[[Any, str], None]
OtherHandler = Callable[[AnyWebhook], None]
H = TypeVar("H", bound=DeliveryHandler)


class _ParseError(Exception):
    pass


class WebhookRouter:
    """
    Dispatches verified deliveries to handlers registered per trigger. A handler
    is called as ``handler(delivery, trigger)``, where ``delivery`` is the model
    of the whole body for that trigger (``FileDelivery`` for ``file.created``,
    ...). Any number of handlers can be registered for one trigger; `on` returns
    a function that removes the registration again, and `off` does the same.

    `handle` checks the signature, drops a duplicate, reads the body into the
    delivery model of its trigger and calls every handler of that trigger in
    registration order. Answer DocSpace with the result's ``status_code``: a
    handler that raises makes it a failed attempt, and DocSpace sends the
    delivery again, so handlers have to be idempotent.
    """

    def __init__(self, secret: str, store: Optional[EventStore] = None) -> None:
        if not secret:
            raise ValueError("a secret is required; use WebhookRouter.for_unsigned_replay() to skip the check")
        self._init(secret, store)

    def _init(self, secret: Optional[str], store: Optional[EventStore]) -> None:
        self._secret = secret
        self._store = store
        self._lock = threading.Lock()
        # Copy-on-write: handle() reads a snapshot, registration swaps in new lists.
        self._handlers: Dict[str, List[_Entry]] = {}
        self._other: List[_Entry] = []

    @classmethod
    def for_unsigned_replay(cls, store: Optional[EventStore] = None) -> "WebhookRouter":
        """
        A router that skips the signature check, for replaying saved bodies
        during development only. Never put it behind a reachable endpoint:
        anyone could send it a delivery.
        """
        router = cls.__new__(cls)
        router._init(None, store)
        return router

    def on(
        self,
        triggers: Union[str, Sequence[str]],
        handler: Optional[H] = None,
    ) -> Any:
        """
        Registers ``handler`` for each of ``triggers`` (a name or a list) and
        returns the function that removes it. Without ``handler`` it is a
        decorator that registers the decorated function and returns it.

        Raises ValueError for a trigger with no delivery model: the catch-all
        ``*`` and triggers unknown to this build go to `on_other`.
        """
        names = _trigger_list(triggers)
        for name in names:
            if delivery_type(name) is None:
                raise ValueError(f"'{name}' has no delivery model; handle it with on_other")

        if handler is None:
            def register(fn: H) -> H:
                self.on(names, fn)
                return fn
            return register

        entry = _Entry(handler)
        with self._lock:
            for name in names:
                self._handlers = {**self._handlers, name: [*self._handlers.get(name, []), entry]}

        def remove() -> None:
            with self._lock:
                self._handlers = _without(self._handlers, names, lambda h: h is entry)
        return remove

    def off(self, triggers: Union[str, Sequence[str]], handler: DeliveryHandler) -> None:
        """Removes every registration of ``handler`` for each of ``triggers``."""
        names = _trigger_list(triggers)
        with self._lock:
            self._handlers = _without(self._handlers, names, lambda h: h.fn == handler)

    def on_other(self, handler: OtherHandler) -> Callable[[], None]:
        """
        Registers ``handler`` for the deliveries no other handler takes: triggers
        without a registration, the untyped catch-all ``*``, and triggers added
        after this SDK was built. It gets an `AnyWebhook`. Returns the function
        that removes it.
        """
        entry = _Entry(handler)
        with self._lock:
            self._other = [*self._other, entry]

        def remove() -> None:
            with self._lock:
                self._other = [h for h in self._other if h is not entry]
        return remove

    def handle(
        self,
        body: Union[bytes, str],
        signature: Optional[str],
        event_id_header: Optional[str] = None,
    ) -> HandleResult:
        """
        Handles one delivery.

        :param body: The request body, exactly as received.
        :param signature: The ``x-docspace-signature-256`` header.
        :param event_id_header: The ``x-docspace-event-id`` header, if at hand: an
            already processed id is dropped before any work. It is not signed, so
            it is only ever used to drop a delivery, never to accept one.
        """
        raw = body.encode("utf-8") if isinstance(body, str) else bytes(body)

        header_id = _parse_int(event_id_header)
        if self._store is not None and header_id is not None and self._store.is_processed(header_id):
            return HandleResult(WebhookOutcome.DUPLICATE, 200, event_id=header_id)

        if self._secret is not None and not verify_signature(raw, self._secret, signature):
            return HandleResult(WebhookOutcome.REJECTED, 401, error="signature is missing or does not match")

        try:
            envelope, trigger = _read_envelope(raw)
            model = delivery_type(trigger)
            delivery: Any = None
            if model is not None:
                if not isinstance(envelope.get("payload"), dict):
                    raise _ParseError(f"no payload object for '{trigger}'")
                delivery = model.from_dict(envelope)
            event = WebhookEventInfo.from_dict(envelope["event"])
            webhook = _config(envelope.get("webhook"))
        except Exception as err:  # _ParseError, pydantic's ValidationError, or a bad value
            return HandleResult(WebhookOutcome.INVALID, 400, error=str(err), cause=err)

        event_id = event.id if event is not None else None

        if self._store is not None and event_id is not None and self._store.is_processed(event_id):
            return HandleResult(WebhookOutcome.DUPLICATE, 200, trigger=trigger, event_id=event_id)

        typed = self._handlers.get(trigger, []) if model is not None else []
        other = self._other if not typed else []

        try:
            for entry in typed:
                entry.fn(delivery, trigger)
            if other:
                hook = AnyWebhook(
                    trigger=trigger,
                    event=event,
                    webhook=webhook,
                    payload=envelope.get("payload"),
                    raw=envelope,
                )
                for entry in other:
                    entry.fn(hook)
        except Exception as err:
            return HandleResult(WebhookOutcome.FAILED, 500, trigger=trigger, event_id=event_id,
                                error=str(err), cause=err)

        if self._store is not None and event_id is not None:
            self._store.mark_processed(event_id)

        outcome = WebhookOutcome.HANDLED if typed or other else WebhookOutcome.UNHANDLED
        return HandleResult(outcome, 200, trigger=trigger, event_id=event_id)


class _Entry:
    """One registration: the same function registered twice is two entries."""

    __slots__ = ("fn",)

    def __init__(self, fn: Callable[..., None]) -> None:
        self.fn = fn


def _trigger_list(triggers: Union[str, Iterable[str]]) -> List[str]:
    return [triggers] if isinstance(triggers, str) else list(triggers)


def _without(
    handlers: Dict[str, List[Any]], names: Iterable[str], drop: Callable[[Any], bool]
) -> Dict[str, List[Any]]:
    result = dict(handlers)
    for name in names:
        left = [h for h in result.get(name, []) if not drop(h)]
        if left:
            result[name] = left
        else:
            result.pop(name, None)
    return result


def _parse_int(value: Optional[str]) -> Optional[int]:
    if not value:
        return None
    try:
        return int(value.strip())
    except ValueError:
        return None


def _read_envelope(body: bytes) -> "tuple[Dict[str, Any], str]":
    try:
        envelope = json.loads(body)
    except ValueError as err:
        raise _ParseError(f"body is not JSON: {err}") from err
    if not isinstance(envelope, dict):
        raise _ParseError("body is not a webhook envelope")
    event = envelope.get("event")
    trigger = event.get("trigger") if isinstance(event, dict) else None
    if not isinstance(trigger, str):
        raise _ParseError("envelope has no event.trigger")
    return envelope, trigger


def _config(value: Any) -> Optional[WebhookConfigInfo]:
    return WebhookConfigInfo.from_dict(value) if isinstance(value, dict) else None
