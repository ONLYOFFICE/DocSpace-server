<?php
/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *   php log_webhook.php                          # replay the shared fixture
 *   php log_webhook.php <body> <secret> <sig>
 *
 * PHP has no src/ runtime yet, so signature checking lives here.
 */

declare(strict_types=1);

// Map only the classes this sample touches. Composer would work too, but the
// generated composer.json requires guzzle for the HTTP client -- and a webhook
// receiver never makes a request, so none of that needs to be installed.
spl_autoload_register(static function (string $class): void {
    $prefix = 'OnlyOffice\\DocSpace\\Webhooks\\Sdk\\';
    if (!str_starts_with($class, $prefix)) {
        return;
    }
    $relative = str_replace('\\', '/', substr($class, strlen($prefix)));
    $file = __DIR__ . '/../generated/lib/' . $relative . '.php';
    if (is_file($file)) {
        require_once $file;
    }
});

use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileEntryPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookConfigInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookEventInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\ObjectSerializer;

const SIGNATURE_HEADER = 'x-docspace-signature-256';

/** `sha256=` + UPPERCASE hex, mirroring WebhookSender.GetSecretHash. */
function compute_signature(string $body, string $secret): string
{
    return 'sha256=' . strtoupper(hash_hmac('sha256', $body, $secret));
}

/**
 * Constant-time, case-insensitive check over the RAW body. DocSpace emits
 * uppercase hex where GitHub emits lowercase, so the comparison must fold case.
 */
function verify_signature(string $body, string $secret, ?string $signature): bool
{
    if ($signature === null || $signature === '') {
        return false;
    }
    return hash_equals(
        strtolower(compute_signature($body, $secret)),
        strtolower(trim($signature))
    );
}

/** Which model a trigger's payload deserializes to. */
function payload_class(string $trigger): string
{
    if (str_starts_with($trigger, 'user.')) {
        return UserPayload::class;
    }
    if (str_starts_with($trigger, 'group.')) {
        return GroupPayload::class;
    }
    // file.*, folder.*, room.*, agent.* and form.* all arrive as the
    // FileEntry<T> base -- no subtype-specific field is ever sent.
    return FileEntryPayload::class;
}

$fixtures  = __DIR__ . '/../../samples';
$bodyPath  = $argv[1] ?? $fixtures . '/file.created.json';
$secret    = $argv[2] ?? trim((string) file_get_contents($fixtures . '/SECRET'));
$signature = $argv[3] ?? trim((string) file_get_contents($fixtures . '/file.created.sig'));

// Raw bytes: the signature covers exactly what arrived.
$body = (string) file_get_contents($bodyPath);

if (!verify_signature($body, $secret, $signature)) {
    fwrite(STDERR, "REJECTED: signature does not match\n");
    exit(1);
}

$envelope = json_decode($body);
if (!is_object($envelope) || !isset($envelope->event->trigger)) {
    fwrite(STDERR, "envelope has no event.trigger\n");
    exit(1);
}

$trigger = (string) $envelope->event->trigger;
$event   = ObjectSerializer::deserialize($envelope->event, WebhookEventInfo::class);
$config  = ObjectSerializer::deserialize($envelope->webhook, WebhookConfigInfo::class);
$payload = ObjectSerializer::deserialize($envelope->payload, payload_class($trigger));

echo $trigger, PHP_EOL;
echo '  signature ok', PHP_EOL;
printf("  event #%d  at %s  by %s\n",
    $event->getId(),
    $event->getCreateOn()?->format('Y-m-d\TH:i:s\Z'),
    $event->getCreateBy());
printf("  subscription #%d \"%s\"\n", $config->getId(), $config->getName());

if ($payload instanceof FileEntryPayload) {
    // 1 = folder, 2 = file. The only discriminator there is.
    $kind = $payload->getFileEntryType() === 2 ? 'file' : 'folder';

    // Ids come from the raw envelope, not from the model. The PHP generator
    // renders the EntryId oneOf as a property-less class, so getId() yields an
    // empty object and the value is lost. Every other field maps correctly.
    printf("  %s: %s  id=%s  parent=%s\n",
        $kind,
        $payload->getTitle(),
        var_export($envelope->payload->id ?? null, true),
        var_export($envelope->payload->parentId ?? null, true));
} elseif ($payload instanceof UserPayload) {
    printf("  user: %s <%s>\n", $payload->getUserName(), $payload->getEmail());
} elseif ($payload instanceof GroupPayload) {
    printf("  group: %s\n", $payload->getName());
}

echo PHP_EOL;
$pretty = json_encode($envelope->payload, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
echo '  ' . str_replace("\n", "\n  ", (string) $pretty), PHP_EOL;
