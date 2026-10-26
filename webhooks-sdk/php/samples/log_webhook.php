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

// Map only the classes this sample touches, so it runs straight from the
// generated tree without `composer install`. A real receiver requires
// onlyoffice/docspace-webhooks-sdk and uses Composer's autoloader instead.
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

use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FormSubmitPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookConfigInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookEventInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\ObjectSerializer;
use OnlyOffice\DocSpace\Webhooks\Sdk\WebhookTriggers;

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

/**
 * The payload model the generated trigger table names: "untyped" for the legacy
 * catch-all, "unknown" for a trigger newer than this SDK.
 */
function model_name(string $trigger): string
{
    $class = WebhookTriggers::payloadType($trigger);
    if ($class !== null) {
        return substr($class, strrpos($class, '\\') + 1);
    }
    return in_array($trigger, WebhookTriggers::UNTYPED_TRIGGERS, true) ? 'untyped' : 'unknown';
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
if ($trigger === '') {
    fwrite(STDERR, "envelope has no event.trigger\n");
    exit(1);
}
$event   = ObjectSerializer::deserialize($envelope->event, WebhookEventInfo::class);
$config  = ObjectSerializer::deserialize($envelope->webhook, WebhookConfigInfo::class);
// The catch-all, or a trigger added on the server after this SDK was generated,
// is acknowledged with its raw payload and never parsed with some other model.
$model = WebhookTriggers::payloadType($trigger);
try {
    $payload = $model === null ? null : ObjectSerializer::deserialize($envelope->payload ?? null, $model);
} catch (\Throwable $e) {
    fwrite(STDERR, "unparseable {$trigger} payload: {$e->getMessage()}\n");
    exit(1);
}
if ($model !== null && !($payload instanceof $model)) {
    fwrite(STDERR, "unparseable {$trigger} payload: not a " . model_name($trigger) . "\n");
    exit(1);
}

echo $trigger, PHP_EOL;
echo '  signature ok', PHP_EOL;
echo '  model: ', model_name($trigger), PHP_EOL;
printf("  event #%d  at %s  by %s\n",
    $event->getId(),
    $event->getCreateOn()?->format('Y-m-d\TH:i:s\Z'),
    $event->getCreateBy());
printf("  subscription #%d \"%s\"\n", $config->getId(), $config->getName());

if ($payload instanceof FilePayload || $payload instanceof FolderPayload || $payload instanceof RoomPayload) {
    // Ids are an int for DocSpace storage and a string for third-party entries.
    $kind = match (true) {
        $payload instanceof RoomPayload => 'room',
        $payload instanceof FolderPayload => 'folder',
        default => 'file',
    };
    printf("  %s: %s  id=%s  parent=%s\n",
        $kind,
        $payload->getTitle(),
        var_export($payload->getId(), true),
        var_export($payload->getParentId(), true));
} elseif ($payload instanceof FormSubmitPayload) {
    printf("  form: %s  submitted as %s\n",
        $payload->getOriginalForm()?->getTitle(),
        $payload->getSubmittedForm()?->getTitle());
} elseif ($payload instanceof UserPayload) {
    printf("  user: %s <%s>\n", $payload->getUserName(), $payload->getEmail());
} elseif ($payload instanceof GroupPayload) {
    printf("  group: %s\n", $payload->getName());
} else {
    echo '  no payload model for this trigger; raw payload below', PHP_EOL;
}

echo PHP_EOL;
$pretty = json_encode($envelope->payload, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
echo '  ' . str_replace("\n", "\n  ", (string) $pretty), PHP_EOL;
