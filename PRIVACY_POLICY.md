# Privacy Policy

**Effective date:** August 1, 2026

This policy describes how the Sticky TODO Windows app ("Sticky TODO", "the app") handles data
in its current release. Sticky TODO is developed and maintained by DefineStack.

## Summary

Sticky TODO is a fully local, offline application. In this version:

- All of your data stays on your device.
- Nothing is transmitted over the network.
- No account, sign-in, or cloud sync is required or available.
- No telemetry, analytics, or crash reporting is collected.

## Data Collection

Sticky TODO does not collect, transmit, or share any personal data, usage data, or diagnostic
data. The app does not include any telemetry, analytics, or crash-reporting SDKs, and it does
not make network or internet calls of any kind.

## Data Storage

All notes and application data created in Sticky TODO are stored locally on your computer, under
your Windows user profile:

```
%LocalAppData%\DefineStack\StickyDO\
├── Data\       Your notes, stored as individual JSON files
├── Settings\   Application preferences
├── Logs\       Local diagnostic logs (written to disk only, never transmitted)
└── Backups\    Local backup copies of your notes
```

This data is never uploaded, synced, or shared with DefineStack or any third party. Uninstalling
the app does not automatically delete this folder; you can remove it manually if you want to
delete all app data from your device.

## Accounts and Sync

This version of Sticky TODO does not support user accounts, sign-in, or synchronization with any
server or cloud service. A future release may add optional background sync so notes can be
shared across your own devices; if and when that happens, this policy will be updated before
that feature is enabled, and it will describe what data is transmitted and how it is protected.

## Children's Privacy

Sticky TODO does not knowingly collect any data from anyone, including children, because it does
not collect data at all.

## Changes to This Policy

If a future version of Sticky TODO changes how data is handled (for example, by introducing
account-based sync), this policy will be updated accordingly, and the effective date above will
reflect the most recent change.

## Contact

Questions about this privacy policy can be raised by opening an issue at
[github.com/anuviswan/sticky-todo/issues](https://github.com/anuviswan/sticky-todo/issues).
