using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Monitorian.Core.Views.Controls;

/// <summary>
/// TextBox-derived control that captures a hotkey combination on key press.
/// </summary>
public class HotKeyBox : TextBox
{
	public event EventHandler<HotKeyCapturedEventArgs> HotKeyCaptured;

	public HotKeyBox()
	{
		this.IsReadOnly = true;
		this.IsReadOnlyCaretVisible = false;
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		e.Handled = true;

		var key = e.Key == Key.System ? e.SystemKey : e.Key;

		if (key is Key.LeftCtrl or Key.RightCtrl
			or Key.LeftAlt or Key.RightAlt
			or Key.LeftShift or Key.RightShift
			or Key.LWin or Key.RWin
			or Key.None)
		{
			return;
		}

		if (key is Key.Escape)
		{
			HotKeyCaptured?.Invoke(this, new HotKeyCapturedEventArgs(ModifierKeys.None, Key.None, isClear: true));
			return;
		}

		var mods = Keyboard.Modifiers;
		HotKeyCaptured?.Invoke(this, new HotKeyCapturedEventArgs(mods, key, isClear: false));
	}
}

public class HotKeyCapturedEventArgs : EventArgs
{
	public ModifierKeys Modifiers { get; }
	public Key Key { get; }
	public bool IsClear { get; }

	public HotKeyCapturedEventArgs(ModifierKeys modifiers, Key key, bool isClear)
	{
		this.Modifiers = modifiers;
		this.Key = key;
		this.IsClear = isClear;
	}
}
