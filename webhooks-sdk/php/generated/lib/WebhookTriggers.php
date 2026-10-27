<?php
// Trigger -> payload dispatch, generated from docspace-webhooks.yaml by
// tools/gen-trigger-map.py. Do not edit; regenerate the webhook SDKs instead.

declare(strict_types=1);

namespace OnlyOffice\DocSpace\Webhooks\Sdk;

/** Which payload model each DocSpace trigger carries. */
final class WebhookTriggers
{
    /** Trigger name to payload model, for every trigger with a typed body. */
    public const PAYLOAD_TYPES = [
        'user.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload::class,
        'user.invited' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload::class,
        'user.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload::class,
        'user.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserPayload::class,
        'group.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupPayload::class,
        'group.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupPayload::class,
        'group.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupPayload::class,
        'file.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.uploaded' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.trashed' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.restored' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.copied' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.moved' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'file.downloaded' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'folder.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.trashed' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.restored' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.copied' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.moved' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'folder.downloaded' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderPayload::class,
        'room.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'room.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'room.archived' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'room.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'room.restored' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'room.copied' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'form.submit' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FormSubmitPayload::class,
        'form.filled.out' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'form.stopped' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FilePayload::class,
        'agent.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'agent.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
        'agent.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomPayload::class,
    ];

    /** The legacy catch-all: sent only when the server cannot read a stored payload; no typed body. */
    public const UNTYPED_TRIGGERS = ['*'];

    /** True when the trigger is in the contract this build was generated from, the catch-all included. */
    public static function isKnown(string $trigger): bool
    {
        return isset(self::PAYLOAD_TYPES[$trigger]) || in_array($trigger, self::UNTYPED_TRIGGERS, true);
    }

    /** The payload model class of the trigger, or null when it has no typed body. */
    public static function payloadType(string $trigger): ?string
    {
        return self::PAYLOAD_TYPES[$trigger] ?? null;
    }

    /** Trigger name to the model of the whole delivery (the envelope with payload typed), for every typed trigger. */
    public const DELIVERY_TYPES = [
        'user.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserDelivery::class,
        'user.invited' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserDelivery::class,
        'user.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserDelivery::class,
        'user.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserDelivery::class,
        'group.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupDelivery::class,
        'group.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupDelivery::class,
        'group.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\GroupDelivery::class,
        'file.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.uploaded' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.trashed' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.restored' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.copied' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.moved' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'file.downloaded' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'folder.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.trashed' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.restored' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.copied' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.moved' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'folder.downloaded' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FolderDelivery::class,
        'room.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'room.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'room.archived' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'room.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'room.restored' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'room.copied' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'form.submit' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FormSubmitDelivery::class,
        'form.filled.out' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'form.stopped' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\FileDelivery::class,
        'agent.created' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'agent.updated' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
        'agent.deleted' => \OnlyOffice\DocSpace\Webhooks\Sdk\Model\RoomDelivery::class,
    ];

    /** The delivery model class of the trigger, or null when it has no typed body. */
    public static function deliveryType(string $trigger): ?string
    {
        return self::DELIVERY_TYPES[$trigger] ?? null;
    }
}
