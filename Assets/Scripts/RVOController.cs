using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

namespace RVO
{
    public class RVOController : MonoBehaviour
    {
        // Bara debug variables, deleta dom efter.

        // Riktiga variables
        private Vector3[] stations;
        private bool[] enqueued;
        private int stationCount = 2;
        private int agentPhases = 2;
        private int[] currAgentPhase;
        private GameObject[] unityObstacles;
        private Vector3[] goals;
        private GameObject[] RVOAgents;
        public GameObject prefab;
        private Seeker[] seekers;
        private Path[] paths;
        private KeyValuePair<int, GameObject> agentPair = new KeyValuePair<int, GameObject>();
        private float qTimer = 0.0f;
        private float qTime = 3.0f;
        private int[] currentNodeInPath;
        private int agentCount = 20; // Total number of agents
        public List<KeyValuePair<int, GameObject>> inQueueP1;
        public List<KeyValuePair<int, GameObject>> inQueueP3;


        public void AddUnityObstacleToRVO(GameObject unityObstacle)
        {
            // Assuming the obstacle is aligned with the world axes and
            // using localScale to determine the size
            Vector3 size = unityObstacle.transform.localScale / 2.0f; // half size for offset
            Vector3 center = unityObstacle.transform.position;

            // Calculate the corners of the base of the cube
            Vector3 corner1 = center + new Vector3(-size.x, 0, size.z);
            Vector3 corner2 = center + new Vector3(-size.x, 0, -size.z);
            Vector3 corner3 = center + new Vector3(size.x, 0, -size.z);
            Vector3 corner4 = center + new Vector3(size.x, 0, size.z);

            // Create the obstacle for RVO
            IList<Vector2> rvoObstacle = new List<Vector2>
            {
                new Vector2(corner1.x, corner1.z),
                new Vector2(corner2.x, corner2.z),
                new Vector2(corner3.x, corner3.z),
                new Vector2(corner4.x, corner4.z)
            };

            // Add the obstacle to the RVO simulator
            Simulator.Instance.addObstacle(rvoObstacle);

            // Process the obstacle to take effect in the simulation
            Simulator.Instance.processObstacles();
        }

        RVO.Vector2 toRVOVector(Vector3 param)
        {
            return new RVO.Vector2(param.x, param.z);
        }

        Vector3 toUnityVector(RVO.Vector2 param)
        {
            return new Vector3(param.x(), 2.5f, param.y());
        }

        void UpdateSeekers(Vector3 start, Vector3 end, int i){
            void OnPathComplete(Path p)
            {
                if (!p.error)
                {
                    paths[i] = p;
                }
                else
                {
                    Debug.LogError("Path error: " + p.errorLog);
                }
            }
            seekers[i].StartPath(start, end, OnPathComplete);
        }

        void UpdateAgentGoal(int agentIndex, Vector3 start, Vector3 goal){
            currentNodeInPath[agentIndex] = 0;
            goals[agentIndex] = goal;

            UpdateSeekers(start, goals[agentIndex], agentIndex);
        }

        void UpdateGoalsInQueue(int agentIndex, int phase){
            for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
                if (!inQueueP1.Any(pair => pair.Value == RVOAgents[i]) & currAgentPhase[i] == phase){
                    var agentPos = RVOAgents[agentIndex].transform.position;
                    agentPos.z += 3;
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, agentPos);
                    RVOAgents[i].name = "Agent: " + i + "-- Goal: " + agentPos + "-- AT: " + agentIndex;
                }
                else if (!inQueueP3.Any(pair => pair.Value == RVOAgents[i]) & currAgentPhase[i] == phase){
                    var agentPos = RVOAgents[agentIndex].transform.position;
                    agentPos.z += 3;
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, agentPos);
                    RVOAgents[i].name = "Agent: " + i + "-- Goal: " + agentPos + "-- AT: " + agentIndex;
                }
            }
        }

        void UpdateGoalsInQueue(Vector3 firstPlace, int phase){
            for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
                if (inQueueP1.Any(pair => pair.Value == RVOAgents[i]) & (currAgentPhase[i] == phase)){
                    Debug.Log("Update in Queue should occur, agents should move a step forward.");
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, firstPlace);
                }
                else{
                    Debug.Log("In Queue: " + inQueueP1.Any(pair => pair.Value == RVOAgents[i]) + " -- Current Phase: " + currAgentPhase[i]);
                }
            }
        }

        void MoveQueue(Vector3 firstPlace, int phase){
            bool first = true;
            Vector3 prevPos = new Vector3(0f, 0f, 0f);
            Vector3 prevPosT =  new Vector3(0f, 0f, 0f);
            int last = -1;
            foreach (KeyValuePair<int, GameObject> agentPair in inQueueP1){
                int i = agentPair.Key;
                if (first){
                    prevPos = RVOAgents[i].transform.position;
                    RVOAgents[i].transform.position = firstPlace;
                    Simulator.Instance.setAgentPosition(i, toRVOVector(RVOAgents[i].transform.position));
                    first = false;
                    last = i;
                } 
                else {
                    prevPosT = prevPos;
                    prevPos = RVOAgents[i].transform.position;
                    RVOAgents[i].transform.position = prevPosT;
                    Simulator.Instance.setAgentPosition(i, toRVOVector(RVOAgents[i].transform.position));
                    last = i;
                }
            }
            UpdateGoalsInQueue(last, phase);
        }

        public void SetTimeStep(float t){
            Simulator.Instance.setTimeStep(t);
        }

        public void InitializeObstacles(){
            unityObstacles = GameObject.FindGameObjectsWithTag("Obstacle");

            for (int i = 0; i < unityObstacles.Length; i++){
                AddUnityObstacleToRVO(unityObstacles[i]);
                Debug.Log(unityObstacles[i].name);
                Debug.Log("Added");
            }
        }

        public void InitializeAgents(){
            Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, 0.6f, 1.0f, new RVO.Vector2(0.0f, 0.0f));

            inQueueP1 = new List<KeyValuePair<int, GameObject>>();
            inQueueP3 = new List<KeyValuePair<int, GameObject>>();
            RVOAgents = new GameObject[agentCount];
            goals = new Vector3[agentCount];
            seekers = new Seeker[agentCount];
            paths = new Path[agentCount];
            currentNodeInPath = new int[agentCount];
            currAgentPhase = new int[agentCount];
            stations = new Vector3[stationCount];
            enqueued = new bool[agentCount];
            
            stations[0] = new Vector3(30,1,-7);
            stations[1] = new Vector3(31,1,-25);

            // Instantiate agents and set their goals
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 spawnPosition, goalPosition;
                currentNodeInPath[i] = 0;
                currAgentPhase[i] = 1;
                enqueued[i] = false;

                if (i < agentCount / 2)
                {
                    // First half of agents
                    spawnPosition = new Vector3(-30 -i,1,7);
                    goalPosition = stations[0];
                }
                else
                {
                    // Second half of agents
                    spawnPosition = new Vector3(30+i,1,-7);
                    goalPosition = stations[0];
                    goalPosition.x = -goalPosition.x ;
                }

                GameObject go = GameObject.Instantiate(prefab, spawnPosition, Quaternion.identity) as GameObject;
                if (i < agentCount/2){
                    var agentRenderer = go.GetComponent<Renderer>();
                    agentRenderer.material.SetColor("_Color", Color.green);
                }
                RVOAgents[i] = go;
                go.transform.parent = transform;
                goals[i] = goalPosition;
                seekers[i] = RVOAgents[i].AddComponent<Seeker>();

                UpdateSeekers(spawnPosition, goals[i], i);
                
                Simulator.Instance.addAgent(toRVOVector(spawnPosition));
                
            }
        }

        public void InitializeSimulation(){
            Simulator.Instance.setTimeStep(0.0f);
            InitializeObstacles();
            InitializeAgents();
        }

        public void ClearSimulation(){
            for (int i = 0; i < agentCount; i++){
                Destroy(RVOAgents[i]);
            }
            Simulator.Instance.Clear();
            ResetTimers();
        }

        void ResetTimers(){
            qTimer = 0f;
            qTime = 3f;
        }

        void Start()
        {

            Debug.Log("Simulation script starting!");
            InitializeSimulation();

            if (Simulator.Instance == null)
            {
                Debug.LogError("Simulator.Instance is null");
                return;
            }
            else
            {
                Debug.Log("Everything seems to be working as it should!");
                Debug.Log($"This data -> {Simulator.Instance.getNumAgents()}");
            }
        }


        void UpdateCalc(int i){
            Vector3 currentWaypoint = paths[i].vectorPath[currentNodeInPath[i]];
            Vector3 directionToWaypoint = currentWaypoint - RVOAgents[i].transform.position;

            // Vector2 goalVector = toRVOVector(goals[i]) - Simulator.Instance.getAgentPosition(i);

            // Use the direction to the waypoint to set the preferred velocity
            
            Vector2 goalVector = toRVOVector(directionToWaypoint);
            // Check if close to the current waypoint and increment index
            float dist = Vector3.Distance(RVOAgents[i].transform.position, currentWaypoint);
            if (dist < 3f)
            {
                currentNodeInPath[i]++; // Increment the waypoint index for this agent
                
            }

            if (RVOMath.absSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.normalize(goalVector);
            }

            Simulator.Instance.setAgentPrefVelocity(i, goalVector);
            RVOAgents[i].transform.position = toUnityVector(Simulator.Instance.getAgentPosition(i)); 
        }

        void StopRVOAgent(int i){
            Simulator.Instance.setAgentPosition(i, toRVOVector(RVOAgents[i].transform.position)); // Den driftar forfarande lite om man bara sätter pref velocity.
            Simulator.Instance.setAgentPrefVelocity(i, new RVO.Vector2(0.0f, 0.0f)); // Måste stoppa agenten i RVO också. 
            Simulator.Instance.setAgentVelocity(i, new RVO.Vector2(0.0f, 0.0f)); // Måste stoppa agenten i RVO också. 
        }

        void Update()
        {
            for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
                if (paths[i] == null){
                    continue;
                } // ANVÄND ENQUEUED ISTÄLLET FÖR CHECKA OM I QUEUEN. TESTA OCKSÅ FIXA ATT GOAL ÄR I DIRECTION AV VAR MAN KMR IFRÅN
                if (currentNodeInPath[i] >= paths[i].vectorPath.Count & !inQueueP1.Any(pair => pair.Value == RVOAgents[i]) & currAgentPhase[i] == 1){
                    inQueueP1.Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                    currAgentPhase[i] += 1;
                    UpdateGoalsInQueue(i, currAgentPhase[i]-1); 
                    
                    //StopRVOAgent(i);
                    Debug.Log("Agent added to Queue");
                    continue;
                }

                if (currentNodeInPath[i] >= paths[i].vectorPath.Count & !inQueueP3.Any(pair => pair.Value == RVOAgents[i]) & currAgentPhase[i] == 3){
                    inQueueP3.Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                    currAgentPhase[i] += 1;
                    UpdateGoalsInQueue(i, currAgentPhase[i]-1);
                    stations[1].z += 3; 
                    StopRVOAgent(i);
                    Debug.Log("Agent added to Queue");
                    continue;
                }
                if (currentNodeInPath[i] >= paths[i].vectorPath.Count){

                    int indexInQueue = inQueueP1.FindIndex(pair => pair.Key.Equals(i));
                    Debug.Log("DE2-- Index of Agent: " + i + " is in Queue Pos: " + indexInQueue);
                    if (indexInQueue>0){
                        agentPair = inQueueP1[indexInQueue-1];
                        int j = agentPair.Key;
                        Vector3 g = RVOAgents[j].transform.position;
                        g.z += 3;
                        goals[i] = g;
                    }
                    Vector2 goalVector = toRVOVector(goals[i]) - Simulator.Instance.getAgentPosition(i);
                    if (RVOMath.absSq(goalVector) > 1.0f)
                    {
                        goalVector = RVOMath.normalize(goalVector);
                    }

                    Simulator.Instance.setAgentPrefVelocity(i, goalVector);
                    RVOAgents[i].transform.position = toUnityVector(Simulator.Instance.getAgentPosition(i)); 
                    continue;
                }

               UpdateCalc(i);

            }

            if (inQueueP1.Count > 0){
                if (qTimeCheck()){
                    //Debug.Log("Agent: " + inQueueP1.Peek() + " has been in Queue for " + qTime + " seconds");
                    agentPair = inQueueP1[0];
                    inQueueP1.RemoveAt(0); 
                    int i = agentPair.Key;
                    currAgentPhase[i] += 1;

                    Simulator.Instance.setAgentPosition(i, toRVOVector(RVOAgents[i].transform.position));
                    //UpdateGoalsInQueue(RVOAgents[i].transform.position, currAgentPhase[i]-1);
                    if (inQueueP1.Count > 0){
                        Debug.Log("DE3: ENTERED");
                        agentPair = inQueueP1[0];
                        int j = agentPair.Key;
                        Vector3 agentGoalPos = RVOAgents[i].transform.position;
                        UpdateAgentGoal(j, RVOAgents[j].transform.position, agentGoalPos);
                    }

                    UpdateAgentGoal(i, RVOAgents[i].transform.position, stations[1]);
                    
                }
            }
            
            Simulator.Instance.doStep();
        }

        private bool qTimeCheck(){
            qTimer += Time.deltaTime;
            //Debug.Log("time in Queue: " + qTimer + " Max Time: " + qTime);
            if (qTimer > qTime){
                qTimer = 0;
                return true;
            }
            return false;
        }
    }
}