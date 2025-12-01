# EarthTool.PAR Module - Entities Inheritance Hierarchy

This document provides a comprehensive overview of the inheritance structure in the `EarthTool.PAR.Models.Entities` namespace, with all 44 entity types and their relationships.

## Quick Reference

- **Total Classes in Entities Folder**: 44 classes
- **Abstract Classes**: 10
- **Concrete Classes**: 34
- **Root Abstract Class**: `ParameterEntry`
- **Main Entity Base**: `Entity`
- **Primary Inheritance Branches**: TypedEntity, TypelessEntity

## Full Inheritance Hierarchy

```
ParameterEntry (abstract)
├── Entity (abstract) [IBinarySerializable]
│   ├── TypedEntity (abstract)
│   │   ├── InteractableEntity (abstract)
│   │   │   ├── DestructibleEntity (abstract)
│   │   │   │   ├── EquipableEntity (abstract)
│   │   │   │   │   ├── Vehicle (concrete)
│   │   │   │   │   │   ├── Sapper [concrete]
│   │   │   │   │   │   ├── Harvester [concrete]
│   │   │   │   │   │   ├── Builder [concrete]
│   │   │   │   │   │   └── SupplyTransporter [concrete]
│   │   │   │   │   ├── VerticalTransporter (abstract)
│   │   │   │   │   │   ├── BuildingTransporter [concrete]
│   │   │   │   │   │   ├── ResourceTransporter [concrete]
│   │   │   │   │   │   └── UnitTransporter [concrete]
│   │   │   │   │   ├── Platoon [concrete]
│   │   │   │   │   ├── Building [concrete]
│   │   │   │   │   └── StartingPosition [concrete]
│   │   │   │   ├── PassiveEntity (abstract)
│   │   │   │   │   ├── Passive [concrete]
│   │   │   │   │   └── Artifact [concrete]
│   │   │   │   ├── Missile [concrete]
│   │   │   │   ├── Mine [concrete]
│   │   │   │   ├── Smoke [concrete]
│   │   │   │   ├── WallLaser [concrete]
│   │   │   │   ├── FlyingWaste [concrete]
│   │   │   │   ├── Explosion [concrete]
│   │   │   │   └── BuilderLine [concrete]
│   │   │   ├── Equipment (concrete)
│   │   │   │   ├── UpgradeCopula [concrete]
│   │   │   │   ├── TransporterHook [concrete]
│   │   │   │   ├── Repairer [concrete]
│   │   │   │   ├── ContainerTransporter [concrete]
│   │   │   │   └── OmnidirectionalEquipment [concrete]
│   │   │   ├── Weapon [concrete]
│   │   │   ├── Special [concrete]
│   │   │   └── MultiExplosion [concrete]
│   └── TypelessEntity (abstract)
│       ├── ShieldGenerator [concrete]
│       ├── PlayerTalkPack [concrete]
│       ├── TalkPack [concrete]
│       ├── Parameter [concrete]
│       ├── SoundPack [concrete]
│       └── SpecialUpdateLink [concrete]
└── Research (concrete) [IBinarySerializable]
```

## Class Categories by Function

### A. Core Abstract Classes (10 total)

#### `ParameterEntry`
- **Purpose**: Root base class for all parameter entries
- **Location**: `Abstracts/ParameterEntry.cs`
- **Key Properties**: `Name` (string)
- **Direct Children**: Entity, Research

#### `Entity` *(abstract)*
- **Purpose**: Base entity class with type name tracking for JSON serialization
- **Location**: `Abstracts/Entity.cs`
- **Implements**: `IBinarySerializable`
- **Key Properties**:
  - `TypeName` (string) - Full type name for polymorphic deserialization
  - `RequiredResearch` (IEnumerable<int>) - Research requirements
  - `ClassId` (EntityClassType) - Entity classification
  - `FieldTypes` (abstract) - Boolean array indicating string fields
- **Key Methods**: `ToByteArray(Encoding)` - Serialization
- **Direct Children**: TypedEntity, TypelessEntity

#### `TypedEntity` *(abstract)*
- **Purpose**: Entity with a specific type classification
- **Location**: `Abstracts/TypedEntity.cs`
- **Key Features**: Implements `FieldTypes` based on ClassId
- **Parent**: Entity
- **Children**: InteractableEntity

#### `TypelessEntity` *(abstract)*
- **Purpose**: Entity without type classification (ClassId = None)
- **Location**: `Abstracts/TypelessEntity.cs`
- **Parent**: Entity
- **Concrete Implementations**:
  - ShieldGenerator
  - PlayerTalkPack
  - TalkPack
  - Parameter
  - SoundPack
  - SpecialUpdateLink

#### `InteractableEntity` *(abstract)*
- **Purpose**: Entities with visual/interactive properties (mesh, shadow, sound, etc.)
- **Parent**: TypedEntity
- **Location**: `Abstracts/InteractableEntity.cs`
- **Key Properties**:
  - `Mesh` (string) - 3D mesh reference
  - `ShadowType` (ShadowType) - Shadow rendering type
  - `ViewParamsIndex` (int) - View parameter index
  - `Cost` (int) - Build cost
  - `TimeOfBuild` (int) - Build time
  - `SoundPackId` (string) - Reference to sound pack
  - `SmokeId` (string) - Reference to smoke effect
  - `KillExplosionId` (string) - Reference to kill explosion
  - `DestructedId` (string) - Reference to destructed state
- **Children**: DestructibleEntity, Equipment, Weapon, Special, MultiExplosion

#### `DestructibleEntity` *(abstract)*
- **Purpose**: Entities with health, armor, and physical destruction
- **Parent**: InteractableEntity
- **Location**: `Abstracts/DestructibleEntity.cs`
- **Key Properties**:
  - `HP` (int) - Health points
  - `HpRegeneration` (int) - Health regeneration rate
  - `Armor` (int) - Armor value
  - `CalorificCapacity` (int) - Fuel/energy capacity
  - `DisableResist` (int) - Disable/stun resistance
  - `StoreableFlags` (StoreableFlags) - Storage capabilities
  - `StandType` (StandType) - Stance/stand type
- **Main Children**: EquipableEntity, PassiveEntity
- **Concrete Direct Children**:
  - Missile
  - Mine
  - Smoke
  - WallLaser
  - FlyingWaste
  - Explosion
  - BuilderLine

#### `EquipableEntity` *(abstract)*
- **Purpose**: Entities that can equip weapons and equipment
- **Parent**: DestructibleEntity
- **Location**: `Abstracts/EquipableEntity.cs`
- **Key Properties**:
  - `SightRange` (int) - Vision range
  - `TalkPackId` (string) - Reference to talk pack
  - `ShieldGeneratorId` (string) - Reference to shield generator
  - `MaxShieldUpgrade` (MaxShieldUpgradeType) - Max shield upgrade level
  - `Slot1Type-Slot4Type` (ConnectorType) - Equipment slot types
- **Children**: Vehicle, VerticalTransporter, Platoon, Building, StartingPosition

#### `VerticalTransporter` *(abstract)*
- **Purpose**: Entities that can transport units vertically
- **Parent**: EquipableEntity
- **Location**: `Abstracts/VerticalTransporter.cs`
- **Key Properties**:
  - `VehicleSpeed` (int) - Movement speed
  - `VerticalVehicleAnimationType` (VerticalVehicleAnimationType) - Animation type
- **Concrete Children**:
  - BuildingTransporter
  - ResourceTransporter
  - UnitTransporter

#### `PassiveEntity` *(abstract)*
- **Purpose**: Stationary, non-combative entities
- **Parent**: DestructibleEntity
- **Location**: `Abstracts/PassiveEntity.cs`
- **Key Properties**:
  - `PassiveMask` (PassiveMask) - Passive behavior flags
  - `WallCopulaId` (string) - Reference to wall copula
- **Concrete Children**:
  - Passive
  - Artifact

### B. Concrete Entity Classes (33)

#### TypedEntity Branch - Equipable Vehicles (5)

**Vehicle** *(concrete)*
- **Parent**: EquipableEntity
- **Location**: `Vehicle.cs`
- **Key Properties**:
  - `SoilSpeed` (int) - Speed on soil terrain
  - `RoadSpeed` (int) - Speed on road terrain
  - `SandSpeed` (int) - Speed on sand terrain
  - `BankSpeed` (int) - Speed on bank terrain
  - `WaterSpeed` (int) - Speed on shallow water
  - `DeepWaterSpeed` (int) - Speed on deep water
  - `AirSpeed` (int) - Air speed/altitude capability
  - `ObjectType` (VehicleObjectType) - Vehicle classification
  - `EngineSmokeId` (string) - Reference to engine smoke effect
  - `DustId` (string) - Reference to dust effect
  - `BillowId` (string) - Reference to billow effect
  - `StandBillowId` (string) - Reference to stand billow effect
  - `TrackId` (string) - Reference to track/trail effect

**Sapper** *(concrete)*
- **Parent**: Vehicle
- **Location**: `Sapper.cs`

**Harvester** *(concrete)*
- **Parent**: Vehicle
- **Location**: `Harvester.cs`

**Builder** *(concrete)*
- **Parent**: Vehicle
- **Location**: `Builder.cs`

**SupplyTransporter** *(concrete)*
- **Parent**: Vehicle
- **Location**: `SupplyTransporter.cs`

#### TypedEntity Branch - Vertical Transporters (4)

**BuildingTransporter** *(concrete)*
- **Parent**: VerticalTransporter
- **Location**: `BuildingTransporter.cs`

**ResourceTransporter** *(concrete)*
- **Parent**: VerticalTransporter
- **Location**: `ResourceTransporter.cs`

**UnitTransporter** *(concrete)*
- **Parent**: VerticalTransporter
- **Location**: `UnitTransporter.cs`

#### TypedEntity Branch - Equipable Structures (3)

**Platoon** *(concrete)*
- **Parent**: EquipableEntity
- **Location**: `Platoon.cs`
- **Purpose**: Military unit grouping/platoon structure

**Building** *(concrete)*
- **Parent**: EquipableEntity
- **Location**: `Building.cs`
- **Purpose**: Stationary structures

**StartingPosition** *(concrete)*
- **Parent**: EquipableEntity
- **Location**: `StartingPosition.cs`
- **Purpose**: Starting positions for units/buildings in maps

#### TypedEntity Branch - Destructible Direct Children (8)

**Missile** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `Missile.cs`

**Mine** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `Mine.cs`

**Smoke** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `Smoke.cs`

**WallLaser** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `WallLaser.cs`

**FlyingWaste** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `FlyingWaste.cs`

**Explosion** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `Explosion.cs`

**BuilderLine** *(concrete)*
- **Parent**: DestructibleEntity
- **Location**: `BuilderLine.cs`

#### TypedEntity Branch - Passive Entities (2)

**Passive** *(concrete)*
- **Parent**: PassiveEntity
- **Location**: `Passive.cs`

**Artifact** *(concrete)*
- **Parent**: PassiveEntity
- **Location**: `Artifact.cs`

#### TypedEntity Branch - Equipment (6)

**Equipment** *(concrete)*
- **Parent**: InteractableEntity
- **Location**: `Equipment.cs`
- **Purpose**: Standalone equipment/weapon components

**UpgradeCopula** *(concrete)*
- **Parent**: Equipment
- **Location**: `UpgradeCopula.cs`

**TransporterHook** *(concrete)*
- **Parent**: Equipment
- **Location**: `TransporterHook.cs`

**Repairer** *(concrete)*
- **Parent**: Equipment
- **Location**: `Repairer.cs`

**ContainerTransporter** *(concrete)*
- **Parent**: Equipment
- **Location**: `ContainerTransporter.cs`

**OmnidirectionalEquipment** *(concrete)*
- **Parent**: Equipment
- **Location**: `OmnidirectionalEquipment.cs`

#### TypedEntity Branch - Special Interactable (3)

**Weapon** *(concrete)*
- **Parent**: InteractableEntity
- **Location**: `Weapon.cs`
- **Purpose**: Weapons that can be equipped by units

**Special** *(concrete)*
- **Parent**: InteractableEntity
- **Location**: `Special.cs`
- **Purpose**: Special structures or effects

**MultiExplosion** *(concrete)*
- **Parent**: InteractableEntity
- **Location**: `MultiExplosion.cs`
- **Purpose**: Multi-target explosion effects

#### TypelessEntity Branch (6)

**ShieldGenerator** *(concrete)*
- **Parent**: TypelessEntity
- **Location**: `ShieldGenerator.cs`

**PlayerTalkPack** *(concrete)*
- **Parent**: TypelessEntity
- **Location**: `PlayerTalkPack.cs`

**TalkPack** *(concrete)*
- **Parent**: TypelessEntity
- **Location**: `TalkPack.cs`

**Parameter** *(concrete)*
- **Parent**: TypelessEntity
- **Location**: `Parameter.cs`

**SoundPack** *(concrete)*
- **Parent**: TypelessEntity
- **Location**: `SoundPack.cs`

**SpecialUpdateLink** *(concrete)*
- **Parent**: TypelessEntity
- **Location**: `SpecialUpdateLink.cs`

### C. Direct ParameterEntry Child (1)

**Research** *(concrete)*
- **Parent**: ParameterEntry
- **Location**: `Research.cs`
- **Implements**: `IBinarySerializable`
- **Purpose**: Research/technology definition

## Inheritance Statistics

| Metric | Value |
|--------|-------|
| Total Classes | 44 |
| Abstract Classes | 10 |
| Concrete Classes | 34 |
| Maximum Inheritance Depth | 8 levels |
| Root Classes | 1 (ParameterEntry) |
| Intermediate Abstract | 9 |
| Leaf Nodes (No Children) | 23 |
| Direct Entity Children | 2 (TypedEntity, TypelessEntity) |

### Inheritance Depth Distribution

- **Depth 1**: ParameterEntry
- **Depth 2**: Entity, Research
- **Depth 3**: TypedEntity, TypelessEntity
- **Depth 4**: InteractableEntity
- **Depth 5**: DestructibleEntity
- **Depth 6**: EquipableEntity, PassiveEntity
- **Depth 7**: Vehicle, VerticalTransporter, Platoon, Building, StartingPosition, Passive, Artifact
- **Depth 8**: Sapper, Harvester, Builder, SupplyTransporter, BuildingTransporter, ResourceTransporter, UnitTransporter (max depth from ParameterEntry)

### Children Count Distribution

| Class | Direct Children | Total Descendants |
|-------|-----------------|------------------|
| ParameterEntry | 2 | 42 |
| Entity | 2 | 41 |
| TypedEntity | 1 | 33 |
| TypelessEntity | 6 | 6 |
| InteractableEntity | 5 | 15 |
| DestructibleEntity | 8 | 20 |
| EquipableEntity | 5 | 15 |
| VerticalTransporter | 3 | 3 |
| PassiveEntity | 2 | 2 |
| Equipment | 5 | 5 |
| Vehicle | 4 | 4 |

## Design Patterns & Principles

### 1. Type Classification System
- **TypedEntity**: Entities with explicit EntityClassType (most game entities)
- **TypelessEntity**: Entities without type classification (utility/support)

### 2. Capability Stacking Architecture
The design uses multiple parallel hierarchies to compose features:
- **InteractableEntity**: Adds visual/audio properties (mesh, shadow, sound)
- **DestructibleEntity**: Adds health/armor/destruction (combatable entities)
- **EquipableEntity**: Adds equipment slots/shield/sight (units)
- **PassiveEntity**: Adds passive properties (stationary buildings)
- **Vehicle**: Adds terrain-specific speeds
- **VerticalTransporter**: Adds vertical transport capability

### 3. Binary Serialization
- All entities implement `IBinarySerializable`
- Each entity defines `FieldTypes` (boolean array) indicating which fields are strings
- Supports efficient binary encoding/decoding for file I/O

### 4. Polymorphic JSON Deserialization
- Entities track `TypeName` property for runtime type resolution
- Used by `EntityConverter` for deserializing polymorphic entities from JSON
- Enables proper type reconstruction during deserialization

## Complete Entity Checklist

**Abstract Classes (10):**
- ✓ ParameterEntry
- ✓ Entity
- ✓ TypedEntity
- ✓ TypelessEntity
- ✓ InteractableEntity
- ✓ DestructibleEntity
- ✓ EquipableEntity
- ✓ VerticalTransporter
- ✓ PassiveEntity

**Concrete Classes (34):**
- ✓ Artifact
- ✓ Builder
- ✓ BuilderLine
- ✓ Building
- ✓ BuildingTransporter
- ✓ ContainerTransporter
- ✓ Equipment
- ✓ Explosion
- ✓ FlyingWaste
- ✓ Harvester
- ✓ Mine
- ✓ Missile
- ✓ MultiExplosion
- ✓ OmnidirectionalEquipment
- ✓ Parameter
- ✓ Passive
- ✓ Platoon
- ✓ PlayerTalkPack
- ✓ Repairer
- ✓ Research
- ✓ ResourceTransporter
- ✓ Sapper
- ✓ ShieldGenerator
- ✓ Smoke
- ✓ SoundPack
- ✓ Special
- ✓ SpecialUpdateLink
- ✓ StartingPosition
- ✓ SupplyTransporter
- ✓ TalkPack
- ✓ TransporterHook
- ✓ UnitTransporter
- ✓ UpgradeCopula
- ✓ Vehicle
- ✓ WallLaser
- ✓ Weapon

## File Organization

```
EarthTool.PAR/Models/Entities/
├── Abstracts/
│   ├── DestructibleEntity.cs
│   ├── Entity.cs
│   ├── EquipableEntity.cs
│   ├── InteractableEntity.cs
│   ├── ParameterEntry.cs
│   ├── PassiveEntity.cs
│   ├── TypedEntity.cs
│   ├── TypelessEntity.cs
│   └── VerticalTransporter.cs
├── Artifact.cs
├── Builder.cs
├── BuilderLine.cs
├── Building.cs
├── BuildingTransporter.cs
├── ContainerTransporter.cs
├── Equipment.cs
├── Explosion.cs
├── FlyingWaste.cs
├── Harvester.cs
├── Mine.cs
├── Missile.cs
├── MultiExplosion.cs
├── OmnidirectionalEquipment.cs
├── Parameter.cs
├── Passive.cs
├── Platoon.cs
├── PlayerTalkPack.cs
├── Repairer.cs
├── Research.cs
├── ResourceTransporter.cs
├── Sapper.cs
├── ShieldGenerator.cs
├── Smoke.cs
├── SoundPack.cs
├── Special.cs
├── SpecialUpdateLink.cs
├── StartingPosition.cs
├── SupplyTransporter.cs
├── TalkPack.cs
├── TransporterHook.cs
├── UnitTransporter.cs
├── UpgradeCopula.cs
├── Vehicle.cs
├── WallLaser.cs
└── Weapon.cs
```

## Related Resources

- **Serialization**: See `/EarthTool.PAR/Serialization/EntityConverter.cs` for JSON polymorphic deserialization
- **Enums**: See `/EarthTool.PAR/Enums/` for entity-related enumerations
- **Binary I/O**: See `/EarthTool.PAR/Extensions/BinaryExtensions.cs` for binary read/write helpers
- **Collections**: See `/EarthTool.PAR/Models/Collections/` for collection management classes
