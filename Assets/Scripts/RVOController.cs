using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

namespace RVO
{
    public class RVOController : MonoBehaviour
    {
        // Bara debug variabler, deleta dom efter.


        // Dålig lösning, se kommentar under update() för detta
        private bool delayFirstAgent;
        private float delayTimer = 0f;

        // Riktiga variabler
        private List<GameObject> ballotToUpdate;
        private List<GameObject> boothToUpdate;
        private float turninTimer;
        private List<float> boothTimer;
        private List<float> ballotTimer;
        public GameObject[] queueStations;
        private GameObject[] booths;
        private GameObject[] ballots;
        private Dictionary<GameObject, bool> boothsDict = new Dictionary<GameObject, bool>();
        private Dictionary<GameObject, bool> ballotsDict = new Dictionary<GameObject, bool>();
        private Dictionary<GameObject, int> boothAgent = new Dictionary<GameObject, int>();
        private Dictionary<GameObject, int> ballotsAgent = new Dictionary<GameObject, int>();
        private int qStationCount;
        private int boothCount;
        private int ballotCount;
        private bool[] enqueued;
        private int[] currAgentPhase; // 1 = Ingång, 2 = Valsedlar, 3 = Röstbås, 4 = Inlämning, 5 = Utgång
        private GameObject[] unityObstacles;
        private Vector3[] goals;
        private GameObject[] RVOAgents;
        public GameObject prefab;
        private Seeker[] seekers;
        private Path[] paths;
        private KeyValuePair<int, GameObject> agentPair = new KeyValuePair<int, GameObject>();
        private int[] currentNodeInPath;
        private int agentCount = 10; // Total number of agents
        private int agentTotalCount = 20;
        public List<List<KeyValuePair<int, GameObject>>> queuesList;
        public UIManager UIManager;


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
            return new Vector3(param.x(), 1f, param.y());
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
                if (!enqueued[i] & currAgentPhase[i] == phase){
                    var agentPos = RVOAgents[agentIndex].transform.position;

                    agentPos.z += 3;
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, agentPos);
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
            }
        }

        public void InitializeAgents(){
            Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, 0.6f, 1.0f, new RVO.Vector2(0.0f, 0.0f));
            

            // Setup Queues & Timers
            queuesList = new List<List<KeyValuePair<int, GameObject>>>();
            turninTimer = 0f;
            delayFirstAgent = true;
            delayTimer = 0f;
            boothTimer = new List<float>();
            ballotTimer = new List<float>();

            // Setup Stations
            qStationCount = queueStations.Length;
            booths = GameObject.FindGameObjectsWithTag("Röstbås");
            ballots = GameObject.FindGameObjectsWithTag("Valsedlar");
            boothCount = booths.Length;
            ballotCount = ballots.Length;
            boothToUpdate = new List<GameObject>();
            ballotToUpdate = new List<GameObject>();

            //Booths and Ballots will have an accompanying bool value to indicate if the station is vacant or not.
            for (int i = 0; i < boothCount; i++){
                boothsDict.Add(booths[i], true);
                boothAgent.Add(booths[i], -1);
                boothTimer.Add(0f);
            }
            for (int i = 0; i < ballotCount; i++){
                ballotsDict.Add(ballots[i], true);
                ballotsAgent.Add(ballots[i], -1);
                ballotTimer.Add(0f);
            }

            // Setup Agents
            RVOAgents = new GameObject[agentCount];
            goals = new Vector3[agentCount];
            seekers = new Seeker[agentCount];
            paths = new Path[agentCount];
            currentNodeInPath = new int[agentCount];
            currAgentPhase = new int[agentCount];
            enqueued = new bool[agentCount];
            
            
            for (int i = 0; i < qStationCount + ballotCount + boothCount; i++){
                List<KeyValuePair<int, GameObject>> queue = new List<KeyValuePair<int, GameObject>>();
                queuesList.Add(queue);
            }

            // Instantiate agents and set their goals
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 spawnPosition, goalPosition;
                currentNodeInPath[i] = 0;
                currAgentPhase[i] = 1;
                enqueued[i] = false;

                spawnPosition = new Vector3(-31,1,16+i);
                goalPosition = queueStations[0].transform.position;

                GameObject go = GameObject.Instantiate(prefab, spawnPosition, Quaternion.identity) as GameObject;

                RVOAgents[i] = go;
                go.transform.parent = transform;
                goals[i] = goalPosition;
                seekers[i] = RVOAgents[i].AddComponent<Seeker>();
                RVOAgents[i].name = "Agent: " + i; 

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
            
        }

        void Start()
        {
            Simulator.Instance.Clear();
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
            //float currentFrameDuration = Time.deltaTime;
            //Simulator.Instance.setTimeStep(currentFrameDuration);
            for (int i = 0; i < Simulator.Instance.getNumAgents();i++) {
                if (paths[i] == null){
                    continue;
                }
                
                // Om en agent når en station som kan ha köer, sätts agenten i kö och uppdaterar alla andra på väg till målet 
                if (currentNodeInPath[i] >= paths[i].vectorPath.Count & !enqueued[i]){
                    if (currAgentPhase[i] == 1){
                        queuesList[0].Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                        enqueued[i] = true;
                        UpdateGoalsInQueue(i, currAgentPhase[i]); // Uppdatera alla agenter som var påväg till samma mål till att nu ställa sig bakom denna agent
                        StopRVOAgent(i);
                        continue;
                    }
                    if (currAgentPhase[i] == 4){
                        queuesList[3].Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                        enqueued[i] = true;
                        UpdateGoalsInQueue(i, currAgentPhase[i]); // Uppdatera alla agenter som var påväg till samma mål till att nu ställa sig bakom denna agent
                        StopRVOAgent(i);
                        continue;
                    }
                    if (currAgentPhase[i] == 5){
                        if (agentTotalCount - agentCount > 0){
                            agentTotalCount--;
                            //RestartAgent(i);
                        }
                        else{
                            // End Simulation, print results
                            queuesList[4].Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                            enqueued[i] = true;
                            UpdateGoalsInQueue(i, currAgentPhase[i]); // Uppdatera alla agenter som var påväg till samma mål till att nu ställa sig bakom denna agent
                            StopRVOAgent(i);
                            UIManager.PrintResults();
                        }
                        continue;
                    }
                }

                if (currentNodeInPath[i] >= paths[i].vectorPath.Count){
                    // För att kontinuerligt se till att agenter når hela vägen till målet samt ställer sig prydligt bakom varandra.
                    if (currAgentPhase[i] == 1){
                        int indexInQueue = queuesList[0].FindIndex(pair => pair.Key.Equals(i));
                        if (indexInQueue>0){
                            agentPair = queuesList[0][indexInQueue-1];
                            int k = agentPair.Key;
                            Vector3 g = RVOAgents[k].transform.position;
                            g.z += 3;
                            goals[i] = g;
                        }
                        else{
                            goals[i] = queueStations[0].transform.position;
                        }
                    }
                    // För att kontinuerligt se till att agenter når hela vägen till målet samt ställer sig prydligt bakom varandra.
                    else if (currAgentPhase[i] == 4){
                        int indexInQueue = queuesList[3].FindIndex(pair => pair.Key.Equals(i));
                        if (indexInQueue>0){
                            agentPair = queuesList[3][indexInQueue-1];
                            int k = agentPair.Key;
                            Vector3 g = RVOAgents[k].transform.position;
                            g.x -= 2;
                            goals[i] = g;
                        }
                        else{
                            goals[i] = queueStations[1].transform.position;
                        }
                    }
                    // För att kontinuerligt se till att agenter når hela vägen till målet samt ställer sig prydligt bakom varandra.
                    else if (currAgentPhase[i] == 5){
                        int indexInQueue = queuesList[4].FindIndex(pair => pair.Key.Equals(i));
                        if (indexInQueue>0){
                            agentPair = queuesList[4][indexInQueue-1];
                            int k = agentPair.Key;
                            Vector3 g = RVOAgents[k].transform.position;
                            g.z -= 3;
                            goals[i] = g;
                        }
                        else{
                            goals[i] = queueStations[2].transform.position;
                        }
                    }
                    // Endast för att agenten ska finnas med i queuesList. Även om denna fas aldrig har en kö. Underlättar koden för att trigga att en agent lämnar en station.
                    else if (currAgentPhase[i] == 2 & !enqueued[i]){
                        queuesList[1].Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                        enqueued[i] = true;
                    }
                    // Endast för att agenten ska finnas med i queuesList. Även om denna fas aldrig har en kö. Underlättar koden för att trigga att en agent lämnar en station.
                    else if (currAgentPhase[i] == 3 & !enqueued[i]){
                        queuesList[2].Add(new KeyValuePair<int, GameObject>(i, RVOAgents[i])); 
                        enqueued[i] = true;
                    }

                    // Byt till RVO utan A* när agenten är inom 3f av sista noden, för att nå exakt målet.
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

            /**
             * Below is for handling leaving stations 
             */

            if (queuesList[0].Count > 0){
                if (!delayFirstAgent & vacantSpotToVote()){
                    agentPair = queuesList[0][0];
                    queuesList[0].RemoveAt(0);
                    int i = agentPair.Key;
                    enqueued[i] = false;
                    currAgentPhase[i] += 1;
                    Simulator.Instance.setAgentPosition(i, toRVOVector(RVOAgents[i].transform.position));

                    // Move up everyone behind the agent leaving the station
                    if (queuesList[0].Count > 0){
                        agentPair = queuesList[0][0];
                        int j = agentPair.Key;
                        Vector3 agentGoalPos = RVOAgents[i].transform.position;
                        UpdateAgentGoal(j, RVOAgents[j].transform.position, agentGoalPos);
                        Debug.Log("Updated Agent Behind To Move Up: " + RVOAgents[i].name);
                    }

                    GameObject ballot = getVacantBallot(i);
                    Debug.Log("Agent: " + RVOAgents[i].name + " is moving to ballot: " + ballot.name);
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, ballot.transform.position);
                }
            }

            // For handling the ballot stations
            if (queuesList[1].Count > 0)
            {
                foreach (var ballotEntry in ballotsDict)
                {
                    if (!ballotEntry.Value)
                    {
                        int ballotIndex = Array.IndexOf(ballots, ballotEntry.Key); // Assuming ballots is an array of GameObjects
                        if (ballotTimeCheck(ballotIndex))
                        {
                            foreach (var boothEntry in boothsDict)
                            {
                                // Check if the booth is vacant
                                if (boothEntry.Value)
                                {
                                    // Flag ballot and booth to update after iteration (dicts arent allowed to be updated during loops)
                                    ballotToUpdate.Add(ballotEntry.Key);
                                    boothToUpdate.Add(boothEntry.Key);

                                    // Reset the timer for the ballot
                                    ballotTimer[ballotIndex] = 0f;

                                    // Get the GameObject of the booth
                                    GameObject booth = boothEntry.Key;

                                    // Get index of agent
                                    int agentIndex = ballotsAgent[ballotEntry.Key];
                                    ballotsAgent[ballotEntry.Key] = -1;

                                    // Update boothAgent dictionary
                                    boothAgent[booth] = agentIndex;

                                    Debug.Log("QError: AgentIndex is xxx: " + agentIndex);
                                    // Remove the agent from the queue and update their status
                                    int indexInQueue = queuesList[1].FindIndex(pair => pair.Key.Equals(agentIndex));
                                    Debug.Log("QError: IndexInQueue: " + indexInQueue);
                                    queuesList[1].RemoveAt(indexInQueue);
                                    DebugPrintList();
                                    enqueued[agentIndex] = false;
                                    currAgentPhase[agentIndex] += 1;

                                    // Update the agent's position and goal
                                    Simulator.Instance.setAgentPosition(agentIndex, toRVOVector(RVOAgents[agentIndex].transform.position));
                                    UpdateAgentGoal(agentIndex, RVOAgents[agentIndex].transform.position, booth.transform.position);
                                    Debug.Log("Agent: " + RVOAgents[agentIndex].name + " is triggered to go to booth: "+ booth.name +" by ballot index: " + ballotIndex);
                                    break;
                                }
                            }
                        }
                    }
                    if (boothToUpdate.Count > 0)
                    {
                        for (int i = boothToUpdate.Count - 1; i >= 0; i--)
                        {
                            GameObject boothEntry = boothToUpdate[i];
                            boothsDict[boothEntry] = false;

                            // Remove the entry from the list
                            boothToUpdate.RemoveAt(i);
                        }
                    }
                }
                // Update the ballot flagged to be updated.
                if (ballotToUpdate.Count > 0)
                {
                    for (int i = ballotToUpdate.Count - 1; i >= 0; i--)
                    {
                        GameObject ballotEntry = ballotToUpdate[i];
                        ballotsDict[ballotEntry] = true;

                        // Remove the entry from the list
                        ballotToUpdate.RemoveAt(i);
                    }
                }
            }

            // For handling the booth stations
            if (queuesList[2].Count > 0) {
                // Now send it to the Turn-in
                foreach (var boothEntry in boothsDict)
                {
                    if (!boothEntry.Value){
                        int boothIndex = Array.IndexOf(booths, boothEntry.Key); // Assuming ballots is an array of GameObjects
                        if (boothTimeCheck(boothIndex)){
                            Debug.Log("Booth number: " + boothIndex + " will now be vacant");
                            boothTimer[boothIndex] = 0f;
                            boothToUpdate.Add(boothEntry.Key);
                            int agentIndex = boothAgent[boothEntry.Key];
                            boothAgent[boothEntry.Key] = -1;
                            int indexInQueue = queuesList[2].FindIndex(pair => pair.Key.Equals(agentIndex));
                            queuesList[2].RemoveAt(indexInQueue);
                            enqueued[agentIndex] = false;
                            currAgentPhase[agentIndex] += 1;
                            if (queuesList[3].Count > 0){ // If there is someone at the Turn-in already
                                Simulator.Instance.setAgentPosition(agentIndex, toRVOVector(RVOAgents[agentIndex].transform.position));
                                int nextAgent = queuesList[3][queuesList[3].Count-1].Key;
                                Vector3 nextAgentPos = RVOAgents[nextAgent].transform.position;
                                nextAgentPos.z += 3;
                                UpdateAgentGoal(agentIndex, RVOAgents[agentIndex].transform.position, nextAgentPos);
                            }

                            else{ // If Turn-ins empty
                                UpdateAgentGoal(agentIndex, RVOAgents[agentIndex].transform.position, queueStations[1].transform.position);
                            }
                        }
                    }
                }
                if (boothToUpdate.Count > 0)
                {
                    for (int i = boothToUpdate.Count - 1; i >= 0; i--)
                    {
                        GameObject boothEntry = boothToUpdate[i];
                        boothsDict[boothEntry] = true;

                        // Remove the entry from the list
                        boothToUpdate.RemoveAt(i);
                    }
                }
            }

            // For handling the turn-in station 
            if (queuesList[3].Count > 0){
                if (turninTimerCheck()){
                    Debug.Log("Left the Station");
                    agentPair = queuesList[3][0];           
                    queuesList[3].RemoveAt(0); 
                    int i = agentPair.Key;
                    enqueued[i] = false;
                    currAgentPhase[i] += 1;
        
                    // Move up everyone behind the agent leaving the station
                    if (queuesList[3].Count > 0){
                        agentPair = queuesList[3][0];
                        int j = agentPair.Key;
                        Vector3 agentGoalPos = RVOAgents[i].transform.position;
                        UpdateAgentGoal(j, RVOAgents[j].transform.position, agentGoalPos);
                    }
                    

                    Simulator.Instance.setAgentPosition(i, toRVOVector(RVOAgents[i].transform.position));
                    UpdateAgentGoal(i, RVOAgents[i].transform.position, queueStations[2].transform.position);
                    
                    for (int k = 0; k < Simulator.Instance.getNumAgents(); k++){
                        if (currAgentPhase[k] == 4 & !boothAgent.ContainsValue(k)){
                            UpdateAgentGoal(k, RVOAgents[k].transform.position, queueStations[1].transform.position); // Uppdatera alla agenter som var påväg till att ställa sig bakom denna agent att nu ställa sig längst fram
                        }
                    }
                }
            }

            // Superful lösning för att få första agenten att inte gå direkt in i rummet utan vänta 1 sekund så att det inte blir bug i startkön. 
            // Radera delayFirstAgent, delayTimer, qFirstAgentDelay() och nästlade if-satsen nedan vid bättre lösning.
            if (queuesList[0].Count > 1){
                if (qFirstAgentDelay()){
                    delayFirstAgent = false;
                }
            }
            Simulator.Instance.doStep();
        }


        public void ResetSimulation(){
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
        private GameObject getVacantBallot(int i){
            foreach (var ballotEntry in ballotsDict)
            {
                if (ballotEntry.Value) // Check if the ballot is vacant
                {
                    // to keep track of which agent is using which ballot
                    ballotsAgent[ballotEntry.Key] = i;

                    // Mark the ballot as no longer vacant
                    ballotsDict[ballotEntry.Key] = false;

                    // Return the GameObject of the ballot
                    return ballotEntry.Key;
                }
            }

            return null; 
        }
        private GameObject getVacantBooth(int i)
        {
            foreach (var boothEntry in boothsDict)
            {
                if (boothEntry.Value) // Check if the booth is vacant
                {
                    // to keep track of which agent is using which booth
                    boothAgent[boothEntry.Key] = i;

                    // Mark the booth as no longer vacant
                    boothsDict[boothEntry.Key] = false;

                    // Return the GameObject of the booth
                    return boothEntry.Key;
                }
            }

            // Return null if no vacant booth is found
            return null;
        }

        private bool vacantSpotToVote()
        {
            // Check if there is at least one vacant ballot
            bool isVacantBallot = ballotsDict.Values.Any(vacant => vacant);

            // Check if there is at least one vacant booth
            bool isVacantBooth = boothsDict.Values.Any(vacant => vacant);

            // Return true if both a vacant ballot and a vacant booth are found
            Debug.Log("Vacant Status-- Ballot: " + isVacantBallot + " Booth: " + isVacantBooth);
            return isVacantBallot && isVacantBooth;
        }

        public void UpdateStationStatus(GameObject station, bool isVacant)
        {
            if (boothsDict.ContainsKey(station))
            {
                boothsDict[station] = isVacant;
            }

            if (ballotsDict.ContainsKey(station))
            {
                ballotsDict[station] = isVacant;
            }
        }


        private bool qFirstAgentDelay(){
            delayTimer += Time.deltaTime;
            if (delayTimer > 1){
                return true;
            }
            return false;
        }

        private bool turninTimerCheck(){
            turninTimer += Time.deltaTime;
            if (turninTimer > 2){
                turninTimer = 0f;
                return true;
            }
            return false;
        }
    
        private bool ballotTimeCheck(int i){
            ballotTimer[i] += Time.deltaTime;
            if (ballotTimer[i] > 2){
                return true;
            }
            return false;
        }

        private bool boothTimeCheck(int i){
            boothTimer[i] += Time.deltaTime;
            if (boothTimer[i] > 5){
                boothTimer[i] = 0;

                return true;
            }
            return false;
        }

        private void DebugPrintList(){
            foreach (var kv in queuesList[1]){
                Debug.Log("QError: Has key: " + kv.Key + " and value: " + kv.Value);
            }
        }

        // private void RestartAgent(int i){
        //     goals[i] = queueStations[0].transform.position;
        //     currentNodeInPath[i] = 0;
        //     currAgentPhase[i] = 1;
        //     enqueued[i] = false;
        //     Vector3 spawnPosition = new Vector3(-31,1,16+agentCount);
        //     RVOAgents[i].transform.position = spawnPosition;
        //     Simulator.Instance.setAgentPosition(i, toRVOVector(spawnPosition));
        //     UpdateSeekers(spawnPosition, goals[i], i);
        // }
        

    }
}