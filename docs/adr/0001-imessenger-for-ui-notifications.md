# ADR 0001: Use CommunityToolkit.Mvvm's IMessenger for cross-view-model notifications

## Status

Accepted

## Context

Sticky note windows and the notes list need to react to events they didn't
directly cause — e.g. a note's save state changing (`Saving` / `Saved` /
`NotSaved`), or a note being created/changed elsewhere — without polling and
without every producer holding direct references to every consumer (note
windows are created and closed dynamically).

`StickyDo.Widget` already depends on `CommunityToolkit.Mvvm` for `ObservableObject`
and `RelayCommand`, so its `IMessenger` (`WeakReferenceMessenger`) is available
at no extra dependency cost, and already provides:

- Weak-reference subscriptions, so note view models don't leak when their
  window closes without an explicit unregister.
- Strongly-typed messages instead of stringly-typed events.

The alternative considered was a dedicated Event Aggregator (e.g. Prism's
`IEventAggregator`) or a hand-rolled pub/sub type. Either would duplicate
what `IMessenger` already provides, and would add a second messaging
convention alongside the one `NotesListViewModel` / `StickyNoteCreationService`
already used for `StickyNoteChangedMessage`.

The source of the save-state event, `PersistenceService`, lives in
`StickyDo.Domain` — a project with **no package references**, intended to be
shared with the future MAUI Android app. `IMessenger` and any Widget-specific
message type (e.g. `NoteSaveStateChangedMessage`) are types Domain must not
reference:

- `StickyDo.Domain.csproj` has zero `PackageReference`s; adding
  `CommunityToolkit.Mvvm` there would tie a shared, cross-platform layer to a
  WPF-app-specific messaging package.
- `NoteSaveStateChangedMessage` is defined in `StickyDo.Widget.Messages`.
  `StickyDo.Widget` already references `StickyDo.Domain`, so `StickyDo.Domain`
  referencing it back would be a circular project reference — not just
  undesirable, but impossible to build.

## Decision

- Use `CommunityToolkit.Mvvm`'s `IMessenger` for all cross-view-model /
  cross-window notifications in `StickyDo.Widget`.
- Keep `StickyDo.Domain` services (e.g. `PersistenceService`) messenger-free.
  They expose plain `.NET` events (`EventHandler<TEventArgs>`) using
  Domain-owned event-args types.
- Bridge Domain events onto `IMessenger` with a decorator living in
  `StickyDo.Widget`, e.g. `MessengerBridgedPersistenceService` wraps
  `PersistenceService` and republishes `NoteSaveStateChanged` as a
  `NoteSaveStateChangedMessage`. DI resolves `IPersistenceService` to the
  decorator (see `ServiceConfiguration.cs`), so consumers get
  messenger-backed notifications while the concrete `PersistenceService`
  stays Domain-pure.

## Consequences

- `StickyDo.Domain` remains free of UI/messaging package dependencies and
  stays reusable by the future mobile app regardless of what notification
  mechanism it ends up using.
- Every Domain event that needs to reach the Widget UI requires a matching
  bridge/decorator in `StickyDo.Widget`. This is a small amount of
  boilerplate per event, traded for keeping the dependency direction
  correct (Widget → Domain, never the reverse).
- `StickyDo.Domain.Tests` can test `PersistenceService` against a plain
  `EventHandler` with no messenger mocking required.
- Only one pub/sub mechanism (`IMessenger`) exists in the Widget app,
  consistent with its prior use for `StickyNoteChangedMessage`.
