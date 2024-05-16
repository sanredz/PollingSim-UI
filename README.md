# PollingSim-UI
UI for simulating the flow of pedestrians at polling stations. Uses RVO2 for Collision Avoidance and A* for pathfinding. Custom (trivial and rigid) queueing algorithm implemented.

# Metrics
Measures:
- **Voter Flow Rate:** Number of agents who go through, and finish, the process of voting each minute.
- **Privacy Infringements:** Number of times other agents pass by behind an agent occupying either a ballot stand or a voting booth.
- **Halt Ratio:** Percentage of total simulation time during which entry to the polling station is paused
- **Empty Turn-in Ratio:** Percentage of total simulation time during which Turning-in Desk is unoccupied
