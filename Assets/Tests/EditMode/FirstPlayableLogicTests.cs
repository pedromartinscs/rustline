using NUnit.Framework;
using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class FirstPlayableLogicTests
    {
        [Test]
        public void Sentry_AlertsPursuesTelegraphsStrikesOnceAndRecovers()
        {
            var brain = new SentryBrain2D();
            Assert.That(brain.Step(0.02f, true, false), Is.False);
            Assert.That(brain.State, Is.EqualTo(SentryState.Alert));
            brain.Step(SentryBrain2D.AlertDuration, true, false);
            Assert.That(brain.State, Is.EqualTo(SentryState.Pursuit));
            brain.Step(0.02f, true, true);
            Assert.That(brain.State, Is.EqualTo(SentryState.Windup));
            Assert.That(brain.Step(0.1f, false, false), Is.False);
            Assert.That(brain.Step(SentryBrain2D.WindupDuration, false, false), Is.True,
                "A committed attack emits one strike edge even if its target dodges.");
            Assert.That(brain.State, Is.EqualTo(SentryState.Recovery));
            Assert.That(brain.Step(0.1f, true, true), Is.False);
            brain.Step(SentryBrain2D.RecoveryDuration, false, false);
            Assert.That(brain.State, Is.EqualTo(SentryState.Patrol));
        }

        [Test]
        public void Sentry_DeathCancelsWindupAndResetStartsPatrol()
        {
            var brain = new SentryBrain2D();
            brain.Step(0f, true, true);
            brain.Step(1f, true, true);
            brain.Step(0f, true, true);
            Assert.That(brain.State, Is.EqualTo(SentryState.Windup));
            brain.Kill();
            Assert.That(brain.Step(20f, true, true), Is.False);
            Assert.That(brain.State, Is.EqualTo(SentryState.Dead));
            brain.Reset();
            Assert.That(brain.State, Is.EqualTo(SentryState.Patrol));
            Assert.That(brain.Remaining, Is.Zero);
        }

        [Test]
        public void Sentry_LosingDetectionDuringAlertReturnsToPatrol()
        {
            var brain = new SentryBrain2D();
            brain.Step(0f, true, false);
            brain.Step(1f, false, false);
            Assert.That(brain.State, Is.EqualTo(SentryState.Patrol));
        }

        [TestCase(10f, 3f, true)]
        [TestCase(-10f, -3f, true)]
        [TestCase(10.01f, 0f, false)]
        [TestCase(0f, 3.01f, false)]
        public void Sentry_DetectionBounds(float x, float y, bool expected) =>
            Assert.That(Sentry2D.InDetectionWindow(Vector2.zero, new Vector2(x, y)), Is.EqualTo(expected));

        [TestCase(1.8f, 1.5f, 1, true)]
        [TestCase(-1.8f, 0f, -1, true)]
        [TestCase(1.81f, 0f, 1, false)]
        [TestCase(1f, 1.51f, 1, false)]
        [TestCase(-1f, 0f, 1, false)]
        public void Sentry_StrikeRequiresReachHeightAndLockedFacing(float x, float y, int facing, bool expected) =>
            Assert.That(Sentry2D.InStrikeWindow(Vector2.zero, new Vector2(x, y), facing), Is.EqualTo(expected));

        [Test]
        public void Progress_RejectsSkippedOrRepeatedEncountersAndEarlyCheckpoint()
        {
            var run = new FirstPlayableProgress(3);
            Assert.That(run.ClearEncounter(1), Is.False);
            Assert.That(run.TryCheckpoint(0, 2, true), Is.False);
            Assert.That(run.ClearEncounter(0), Is.True);
            Assert.That(run.ClearEncounter(0), Is.False);
            Assert.That(run.TryCheckpoint(0, 2, true), Is.False);
            Assert.That(run.ClearEncounter(1), Is.True);
            Assert.That(run.TryCheckpoint(0, 2, false), Is.False);
            Assert.That(run.TryCheckpoint(0, 2, true), Is.True);
            Assert.That(run.TryCheckpoint(0, 2, true), Is.False);
        }

        [Test]
        public void Progress_RetryRestoresOnlyCommittedClearsAndRestartErasesCheckpoint()
        {
            var run = new FirstPlayableProgress(3);
            run.ClearEncounter(0);
            run.Retry();
            Assert.That(run.ClearedEncounters, Is.Zero);
            run.ClearEncounter(0); run.ClearEncounter(1);
            run.TryCheckpoint(0, 2, true);
            run.ClearEncounter(2);
            run.TickExtraction(1f, true, true);
            run.Retry();
            Assert.That(run.ClearedEncounters, Is.EqualTo(2));
            Assert.That(run.ExtractionTime, Is.Zero);
            Assert.That(run.Retries, Is.EqualTo(2));
            run.Restart();
            Assert.That(run.ClearedEncounters, Is.Zero);
            Assert.That(run.HasCheckpoint, Is.False);
            Assert.That(run.Retries, Is.Zero);
        }

        [Test]
        public void Progress_CompletionRequiresClearAliveUninterruptedHoldAndEmitsOnce()
        {
            var run = new FirstPlayableProgress(3);
            Assert.That(run.TickExtraction(20f, true, true), Is.False);
            for (int i = 0; i < 3; i++) run.ClearEncounter(i);
            Assert.That(run.TickExtraction(1f, true, true), Is.False);
            run.TickExtraction(0f, false, true);
            Assert.That(run.ExtractionTime, Is.Zero);
            run.TickExtraction(1f, true, true);
            run.InterruptExtraction();
            Assert.That(run.ExtractionTime, Is.Zero);
            run.TickExtraction(1f, true, true);
            run.TickExtraction(1f, true, false);
            Assert.That(run.ExtractionTime, Is.Zero);
            Assert.That(run.TickExtraction(2f, true, true), Is.True);
            Assert.That(run.TickExtraction(2f, true, true), Is.False);
            run.Retry();
            Assert.That(run.IsComplete, Is.True);
            run.Restart();
            Assert.That(run.IsComplete, Is.False);
        }
    }
}
