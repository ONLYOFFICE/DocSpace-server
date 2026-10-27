package com.onlyoffice.docspace.webhooks.samples

import onlyoffice.docspace.webhooks.sdk.InMemoryWebhookEventStore
import onlyoffice.docspace.webhooks.sdk.WebhookJson
import onlyoffice.docspace.webhooks.sdk.WebhookOutcome
import onlyoffice.docspace.webhooks.sdk.WebhookRouter
import onlyoffice.docspace.webhooks.sdk.WebhookTriggers
import onlyoffice.docspace.webhooks.sdk.models.FileDelivery
import onlyoffice.docspace.webhooks.sdk.models.FilePayload
import onlyoffice.docspace.webhooks.sdk.models.FolderDelivery
import onlyoffice.docspace.webhooks.sdk.models.FolderPayload
import onlyoffice.docspace.webhooks.sdk.models.FormSubmitDelivery
import onlyoffice.docspace.webhooks.sdk.models.FormSubmitPayload
import onlyoffice.docspace.webhooks.sdk.models.GroupDelivery
import onlyoffice.docspace.webhooks.sdk.models.GroupPayload
import onlyoffice.docspace.webhooks.sdk.models.RoomDelivery
import onlyoffice.docspace.webhooks.sdk.models.RoomPayload
import onlyoffice.docspace.webhooks.sdk.models.UserDelivery
import onlyoffice.docspace.webhooks.sdk.models.UserPayload
import onlyoffice.docspace.webhooks.sdk.models.WebhookConfigInfo
import onlyoffice.docspace.webhooks.sdk.models.WebhookEventInfo
import onlyoffice.docspace.webhooks.sdk.onFileCreated

import java.io.File

/**
 * Logs a DocSpace webhook delivery to the console.
 *
 *     mvn -q compile exec:java                                   # shared fixture
 *     mvn -q compile exec:java -Dexec.args="<body> <secret> <sig>"
 *
 * The delivery goes through the SDK's WebhookRouter, which checks the signature,
 * drops a duplicate and hands the delivery model of the trigger to its handlers.
 */
fun main(args: Array<String>) {
    val fixtures = File("..", "samples")

    val bodyFile = if (args.size > 0) File(args[0]) else File(fixtures, "file.created.json")
    val secret = if (args.size > 1) args[1] else File(fixtures, "SECRET").readText().trim()
    val signature =
        if (args.size > 2) args[2] else File(fixtures, "file.created.sig").readText().trim()

    // Bytes, not a re-encoded object: the signature covers what arrived.
    val body = bodyFile.readBytes()

    // A receiver keeps the store for as long as an event can still arrive (see
    // WebhookEventStore); in memory it is enough for one replay.
    val router = WebhookRouter(secret, InMemoryWebhookEventStore())

    // One handler for every trigger with a delivery model, from the generated table.
    for ((trigger, type) in WebhookTriggers.deliveryTypes) {
        @Suppress("UNCHECKED_CAST")
        router.on(trigger, type as Class<Any>) { delivery -> logDelivery(delivery) }
    }

    // A second handler for one trigger, through its generated method: it runs after
    // the one above, and only once that one has completed.
    router.onFileCreated { delivery ->
        println("  onFileCreated: \"${delivery.payload?.title}\" in folder ${delivery.payload?.parentId ?: "<none>"}")
    }

    // What has no delivery model: the catch-all and triggers newer than this SDK.
    router.onOther { hook ->
        println(hook.trigger)
        println("  signature ok")
        println("  model: ${modelName(hook.trigger)}")
        println("  delivery: none")
        printEnvelope(hook.event, hook.webhook)
        // The catch-all, or a trigger added on the server after this SDK was
        // generated: acknowledge it and keep the raw payload rather than
        // parsing it with some other model.
        println("  no payload model for this trigger; raw payload below")
        printJson(hook.rawPayload)
    }

    val result = router.handle(body, signature)

    when (result.outcome) {
        WebhookOutcome.HANDLED, WebhookOutcome.UNHANDLED, WebhookOutcome.DUPLICATE -> Unit
        WebhookOutcome.REJECTED -> fail("REJECTED: ${result.error}")
        WebhookOutcome.INVALID -> fail("unparseable body: ${result.error}")
        WebhookOutcome.FAILED -> fail("a handler failed: ${result.error}")
    }
}

private fun fail(message: String): Nothing {
    System.err.println(message)
    kotlin.system.exitProcess(1)
}

/** Prints a delivery read into its model: envelope lines, a payload summary and the payload. */
private fun logDelivery(delivery: Any) {
    val (event, config, payload) = when (delivery) {
        is UserDelivery -> Triple(delivery.event, delivery.webhook, delivery.payload)
        is GroupDelivery -> Triple(delivery.event, delivery.webhook, delivery.payload)
        is FileDelivery -> Triple(delivery.event, delivery.webhook, delivery.payload)
        is FolderDelivery -> Triple(delivery.event, delivery.webhook, delivery.payload)
        is RoomDelivery -> Triple(delivery.event, delivery.webhook, delivery.payload)
        is FormSubmitDelivery -> Triple(delivery.event, delivery.webhook, delivery.payload)
        else -> error("no summary for ${delivery.javaClass.simpleName}")
    }
    val trigger = event?.trigger.orEmpty()

    println(trigger)
    println("  signature ok")
    println("  model: ${modelName(trigger)}")
    println("  delivery: ${delivery.javaClass.simpleName}")
    printEnvelope(event, config)
    println(summarize(payload))
    printJson(payload)
}

private fun printEnvelope(event: WebhookEventInfo?, config: WebhookConfigInfo?) {
    println("  event #${event?.id}  at ${event?.createOn}  by ${event?.createBy}")
    println("  subscription #${config?.id} \"${config?.name}\"")
}

/**
 * The payload model the generated trigger table names: "untyped" for the legacy
 * catch-all, "unknown" for a trigger newer than this SDK.
 */
fun modelName(trigger: String): String =
    WebhookTriggers.payloadType(trigger)?.simpleName
        ?: if (trigger in WebhookTriggers.untypedTriggers) "untyped" else "unknown"

/** A typed one-line summary per payload kind, to show the models actually being used. */
private fun summarize(payload: Any?): String = when (payload) {
    is UserPayload -> "  user: ${payload.userName} <${payload.email}>"
    is GroupPayload -> "  group: ${payload.name}"
    is FilePayload -> "  file: ${payload.title}  id=${payload.id ?: "<none>"}  parent=${payload.parentId ?: "<none>"}"
    is FolderPayload -> "  folder: ${payload.title}  id=${payload.id ?: "<none>"}  parent=${payload.parentId ?: "<none>"}"
    is RoomPayload -> "  room: ${payload.title}  id=${payload.id ?: "<none>"}  parent=${payload.parentId ?: "<none>"}"
    is FormSubmitPayload -> "  form: ${payload.originalForm?.title}  submitted as ${payload.submittedForm?.title}"
    else -> "  (no payload)"
}

private fun printJson(value: Any?) {
    val json = WebhookJson.moshi.adapter(Any::class.java).indent("  ").toJson(value)
    println()
    println("  " + json.replace("\n", "\n  "))
}
