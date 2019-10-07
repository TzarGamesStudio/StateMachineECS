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
    public abstract class StateSystem : ComponentSystem
    {
        List<IState> states = new List<IState>();

        protected override void OnUpdate()
        {
            var commands = PostUpdateCommands;

            Entities.WithNone<StateID>().ForEach((Entity entity, ref StateChangeRequest stateChangeRequest) =>
            {
                EntityManager.AddComponentData(entity, new StateID { TypeIndex = -1 });
            });

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                state.UpdateOnExit(this);
            }

			for (int i = 0; i < states.Count; i++)
			{
				var state = states[i];
				state.UpdateOnEnter(this);
			}

			for (int i = 0; i < states.Count; i++)
			{
				var state = states[i];
				state.Update(this);
			}

			Entities.WithAllReadOnly<StateID, StateChangeRequest>().ForEach((Entity entity) =>
            {
                commands.RemoveComponent<StateChangeRequest>(entity);
            });
        }

        protected void RegisterState<T>() where T : IState, new()
        {
            var state = new T();
            states.Add(state);
        }

        protected void RegisterState(IState state)
        {
            states.Add(state);
        }

        protected interface IState
        {
            void UpdateOnEnter(StateSystem componentSystem);
            void Update(StateSystem componentSystem);
            void UpdateOnExit(StateSystem componentSystem);
        }

		public bool IsInState<T>(Entity e)
        {
            return StateUtility.IsInState<T>(e, EntityManager);
        }

        protected class ComponentDataState<T> : IState where T : struct, IComponentData
        {
            int typeIndex;

            public ComponentDataState()
            {
                typeIndex = StateUtility.GetTypeIndex<T>();
            }

            public void UpdateOnEnter(StateSystem componentSystem)
            {
                var em = componentSystem.EntityManager;

                componentSystem.Entities.WithNone<T>().ForEach((Entity entity, ref StateID stateId, ref StateChangeRequest changeRequest) =>
                {
                    if (changeRequest.ID.TypeIndex != typeIndex)
                    {
                        return;
                    }

                    if (stateId.TypeIndex == typeIndex)
                    {
                        return;
                    }

                    em.AddComponentData(entity, CreateDefaultData());
                });

                componentSystem.Entities.ForEach((Entity entity, ref T state, ref StateID stateID, ref StateChangeRequest changeRequest) =>
                {
                    if (changeRequest.ID.TypeIndex == stateID.TypeIndex)
                    {
                        return;
                    }

                    if (changeRequest.ID.TypeIndex != typeIndex)
                    {
                        return;
                    }

                    stateID.TypeIndex = typeIndex;
                    OnEnter(entity, ref state);
                });
            }

            public void Update(StateSystem componentSystem)
            {
                var em = componentSystem.EntityManager;

                componentSystem.Entities.ForEach((Entity entity, ref T state, ref StateID stateID) =>
                {
                    if (stateID.TypeIndex != typeIndex)
                    {
                        return;
                    }

                    OnUpdate(entity, ref state);
                });
            }

            public void UpdateOnExit(StateSystem componentSystem)
            {
                componentSystem.Entities.ForEach((Entity entity, ref T state, ref StateID stateID, ref StateChangeRequest changeRequest) =>
                {
                    if (changeRequest.ID.TypeIndex == stateID.TypeIndex)
                    {
                        return;
                    }

                    if (changeRequest.ID.TypeIndex == typeIndex)
                    {
                        return;
                    }

                    OnExit(entity, ref state);
                });
            }

            public virtual T CreateDefaultData()
            {
                return new T();
            }

            public virtual void OnEnter(Entity entity, ref T state)
            {
            }

            public virtual void OnExit(Entity entity, ref T state)
            {
            }

            public virtual void OnUpdate(Entity entity, ref T state)
            {
            }
        }
    }
}