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
    public struct GotoState : IComponentData
    {
        internal StateID ID;

        public static GotoState Create<T>()
        {
            return new GotoState
            {
                ID = new StateID() { TypeIndex = TypeManager.GetTypeIndex<T>() }
            };
        }

        public static GotoState Create(int typeIndex)
        {
            return new GotoState
            {
                ID = new StateID() { TypeIndex = typeIndex }
            };
        }
    }

    public struct StateID : IComponentData
    {
        internal int TypeIndex;
    }

    public struct DefaultState : IComponentData
    {
        internal StateID ID;
    }

    public abstract class StateSystem : ComponentSystem
    {
        List<IState> states;

        protected override void OnCreate()
        {
            base.OnCreate();
            states = new List<IState>();
        }

        protected override void OnUpdate()
        {
            var commands = PostUpdateCommands;

            Entities.WithNone<StateID>().ForEach((Entity entity, ref GotoState stateChangeRequest) =>
            {
                EntityManager.AddComponentData(entity, new StateID { TypeIndex = -1 });
            });

            for (int i = 0; i < states.Count; i++)
            {
                IState state = states[i];
                state.UpdateOnExit(this);
                state.UpdateOnEnter(this);
                state.Update(this);
            }

            Entities.WithAllReadOnly<StateID, GotoState>().ForEach((Entity entity) =>
            {
                commands.RemoveComponent<GotoState>(entity);
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

        public bool IsInState<T>(Entity e)
        {
            return IsInState<T>(e, EntityManager);
        }

        private static int GetTypeIndex<T>()
        {
            return TypeManager.GetTypeIndex<T>();
        }

        protected class ComponentDataState<T> : IState where T : struct, IComponentData
        {
            int typeIndex;

            public ComponentDataState()
            {
                typeIndex = GetTypeIndex<T>();
            }

            public void UpdateOnEnter(StateSystem componentSystem)
            {
                var em = componentSystem.EntityManager;

                componentSystem.Entities.WithNone<T>().ForEach((Entity entity, ref StateID stateId, ref GotoState changeRequest) =>
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

                componentSystem.Entities.ForEach((Entity entity, ref T state, ref StateID stateID, ref GotoState changeRequest) =>
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
                    OnEnter(ref state);
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

                    OnUpdate(ref state);
                });
            }

            public void UpdateOnExit(StateSystem componentSystem)
            {
                componentSystem.Entities.ForEach((Entity entity, ref T state, ref StateID stateID, ref GotoState changeRequest) =>
                {
                    if (changeRequest.ID.TypeIndex == stateID.TypeIndex)
                    {
                        return;
                    }

                    if (changeRequest.ID.TypeIndex == typeIndex)
                    {
                        return;
                    }

                    OnExit(ref state);
                });
            }

            public virtual T CreateDefaultData()
            {
                return new T();
            }

            public virtual void OnEnter(ref T state)
            {
                UnityEngine.Debug.LogFormat("Entered to state {0}", state.GetType().Name);
            }

            public virtual void OnExit(ref T state)
            {
                UnityEngine.Debug.LogFormat("Exit from state {0}", state.GetType().Name);
            }

            public virtual void OnUpdate(ref T state)
            {
                UnityEngine.Debug.LogFormat("Updating state {0}", state.GetType().Name);
            }
        }
    }
}