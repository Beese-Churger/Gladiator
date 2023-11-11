using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Canvas : MonoBehaviour
{
    PhotonView PV;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        PV = GetComponent<PhotonView>();

        if (!PV)
            Destroy(this);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
