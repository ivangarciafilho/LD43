#include "Frustum.h"

#include <algorithm>

//#define USING_REFRAMEWORK
#ifndef USING_REFRAMEWORK
Vector3::Vector3()
{
	x = 0;
	y = 0;
	z = 0;
}
Vector3::Vector3( float X, float Y, float Z )
{
	x = X;
	y = Y;
	z = Z;
}
//Operators with floats	
Vector3 Vector3::operator*( float num )
{
	return Vector3( x * num, y * num, z * num );
}
Vector3 Vector3::operator*=( float num )
{
	Set( x*num, y*num, z*num );
	return *this;
}
Vector3 Vector3::operator/( float num )
{
	return Vector3( x / num, y / num, z / num );
}
Vector3 Vector3::operator/=( float num )
{
	Set( x / num, y / num, z / num );
	return *this;
}
//operators with other vectors
Vector3 Vector3::operator+( Vector3 Vector )
{
	return Vector3( Vector.x + x, Vector.y + y, Vector.z + z );
}
Vector3 Vector3::operator+=( Vector3 Vector )
{//A+=B;
	Set( x + Vector.x, y + Vector.y, z + Vector.z );
	return *this;
}
Vector3 Vector3::operator-( Vector3 Vector )
{
	return Vector3( x - Vector.x, y - Vector.y, z - Vector.z );
}
Vector3 Vector3::operator-=( Vector3 Vector )
{//A-=B;
	Set( x - Vector.x, y - Vector.y, z - Vector.z );
	return *this;
}
Vector3 Vector3::operator*( Vector3 Vector )
{
	return Vector3( x*Vector.x, y*Vector.y, z*Vector.z );
}
Vector3 Vector3::operator*=( Vector3 Vector )
{//A-=B;
	Set( x*Vector.x, y*Vector.y, z*Vector.z );
	return *this;
}
Vector3 Vector3::operator/( Vector3 Vector )
{
	return Vector3( x / Vector.x, y / Vector.y, z / Vector.z );
}
Vector3 Vector3::operator/=( Vector3 Vector )
{
	Set( x / Vector.x, y / Vector.y, z / Vector.z );
	return *this;
}
bool Vector3::operator==( Vector3 Vector )
{
	return ( x == Vector.x && y == Vector.y && z == Vector.z );
}
bool Vector3::operator!=( Vector3 Vector )
{
	return ( x != Vector.x || y != Vector.y || z != Vector.z );
}
//Functions
float Vector3::length()
{
	return (float)sqrt( x*x + y * y + z * z );
}
void Vector3::Set( float X, float Y, float Z )
{
	x = X;
	y = Y;
	z = Z;
}
void Vector3::Set( Vector3 V )
{
	*this = V;
}
void Vector3::Add( float X, float Y, float Z )
{
	x += X;
	y += Y;
	z += Z;
}
void Vector3::Add( Vector3 A )
{
	x += A.x;
	y += A.y;
	z += A.z;
}

float Magnitude( Vector3 vNormal )
{
	return (float)sqrt( ( vNormal.x * vNormal.x )
						+ ( vNormal.y * vNormal.y ) + ( vNormal.z * vNormal.z ) );
}
Vector3 Normalize( Vector3 vNormal )
{
	double Mag;
	Mag = Magnitude( vNormal );
	//prevent bugs, the vector is already 0
	if ( Mag == 0 )
		return vNormal;
	vNormal.x /= (float)Mag;
	vNormal.y /= (float)Mag;
	vNormal.z /= (float)Mag;
	return vNormal;
}
float DotProduct( Vector3 V1, Vector3 V2 )
{
	return ( ( V1.x * V2.x ) + ( V1.y * V2.y ) + ( V1.z * V2.z ) );
}
Vector3 CrossProduct( Vector3 V1, Vector3 V2 )
{
	Vector3 Normal;
	Normal.x = ( ( V1.y * V2.z ) - ( V1.z * V2.y ) );
	Normal.y = ( ( V1.z * V2.x ) - ( V1.x * V2.z ) );
	Normal.z = ( ( V1.x * V2.y ) - ( V1.y * V2.x ) );
	Normal = Normalize( Normal );
	return Normal;
}
#else
	//#include <3DMath.h>
	Vector3 CrossProduct( Vector3 V1, Vector3 V2 );
	float DotProduct( Vector3 V1, Vector3 V2 );
#endif

enum PlaneData
{
	A = 0,
	B = 1,
	C = 2,
	D = 3
};

enum FRUSTUM_PLANES
{
	NEAR_PLANE     = 0,
	FAR_PLANE      = 1,
	LEFT_PLANE     = 2,
	RIGHT_PLANE    = 3,
	TOP_PLANE      = 4,
	BOTTOM_PLANE   = 5,
	MAX_PLANES     = 6
};
PLANE::PLANE(Vector3 N, float C)
{
	Normal = N;
	Constant = C;
}
PLANE::PLANE(Vector3 N, Vector3 Point)
{
    Normal = N;//Normalize( N );
    Constant = DotProduct( N , Point );
}
PLANE::PLANE()
{
	Constant = 0;
}
float PLANE::Distance(Vector3 Point)  
{
    return DotProduct(Normal, Point) - Constant;
}
int PLANE::WhichSide(Vector3 Point, float Radius)
{
	float Dist = Distance( Point );	

    if (Dist <= -Radius)
        return -1;
    else if (Dist >= Radius)
        return 1;
    else
        return 0;
}
FRUSTUM::FRUSTUM()
{
	Enabled = true;
}
FRUSTUM::~FRUSTUM()
{
}
bool FRUSTUM::IsVisible(Vector3 Point, float Radius)
{
	int i;
	for ( i = 0; i < MAX_PLANES; i++)
    {
		int Side = CullingPlanes[i].WhichSide( Point, Radius );
		if (Side == -1)
        {
			break;
		}
	}

	//FRUSTUM_PLANES LastPlane = (FRUSTUM_PLANES) i;

	if (i == MAX_PLANES)
		return true;

	return false;
}
bool FRUSTUM::IsVisible( Vector3 Center, Vector3 Size )
{
	float Radius = std::max( Size.x, std::max( Size.y, Size.z ) );
	bool Ret = IsVisible( Center, Radius );
	return Ret;
}
void FRUSTUM::Set( float fAspectRatio, float VerticalFOV, float Near, float Far, Vector3 Position, Vector3 View, Vector3 UpVector )
{
    // Setup the camera frustum and viewport
    float fVerticalFieldOfViewDegrees = VerticalFOV;
    float fVerticalFieldOfViewRad     = (float)( M_PI / 180.0f * fVerticalFieldOfViewDegrees);
    float fViewPlaneHalfHeight        = tanf(fVerticalFieldOfViewRad * 0.5f);
    float fViewPlaneHalfWidth         = fViewPlaneHalfHeight * fAspectRatio;

	float m_fNear = Near;
	float m_fFar  = Far;
	
	float m_fLeft   = -fViewPlaneHalfWidth;
	float m_fRight  =  fViewPlaneHalfWidth;
	float m_fTop    =  fViewPlaneHalfHeight;
    float m_fBottom = -fViewPlaneHalfHeight;	

	Vector3 kLoc = Position;//kXform.m_Translate;
    Vector3 kDVector = View;
	Vector3 kUVector = UpVector;	
	Vector3 kRVector = CrossProduct( UpVector, View);//*-1;	

	kUVector = CrossProduct( kDVector, kRVector);

    Vector3 kPoint = kLoc + kDVector * m_fNear;
    CullingPlanes[NEAR_PLANE] = PLANE(kDVector, kPoint);

    kPoint = kLoc + kDVector * m_fFar;
    CullingPlanes[FAR_PLANE] = PLANE(kDVector*-1, kPoint);

	bool m_bOrtho = false;

    if (m_bOrtho)
    {
        kPoint = kLoc + kRVector * m_fLeft;
        CullingPlanes[LEFT_PLANE] = PLANE(kRVector, kPoint);

        kPoint = kLoc + kRVector * m_fRight;
        CullingPlanes[RIGHT_PLANE] = PLANE(kRVector*-1, kPoint);

        kPoint = kLoc + kUVector * m_fTop;
        CullingPlanes[TOP_PLANE] = PLANE(kUVector*-1, kPoint);

        kPoint = kLoc + kUVector * m_fBottom;
        CullingPlanes[BOTTOM_PLANE] = PLANE(kUVector, kPoint);
    }
    else
    {
        float fTmp = m_fLeft * m_fLeft;
        float fInv = (float)( 1.0f / sqrt(1.0f + fTmp) );
		//float fInv = 1.0f / sqrt( fTmp);
        float fC0 = -m_fLeft * fInv;
        float fC1 = fInv;
        Vector3 kNormal = kDVector * fC0 + kRVector * fC1;
        CullingPlanes[LEFT_PLANE] = PLANE(kNormal, kLoc);

        fTmp = m_fRight * m_fRight;
        fInv = (float)( 1.0f / sqrt(1.0f + fTmp) );
        fC0 = m_fRight * fInv;
        fC1 = -fInv;
        kNormal = kDVector * fC0 + kRVector * fC1;
        CullingPlanes[RIGHT_PLANE] = PLANE(kNormal, kLoc);

        fTmp = m_fTop * m_fTop;
        fInv = (float)(1.0f / sqrt(1.0f + fTmp));
        fC0 = m_fTop * fInv;
        fC1 = -fInv;
        kNormal = kDVector * fC0 + kUVector * fC1;
        CullingPlanes[TOP_PLANE] = PLANE(kNormal, kLoc);

        fTmp = m_fBottom * m_fBottom;
        fInv = (float)( 1.0f / sqrt(1.0f + fTmp) );
        fC0 = -m_fBottom * fInv;
        fC1 = fInv;
        kNormal = kDVector * fC0 + kUVector * fC1;
        CullingPlanes[BOTTOM_PLANE] = PLANE(kNormal, kLoc);
    }
}