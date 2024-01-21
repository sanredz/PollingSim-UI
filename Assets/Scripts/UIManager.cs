using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{

    public RVO.RVOController rvo;
    public Slider slider;
    public TextMeshProUGUI sliderText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float sliderValue = Mathf.Round(slider.value);
        sliderText.text = sliderValue.ToString();
        SetTimeStep(sliderValue);
    }

    public void SetTimeStep(float t){
        float ts = t/100;
        rvo.SetTimeStep(ts);
    }

    //Reloads the Simulation
	public void Reload(){
        rvo.ClearSimulation();
        rvo.ResetSimulation();
	}

    public void PrintResults(){
        
    }
}
