#!/usr/bin/env python3
"""
Logs a DocSpace webhook delivery to the console.

    python log_webhook.py                          # replay the shared fixture
    python log_webhook.py <body> <secret> <sig> [<x-docspace-event-id>]

The delivery goes through docspace_webhooks_sdk.router.WebhookRouter, which
checks the signature and reads the body into the delivery model of its trigger
from the generated table (docspace_webhooks_sdk.triggers). Regeneration (the
generate-webhook-sdk skill) runs this over every fixture in ../../samples after
each generation.
"""

import json
import sys
from pathlib import Path
from typing import Any, Optional

HERE = Path(__file__).resolve().parent
FIXTURES = HERE.parents[1] / "samples"

sys.path.insert(0, str(HERE.parent / "generated"))

from docspace_webhooks_sdk.models.file_delivery import FileDelivery  # noqa: E402
from docspace_webhooks_sdk.models.file_payload import FilePayload  # noqa: E402
from docspace_webhooks_sdk.models.folder_payload import FolderPayload  # noqa: E402
from docspace_webhooks_sdk.models.form_submit_payload import FormSubmitPayload  # noqa: E402
from docspace_webhooks_sdk.models.group_payload import GroupPayload  # noqa: E402
from docspace_webhooks_sdk.models.room_payload import RoomPayload  # noqa: E402
from docspace_webhooks_sdk.models.user_payload import UserPayload  # noqa: E402
from docspace_webhooks_sdk.models.webhook_config_info import WebhookConfigInfo  # noqa: E402
from docspace_webhooks_sdk.models.webhook_event_info import WebhookEventInfo  # noqa: E402
from docspace_webhooks_sdk.router import (  # noqa: E402
    AnyWebhook,
    InMemoryEventStore,
    WebhookOutcome,
    WebhookRouter,
)
from docspace_webhooks_sdk.triggers import (  # noqa: E402
    DELIVERY_TYPES,
    UNTYPED_TRIGGERS,
    is_known_trigger,
    payload_type,
)


def model_name(trigger: str) -> str:
    """The payload model the generated table names; "untyped" for the catch-all, "unknown" for a newer trigger."""
    model = payload_type(trigger)
    if model is not None:
        return model.__name__
    return "untyped" if trigger in UNTYPED_TRIGGERS else "unknown"


def entry_id(value) -> str:
    """An EntryId oneOf: int for DocSpace storage, str for third-party entries."""
    return "<none>" if value is None else str(value.actual_instance)


def print_header(trigger: str, event: Optional[WebhookEventInfo],
                 webhook: Optional[WebhookConfigInfo], delivery: str) -> None:
    known = "" if is_known_trigger(trigger) else "  (unknown to this build)"
    print(f"{trigger}{known}")
    print("  signature ok")
    print(f"  model: {model_name(trigger)}")
    print(f"  delivery: {delivery}")
    print(f"  event #{event and event.id}  at {event and event.create_on}  by {event and event.create_by}")
    print(f'  subscription #{webhook and webhook.id} "{webhook and webhook.name}"')


def print_summary(payload: Any) -> None:
    if isinstance(payload, (FilePayload, FolderPayload, RoomPayload)):
        kind = {RoomPayload: "room", FolderPayload: "folder"}.get(type(payload), "file")
        print(f"  {kind}: {payload.title}  id={entry_id(payload.id)}"
              f"  parent={entry_id(payload.parent_id)}")
    elif isinstance(payload, FormSubmitPayload):
        original = payload.original_form.title if payload.original_form else None
        submitted = payload.submitted_form.title if payload.submitted_form else None
        print(f"  form: {original}  submitted as {submitted}")
    elif isinstance(payload, UserPayload):
        print(f"  user: {payload.user_name} <{payload.email}>")
    elif isinstance(payload, GroupPayload):
        print(f"  group: {payload.name}")
    else:
        # The catch-all, or a trigger added on the server after this SDK was
        # generated: acknowledge it and keep the raw payload rather than parsing
        # it with some other model.
        print("  no payload model for this trigger; raw payload below")


def print_raw(payload: Any) -> None:
    print()
    print("\n".join("  " + line for line in json.dumps(payload, indent=2, default=str).splitlines()))


def build_router(secret: str) -> WebhookRouter:
    """
    The router this sample hands the delivery to. It checks the signature, drops
    an event it has processed before (DocSpace delivers at least once, and a
    retry carries the same event.id), reads the body into the delivery model of
    its trigger and calls the handlers. The store lives in memory for the sake
    of the sample; a real receiver persists the ids for as long as an event can
    still arrive -- the automatic retries span about 18 hours on the default
    schedule, and a manual retry can come at any time later.
    """
    router = WebhookRouter(secret, store=InMemoryEventStore())

    # One handler for every trigger with a typed body, from the generated table.
    # A receiver normally registers only the triggers it cares about.
    def log_delivery(delivery: Any, trigger: str) -> None:
        print_header(trigger, delivery.event, delivery.webhook, type(delivery).__name__)
        print_summary(delivery.payload)
        print_raw(delivery.payload.to_dict() if delivery.payload is not None else None)

    router.on(list(DELIVERY_TYPES), log_delivery)

    # A handler for one trigger, registered as a decorator: it gets a FileDelivery.
    @router.on("file.created")
    def file_created(delivery: FileDelivery, trigger: str) -> None:
        payload = delivery.payload
        print(f'  on("file.created"): "{payload and payload.title}"'
              f" in folder {entry_id(payload and payload.parent_id)}")

    # Whatever has no handler: the catch-all '*', and triggers newer than this SDK.
    def log_other(hook: AnyWebhook) -> None:
        print_header(hook.trigger, hook.event, hook.webhook, "none")
        print_summary(None)
        print_raw(hook.payload)

    router.on_other(log_other)
    return router


def main() -> int:
    body_path = Path(sys.argv[1]) if len(sys.argv) > 1 else FIXTURES / "file.created.json"
    secret = sys.argv[2] if len(sys.argv) > 2 else (FIXTURES / "SECRET").read_text().strip()
    signature = (
        sys.argv[3] if len(sys.argv) > 3
        else (FIXTURES / "file.created.sig").read_text().strip()
    )
    event_id_header = sys.argv[4] if len(sys.argv) > 4 else None

    # Bytes, not text: the signature covers exactly what arrived.
    body = body_path.read_bytes()

    # In an HTTP handler, answer with result.status_code: a 2xx acknowledges the
    # delivery, anything else makes DocSpace send it again.
    result = build_router(secret).handle(body, signature, event_id_header)

    if result.outcome is WebhookOutcome.REJECTED:
        print("REJECTED: signature does not match", file=sys.stderr)
    elif result.outcome is WebhookOutcome.INVALID:
        print(f"unparseable body: {result.error}", file=sys.stderr)
    elif result.outcome is WebhookOutcome.FAILED:
        print(f"a handler failed: {result.error}", file=sys.stderr)
    elif result.outcome is WebhookOutcome.DUPLICATE:
        print(f"duplicate of event #{result.event_id}, already processed; skipped")

    return 0 if 200 <= result.status_code < 300 else 1


if __name__ == "__main__":
    raise SystemExit(main())
