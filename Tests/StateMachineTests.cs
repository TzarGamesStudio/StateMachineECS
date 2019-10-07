using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Entities;

namespace TzarGames.StateMachineECS.Tests
{
    public class StateMachineTests
    {
        struct Dead : IComponentData
        {
        }

        struct Alive : IComponentData
        {
        }

		[DisableAutoCreation]
		class CharacterSystem : StateSystem
		{
            protected override void OnCreate()
			{
				RegisterState<DeadState>();
				RegisterState<AliveState>();
			}

            class BaseState<T> : ComponentDataState<T> where T : struct, IComponentData
			{
				public override void OnEnter(Entity entity, ref T state)
				{
					base.OnEnter(entity, ref state);
					Debug.LogFormat("{0} entered to state {1}", entity.ToString(), state.GetType().Name);
				}

				public override void OnExit(Entity entity, ref T state)
				{
					base.OnExit(entity, ref state);
					Debug.LogFormat("{0} exit from state {1}", entity.ToString(), state.GetType().Name);
				}

				public override void OnUpdate(Entity entity, ref T state)
				{
					base.OnUpdate(entity, ref state);
					Debug.LogFormat("{0} Update state {1}", entity.ToString(), state.GetType().Name);
				}
			}

            class DeadState : BaseState<Dead>
			{
			}

            class AliveState : BaseState<Alive>
			{
			}
		}

		[Test]
		public void TestStateChange()
		{
			var world = new World("Test world");
			var em = world.EntityManager;

			var system = world.CreateSystem<CharacterSystem>();

			var obj = em.CreateEntity();
			var obj2 = em.CreateEntity();

            em.AddComponentData(obj, StateChangeRequest.Create<Dead>());
			em.AddComponentData(obj2, StateChangeRequest.Create<Alive>());
			system.Update();
			
            Assert.IsTrue(em.HasComponent<Dead>(obj));
			Assert.IsTrue(em.HasComponent<Alive>(obj2));

			em.AddComponentData(obj, StateChangeRequest.Create<Alive>());
			em.AddComponentData(obj2, StateChangeRequest.Create<Dead>());
			system.Update();

			Assert.IsTrue(em.HasComponent<Alive>(obj));
			Assert.IsTrue(em.HasComponent<Dead>(obj2));
		}
	}
}
