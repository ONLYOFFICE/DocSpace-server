// Logs a DocSpace webhook delivery to the console.
//
//     swift run WebhookLogger                          # replay the shared fixture
//     swift run WebhookLogger <body> <secret> <sig>
//
// Swift has no src/ runtime yet, so signature checking lives here.
//
// Regeneration (the generate-webhook-sdk skill) builds this and runs it over
// every fixture in ../samples after each generation.

import Foundation
import Crypto
import DocSpaceWebhooksSDK

let signatureHeader = "x-docspace-signature-256"

/// `sha256=` + UPPERCASE hex, mirroring `WebhookSender.GetSecretHash`.
func computeSignature(body: Data, secret: String) -> String {
    let key = SymmetricKey(data: Data(secret.utf8))
    let mac = HMAC<SHA256>.authenticationCode(for: body, using: key)
    return "sha256=" + mac.map { String(format: "%02X", $0) }.joined()
}

private func hexToData(_ hex: String) -> Data? {
    guard hex.count % 2 == 0 else { return nil }
    var out = Data(capacity: hex.count / 2)
    var index = hex.startIndex
    while index < hex.endIndex {
        let next = hex.index(index, offsetBy: 2)
        guard let byte = UInt8(hex[index..<next], radix: 16) else { return nil }
        out.append(byte)
        index = next
    }
    return out
}

/// Checks the header against the RAW body.
///
/// Comparing the decoded bytes rather than the strings gets both awkward parts
/// for free: `isValidAuthenticationCode` is constant-time, and hex parsing is
/// case-insensitive -- which matters because DocSpace emits uppercase hex where
/// GitHub emits lowercase.
func verifySignature(body: Data, secret: String, signature: String?) -> Bool {
    guard let signature, !signature.isEmpty else { return false }

    let trimmed = signature.trimmingCharacters(in: .whitespacesAndNewlines)
    guard trimmed.lowercased().hasPrefix("sha256=") else { return false }

    let hex = String(trimmed.dropFirst("sha256=".count))
    guard let expected = hexToData(hex) else { return false }

    return HMAC<SHA256>.isValidAuthenticationCode(
        expected,
        authenticating: body,
        using: SymmetricKey(data: Data(secret.utf8))
    )
}

/// Minimal shape of the envelope; `payload` stays raw so it can be decoded
/// against whichever model the trigger calls for.
struct Envelope: Decodable {
    let event: WebhookEventInfo?
    let webhook: WebhookConfigInfo?
}

func describe(_ id: EntryId?) -> String {
    switch id {
    case .some(.typeInt(let value)): return String(value)
    case .some(.typeString(let value)): return value
    case .none: return "<none>"
    }
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

// Bytes, not a re-encoded object: the signature covers what arrived.
let body = try Data(contentsOf: bodyURL)

guard verifySignature(body: body, secret: secret, signature: signature) else {
    FileHandle.standardError.write(Data("REJECTED: signature does not match\n".utf8))
    exit(1)
}

// ------------------------------------------------------------------ parsing

let decoder = JSONDecoder()
// The contract emits UTC ISO-8601; Date properties will not decode without this.
decoder.dateDecodingStrategy = .iso8601

guard let root = try JSONSerialization.jsonObject(with: body) as? [String: Any],
      let eventObject = root["event"] as? [String: Any],
      let trigger = eventObject["trigger"] as? String
else {
    FileHandle.standardError.write(Data("envelope has no event.trigger\n".utf8))
    exit(1)
}

let envelope = try decoder.decode(Envelope.self, from: body)

// Re-encode the payload subtree so it can go through the typed decoder.
let payloadData = try JSONSerialization.data(
    withJSONObject: root["payload"] ?? NSNull(), options: [])

// Spelled out rather than chained through `.map(String.init)`: that form is a
// reliable source of "ambiguous use of 'init'" errors, since String has many
// initialisers and the optional chain gives the compiler nothing to pin on.
let eventId: String = envelope.event?.id.flatMap { String($0) } ?? "?"
let eventAt: String = envelope.event?.createOn.flatMap { ISO8601DateFormatter().string(from: $0) } ?? "?"
let eventBy: String = envelope.event?.createBy?.uuidString ?? "?"
let configId: String = envelope.webhook?.id.flatMap { String($0) } ?? "?"
let configName: String = envelope.webhook?.name ?? ""

// The payload model the generated trigger table names: "untyped" for the legacy
// catch-all, "unknown" for a trigger newer than this SDK.
let modelName = WebhookTriggers.payloadModels[trigger]
    ?? (WebhookTriggers.untypedTriggers.contains(trigger) ? "untyped" : "unknown")

// The catch-all, or a trigger added on the server after this SDK was generated,
// is acknowledged with its raw payload and never parsed with some other model.
let payload: Any?
do {
    payload = try WebhookTriggers.decodePayload(trigger: trigger, from: payloadData, decoder: decoder)
} catch {
    FileHandle.standardError.write(Data("unparseable \(trigger) payload: \(error)\n".utf8))
    exit(1)
}

print(trigger)
print("  signature ok")
print("  model: \(modelName)")
print("  event #\(eventId)  at \(eventAt)  by \(eventBy)")
print("  subscription #\(configId) \"\(configName)\"")

switch payload {
case let user as UserPayload:
    print("  user: \(user.userName ?? "") <\(user.email ?? "")>")
case let group as GroupPayload:
    print("  group: \(group.name ?? "")")
case let form as FormSubmitPayload:
    // The one wrapper payload: the original form and the submitted copy.
    print("  form: \(form.originalForm?.title ?? "")  submitted as \(form.submittedForm?.title ?? "")")
case let folder as FolderPayload:
    print("  folder: \(folder.title ?? "")"
          + "  id=\(describe(folder.id))  parent=\(describe(folder.parentId))")
case let room as RoomPayload:
    print("  room: \(room.title ?? "")"
          + "  id=\(describe(room.id))  parent=\(describe(room.parentId))")
case let file as FilePayload:
    print("  file: \(file.title ?? "")"
          + "  id=\(describe(file.id))  parent=\(describe(file.parentId))")
default:
    print("  no payload model for this trigger; raw payload below")
}

print()
let pretty = try JSONSerialization.data(
    withJSONObject: root["payload"] ?? NSNull(),
    options: [.prettyPrinted, .sortedKeys])
print("  " + (String(data: pretty, encoding: .utf8) ?? "")
    .replacingOccurrences(of: "\n", with: "\n  "))
