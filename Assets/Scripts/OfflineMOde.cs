using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class OfflineMOde : MonoBehaviour
{
    // Start is called before the first frame update
    bool offlineMode = true;
    void Start()
    {
        if (offlineMode)
        {
            // Initialize Photon without connecting to the server
            PhotonNetwork.OfflineMode = true;
            // Additional setup for offline mode if needed
        }
        else
        {
            // Connect to the Photon server
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
