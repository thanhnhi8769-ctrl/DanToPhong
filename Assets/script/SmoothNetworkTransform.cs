using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class SmoothNetworkTransform : MonoBehaviour
{
    private void Start()
    {
        scooter vehicle = GetComponent<scooter>();
        Rigidbody body = GetComponent<Rigidbody>();
        if (vehicle != null && vehicle.IsRemoteControlled && body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }
}
