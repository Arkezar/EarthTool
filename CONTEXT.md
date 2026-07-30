# MSH Format

This context describes the domain language used for Earth-series MSH geometry and effect records.

## Language

**Base header**:
The common `0x368`-byte `MESH` record shared by static meshes and dynamic effects. It excludes the static trailing hierarchy unwind count and dynamic effect extension.
_Avoid_: Static descriptor, subtype header

**Mesh kind**:
The base-header discriminator whose values are `Static` and `Dynamic`.
_Avoid_: Mesh type, Model

**Archive framing**:
The required top-level prefix before `MESH`. Static files use `0x20D0A1FF` plus a 16-byte GUID; a top-level payload beginning directly at `MESH` is invalid.
_Avoid_: MESH header

**Trailing hierarchy unwind count**:
The pending source-hierarchy unwind stored after a static MSH base header when no later render record remains to carry it. It equals the final render record's source depth plus one.
_Avoid_: ConverterObjectCounter, StaticObjectCount, RegularMeshSubType, Unit/Building subtype

**Static light**:
A common-header spot or omni light record numbered 1 through 4 and composed of a position, light parameters, and shape-specific values. A static light is not itself a coordinate vector, and gaps in light numbers are meaningful.
_Avoid_: Light vector

**Active static light**:
A static light whose corresponding quantized attachment record is set. Activity is never inferred from the full-precision light position.
_Avoid_: Available light, nonzero-position light

**Light attachment**:
The quantized attachment record corresponding to a static light. It controls light activity but remains independently serialized from the full-precision light record.
_Avoid_: Synced light position

**Light parameters**:
The three raw floating-point values stored after a static spot or omni light position. Their individual game-facing meanings remain unconfirmed.
_Avoid_: Color, RGB, LightColor

**Spot heading**:
The exact one-byte target heading in a static spot light record, using 256 units per turn, followed by three separately preserved reserved bytes. Radians are a derived view of the exact byte.
_Avoid_: Direction dword

**Spot shape values**:
The format-derived values `HorizontalTargetDistance`, `ConeHalfAngleTangent`, `DistanceScaledCone`, `VerticalTargetSlope`, and `FinalParameter` in a static spot light record.
_Avoid_: Length, Width, U3, Tilt, Ambience

**Dynamic color**:
The confirmed RGB triplet `ColorRgb` plus the separate raw fourth value `ColorParameter` in a dynamic effect record.
_Avoid_: Color, ColorIntensity

**Light vector**:
The three raw floating-point values stored by a dynamic effect after the source `Light r,g,b,k` values have been scalar-multiplied.
_Avoid_: LightColor, LightRgb
