// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "DocSpaceWebhooks",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .library(name: "DocSpaceWebhooksSDK", targets: ["DocSpaceWebhooksSDK"]),
        .executable(name: "WebhookLogger", targets: ["WebhookLogger"])
    ],
    dependencies: [
        // Foundation has no HMAC; swift-crypto gives the CryptoKit API on
        // Linux and Windows as well as Apple platforms. The router checks
        // signatures, so the library itself depends on it.
        .package(url: "https://github.com/apple/swift-crypto.git", from: "3.0.0")
    ],
    targets: [
        // Contract models; wiped and rewritten on every regeneration. The router
        // and the signature check are hand-written in shim/ and copied in after.
        .target(
            name: "DocSpaceWebhooksSDK",
            dependencies: [
                .product(name: "Crypto", package: "swift-crypto")
            ],
            path: "generated/Sources/DocSpaceWebhooksSDK"
        ),
        .executableTarget(
            name: "WebhookLogger",
            dependencies: [
                "DocSpaceWebhooksSDK"
            ],
            path: "Sources/WebhookLogger"
        )
    ]
)
