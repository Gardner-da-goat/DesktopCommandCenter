using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Core.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopCommandCenter.Tests;

[TestClass]
public sealed class SidebarStateTests
{
    [TestMethod]
    public void ExpandStateUsesExpandedWidth()
    {
        var state = new SidebarState(new AppSettings { StartCollapsed = true, SidebarWidth = 400 });
        state.Expand();

        Assert.IsTrue(state.IsExpanded);
        Assert.AreEqual(400d, state.CurrentWidth);
    }

    [TestMethod]
    public void CollapseStateUsesHandleWidth()
    {
        var state = new SidebarState(new AppSettings { StartCollapsed = false });
        state.Collapse();

        Assert.IsFalse(state.IsExpanded);
        Assert.AreEqual(34d, state.CurrentWidth);
    }

    [TestMethod]
    public void WidthChangeIsRetainedAcrossCollapseAndExpand()
    {
        var state = new SidebarState(new AppSettings());
        state.SetWidth(470);
        state.Collapse();
        state.Expand();

        Assert.AreEqual(470d, state.CurrentWidth);
    }

    [TestMethod]
    public void AnimationEnabledStateCanChange()
    {
        var state = new SidebarState(new AppSettings { AnimationsEnabled = true });
        state.SetAnimationsEnabled(false);

        Assert.IsFalse(state.AnimationsEnabled);
    }
}