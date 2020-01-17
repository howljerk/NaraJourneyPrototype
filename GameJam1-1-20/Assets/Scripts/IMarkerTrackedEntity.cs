using UnityEngine;

public interface IMarkerTrackedEntity
{
    Vector3 GetPosition();
    int GetTrackingDir();
}
