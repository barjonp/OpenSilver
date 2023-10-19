﻿
/*===================================================================================
*
*   Copyright (c) Userware/OpenSilver.net
*
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*
\*====================================================================================*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers.Tests
{
    [TestClass]
    public class ScrollBarAutomationPeerTest
    {
        [TestMethod]
        public void GetClickablePoint()
        {
            var peer = new ScrollBarAutomationPeer(new ScrollBar());
            var point = peer.GetClickablePoint();
            double.IsNaN(point.X).Should().BeTrue();
            double.IsNaN(point.Y).Should().BeTrue();
        }

        [TestMethod]
        public void GetOrientation()
        {
            var scrollbar = new ScrollBar();
            var peer = new ScrollBarAutomationPeer(scrollbar);

            scrollbar.Orientation = Orientation.Horizontal;
            peer.GetOrientation().Should().Be(AutomationOrientation.Horizontal);
            scrollbar.Orientation = Orientation.Vertical;
            peer.GetOrientation().Should().Be(AutomationOrientation.Vertical);
        }
    }
}
