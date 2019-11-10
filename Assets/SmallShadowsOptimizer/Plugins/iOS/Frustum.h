
#pragma once

#define _USE_MATH_DEFINES
#include <math.h>

class Vector3
{
public:
	float x, y, z;
	//Constructors
	Vector3();
	Vector3( float X, float Y, float Z );

	//Operators with floats
	//void operator=( const Vector2 & V2 );

	Vector3 operator* ( float num );
	Vector3 operator*=( float num );
	Vector3 operator/ ( float num );
	Vector3 operator/=( float num );

	//Operators with self
	Vector3 operator+ ( Vector3 Vector );
	Vector3 operator+=( Vector3 Vector );
	Vector3 operator- ( Vector3 Vector );
	Vector3 operator-=( Vector3 Vector );
	Vector3 operator* ( Vector3 Vector );
	Vector3 operator*=( Vector3 Vector );
	Vector3 operator/ ( Vector3 Vector );
	Vector3 operator/=( Vector3 Vector );
	bool operator==( Vector3 Vector );
	bool operator!=( Vector3 Vector );

	//Other vector interactions
	//Vector3( Vector2 *V2, float Z = 0 );
	//Vector3 operator=(Vector2* Vec2);

	//Functions
	float length();
	void Set( float X, float Y, float Z = 0 );
	void Add( float X, float Y, float Z = 0 );
	void Set( Vector3 V );
	//void Set( Vector2 V );
	void Add( Vector3 A );

	void Normalize()
	{
		float len = (float)sqrt( (double)( x*x + y * y + z * z ) );
		x /= len;
		y /= len;
		z /= len;
	}
};

class PLANE
{
public:
	PLANE(Vector3 N, float C);
	PLANE(Vector3 N, Vector3 Point);
	PLANE();

	float Distance(Vector3 Point);
	int   WhichSide(Vector3 Point, float Radius = 0.0f );

	Vector3 Normal;
	float Constant;
};

class RE_CAMERA;
class REMesh;

class FRUSTUM
{
public:
	FRUSTUM();
	~FRUSTUM();	

	
	void Set( float AspectRatio, float VerticalFOV, float Near, float Far, Vector3 Position, Vector3 View, Vector3 UpVector );
	bool IsVisible(Vector3 Point, float Radius = 0.0f);
	
	bool IsVisible( Vector3 Center, Vector3 Size );
	
	PLANE CullingPlanes[6];
	bool Enabled;
};