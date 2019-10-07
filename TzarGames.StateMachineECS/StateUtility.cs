using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace TzarGames.StateMachineECS
{
    public struct StateID : IComponentData
    {
        internal int TypeIndex;
    }

    public struct DefaultState : IComponentData
    {
        internal StateID ID;
    }
    
    public struct StateChangeRequest : IComponentData
    {
        internal StateID ID;

        public static StateChangeRequest Create<T>()
        {
            return new StateChangeRequest
            {
                ID = new StateID() { TypeIndex = TypeManager.GetTypeIndex<T>() }
            };
        }

        public static StateChangeRequest Create(int typeIndex)
        {
            return new StateChangeRequest
            {
                ID = new StateID() { TypeIndex = typeIndex }
            };
        }
    }

    public static class StateUtility
	{
		public static bool IsInState<T>(Entity e, EntityManager em)
		{
			if (em.HasComponent<T>(e) == false)
			{
				return false;
			}

			if (em.HasComponent<StateID>(e) == false)
			{
				return false;
			}

			var currentState = em.GetComponentData<StateID>(e);
			var typeId = GetTypeIndex<T>();

			return currentState.TypeIndex == typeId;
		}

		public static bool IsInState(StateID stateId, int stateTypeId)
		{
			return stateId.TypeIndex == stateTypeId;
		}

		public static bool IsInState<T>(Entity e, StateID stateId)
		{
			var typeId = GetTypeIndex<T>();
			return stateId.TypeIndex == typeId;
		}

		public static bool IsInState<T>(Entity e, ComponentDataFromEntity<StateID> em)
		{
			if (em.Exists(e) == false)
			{
				return false;
			}

			var currentState = em[e];
			var typeId = GetTypeIndex<T>();

			return currentState.TypeIndex == typeId;
		}

		public static int GetTypeIndex<T>()
		{
			return TypeManager.GetTypeIndex<T>();
		}
	}
}