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
        // Linux and Windows as well as Apple platforms.
        .package(url: "https://github.com/apple/swift-crypto.git", from: "3.0.0")
    ],
    targets: [
        // Contract models; wiped and rewritten by ../generate.sh
        .target(
            name: "DocSpaceWebhooksSDK",
            path: "generated/Sources/DocSpaceWebhooksSDK"
        ),
        .executableTarget(
            name: "WebhookLogger",
            dependencies: [
                "DocSpaceWebhooksSDK",
                .product(name: "Crypto", package: "swift-crypto")
            ],
            path: "Sources/WebhookLogger"
        )
    ]
)
