using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVO{
public class ExitTrigger : MonoBehaviour
{
    private int agentIndex;
    public RVO.RVOController rc;

    // Start is called before the first frame update
    void Start()
    {
        rc = FindObjectOfType<RVO.RVOController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setAgent(int i){
        agentIndex = i;
    }

    void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Agent")) {
        rc.checkToDestroyAgent(int.Parse(other.gameObject.name));
    }
}
}
}
