// Resolves docspace.dev.localhost to 127.0.0.1 for Node.js processes.
// Node does not honor RFC 6761 for .localhost TLDs like browsers do.
// Required so SSR services (login/doceditor/management) can fetch
// https://docspace.dev.localhost/... when the outer page is served via HTTPS.
"use strict";

console.log("[docspace-dns-patch] loaded in pid", process.pid);

const dns = require("dns");

const HOSTNAME = "docspace.dev.localhost";
const LOOPBACK_V4 = "127.0.0.1";
const LOOPBACK_V6 = "::1";

const originalLookup = dns.lookup;
dns.lookup = function patchedLookup(hostname, options, callback) {
  if (hostname === HOSTNAME) {
    const family = typeof options === "object" && options !== null ? options.family : options;
    const cb = typeof options === "function" ? options : callback;
    const address = family === 6 ? LOOPBACK_V6 : LOOPBACK_V4;
    if (typeof options === "object" && options !== null && options.all) {
      return process.nextTick(cb, null, [{ address, family: family === 6 ? 6 : 4 }]);
    }
    return process.nextTick(cb, null, address, family === 6 ? 6 : 4);
  }
  return originalLookup.call(this, hostname, options, callback);
};

if (dns.promises && dns.promises.lookup) {
  const originalLookupPromise = dns.promises.lookup;
  dns.promises.lookup = function patchedLookupPromise(hostname, options) {
    if (hostname === HOSTNAME) {
      const family = typeof options === "object" && options !== null ? options.family : options;
      const address = family === 6 ? LOOPBACK_V6 : LOOPBACK_V4;
      if (typeof options === "object" && options !== null && options.all) {
        return Promise.resolve([{ address, family: family === 6 ? 6 : 4 }]);
      }
      return Promise.resolve({ address, family: family === 6 ? 6 : 4 });
    }
    return originalLookupPromise.call(this, hostname, options);
  };
}
