# MSH Format Documentation

## Overview

MSH is the binary 3D model format used by Earth 2150. It supports two primary types of meshes:
- **Model** (Type 0) - Static and animated 3D models (units, buildings)
- **Dynamic** (Type 1) - Special effects, particles, sprites, and dynamic visual elements

## File Structure

```
[FileHeader]
[MESH Identifier]
[MeshDescriptor]
[Geometry Data or Dynamic Part Data]
```

## File Header

Every MSH file begins with:
1. **EarthInfo Header** - Common header structure (see EarthInfo documentation)
2. **MESH Identifier** - 8 bytes: `4D 45 53 48 01 00 00 00` ("MESH" + version)

## MeshDescriptor Structure

The descriptor section contains metadata about the mesh:

| Offset | Type | Description |
|--------|------|-------------|
| 0x00 | int32 | MeshType (0=Model, 1=Dynamic) |
| 0x04 | ModelTemplate | 4x4 bit matrix template |
| 0x08 | MeshFrames | Animation frame counts (4 bytes) |
| 0x0C | int32 | Empty/unused field |
| 0x10 | Vector[4] | Mount points (4 x 12 bytes) |
| 0x58 | SpotLight[4] | Spot lights (4 x 36 bytes each) |
| 0x148 | OmniLight[4] | Omni-directional lights (4 x 16 bytes each) |
| 0x188 | TemplateDetails | Grid template details |
| 0x... | ModelSlots | Various slot positions |
| 0x... | MeshBoundaries | Bounding box (8 bytes) |
| 0x... | int32 | MeshSubType |

### MeshType Enum

```
0 = Model      // Static/animated 3D models
1 = Dynamic    // Effects, particles, sprites
```

### MeshSubType (for Model type)

```
0 = Unknown
1 = Unit        // Mobile units
2 = Building    // Static structures
```

### DynamicMeshSubType (for Dynamic type)

```
0  = Unknown
1  = Explosion
2  = Track
3  = ScaleableObject
4  = MappedExplosion
5  = FlatExplosion
6  = Laser
7  = LaserWall
8  = Shockwave
9  = Line
10 = Sphere
11 = ElectricalCannon
12 = Lighting
13 = Smoke
14 = Keelwater
```

## ModelTemplate Structure

A 4x4 boolean matrix packed into 2 bytes (16 bits):
- Defines which grid cells the model occupies
- Read as bit array, mapped row-by-row
- Followed by 2-byte flag field

## MeshFrames Structure

```
byte BuildingFrames     // Construction animation frames
byte ActionFrames       // Action/attack animation frames
byte MovementFrames     // Movement animation frames
byte LoopedFrames       // Looped animation frames
```

## Lighting Structures

### SpotLight (36 bytes)

```
Vector3  Position       (12 bytes)
Color    Color          (12 bytes) - RGB as floats
float    Length
int32    Direction
float    Width
float    U3             (unknown)
float    Tilt
float    Ambience
```

### OmniLight (16 bytes)

```
Vector3  Position       (12 bytes)
Color    Color          (12 bytes) - RGB as floats
float    Radius
```

## ModelSlots Structure

Defines attachment/hardpoint positions for various game elements:

```
Slot[] Turrets              [4 slots, 4 bytes each]
Slot[] BarrelMuzzels        [4 slots, 4 bytes each]
Slot[] TurretMuzzels        [4 slots, 4 bytes each]
Slot[] Headlights           [4 slots, 4 bytes each]
Slot[] Omnilights           [4 slots, 4 bytes each]
Slot[] UnloadPoints         [4 slots, 4 bytes each]
Slot[] HitSpots             [4 slots, 4 bytes each]
Slot[] SmokeSpots           [4 slots, 4 bytes each]
Slot[] Unknown              [4 slots, 4 bytes each]
Slot[] Chimneys             [2 slots, 4 bytes each]
Slot[] SmokeTraces          [2 slots, 4 bytes each]
Slot[] Exhausts             [2 slots, 4 bytes each]
Slot[] KeelTraces           [2 slots, 4 bytes each]
Slot[] InterfacePivot       [1 slot,  4 bytes]
Slot[] CenterPivot          [1 slot,  4 bytes]
Slot[] ProductionSpotStart  [1 slot,  4 bytes]
Slot[] ProductionSpotEnd    [1 slot,  4 bytes]
Slot[] LandingSpot          [1 slot,  4 bytes]
```

### Slot Structure (4 bytes)

```
int16  X         (scaled by /255.0)
int16  Y         (scaled by /255.0, negated)
int16  Z         (scaled by /255.0)
byte   Direction (scaled by /255.0 * PI * 2)
byte   Flag
```

## TemplateDetails Structure

Grid-based template information for building placement:

```
short[4,4] SectionHeights       // Height values per grid cell
byte[4,4]  SectionFlags         // Flags per grid cell
ModelTemplate[4] SectionRotations        // 4 rotation templates
byte[4,4][4] SectionFlagRotations        // Packed rotation flags
```

## MeshBoundaries Structure (8 bytes)

```
int16  MaxY
int16  MinY
int16  MaxX
int16  MinX
```

## Model Geometry Data (MeshType = Model)

For static/animated models, geometry data consists of one or more `ModelPart` structures.

### ModelPart Structure

```
[Vertices]
byte            BackTrackDepth      // Hierarchy depth
byte            PartType            // Part type flags
int16           Empty               // Unused
[TextureInfo]
[Faces]
[Animations]
int32           AnimationType
[Offset Vector]
byte            RiseAngle           // Angle * 255/360
byte            UnknownFlag
byte            UnknownByte1
byte            UnknownByte2
byte            UnknownByte3
```

### PartType Enum (Flags)

```
0x00 = Base             // Root part
0x01 = ViewerFaced      // Billboard/always faces camera
0x02 = Barrel           // Gun barrel
0x04 = Rotor            // Rotating part (helicopter rotor, etc.)
0x08 = Subpart          // Child part
0x10 = Emitter1         // Particle emitter slot 1
0x20 = Emitter2         // Particle emitter slot 2
0x40 = Emitter3         // Particle emitter slot 3
0x80 = Emitter4         // Particle emitter slot 4
```

### AnimationType Enum

```
0 = Looped      // Continuous loop
1 = TwoWay      // Plays forward then backward
2 = Single      // Plays once
3 = Lift        // Elevator/lift animation
```

### Vertices Section

```
int32  VertexCount
int32  BlockCount       // Ceiling(VertexCount / 4)
byte[160 * BlockCount]  // Vertex blocks
```

Each 160-byte block contains 4 vertices in structure-of-arrays format:

```
Offset | Size | Content
-------|------|--------
0x00   | 16   | X positions [4 floats]
0x10   | 16   | Y positions [4 floats, negated on read]
0x20   | 16   | Z positions [4 floats]
0x30   | 16   | Normal X [4 floats]
0x40   | 16   | Normal Y [4 floats, negated on read]
0x50   | 16   | Normal Z [4 floats]
0x60   | 16   | Texture U [4 floats]
0x70   | 16   | Texture V [4 floats, inverted: 1-V on read]
0x80   | 16   | Unknown/padding [4 ints]
0x90   | 8    | Normal vector indices [4 int16]
0x98   | 8    | Position vector indices [4 int16]
```

### Vertex Structure

```
Vector3             Position
Vector3             Normal
TextureCoordinate   UV
int16               NormalVectorIdx
int16               PositionVectorIdx
```

### Face Structure (8 bytes)

```
int16  V1       // Vertex index 1
int16  V2       // Vertex index 2
int16  V3       // Vertex index 3
int16  UNKNOWN  // Unknown/unused
```

### Faces Section

```
int32     FaceCount
Face[FaceCount]
```

### Animations Structure

```
int32                   ScaleFrameCount
Vector3[ScaleFrameCount]  ScaleFrames

int32                     TranslationFrameCount
Vector3[TranslationFrameCount] TranslationFrames

int32                     RotationFrameCount
Matrix4x4[RotationFrameCount] RotationFrames
```

### RotationFrame (64 bytes)

```
float[16]  Matrix4x4    // 4x4 transformation matrix, row-major
```

### TextureInfo Structure

```
int32      FileNameLength
char[FileNameLength]    FileName
```

### Vector3 Structure (12 bytes)

```
float  X
float  Y    (negated on read/write)
float  Z
```

## Dynamic Part Data (MeshType = Dynamic)

For special effects and dynamic elements:

```
int32       LightType
int32       SpriteStartIndex
int32       SpriteAnimationLength
int32       SpriteSheetVertical
int32       SpriteSheetHorizontal
int32       Framerate
float       TextureSplitRatioVertical
float       TextureSplitRatioHorizontal
Size        Size1               (16 bytes)
Size        Size2               (16 bytes)
float       SizeZ
float       Radius
int32       Unknown
int32       Additive            (boolean)
Color       LightColor          (12 bytes as float RGB)
Color       Color               (12 bytes as float RGB)
float       ColorIntensity
int32       AlphaInt
float       AlphaB
float       AlphaA
Vector2     Scale               (8 bytes)
Vector3     Position1           (12 bytes)
Vector3     Position2           (12 bytes)
TextureInfo Model
TextureInfo Texture
int32       SubMeshCount
Mesh[SubMeshCount]  SubMeshes   // Recursive mesh structures
```

### LightType Enum

```
0 = Const       // Constant intensity
1 = Pyramid     // Pyramid-shaped intensity
2 = Trapezium   // Trapezoid-shaped intensity
3 = Random      // Random intensity
```

### Size Structure (16 bytes)

```
float  X1
float  X2
float  Y1
float  Y2
```

### Color Structure (as floats)

```
float  R    (0.0 - 1.0, multiply by 255 for RGB)
float  G    (0.0 - 1.0, multiply by 255 for RGB)
float  B    (0.0 - 1.0, multiply by 255 for RGB)
```

## Coordinate System

The MSH format uses a right-handed coordinate system with Y-up:
- X: Right
- Y: Up (values negated when reading/writing)
- Z: Forward

## Hierarchy System

Model parts form a tree hierarchy using the `BackTrackDepth` field:
- `BackTrackDepth = 0`: Root part
- `BackTrackDepth > 0`: Child part, parent is `BackTrackDepth` steps back in the part list

The hierarchy is used for:
- Skeletal animation
- Attachment points (turrets, barrels)
- Part inheritance (transformations cascade)

## Implementation Notes

1. **Encoding**: The reader/writer uses a configurable text encoding (typically Windows-1252 for Earth 2150)

2. **Vertex Padding**: Vertex blocks always contain 4 vertices. If the last block has fewer than 4 vertices, pad with zero-initialized vertices.

3. **Y-Axis Negation**: All Y-axis values are negated when reading/writing to convert between Earth 2150's coordinate system and standard conventions.

4. **Texture V Coordinate**: V coordinates are inverted (1-V) when reading from the file.

5. **File Reading**: The reader processes parts sequentially until EOF. Each part begins immediately after the previous one with no padding.

6. **Dynamic SubMeshes**: Dynamic parts can contain nested mesh structures recursively. Each submesh has its own descriptor and dynamic data.

## Example File Structure

### Simple Unit Model

```
[EarthInfo Header]
[MESH Identifier: 4D 45 53 48 01 00 00 00]
[MeshDescriptor]
  - MeshType: 0 (Model)
  - MeshSubType: 1 (Unit)
  - Template, Frames, Slots, Boundaries
[ModelPart 1]  // Body (BackTrackDepth=0)
  - Vertices: 120 vertices (30 blocks)
  - Texture: "unit_body.tex"
  - Faces: 240 faces
  - Animations: Rotation frames for movement
[ModelPart 2]  // Turret (BackTrackDepth=1, parent=Part 1)
  - Vertices: 60 vertices (15 blocks)
  - Texture: "unit_turret.tex"
  - Faces: 120 faces
  - Animations: Rotation frames for turret rotation
[ModelPart 3]  // Barrel (BackTrackDepth=1, parent=Part 2)
  - Vertices: 20 vertices (5 blocks)
  - Texture: "unit_barrel.tex"
  - Faces: 40 faces
  - Animations: Translation frames for recoil
```

### Explosion Effect

```
[EarthInfo Header]
[MESH Identifier: 4D 45 53 48 01 00 00 00]
[MeshDescriptor]
  - MeshType: 1 (Dynamic)
  - DynamicMeshSubType: 1 (Explosion)
[DynamicPart]
  - LightType: 2 (Trapezium)
  - SpriteAnimation: 16 frames, 4x4 sprite sheet
  - Additive: true
  - Color: Orange/Red
  - Texture: "explosion.tex"
  - SubMeshes: 0
```

## Tools

The EarthTool.MSH library provides:
- `EarthMeshReader`: Reads MSH files into `EarthMesh` objects
- `EarthMeshWriter`: Writes `EarthMesh` objects to MSH files
- `HierarchyBuilder`: Constructs part hierarchy trees from flat part lists

## Related Formats

- **TEX**: Texture format used for model textures
- **DAE**: Collada export format for 3D editing tools
- **PAR**: Parameter files that reference MSH models

## Version History

Current format version: 1 (as indicated in the MESH identifier)

---

*This documentation is based on the EarthTool.MSH implementation for Earth 2150.*
