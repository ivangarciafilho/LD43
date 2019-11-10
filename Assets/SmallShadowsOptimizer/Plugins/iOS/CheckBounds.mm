
#include "Frustum.h"
#include "CheckBounds.h"

class UnityBounds
{
public:
	Vector3 Center;
	Vector3 Size;
};

FRUSTUM GlobalFrustum;
Vector3 GlobalCameraPosition;
void SetFrustum( float fAspectRatio, float VerticalFOV, float Near, float Far, Vector3 Position, Vector3 View, Vector3 UpVector )
{
	GlobalCameraPosition = Position;
	GlobalFrustum.Set( fAspectRatio, VerticalFOV, Near, Far, Position, View, UpVector );
}
double averageTime = 0;
int Frames = 0;
void CheckBounds( float* boundsArray, int numBounds, int* results, float SizeThreshold, int* resultsDiff, int* diffSize )
{
#if 0
	double before = REFramework::GetCurrentTime();
#endif
	UnityBounds* unityBounds = (UnityBounds*)boundsArray;
	*diffSize = 0;


	for ( int i = 0; i < numBounds; i++ )
	{
		UnityBounds & bounds = unityBounds[ i ];
		bool visible = GlobalFrustum.IsVisible( bounds.Center, bounds.Size );
		bool castShadows = true;
		if ( visible )
		{
			float Distance = ( bounds.Center - GlobalCameraPosition ).length();

			float Score = Distance / bounds.Size.y;

			if ( Score > SizeThreshold )
			{
				castShadows = false;
			}
		}

		bool prev = (bool)results[ i ];
		if ( prev != castShadows )
		{
			//object index that changed
			resultsDiff[ *diffSize ] = i;
			( *diffSize )++;
		}
		results[ i ] = castShadows;
	}

#if 0
	double after = REFramework::GetCurrentTime();
	double diff = after - before;
	Frames++;
	averageTime += diff;
	if ( Frames >= 60 )
	{
		float average = (float)( averageTime / (double)Frames );
		RELog( "CheckBounds[%d] took on average %f ms", numBounds, average );
		Frames = 0;
		averageTime = 0;
	}
#endif
}