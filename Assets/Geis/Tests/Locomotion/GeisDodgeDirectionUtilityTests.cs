/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Locomotion;
using NUnit.Framework;
using UnityEngine;

namespace Geis.Tests.Locomotion
{
    public sealed class GeisDodgeDirectionUtilityTests
    {
        private static readonly Vector3 Forward = Vector3.forward;
        private static readonly Vector3 Right = Vector3.right;

        [Test]
        public void WorldDirectionToIndex_Forward_ReturnsZero()
        {
            Assert.AreEqual(0, GeisDodgeDirectionUtility.WorldDirectionToIndex(Forward, Forward, Right));
        }

        [Test]
        public void WorldDirectionToIndex_Back_ReturnsOne()
        {
            Assert.AreEqual(1, GeisDodgeDirectionUtility.WorldDirectionToIndex(-Forward, Forward, Right));
        }

        [Test]
        public void WorldDirectionToIndex_Left_ReturnsTwo()
        {
            Assert.AreEqual(2, GeisDodgeDirectionUtility.WorldDirectionToIndex(-Right, Forward, Right));
        }

        [Test]
        public void WorldDirectionToIndex_Right_ReturnsThree()
        {
            Assert.AreEqual(3, GeisDodgeDirectionUtility.WorldDirectionToIndex(Right, Forward, Right));
        }

        [Test]
        public void WorldDirectionToIndex_NearZero_ReturnsBack()
        {
            Assert.AreEqual(1, GeisDodgeDirectionUtility.WorldDirectionToIndex(Vector3.zero, Forward, Right));
        }
    }
}
