using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Windows.System;

namespace Tests;

[TestClass]
public class Hotkeys
{
    [TestMethod]
    public void SavedTextRoundTrips()
    {
        foreach (var text in new[] { "Ctrl+Alt+1", "Ctrl+Alt+Shift+F9", "Alt+Win+A", "Ctrl+NumberPad5" })
        {
            Assert.IsTrue(Hotkey.TryParse(text, out var hotkey), text);
            Assert.AreEqual(text, hotkey.ToString());
        }

        Assert.IsTrue(Hotkey.TryParse(" shift + ctrl + q ", out var typed));
        Assert.AreEqual(new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Shift, VirtualKey.Q), typed);
        Assert.AreEqual("Ctrl+Shift+Q", typed.ToString());
    }

    [TestMethod]
    public void RejectsCombinationsThatCannotBeRegistered()
    {
        foreach (var text in new[] { null, "", "F9", "Ctrl+Alt", "Ctrl+A+B", "Ctrl+Shift+Control", "Ctrl+12" })
            Assert.IsFalse(Hotkey.TryParse(text, out _), text ?? "null");
    }
}
