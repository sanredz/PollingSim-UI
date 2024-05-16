using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVO{
public class Privacy : MonoBehaviour
{
    private GameObject agent;
    public RVO.RVOController rc;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setAgent(GameObject ag){
        agent = ag;
    }

    void OnTriggerEnter(Collider other) {
        if (other.gameObject != agent && agent != null) {
            Debug.Log("Triggers Work");
            rc.updatePrivacyInfringements();
        }
    }
}
}
