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
        List<StateBase> states = new List<StateBase>();
        EntityQuery requestsWithoutState;
        EntityQuery requestsWithState;

        protected override void OnCreate()
        {
            base.OnCreate();

            requestsWithoutState = GetEntityQuery(ComponentType.ReadOnly<StateChangeRequest>(), ComponentType.Exclude<StateID>());
            requestsWithState = GetEntityQuery(ComponentType.ReadOnly<StateChangeRequest>(), ComponentType.ReadOnly<StateID>());
        }

        protected override void OnUpdate()
        {
            var commands = PostUpdateCommands;
            var em = EntityManager;
            var reqEntities = requestsWithoutState.ToEntityArray(Allocator.TempJob);

            for(int i=0; i<reqEntities.Length; i++)
            {
                var entity = reqEntities[i];
                commands.AddComponent(entity, new StateID { TypeIndex = -1 });
            }
            reqEntities.Dispose();

            Entities.ForEach((Entity entity, ref StateChangeRequest changeRequest, ref StateID stateId) =>
            {
                for(int i=0; i<states.Count; i++)
                {
                    var state = states[i];
                    var stateComponentType = state.GetComponentType();
                    var id = state.StateID;

                    if (em.HasComponent(entity, stateComponentType))
                    {
                        continue;
                    }

                    if (changeRequest.ID.TypeIndex != id)
                    {
                        continue;
                    }
                    
                    if (stateId.TypeIndex == id)
                    {
                        continue;
                    }

                    em.AddComponent(entity, stateComponentType);
                }
            });
            
            updateStates();

            var requestWithStateEntities = requestsWithState.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < requestWithStateEntities.Length; i++)
            {
                var entity = requestWithStateEntities[i];
                commands.RemoveComponent<StateChangeRequest>(entity);
            }

            requestWithStateEntities.Dispose();
        }

        void updateStates()
        {
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                state.OnBeforeUpdate();
            }

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
        }

        protected void RegisterState<T>() where T : StateBase, new()
        {
            var state = new T();
            RegisterState(state);
        }

        protected void RegisterState(StateBase state)
        {
            var stateType = state.GetType();
            bool replaced = false;

            for (int i = 0; i < states.Count; i++)
            {
                var otherState = states[i];
                var otherType = otherState.GetType();

                if(stateType.IsSubclassOf(otherType))
                {
                    states[i] = state;
                    replaced = true;
                }
            }

            state.Initialize(this);

            if(replaced == false)
            {
                states.Add(state);
            }
        }

        protected abstract class StateBase
        {
            public StateSystem System { get; protected set; }
            public abstract int StateID { get; }
            public abstract ComponentType GetComponentType();
            public abstract void Initialize(StateSystem system);
            public abstract void OnBeforeUpdate();
            public abstract void UpdateOnEnter(StateSystem componentSystem);
            public abstract void Update(StateSystem componentSystem);
            public abstract void UpdateOnExit(StateSystem componentSystem);
        }

		public bool IsInState<T>(Entity e)
        {
            return StateUtility.IsInState<T>(e, EntityManager);
        }

        protected class State<T> : StateBase where T : struct, IComponentData
        {
            public override int StateID
            {
                get
                {
                    return typeIndex;
                }
            }

            int typeIndex;
            ComponentType componentType;

            protected EntityManager EntityManager;

            public State()
            {
                typeIndex = StateUtility.GetTypeIndex<T>();
            }

            public override void Initialize(StateSystem system)
            {
                System = system;
                EntityManager = System.EntityManager;
                componentType = new ComponentType(typeof(T));
            }

            public override ComponentType GetComponentType()
            {
                return componentType;
            }

            public void RequestStateChange<K>(Entity entity) where K : IComponentData
            {
                if(System.EntityManager.HasComponent<StateChangeRequest>(entity))
                {
                    System.EntityManager.SetComponentData(entity, StateChangeRequest.Create<K>());
                    return;
                }

                System.PostUpdateCommands.AddComponent(entity, StateChangeRequest.Create<K>());
            }

            public void RequestStateChange<K>(Entity entity, K data) where K : struct, IComponentData
            {
                if(System.EntityManager.HasComponent<K>(entity))
                {
                    System.EntityManager.SetComponentData(entity, data);
                }
                else
                {
                    System.PostUpdateCommands.AddComponent(entity, data);
                }

                RequestStateChange<K>(entity);
            }

            public override void UpdateOnEnter(StateSystem componentSystem)
            {
                var em = componentSystem.EntityManager;
                
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

            public override void Update(StateSystem componentSystem)
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

            public override void UpdateOnExit(StateSystem componentSystem)
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

            public override void OnBeforeUpdate()
            {
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