// Logs a DocSpace webhook delivery to the console.
//
//     swift run WebhookLogger                          # replay the shared fixture
//     swift run WebhookLogger <body> <secret> <sig> [<x-docspace-event-id>]
//
// Regeneration (the generate-webhook-sdk skill) builds this and runs it over
// every fixture in ../samples after each generation.

import Foundation
import DocSpaceWebhooksSDK

func describe(_ id: EntryId?) -> String {
    switch id {
    case .some(.typeInt(let value)): return String(value)
    case .some(.typeString(let value)): return value
    case .none: return "<none>"
    }
}

// The payload model the generated trigger table names: "untyped" for the legacy
// catch-all, "unknown" for a trigger newer than this SDK.
func modelName(_ trigger: String) -> String {
    WebhookTriggers.payloadModels[trigger]
        ?? (WebhookTriggers.untypedTriggers.contains(trigger) ? "untyped" : "unknown")
}

func printHeader(_ trigger: String, _ event: WebhookEventInfo?, _ webhook: WebhookConfigInfo?, delivery: String) {
    // Spelled out rather than chained through `.map(String.init)`: that form is a
    // reliable source of "ambiguous use of 'init'" errors, since String has many
    // initialisers and the optional chain gives the compiler nothing to pin on.
    let eventId: String = event?.id.flatMap { String($0) } ?? "?"
    let eventAt: String = event?.createOn.flatMap { ISO8601DateFormatter().string(from: $0) } ?? "?"
    let eventBy: String = event?.createBy?.uuidString ?? "?"
    let configId: String = webhook?.id.flatMap { String($0) } ?? "?"
    let known = modelName(trigger) == "unknown" ? "  (unknown to this build)" : ""

    print("\(trigger)\(known)")
    print("  signature ok")
    print("  model: \(modelName(trigger))")
    print("  delivery: \(delivery)")
    print("  event #\(eventId)  at \(eventAt)  by \(eventBy)")
    print("  subscription #\(configId) \"\(webhook?.name ?? "")\"")
}

// A typed line per payload kind, to show the models actually being used.
func summarize(_ payload: Any?) -> String? {
    switch payload {
    case let user as UserPayload:
        return "user: \(user.userName ?? "") <\(user.email ?? "")>"
    case let group as GroupPayload:
        return "group: \(group.name ?? "")"
    case let form as FormSubmitPayload:
        // The one wrapper payload: the original form and the submitted copy.
        return "form: \(form.originalForm?.title ?? "")  submitted as \(form.submittedForm?.title ?? "")"
    case let folder as FolderPayload:
        return "folder: \(folder.title ?? "")  id=\(describe(folder.id))  parent=\(describe(folder.parentId))"
    case let room as RoomPayload:
        return "room: \(room.title ?? "")  id=\(describe(room.id))  parent=\(describe(room.parentId))"
    case let file as FilePayload:
        return "file: \(file.title ?? "")  id=\(describe(file.id))  parent=\(describe(file.parentId))"
    default:
        return nil
    }
}

func printPayload<P: Encodable>(_ payload: P?) {
    if let summary = summarize(payload) {
        print("  \(summary)")
    } else {
        print("  no payload model for this trigger; raw payload below")
    }

    let encoder = JSONEncoder()
    encoder.outputFormatting = [.prettyPrinted, .sortedKeys, .withoutEscapingSlashes]
    encoder.dateEncodingStrategy = .iso8601
    let pretty = payload.flatMap { try? encoder.encode($0) }.flatMap { String(data: $0, encoding: .utf8) } ?? "null"

    print()
    print("  " + pretty.replacingOccurrences(of: "\n", with: "\n  "))
}

// ---------------------------------------------------------------- arguments

let args = Array(CommandLine.arguments.dropFirst())
let fixtures = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
    .appendingPathComponent("../samples")
    .standardizedFileURL

let bodyURL = args.count > 0
    ? URL(fileURLWithPath: args[0])
    : fixtures.appendingPathComponent("file.created.json")

let secret = args.count > 1
    ? args[1]
    : (try! String(contentsOf: fixtures.appendingPathComponent("SECRET"), encoding: .utf8))
        .trimmingCharacters(in: .whitespacesAndNewlines)

let signature = args.count > 2
    ? args[2]
    : (try! String(contentsOf: fixtures.appendingPathComponent("file.created.sig"),
                   encoding: .utf8))
        .trimmingCharacters(in: .whitespacesAndNewlines)

let eventIdHeader = args.count > 3 ? args[3] : nil

// ------------------------------------------------------------------- router

// The router this sample hands the delivery to. It checks the signature, drops
// an event it has processed before (DocSpace delivers at least once, and a retry
// carries the same event.id), reads the body into the delivery model of its
// trigger and awaits the handlers. The store lives in memory for the sake of the
// sample; a real receiver persists the ids for as long as an event can still
// arrive (see WebhookEventStore).
let router = try WebhookRouter(secret: secret, store: InMemoryWebhookEventStore())

// One handler for every trigger with a typed body, from the generated table. A
// receiver normally registers only the triggers it cares about, through the
// generated on<Trigger> methods.
for (trigger, model) in WebhookTriggers.deliveryModels.sorted(by: { $0.key < $1.key }) {
    switch model {
    case "UserDelivery":
        router.on(trigger, UserDelivery.self) { d in
            printHeader(trigger, d.event, d.webhook, delivery: model); printPayload(d.payload)
        }
    case "GroupDelivery":
        router.on(trigger, GroupDelivery.self) { d in
            printHeader(trigger, d.event, d.webhook, delivery: model); printPayload(d.payload)
        }
    case "FileDelivery":
        router.on(trigger, FileDelivery.self) { d in
            printHeader(trigger, d.event, d.webhook, delivery: model); printPayload(d.payload)
        }
    case "FolderDelivery":
        router.on(trigger, FolderDelivery.self) { d in
            printHeader(trigger, d.event, d.webhook, delivery: model); printPayload(d.payload)
        }
    case "RoomDelivery":
        router.on(trigger, RoomDelivery.self) { d in
            printHeader(trigger, d.event, d.webhook, delivery: model); printPayload(d.payload)
        }
    case "FormSubmitDelivery":
        router.on(trigger, FormSubmitDelivery.self) { d in
            printHeader(trigger, d.event, d.webhook, delivery: model); printPayload(d.payload)
        }
    default:
        FileHandle.standardError.write(Data("no sample handler for \(model) (\(trigger))\n".utf8))
    }
}

// A handler for one trigger, through its generated method: the argument is a FileDelivery.
router.onFileCreated { d in
    print("  onFileCreated: \"\(d.payload?.title ?? "")\" in folder \(describe(d.payload?.parentId))")
}

// Whatever has no handler: the catch-all '*', and triggers newer than this SDK.
router.onOther { hook in
    printHeader(hook.trigger, hook.event, hook.webhook, delivery: "none")
    printPayload(hook.payload)
}

// ------------------------------------------------------------------ handling

// Bytes, not a re-encoded object: the signature covers what arrived.
let body = try Data(contentsOf: bodyURL)

// In an HTTP handler, answer with result.statusCode once this returns: a 2xx
// acknowledges the delivery, anything else makes DocSpace send it again.
let result = await router.handle(body: body, signature: signature, eventIdHeader: eventIdHeader)

switch result.outcome {
case .rejected:
    FileHandle.standardError.write(Data("REJECTED: signature does not match\n".utf8))
case .invalid:
    FileHandle.standardError.write(Data("unparseable body: \(result.error ?? "")\n".utf8))
case .failed:
    FileHandle.standardError.write(Data("a handler failed: \(result.error ?? "")\n".utf8))
case .duplicate:
    print("duplicate of event #\(result.eventId.map { String($0) } ?? "?"), already processed; skipped")
case .handled, .unhandled:
    break
}

exit((200..<300).contains(result.statusCode) ? 0 : 1)
