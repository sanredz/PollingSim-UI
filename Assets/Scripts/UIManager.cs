using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


namespace RVO{
    public class UIManager : MonoBehaviour
    {

        private float ts;
        private int agentStartCount;
        private int agentTotalCount;
        public RVO.RVOController rvo;
        public Slider slider;
        public float sliderValue;
        public TMP_InputField inputAgents;
        private int inputAgentsValue;
        public TextMeshProUGUI sliderText;
        public TextMeshProUGUI totalVotersText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI flowRateText;
        public Button start;
        GameObject[] showOnFinishObjects;
        // Start is called before the first frame update
        void Start()
        {
            showOnFinishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
            HideResults();
        }

        // Update is called once per frame
        void Update()
        {
            sliderValue = Mathf.Round(slider.value);
            sliderText.text = sliderValue.ToString();
        }

        public void SetTimeStep(float t){
            ts = t/100;
        }

        private void ToggleStart(bool on){
            if (on){
                slider.gameObject.SetActive(true);
                inputAgents.gameObject.SetActive(true);
            }else{
                slider.gameObject.SetActive(false);
                inputAgents.gameObject.SetActive(false);
            }
        }

        //Starts the Simulation
        public void StartSim(){
            SetTimeStep(sliderValue);
            HideResults();

            if (ValidateInput(inputAgents)){  
                if (inputAgentsValue < 10){
                    inputAgentsValue = 10;
                }
                else if (inputAgentsValue  > 200){
                    inputAgentsValue = 200;
                }
                SceneVariables.agentTotalCount = inputAgentsValue;
                SceneVariables.timeStep = ts;
                SceneVariables.pause = false;
                //ToggleStart(false);
                rvo.StartSimulation();
            }else{
                // Add some text element that says that the input was wrong
            }

        }

        public void Reload(){
            SceneVariables.timeStep = 0f;
            SceneVariables.pause = true;
            ToggleStart(true);
            rvo.ClearSimulation();
            rvo.ResetSimulation();
        }

        private bool ValidateInput(TMP_InputField input){

            if (int.TryParse(input.text, out inputAgentsValue))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void PrintResults(float time){
            timeText.text = System.Math.Round(time,2).ToString();
            totalVotersText.text = SceneVariables.agentTotalCount.ToString();
            flowRateText.text = System.Math.Round((SceneVariables.agentTotalCount/(time/60)),2).ToString();
            ShowResults();
        }

        public void HideResults(){
            foreach(GameObject g in showOnFinishObjects){
                g.SetActive(false);
            }
        }

        public void ShowResults(){
            foreach(GameObject g in showOnFinishObjects){
                g.SetActive(true);
            }
        }
    }
}