#pragma pattern_limit 0xF000

#include <std/mem.pat>

enum MeshType : u32 {
	Static,
	Dynamic
};

enum StaticMeshType : u32 {
	Unknown,
	Unit,
	Building
};

enum DynamicMeshType : u32 {
	Unknown
};

struct Template {
	u8 pattern[2];
	u16 flag;
};

struct FlagRotation {
	u16 row[4];
};

struct Position {
	float x;
	float y;
	float z;
};

struct SpotLight {
	float x;
	float y;
	float x;
	float r;
	float g;
	float b;
	float length;
	u32 direction;
	float width;
	float unknown;
	float tilt;
	float ambience;
};

struct OmniLight {
	float x;
	float y;
	float x;
	float r;
	float g;
	float b;
	float intensity;
};

struct Slot {
	u16 x;
	u16 y;
	u16 z;
	u8 direction;
	u8 flag;
};

struct Slots {
	Slot slot_sc[4];
	Slot slot_mi[4];
	Slot slot_ss[4];
	Slot slot_hl[4];
	Slot slot_om[4];
	Slot slot_tr[4];
	Slot slot_ht[4];
	Slot slot_sm[4];
	Slot slot_unk[4];
	Slot slot_ch[2];
	Slot slot_st[2];
	Slot slot_se[2];
	Slot slot_sk[2];
	Slot slot_in;
	Slot slot_ce;
	Slot slot_pr;
	Slot slot_mv;
	Slot slot_ln;
};

struct Boundries {
	u16 maxY;
	u16 minY;
	u16 maxX;
	u16 minX;
};

struct Descriptor {
	MeshType type;
	Template template;
	u8 buildingFrames;
	u8 actionFrames;
	u8 moveFrames;
	u8 loopedFrames;
	u32 empty;
	Position mountPoints[4];
	SpotLight spotLights[4];
	OmniLight omniLight[4];
	u16 heights[16];
	u8 flags[16];
	Template templateRotations[4];
	FlagRotation flagRotations[4];
	Slots slots;
	Boundries boundries;
	if (type == MeshType::Static) {
		StaticMeshType subtype;
	}
	else if (type == MeshType::Dynamic) {
		DynamicMeshType subtype;
	}
};

struct Vertex {
	float x;
	padding[12];
	float y;
	padding[12];
	float z;
	padding[12];
	float normalX;
	padding[12];
	float normalY;
	padding[12];
	float normalZ;
	padding[12];
	float u;
	padding[12];
	float v;
};

struct VertexBlock {
	Vertex vertex1;
	$ -= sizeof(vertex1) - 4;
	Vertex vertex2;
	$ -= sizeof(vertex2) - 4;
	Vertex vertex3;
	$ -= sizeof(vertex3) - 4;
	Vertex vertex4;
	u8 empty[16];
	u16 normalIdx[4];
	u16 positionIdx[4];
};

struct Vertices {
	u32 count;
	u32 blocks;
	VertexBlock vertexBlocks[blocks];
};

struct Texture {
	u32 length;
	char filename[length];
};

struct Face {
	u16 v1;
	u16 v2;
	u16 v3;
	u16 unknown;
};

struct Faces {
	u32 length;
	Face faces[length];
};

struct TranslationFrames {
	u32 length;
	Position offsets[length];
};

struct RotationMatrix {
	float a11;
	float a12;
	float a13;
	float a14;
	float a21;
	float a22;
	float a23;
	float a24;
	float a31;
	float a32;
	float a33;
	float a34;
	float a41;
	float a42;
	float a43;
	float a44;
};

struct RotationFrames {
	u32 length;
	RotationMatrix rotations[length];
};

struct Animations {
	TranslationFrames unknown;
	TranslationFrames movement;
	RotationFrames rotation;
};

struct StaticGeometry {
	Vertices vertices;
	u8 backtrackLevel;
	u8 type;
	u8 unknown[2];
	Texture texture;
	Faces faces;
	Animations animations;
	u32 animationType;
	Position offset;
	u8 riseAngleRad;
	u8 unknown[4];
};

struct Mesh {
	char identifier[8];
	Descriptor descriptor;
	if (descriptor.type == MeshType::Static) {
		StaticGeometry geometries[while(!std::mem::eof())];
	}
	else if (descriptor.type == MeshType::Dynamic) {
		
	}
};

Mesh data @ std::mem::find_sequence(0, 0x4D, 0x45, 0x53, 0x48);


