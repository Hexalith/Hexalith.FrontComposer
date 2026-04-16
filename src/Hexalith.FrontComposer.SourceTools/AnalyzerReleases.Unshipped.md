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
HFC1008 | HexalithFrontComposer | Warning | Command property is a [Flags] enum (renders as placeholder)
HFC1009 | HexalithFrontComposer | Error   | Command type has no parameterless constructor
HFC1010 | HexalithFrontComposer | Info | Reserved — full restart required (not yet implemented)
HFC1011 | HexalithFrontComposer | Error   | Command exceeds 200-property hard limit
HFC1012 | HexalithFrontComposer | Error   | [DefaultValue] value type does not match property type
HFC1014 | HexalithFrontComposer | Error   | Nested [Command] type is unsupported
HFC1015 | HexalithFrontComposer | Warning | RenderMode incompatible with command density
HFC1016 | HexalithFrontComposer | Error   | Command non-derivable property is read-only or init-only
