using NUnit.Framework;
using Rustline.Gameplay.Combat;

namespace Rustline.Tests
{
    public sealed class ExpandedProgressTests
    {
        [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void ConfiguredCount_ControlsFinalExtraction(int count)
        {
            var progress = new FirstPlayableProgress(count);
            for (int i = 0; i < count - 1; i++) Assert.That(progress.ClearEncounter(i),Is.True);
            Assert.That(progress.TickExtraction(10f,true,true),Is.False);
            Assert.That(progress.ClearEncounter(count-1),Is.True);
            Assert.That(progress.TickExtraction(2f,true,true),Is.True);
        }

        [Test]
        public void MultipleCheckpoints_CommitPrefixesAndCannotRollbackOrCommitPendingKills()
        {
            var progress = new FirstPlayableProgress(7);
            Assert.That(progress.TryCheckpoint(2,5,true),Is.False);
            progress.ClearEncounter(0); progress.ClearEncounter(1);
            Assert.That(progress.TryCheckpoint(0,2,true),Is.True);
            progress.ClearEncounter(2);
            Assert.That(progress.TryCheckpoint(1,3,true),Is.True);
            Assert.That(progress.TryCheckpoint(0,2,true),Is.False);
            progress.ClearEncounter(3); progress.ClearEncounter(4);
            Assert.That(progress.TryCheckpoint(2,5,false),Is.False);
            progress.Retry();
            Assert.That(progress.ClearedEncounters,Is.EqualTo(3));
            progress.ClearEncounter(3); progress.ClearEncounter(4);
            Assert.That(progress.TryCheckpoint(2,5,true),Is.True);
            progress.ClearEncounter(5);
            progress.Retry();
            Assert.That(progress.ClearedEncounters,Is.EqualTo(5));
            Assert.That(progress.CheckpointIndex,Is.EqualTo(2));
            progress.Restart();
            Assert.That(progress.ClearedEncounters,Is.Zero);
            Assert.That(progress.CommittedEncounters,Is.Zero);
            Assert.That(progress.CheckpointIndex,Is.EqualTo(-1));
        }
    }
}
