using System.Collections.Generic;
using NUnit.Framework;

namespace CircleWar.Tests
{
    public sealed class ResourceGainFeedbackTests
    {
        [Test]
        public void GrantsNotifyActualResourceAndAmount()
        {
            GameState state = new GameState();
            var gains = new List<string>();
            state.ResourceGained += (id, amount) => gains.Add(id + ":" + amount);
            state.AddResource("food", 15);
            state.AddResource("industry", 5);
            CollectionAssert.AreEqual(new[] { "food:15", "industry:5" }, gains);
            Assert.AreEqual(15, state.GetResourceAmount("food"));
            Assert.AreEqual(5, state.GetResourceAmount("industry"));
        }

        [Test]
        public void InitializationSpendingAndResetDoNotAnnounceRewards()
        {
            GameState state = new GameState();
            int notifications = 0;
            state.ResourceGained += (_, __) => notifications++;
            state.SetResourceAmount("industry", 100);
            state.AddResource("industry", -1);
            state.AddResource("industry", 0);
            state.AddResource("industry", -200);
            state.StartNewRun();
            Assert.AreEqual(0, notifications);
            state.AddResource("industry", 3);
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(3, state.GetResourceAmount("industry"));
        }
    }
}
