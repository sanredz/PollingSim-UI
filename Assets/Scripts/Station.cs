using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVO{
public class Station : MonoBehaviour
{
    public GameObject privacyZone;
    public GameObject privacyZoneB;
    RVO.Privacy privacy;
    RVO.PrivacyB privacyB;

    // Start is called before the first frame update
    void Start()
    {
        privacy = privacyZone.GetComponent<Privacy>();
        privacyB = privacyZoneB.GetComponent<PrivacyB>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setAgent(GameObject agent){
        privacyB.setAgent(agent);
        privacy.setAgent(agent);
    }
}
}