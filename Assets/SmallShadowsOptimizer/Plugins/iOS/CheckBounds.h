
#include "Frustum.h"

extern "C"
{
	//CheckBounds
	void  __declspec( dllexport ) SetFrustum( float fAspectRatio, float VerticalFOV, float Near, float Far, Vector3 Position, Vector3 View, Vector3 UpVector );
	void  __declspec( dllexport ) CheckBounds( float* boundsArray, int numBounds, int* results, float SizeThreshold, int* resultsDiff, int* diffSize );
}