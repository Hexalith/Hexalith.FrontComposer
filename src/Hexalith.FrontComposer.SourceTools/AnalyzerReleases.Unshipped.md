### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
HFC1001 | HexalithFrontComposer | Warning | No [Command] or [Projection] types found in compilation
HFC1002 | HexalithFrontComposer | Warning | Unsupported field type in [Projection]
HFC1003 | HexalithFrontComposer | Warning | Projection type should be partial
HFC1004 | HexalithFrontComposer | Warning | Projection on unsupported type kind
HFC1005 | HexalithFrontComposer | Warning | Invalid attribute argument
HFC1006 | HexalithFrontComposer | Warning | Command missing MessageId property
HFC1007 | HexalithFrontComposer | Warning | Command has too many non-derivable properties
HFC1008 | HexalithFrontComposer | Warning | [Flags] enum in a single-value UI context
HFC1009 | HexalithFrontComposer | Error | Command type has no parameterless constructor
HFC1010 | HexalithFrontComposer | Info | Reserved — full restart required (not yet implemented)
HFC1011 | HexalithFrontComposer | Error | Command exceeds 200-property hard limit
HFC1012 | HexalithFrontComposer | Error | [DefaultValue] value type does not match property type
HFC1014 | HexalithFrontComposer | Error | Nested [Command] type is unsupported
HFC1015 | HexalithFrontComposer | Warning | RenderMode incompatible with command density
HFC1016 | HexalithFrontComposer | Error | Command non-derivable property is read-only or init-only
HFC1017 | HexalithFrontComposer | Error | Command type is generic (unsupported; specialize or remove type parameters)
HFC1020 | HexalithFrontComposer | Info | Command appears destructive by name but is missing [Destructive] attribute
HFC1021 | HexalithFrontComposer | Error | Destructive command must have at least one non-derivable property
HFC1022 | HexalithFrontComposer | Warning | ProjectionRole.WhenState references unknown enum member
HFC1023 | HexalithFrontComposer | Info | Dashboard projection rendering is deferred (renders Default body in v1)
HFC1024 | HexalithFrontComposer | Warning | Unknown ProjectionRole value (falls back to Default rendering)
HFC1025 | HexalithFrontComposer | Info | Projection enum has partial [ProjectionBadge] coverage (unannotated members render as text)
HFC1026 | HexalithFrontComposer | Warning | Reserved — color-only badge detected (no call site in Story 4-2; held for Story 10-2 specimen checker)
HFC1027 | HexalithFrontComposer | Info | Projection has collection column which does not support automatic filtering (Story 4-3)
HFC1028 | HexalithFrontComposer | Info | [ColumnPriority] collision on projection (Story 4-4 — deterministic tiebreaker is declaration order)
HFC1029 | HexalithFrontComposer | Info | Projection exceeds 15 columns — FcColumnPrioritizer activates (Story 4-4)
HFC1030 | HexalithFrontComposer | Info | [ProjectionFieldGroup] name collides with reserved catch-all label "Additional details" (Story 4-5)
HFC1031 | HexalithFrontComposer | Info | [ProjectionFieldGroup] is ignored for non-detail role (Timeline) (Story 4-5)
HFC1032 | HexalithFrontComposer | Warning | Invalid Level 1 format annotation (Story 6-1 — incompatible type or mutually exclusive format annotations; generated column falls back)
HFC1033 | HexalithFrontComposer | Error | [ProjectionTemplate] references an invalid projection type (Story 6-2 — missing/unresolved/non-Projection/generic/abstract/struct)
HFC1034 | HexalithFrontComposer | Warning | [ProjectionTemplate] component is not a valid typed template component (Story 6-2 — invalid component excluded from manifest)
HFC1035 | HexalithFrontComposer | Warning | [ProjectionTemplate] contract version is incompatible (Story 6-2 — major-version mismatch suppresses selection)
HFC1036 | HexalithFrontComposer | Warning | [ProjectionTemplate] contract version is out of date (Story 6-2 — minor drift only; selection proceeds)
HFC1037 | HexalithFrontComposer | Error | Duplicate [ProjectionTemplate] for the same projection and role (Story 6-2 — duplicates excluded from manifest)
HFC1038 | HexalithFrontComposer | Error | Invalid Level 3 slot selector (Story 6-3 — selector must be direct projection property access)
HFC1039 | HexalithFrontComposer | Warning | Invalid Level 3 slot component (Story 6-3 — component must expose typed FieldSlotContext Context parameter)
HFC1040 | HexalithFrontComposer | Warning | Duplicate Level 3 slot override (Story 6-3 — duplicate descriptor ignored and default renderer wins)
HFC1041 | HexalithFrontComposer | Warning | Level 3 slot contract version is incompatible (Story 6-3 — descriptor ignored)
HFC1042 | HexalithFrontComposer | Error | Invalid Level 4 view override projection type (Story 6-4 — reserved for SourceTools validation)
HFC1043 | HexalithFrontComposer | Warning | Invalid Level 4 view override component (Story 6-4 — component must expose typed ProjectionViewContext Context parameter)
HFC1044 | HexalithFrontComposer | Error | Duplicate Level 4 view override with different component types (Story 6-4 — registry construction fails hard; idempotent re-registration is allowed)
HFC1045 | HexalithFrontComposer | Warning | Level 4 view override contract version is incompatible (Story 6-4 — descriptor ignored)
HFC1046 | HexalithFrontComposer | Warning | Level 4 view override accessibility contract warning (Story 6-4 — partial static/sample enforcement)
