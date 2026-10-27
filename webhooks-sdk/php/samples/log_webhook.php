<?php
/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *   php log_webhook.php                          # replay the shared fixture
 *   php log_webhook.php <body> <secret> <sig> [<x-docspace-event-id>]
 */

declare(strict_types=1);

// Map the SDK's classes onto the generated tree, so the sample runs without
// `composer install`. A real receiver requires onlyoffice/docspace-webhooks-sdk
// and uses Composer's autoloader instead.
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

use OnlyOffice\DocSpace\Webhooks\Sdk\InMemoryWebhookEventStore;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\FormSubmitPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookConfigInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookEnvelope;
use OnlyOffice\DocSpace\Webhooks\Sdk\Model\WebhookEventInfo;
use OnlyOffice\DocSpace\Webhooks\Sdk\WebhookOutcome;
use OnlyOffice\DocSpace\Webhooks\Sdk\WebhookRouter;
use OnlyOffice\DocSpace\Webhooks\Sdk\WebhookTriggers;

/**
 * The payload model the generated trigger table names: "untyped" for the legacy
 * catch-all, "unknown" for a trigger newer than this SDK.
 */
function model_name(string $trigger): string
{
    $class = WebhookTriggers::payloadType($trigger);
    if ($class !== null) {
        return short_name($class);
    }
    return in_array($trigger, WebhookTriggers::UNTYPED_TRIGGERS, true) ? 'untyped' : 'unknown';
}

function short_name(string $class): string
{
    return substr($class, strrpos($class, '\\') + 1);
}

function print_header(string $trigger, ?WebhookEventInfo $event, ?WebhookConfigInfo $config, string $delivery): void
{
    $known = model_name($trigger) === 'unknown' ? '  (unknown to this build)' : '';
    echo $trigger, $known, PHP_EOL;
    echo '  signature ok', PHP_EOL;
    echo '  model: ', model_name($trigger), PHP_EOL;
    echo '  delivery: ', $delivery, PHP_EOL;
    printf("  event #%d  at %s  by %s\n",
        $event?->getId(),
        $event?->getCreateOn()?->format('Y-m-d\TH:i:s\Z'),
        $event?->getCreateBy());
    printf("  subscription #%d \"%s\"\n", $config?->getId(), $config?->getName());
}

function print_payload(mixed $payload): void
{
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
}

$fixtures      = __DIR__ . '/../../samples';
$bodyPath      = $argv[1] ?? $fixtures . '/file.created.json';
$secret        = $argv[2] ?? trim((string) file_get_contents($fixtures . '/SECRET'));
$signature     = $argv[3] ?? trim((string) file_get_contents($fixtures . '/file.created.sig'));
$eventIdHeader = $argv[4] ?? null;

// Raw bytes: the signature covers exactly what arrived.
$body = (string) file_get_contents($bodyPath);

// The raw payload, printed after the summary as it arrived.
$print_raw = static function () use ($body): void {
    $pretty = json_encode(json_decode($body)?->payload ?? null, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
    echo PHP_EOL, '  ' . str_replace("\n", "\n  ", (string) $pretty), PHP_EOL;
};

/*
 * The router checks the signature, drops an event it has processed before
 * (DocSpace delivers at least once, and a retry carries the same event.id),
 * reads the body into the delivery model of its trigger and calls the handlers.
 * The store lives in memory for the sake of the sample; a real receiver persists
 * the ids for as long as an event can still arrive -- the automatic retries span
 * about 18 hours on the default schedule, and a manual retry can come at any
 * time later.
 */
$router = new WebhookRouter($secret, new InMemoryWebhookEventStore());

// One handler for every trigger with a typed body, from the generated table. A
// receiver normally registers only the triggers it cares about.
$router->on(array_keys(WebhookTriggers::DELIVERY_TYPES), static function (object $delivery, string $trigger) use ($print_raw): void {
    print_header($trigger, $delivery->getEvent(), $delivery->getWebhook(), short_name($delivery::class));
    print_payload($delivery->getPayload());
    $print_raw();
});

// A handler for one trigger, which gets that trigger's delivery model.
$router->on('file.created', static function (FileDelivery $d): void {
    printf("  on('file.created'): \"%s\" in folder %s\n",
        $d->getPayload()?->getTitle(),
        var_export($d->getPayload()?->getParentId(), true));
});

// Whatever has no handler: the catch-all '*', and triggers newer than this SDK.
$router->onOther(static function (WebhookEnvelope $envelope, string $trigger) use ($print_raw): void {
    print_header($trigger, $envelope->getEvent(), $envelope->getWebhook(), 'none');
    print_payload(null);
    $print_raw();
});

// In an HTTP endpoint, answer with $result->statusCode: a 2xx acknowledges the
// delivery, anything else makes DocSpace send it again.
$result = $router->handle($body, $signature, $eventIdHeader);

switch ($result->outcome) {
    case WebhookOutcome::Rejected:
        fwrite(STDERR, "REJECTED: signature does not match\n");
        break;
    case WebhookOutcome::Invalid:
        fwrite(STDERR, "unparseable body: {$result->error}\n");
        break;
    case WebhookOutcome::Failed:
        fwrite(STDERR, "a handler failed: {$result->error}\n");
        break;
    case WebhookOutcome::Duplicate:
        echo "duplicate of event #{$result->eventId}, already processed; skipped", PHP_EOL;
        break;
    default:
        break;
}

exit($result->isSuccess() ? 0 : 1);
