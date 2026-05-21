/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Locomotion;
using NUnit.Framework;

namespace Geis.Tests.Locomotion
{
    public sealed class GeisInputBufferUtilityTests
    {
        [Test]
        public void IsFresh_ReturnsFalse_WhenBufferNeverSet()
        {
            Assert.IsFalse(GeisInputBufferUtility.IsFresh(-1f, 0.18f, 10f));
        }

        [Test]
        public void IsFresh_ReturnsTrue_WithinWindow()
        {
            Assert.IsTrue(GeisInputBufferUtility.IsFresh(10f, 0.18f, 10.1f));
        }

        [Test]
        public void IsFresh_ReturnsFalse_AfterWindowExpires()
        {
            Assert.IsFalse(GeisInputBufferUtility.IsFresh(10f, 0.18f, 10.19f));
        }

        [Test]
        public void IsFresh_ReturnsFalse_WhenWindowDisabled()
        {
            Assert.IsFalse(GeisInputBufferUtility.IsFresh(10f, 0f, 10.05f));
        }
    }
}
