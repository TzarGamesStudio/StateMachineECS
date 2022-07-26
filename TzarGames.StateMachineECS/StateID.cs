using Unity.Entities;

namespace TzarGames.StateMachineECS
{
	[System.Serializable]
    public struct StateID : IComponentData
    {
        internal int currentStateID;
		internal int stateIdToSwitch;
		public void RequestSwitchToState(int stateID)
        {
			stateIdToSwitch = stateID;
        }
		public int StateIdToSwitch => stateIdToSwitch;
	}
}