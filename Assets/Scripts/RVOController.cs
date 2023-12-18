using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

namespace RVO
{
    public class RVOController : MonoBehaviour
    {
        private GameObject[] unityObstacles;
        private Vector3[] goals;
        private GameObject[] RVOAgents;
        public GameObject prefab;
        private Seeker[] seekers;
        private Path[] paths;
        public float qTimer = 0.0f;
        private float qTime = 3.0f;
        private int[] currentNodeInPath;
        private int agentCount = 20; // Total number of agents
        public Queue<KeyValuePair<int, GameObject>> inQueue;

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

        void UpdateGoalsInQueue(int agentIndex){
            for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
                if (!inQueue.Any(pair => pair.Value == RVOAgents[i])){
                    var agentPos = RVOAgents[agentIndex].transform.position;
                    agentPos.z += 5;
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, agentPos);
                    RVOAgents[i].name = "Agent: " + i + "-- Goal: " + agentPos + "-- AT: " + agentIndex;
                }
            }
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
            Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, 1.0f, 1.0f, new RVO.Vector2(0.0f, 0.0f));

            inQueue = new Queue<KeyValuePair<int, GameObject>>();
            RVOAgents = new GameObject[agentCount];
            goals = new Vector3[agentCount];
            seekers = new Seeker[agentCount];
            paths = new Path[agentCount];
            currentNodeInPath = new int[agentCount];

            // Instantiate agents and set their goals
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 spawnPosition, goalPosition;
                currentNodeInPath[i] = 0;

                if (i < agentCount / 2)
                {
                    // First half of agents
                    spawnPosition = new Vector3(-30 -i,1,7);
                    goalPosition = new Vector3(30,1,-7);
                }
                else
                {
                    // Second half of agents
                    spawnPosition = new Vector3(30+i,1,-7);
                    goalPosition = new Vector3(-30,1,7);
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

        void Update()
        {
            for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
                if (paths[i] == null){
                    continue;
                }
                if (currentNodeInPath[i] >= paths[i].vectorPath.Count & !inQueue.Any(pair => pair.Value == RVOAgents[i])){
                    inQueue.Enqueue(new KeyValuePair<int, GameObject>(i, RVOAgents[i]));
                    UpdateGoalsInQueue(i); 
                    i = 0;
                    qTimeCheck();
                    continue;
                }
                else if (currentNodeInPath[i] >= paths[i].vectorPath.Count){
                    continue; 
                }

                Vector3 currentWaypoint = paths[i].vectorPath[currentNodeInPath[i]];
                Vector3 directionToWaypoint = (currentWaypoint - RVOAgents[i].transform.position).normalized;

                // Vector2 goalVector = toRVOVector(goals[i]) - Simulator.Instance.getAgentPosition(i);

                // Use the direction to the waypoint to set the preferred velocity
                Vector2 goalVector = toRVOVector(directionToWaypoint);

                // Check if close to the current waypoint and increment index
                if (Vector3.Distance(RVOAgents[i].transform.position, currentWaypoint) < 3f)
                {
                    currentNodeInPath[i]++; // Increment the waypoint index for this agent
                }


                if (RVOMath.absSq(goalVector) > 1.0f)
                {
                    goalVector = RVOMath.normalize(goalVector);
                }

                Simulator.Instance.setAgentPrefVelocity(i, goalVector);
                //Debug.Log($"Prefered: {Simulator.Instance.getAgentPrefVelocity(i)}");
                RVOAgents[i].transform.position = toUnityVector(Simulator.Instance.getAgentPosition(i)); 
            }
            Simulator.Instance.doStep();
            if (inQueue.Count > 0){
                if (qTimeCheck()){
                    Debug.Log("Agent: " + inQueue.Peek() + " has been in Queue for 3 seconds");
                    //inQueue.Dequeue;
                }
            }
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


        // void Update()
        //         {
        //             for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
        //                 Vector2 goalVector = toRVOVector(goals[i]) - Simulator.Instance.getAgentPosition(i);

        //                 if (RVOMath.absSq(goalVector) > 1.0f)
        //                 {
        //                     goalVector = RVOMath.normalize(goalVector);
        //                 }

        //                 Simulator.Instance.setAgentPrefVelocity(i, goalVector);
        //                 //Debug.Log($"Prefered: {Simulator.Instance.getAgentPrefVelocity(i)}");
        //                 RVOAgents[i].transform.localPosition = toUnityVector(Simulator.Instance.getAgentPosition(i)); 
        //             }
        //             Simulator.Instance.doStep();
        //       }


    }
}