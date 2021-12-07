## 0.1.8 (06-12-2021):

Added `net6.0` target.

## 0.1.7 (24.08.2021):

Disable token renew by default.

## 0.1.6 (16.04.2021):

Added traces.

## 0.1.5 (24.02.2021):

Added Vault prefix to structured log event property names to avoid collisions.

## 0.1.4 (16.10.2020):

Fixed token renew.

## 0.1.3 (10.05.2020):

- Fixed https://github.com/vostok/configuration.sources.vault/issues/6
- SecretUpdater: protected against endless instant token renewal attempts.
- Added adaptive throttling to ClusterClient to prevent Vault overload during outages.

## 0.1.2 (04.04.2020):

Fixed https://github.com/vostok/configuration.sources.vault/issues/5

## 0.1.1 (16.02.2020):

Technical release.

## 0.1.0 (16.02.2020):

Initial release.