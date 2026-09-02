using System;
using NUnit.Framework;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class ZoneClassifierTests
    {
        private ZoneClassifier _classifier;

        [SetUp]
        public void SetUp() => _classifier = new ZoneClassifier();

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(29)]
        [TestCase(31)]
        public void RiskyZones_AreNormal(int zone) =>
            Assert.That(_classifier.Classify(zone), Is.EqualTo(ZoneType.Normal));

        /// <summary>The opening zone is always safe, so a run can never end on the very first spin.</summary>
        [Test]
        public void FirstZone_IsSafe() =>
            Assert.That(_classifier.Classify(1), Is.EqualTo(ZoneType.Safe));

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(25)]
        [TestCase(35)]
        [TestCase(55)]
        public void EveryFifthZone_IsSafe(int zone) =>
            Assert.That(_classifier.Classify(zone), Is.EqualTo(ZoneType.Safe));

        /// <summary>
        /// The precedence rule. Zone 30 satisfies both intervals; Super must win, because it is a strict
        /// superset of Safe and resolving the overlap the other way would cost the player the special pool.
        /// </summary>
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(90)]
        [TestCase(300)]
        public void EveryThirtiethZone_IsSuper_NotSafe(int zone) =>
            Assert.That(_classifier.Classify(zone), Is.EqualTo(ZoneType.Super));

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NonPositiveZone_Throws(int zone) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _classifier.Classify(zone));

        /// <summary>
        /// The milestone-badge bug: once past zone 25, the "next safe zone" badge was showing 30 — but 30
        /// is the Golden Super zone, not a regular Safe zone, so the badge must read 35.
        /// </summary>
        [Test]
        public void NextZoneOfType_FromZone26_ResolvesSafe35AndSuper30()
        {
            Assert.That(_classifier.NextZoneOfType(26, ZoneType.Safe), Is.EqualTo(35));
            Assert.That(_classifier.NextZoneOfType(26, ZoneType.Super), Is.EqualTo(30));
        }

        [TestCase(1, 5)]
        [TestCase(4, 5)]
        [TestCase(5, 10)]
        [TestCase(9, 10)]
        [TestCase(25, 35)]   // 30 is Super — the next regular Safe zone is 35
        [TestCase(29, 35)]
        [TestCase(30, 35)]
        [TestCase(31, 35)]
        [TestCase(35, 40)]
        [TestCase(55, 65)]   // 60 is Super
        public void NextZoneOfType_Safe_StepsOverSuperZones(int fromZone, int expected) =>
            Assert.That(_classifier.NextZoneOfType(fromZone, ZoneType.Safe), Is.EqualTo(expected));

        [TestCase(1, 30)]
        [TestCase(26, 30)]
        [TestCase(29, 30)]
        [TestCase(30, 60)]
        [TestCase(31, 60)]
        [TestCase(59, 60)]
        [TestCase(60, 90)]
        public void NextZoneOfType_Super_IsTheNextSuperInterval(int fromZone, int expected) =>
            Assert.That(_classifier.NextZoneOfType(fromZone, ZoneType.Super), Is.EqualTo(expected));

        [Test]
        public void NextZoneOfType_Normal_IsTheImmediateNextRiskyZone() =>
            Assert.That(_classifier.NextZoneOfType(5, ZoneType.Normal), Is.EqualTo(6));

        [Test]
        public void NextZoneOfType_ClampsANonPositiveStart_ThenZoneOneIsTheFirstSafe() =>
            Assert.That(_classifier.NextZoneOfType(-4, ZoneType.Safe), Is.EqualTo(1));

        [Test]
        public void DefaultIntervals_AreConsistent() =>
            Assert.That(_classifier.IntervalsAreConsistent, Is.True);

        [Test]
        public void SuperIntervalNotMultipleOfSafe_IsReportedInconsistent() =>
            Assert.That(new ZoneClassifier(4, 30).IntervalsAreConsistent, Is.False);

        [TestCase(0)]
        [TestCase(-3)]
        public void NonPositiveInterval_Throws(int interval)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneClassifier(interval, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneClassifier(5, interval));
        }
    }
}
