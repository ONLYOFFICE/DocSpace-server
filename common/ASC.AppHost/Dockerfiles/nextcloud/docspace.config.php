<?php
// Copied into the live config by the image on install (see ConnectionStringManager.AddNextcloud).
// The integration tests connect with wrong passwords on purpose; without this Nextcloud would
// start delaying every answer from the portal's address, past the third-party request timeout.
$CONFIG = [
  'auth.bruteforce.protection.enabled' => false,
  'ratelimit.protection.enabled' => false,
];
