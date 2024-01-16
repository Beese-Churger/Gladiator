using UnityEngine;
using Photon.Pun;

public class LagCompensation : MonoBehaviourPun, IPunObservable
{
    private Vector3 networkedPosition;
    private Quaternion networkedRotation;

    private float interpolationFactor = 15f;

    private Vector3 lastReceivedPosition;
    private Quaternion lastReceivedRotation;
    private float lastReceivedTime;

    void Update()
    {
        if (!photonView.IsMine)
        {
            // Interpolate position and rotation for non-local players
            float timeSinceLastUpdate = Time.time - lastReceivedTime;

            Vector3 predictedPosition = lastReceivedPosition + (networkedPosition - lastReceivedPosition) * (timeSinceLastUpdate / interpolationFactor);
            Quaternion predictedRotation = Quaternion.Slerp(lastReceivedRotation, networkedRotation, timeSinceLastUpdate / interpolationFactor);

            transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * interpolationFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, predictedRotation, Time.deltaTime * interpolationFactor);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Sending data to others (your position and rotation)
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Receiving data from others (their position and rotation)
            lastReceivedPosition = networkedPosition;
            lastReceivedRotation = networkedRotation;
            lastReceivedTime = Time.time;

            networkedPosition = (Vector3)stream.ReceiveNext();
            networkedRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}