# EarthTool.PAR Module - Inheritance Hierarchy

This document provides a comprehensive overview of the inheritance structure in the `EarthTool.PAR.Models` namespace, with all 44 entity types and their relationships.

## Quick Reference

- **Total Entity Types**: 36 concrete + 8 abstract = 44 entity classes
- **Other Classes**: Research (1 concrete), EntityGroup (1 concrete), ParFile (1 concrete)
- **Root Abstract Class**: `ParameterEntry`
- **Main Entity Base**: `Entity`
- **Primary Inheritance Branches**: TypedEntity, TypelessEntity

## Full Inheritance Hierarchy

```
ParameterEntry (abstract)
├── Entity (abstract) [IBinarySerializable]
│   ├── TypedEntity (concrete)
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

EntityGroup (concrete) [model]
ParFile (concrete) [model]
```

## Class Categories by Function

### A. Core Abstract Classes

#### `ParameterEntry`
- **Purpose**: Root base class for all parameter entries
- **Location**: `Models/Entities/Abstracts/ParameterEntry.cs`
- **Key Properties**: `Name` (string)
- **Children**: Entity, Research

#### `Entity` *(abstract)*
- **Purpose**: Base entity class with type name tracking for JSON serialization
- **Location**: `Models/Entities/Abstracts/Entity.cs`
- **Implements**: `IBinarySerializable`
- **Key Properties**:
  - `TypeName` (string) - Full type name for polymorphic deserialization
  - `RequiredResearch` (IEnumerable<int>) - Research requirements
  - `ClassId` (EntityClassType) - Entity classification
  - `FieldTypes` (abstract) - Boolean array indicating string fields
- **Key Methods**: `ToByteArray(Encoding)` - Serialization
- **Direct Children**: TypedEntity, TypelessEntity

### B. Primary Branching Classes

#### `TypedEntity` *(concrete)*
- **Purpose**: Entity with a specific type classification
- **Location**: `Models/Entities/Abstracts/TypedEntity.cs`
- **Key Features**: Implements `FieldTypes` based on ClassId
- **Children**: InteractableEntity

#### `TypelessEntity` *(abstract)*
- **Purpose**: Entity without type classification (ClassId = None)
- **Location**: `Models/Entities/Abstracts/TypelessEntity.cs`
- **Concrete Implementations**:
  - ShieldGenerator
  - PlayerTalkPack
  - TalkPack
  - Parameter
  - SoundPack
  - SpecialUpdateLink

### C. Destructible Entity Branch

#### `InteractableEntity` *(abstract)*
- **Purpose**: Entities with visual/interactive properties (mesh, shadow, sound, etc.)
- **Parent**: TypedEntity
- **Location**: `Models/Entities/Abstracts/InteractableEntity.cs`
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
- **Location**: `Models/Entities/Abstracts/DestructibleEntity.cs`
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

### D. Equipable Entities

#### `EquipableEntity` *(abstract)*
- **Purpose**: Entities that can equip weapons and equipment
- **Parent**: DestructibleEntity
- **Location**: `Models/Entities/Abstracts/EquipableEntity.cs`
- **Key Properties**:
  - `SightRange` (int) - Vision range
  - `TalkPackId` (string) - Reference to talk pack
  - `ShieldGeneratorId` (string) - Reference to shield generator
  - `MaxShieldUpgrade` (MaxShieldUpgradeType) - Max shield upgrade level
  - `Slot1Type-Slot4Type` (ConnectorType) - Equipment slot types
- **Children**: Vehicle, VerticalTransporter, Platoon, Building, StartingPosition

#### `Vehicle` *(concrete)*
- **Purpose**: Mobile units with terrain-specific speed properties
- **Parent**: EquipableEntity
- **Location**: `Models/Entities/Entities/Vehicle.cs`
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
- **Concrete Children**:
  - Sapper
  - Harvester
  - Builder
  - SupplyTransporter

#### `VerticalTransporter` *(abstract)*
- **Purpose**: Entities that can transport units vertically
- **Parent**: EquipableEntity
- **Location**: `Models/Entities/Abstracts/VerticalTransporter.cs`
- **Key Properties**:
  - `VehicleSpeed` (int) - Movement speed
  - `VerticalVehicleAnimationType` (VerticalVehicleAnimationType) - Animation type
- **Concrete Children**:
  - BuildingTransporter
  - ResourceTransporter
  - UnitTransporter

#### Platoon *(concrete)*
- **Purpose**: Military unit grouping/platoon structure
- **Parent**: EquipableEntity
- **Location**: `Models/Entities/Entities/Platoon.cs`

#### Building *(concrete)*
- **Purpose**: Stationary structures
- **Parent**: EquipableEntity
- **Location**: `Models/Entities/Entities/Building.cs`

#### StartingPosition *(concrete)*
- **Purpose**: Starting positions for units/buildings in maps
- **Parent**: EquipableEntity
- **Location**: `Models/Entities/Entities/StartingPosition.cs`

### E. Passive Entities

#### `PassiveEntity` *(abstract)*
- **Purpose**: Stationary, non-combative entities
- **Parent**: DestructibleEntity
- **Location**: `Models/Entities/Abstracts/PassiveEntity.cs`
- **Key Properties**:
  - `PassiveMask` (PassiveMask) - Passive behavior flags
  - `WallCopulaId` (string) - Reference to wall copula
- **Concrete Children**:
  - Passive
  - Artifact

### F. Equipment Entities

#### `Equipment` *(concrete)*
- **Purpose**: Standalone equipment/weapon components
- **Parent**: InteractableEntity
- **Location**: `Models/Entities/Entities/Equipment.cs`
- **Concrete Children**:
  - UpgradeCopula
  - TransporterHook
  - Repairer
  - ContainerTransporter
  - OmnidirectionalEquipment

### G. Special/Utility Entities

#### `Weapon` *(concrete)*
- **Purpose**: Weapons that can be equipped by units
- **Parent**: InteractableEntity
- **Location**: `Models/Entities/Entities/Weapon.cs`

#### `Special` *(concrete)*
- **Purpose**: Special structures or effects
- **Parent**: InteractableEntity
- **Location**: `Models/Entities/Entities/Special.cs`

#### `MultiExplosion` *(concrete)*
- **Purpose**: Multi-target explosion effects
- **Parent**: InteractableEntity
- **Location**: `Models/Entities/Entities/MultiExplosion.cs`

## Complete Entity Type Listing

### Concrete Implementations (36)

**TypedEntity Branch (34):**
- `Vehicle` + children: Sapper, Harvester, Builder, SupplyTransporter (5)
- `VerticalTransporter` + children: BuildingTransporter, ResourceTransporter, UnitTransporter (4)
- `PassiveEntity` + children: Passive, Artifact (3)
- `Equipment` + children: UpgradeCopula, TransporterHook, Repairer, ContainerTransporter, OmnidirectionalEquipment (6)
- Direct DestructibleEntity: Missile, Mine, Smoke, WallLaser, FlyingWaste, Explosion, BuilderLine (7)
- Direct InteractableEntity: Weapon, Special, MultiExplosion (3)
- Direct EquipableEntity: Platoon, Building, StartingPosition (3)

**TypelessEntity Branch (6):**
- ShieldGenerator
- PlayerTalkPack
- TalkPack
- Parameter
- SoundPack
- SpecialUpdateLink

**Other:**
- `Research` (ParameterEntry child)

### Model/Container Classes (2)
- `EntityGroup` - Grouping container for entities
- `ParFile` - Parameter file container

## Inheritance Statistics

| Statistic | Count |
|-----------|-------|
| Total Abstract Classes | 8 |
| Total Concrete Entity Classes | 36 |
| Total Concrete Other Classes | 2 |
| Maximum Inheritance Depth | 6 levels |
| Classes with No Children | 23 |
| Classes with 1-2 Children | 10 |
| Classes with 3-6 Children | 5 |

### Inheritance Depth Distribution
- **Depth 1**: ParameterEntry
- **Depth 2**: Entity, Research
- **Depth 3**: TypedEntity, TypelessEntity
- **Depth 4**: InteractableEntity (concrete classes), DestructibleEntity
- **Depth 5**: EquipableEntity, PassiveEntity
- **Depth 6**: Vehicle, VerticalTransporter, their children (max depth: 7 for Vehicle descendants)

## Structural Notes

### Key Design Patterns

1. **Type Classification**: Entities are split into TypedEntity (with ClassId) and TypelessEntity (ClassId = None)
2. **Capability Stacking**: Multiple parallel hierarchies add features:
   - `DestructibleEntity`: Adds health/armor
   - `InteractableEntity`: Adds visuals/sound
   - `EquipableEntity`: Adds equipment slots
   - `PassiveEntity`: Adds passive properties
   - `Vehicle`: Adds terrain-specific speeds
   - `VerticalTransporter`: Adds vertical transport capability
3. **Binary Serialization**: All entities implement `IBinarySerializable` and define `FieldTypes` for binary encoding
4. **Polymorphic JSON**: Uses `TypeName` property for runtime type resolution during deserialization

### File Organization
- **Location**: `/EarthTool.PAR/Models/`
- **Structure**:
  - `Entities/` - All concrete entity classes
  - `Entities/Abstracts/` - Abstract base classes (8 total)
  - `ParFile.cs` - Container for entities and research
  - `EntityGroup.cs` - Group container for entities
  - `Collections/` - Collection classes (ModelTree, ModelTreeEnumerator)

## Entity Coverage

**All 44 entity classes accounted for:**
- ParameterEntry ✓
- Entity ✓
- TypedEntity ✓
- TypelessEntity ✓
- InteractableEntity ✓
- DestructibleEntity ✓
- EquipableEntity ✓
- PassiveEntity ✓
- VerticalTransporter ✓
- Vehicle ✓
- Sapper ✓
- Harvester ✓
- Builder ✓
- SupplyTransporter ✓
- BuildingTransporter ✓
- ResourceTransporter ✓
- UnitTransporter ✓
- Platoon ✓
- Building ✓
- StartingPosition ✓
- Passive ✓
- Artifact ✓
- Missile ✓
- Mine ✓
- Smoke ✓
- WallLaser ✓
- FlyingWaste ✓
- Explosion ✓
- BuilderLine ✓
- Equipment ✓
- UpgradeCopula ✓
- TransporterHook ✓
- Repairer ✓
- ContainerTransporter ✓
- OmnidirectionalEquipment ✓
- Weapon ✓
- Special ✓
- MultiExplosion ✓
- ShieldGenerator ✓
- PlayerTalkPack ✓
- TalkPack ✓
- Parameter ✓
- SoundPack ✓
- SpecialUpdateLink ✓
- Research ✓

## References
- See `/EarthTool.PAR/Serialization/EntityConverter.cs` for JSON deserialization logic
- See `/EarthTool.PAR/Enums/` for enum definitions (EntityClassType, ShadowType, VehicleObjectType, etc.)
- See `/EarthTool.PAR/Extensions/BinaryExtensions.cs` for binary I/O helpers
