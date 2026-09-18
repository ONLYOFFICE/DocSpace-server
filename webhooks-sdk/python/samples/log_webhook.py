#!/usr/bin/env python3
"""
Logs a DocSpace webhook delivery to the console.

    python log_webhook.py                          # replay the shared fixture
    python log_webhook.py <body> <secret> <sig>

Python has no src/ runtime yet, so signature checking lives here. The trigger ->
payload mapping is read from the generated contract rather than retyped.
"""

import hashlib
import hmac
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
FIXTURES = HERE.parents[1] / "samples"

sys.path.insert(0, str(HERE.parent / "generated"))

from docspace_webhooks_sdk.models.file_entry_payload import FileEntryPayload  # noqa: E402
from docspace_webhooks_sdk.models.form_submit_payload import FormSubmitPayload  # noqa: E402
from docspace_webhooks_sdk.models.group_payload import GroupPayload  # noqa: E402
from docspace_webhooks_sdk.models.user_payload import UserPayload  # noqa: E402
from docspace_webhooks_sdk.models.webhook_config_info import WebhookConfigInfo  # noqa: E402
from docspace_webhooks_sdk.models.webhook_event_info import WebhookEventInfo  # noqa: E402

SIGNATURE_HEADER = "x-docspace-signature-256"

# Kept in step with x-docspace-trigger-payloads in the contract.
PAYLOAD_TYPES = {
    "user.": UserPayload,
    "group.": GroupPayload,
    "form.submit": FormSubmitPayload,
    # Everything else about an entry -- file.*, folder.*, room.*, agent.*, and
    # the other form.* triggers -- arrives as the FileEntry<T> base.
    "": FileEntryPayload,
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
    if trigger == "form.submit":
        return FormSubmitPayload
    for prefix, model in PAYLOAD_TYPES.items():
        if prefix and trigger.startswith(prefix):
            return model
    return FileEntryPayload


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
    payload = payload_model(trigger).from_dict(envelope.get("payload") or {})

    print(trigger)
    print("  signature ok")
    print(f"  event #{event.id}  at {event.create_on}  by {event.create_by}")
    print(f'  subscription #{config.id} "{config.name}"')

    if isinstance(payload, FileEntryPayload):
        # fileEntryType is the only file/folder discriminator: 1 folder, 2 file.
        kind = "file" if payload.file_entry_type == 2 else "folder"
        print(f"  {kind}: {payload.title}  id={payload.id.actual_instance}"
              f"  parent={payload.parent_id.actual_instance}")
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
