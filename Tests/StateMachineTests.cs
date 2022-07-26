using NUnit.Framework;
using UnityEngine;
using Unity.Entities;

namespace TzarGames.StateMachineECS.Tests
{
    public class StateMachineTests
    {
		struct CharacterSystemTag : IComponentData
        {
        }

		[DisableAutoCreation]
		class CharacterSystem : StateSystem<CharacterSystemTag>
		{
            protected override void OnCreate()
			{
                base.OnCreate();

				RegisterState<DeadState>();
				RegisterState<AliveState>();
			}

            public class CharacterBaseState : State
			{
				public override void OnEnter(Entity entity)
				{
					base.OnEnter(entity);
					Debug.LogFormat("{0} entered to state {1}", entity.ToString(), GetType().Name);
				}

				public override void OnExit(Entity entity)
				{
					base.OnExit(entity);
					Debug.LogFormat("{0} exit from state {1}", entity.ToString(), GetType().Name);
				}

				public override void OnUpdate(Entity entity)
				{
					base.OnUpdate(entity);
					Debug.LogFormat("{0} Update state {1}", entity.ToString(), GetType().Name);
				}
			}

            public class DeadState : CharacterBaseState
			{
			}

            public class AliveState : CharacterBaseState
			{
			}
		}

		class TestCommandBufferSystem : EntityCommandBufferSystem
        {
        }

		[Test]
		public void TestStateChange()
		{
			var world = new World("Test world");
			var em = world.EntityManager;

			var commandBufferSystem = world.CreateSystem<TestCommandBufferSystem>();
			var system = world.CreateSystem<CharacterSystem>();

			var obj = em.CreateEntity(typeof(StateID), typeof(CharacterSystemTag));
			var obj2 = em.CreateEntity(typeof(StateID), typeof(CharacterSystemTag));

			system.RequestStateChange<CharacterSystem.DeadState>(obj);
			system.RequestStateChange<CharacterSystem.AliveState>(obj2);

            system.Update();
			commandBufferSystem.Update();

			Assert.IsTrue(system.IsInState<CharacterSystem.DeadState>(obj));
			Assert.IsTrue(system.IsInState<CharacterSystem.AliveState>(obj2));

			system.RequestStateChange<CharacterSystem.AliveState>(obj);
			system.RequestStateChange<CharacterSystem.DeadState>(obj2);

			system.Update();
			commandBufferSystem.Update();

			Assert.IsTrue(system.IsInState<CharacterSystem.AliveState>(obj));
            Assert.IsTrue(system.IsInState<CharacterSystem.DeadState>(obj2));
        }
	}
}
