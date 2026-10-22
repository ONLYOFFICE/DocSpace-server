#!/usr/bin/env python3
"""
Logs a DocSpace webhook delivery to the console.

    python log_webhook.py                          # replay the shared fixture
    python log_webhook.py <body> <secret> <sig>

Python has no src/ runtime yet, so signature checking and the trigger ->
payload mapping live here. Regeneration (the generate-webhook-sdk skill) runs
this over every fixture in ../../samples after each generation.
"""

import hashlib
import hmac
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
FIXTURES = HERE.parents[1] / "samples"

sys.path.insert(0, str(HERE.parent / "generated"))

from docspace_webhooks_sdk.models.file_payload import FilePayload  # noqa: E402
from docspace_webhooks_sdk.models.folder_payload import FolderPayload  # noqa: E402
from docspace_webhooks_sdk.models.form_submit_payload import FormSubmitPayload  # noqa: E402
from docspace_webhooks_sdk.models.group_payload import GroupPayload  # noqa: E402
from docspace_webhooks_sdk.models.room_payload import RoomPayload  # noqa: E402
from docspace_webhooks_sdk.models.user_payload import UserPayload  # noqa: E402
from docspace_webhooks_sdk.models.webhook_config_info import WebhookConfigInfo  # noqa: E402
from docspace_webhooks_sdk.models.webhook_event_info import WebhookEventInfo  # noqa: E402

SIGNATURE_HEADER = "x-docspace-signature-256"

# Kept in step with x-docspace-trigger-payloads in the contract. Never match on
# FileEntryPayload: that is only the part files, folders and rooms share.
PAYLOAD_TYPES = {
    "user.": UserPayload,
    "group.": GroupPayload,
    "file.": FilePayload,
    "folder.": FolderPayload,
    "room.": RoomPayload,
    # An agent is an AI room and arrives in the same shape.
    "agent.": RoomPayload,
    "form.filled.out": FilePayload,
    "form.stopped": FilePayload,
}


def compute_signature(body: bytes, secret: str) -> str:
    """`sha256=` + UPPERCASE hex, mirroring WebhookSender.GetSecretHash."""
    digest = hmac.new(secret.encode("utf-8"), body, hashlib.sha256).hexdigest()
    return "sha256=" + digest.upper()


def verify_signature(body: bytes, secret: str, signature: str | None) -> bool:
    """
    Constant-time, case-insensitive check over the RAW body.

    DocSpace emits uppercase hex where GitHub emits lowercase, so a receiver
    ported from GitHub's docs fails unless the comparison folds case.
    """
    if not signature:
        return False
    return hmac.compare_digest(
        compute_signature(body, secret).lower(), signature.strip().lower()
    )


def payload_model(trigger: str):
    """The payload model, or None for a trigger newer than this SDK."""
    if trigger == "form.submit":
        return FormSubmitPayload
    for prefix, model in PAYLOAD_TYPES.items():
        if trigger.startswith(prefix):
            return model
    return None


def entry_id(value) -> str:
    """An EntryId oneOf: int for DocSpace storage, str for third-party entries."""
    return "<none>" if value is None else str(value.actual_instance)


def main() -> int:
    body_path = Path(sys.argv[1]) if len(sys.argv) > 1 else FIXTURES / "file.created.json"
    secret = sys.argv[2] if len(sys.argv) > 2 else (FIXTURES / "SECRET").read_text().strip()
    signature = (
        sys.argv[3] if len(sys.argv) > 3
        else (FIXTURES / "file.created.sig").read_text().strip()
    )

    # Bytes, not text: the signature covers exactly what arrived.
    body = body_path.read_bytes()

    if not verify_signature(body, secret, signature):
        print("REJECTED: signature does not match", file=sys.stderr)
        return 1

    envelope = json.loads(body)
    trigger = envelope.get("event", {}).get("trigger")
    if not trigger:
        print("envelope has no event.trigger", file=sys.stderr)
        return 1

    event = WebhookEventInfo.from_dict(envelope.get("event") or {})
    config = WebhookConfigInfo.from_dict(envelope.get("webhook") or {})
    model = payload_model(trigger)
    payload = model.from_dict(envelope.get("payload") or {}) if model else None

    print(trigger)
    print("  signature ok")
    print(f"  event #{event.id}  at {event.create_on}  by {event.create_by}")
    print(f'  subscription #{config.id} "{config.name}"')

    if model is None:
        # A trigger family added on the server after this SDK was generated:
        # acknowledge it and keep the raw payload rather than failing.
        print("  no payload model for this trigger; raw payload below")
    elif isinstance(payload, (FilePayload, FolderPayload, RoomPayload)):
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

    print()
    print("\n".join("  " + l for l in
                    json.dumps(envelope.get("payload"), indent=2).splitlines()))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
