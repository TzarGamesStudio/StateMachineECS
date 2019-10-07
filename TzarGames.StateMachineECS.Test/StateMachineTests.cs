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

        [Test]
        public void TestStateChange()
        {
            var em = World.Active.EntityManager;

            var obj = em.CreateEntity();
            em.AddComponentData(obj, GotoState.Create<Dead>());

            World.Active.Update();

            Assert.IsTrue(em.HasComponent<Dead>(obj));
        }

        //// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        //// `yield return null;` to skip a frame.
        //[UnityTest]
        //public IEnumerator TessRoutine
        //{
        //    // Use the Assert class to test conditions.
        //    // Use yield to skip a frame.
        //    yield return null;
        //}
    }
}
