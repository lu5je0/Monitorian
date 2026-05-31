using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Interop;

namespace Monitorian.Core.Models;

/// <summary>
/// Registers global hotkeys via Win32 RegisterHotKey/WM_HOTKEY using a
/// dedicated message-only window so it works regardless of UI state.
/// </summary>
public class HotKeyService : IDisposable
{
	private const int WM_HOTKEY = 0x0312;
	private static readonly IntPtr HWND_MESSAGE = new(-3);

	[Flags]
	private enum FsModifiers : uint
	{
		Alt = 0x0001,
		Control = 0x0002,
		Shift = 0x0004,
		Win = 0x0008,
		NoRepeat = 0x4000
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	private const int IdUp = 1;
	private const int IdDown = 2;

	private readonly HwndSource _source;
	private readonly HashSet<int> _registered = new();

	public event Action BrightnessUpPressed;
	public event Action BrightnessDownPressed;

	public HotKeyService()
	{
		var parameters = new HwndSourceParameters("MonitorianHotKey")
		{
			ParentWindow = HWND_MESSAGE,
			WindowStyle = 0
		};
		_source = new HwndSource(parameters);
		_source.AddHook(WndProc);
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == WM_HOTKEY)
		{
			int id = wParam.ToInt32();
			if (id == IdUp)
			{
				BrightnessUpPressed?.Invoke();
				handled = true;
			}
			else if (id == IdDown)
			{
				BrightnessDownPressed?.Invoke();
				handled = true;
			}
		}
		return IntPtr.Zero;
	}

	/// <summary>
	/// Re-registers up/down hotkeys. Pass null to disable a particular hotkey.
	/// </summary>
	public void Apply(HotKeyDefinition up, HotKeyDefinition down, bool enabled)
	{
		UnregisterAll();

		if (!enabled || _source is null)
			return;

		TryRegister(IdUp, up);
		TryRegister(IdDown, down);
	}

	private bool TryRegister(int id, HotKeyDefinition def)
	{
		if (def is null || !def.IsValid)
			return false;

		uint mods = (uint)ToFs(def.Modifiers);
		uint vk = (uint)KeyInterop.VirtualKeyFromKey(def.Key);

		if (RegisterHotKey(_source.Handle, id, mods, vk))
		{
			_registered.Add(id);
			return true;
		}

		int err = Marshal.GetLastWin32Error();
		Debug.WriteLine($"RegisterHotKey failed for id={id}, err={err}");
		return false;
	}

	private void UnregisterAll()
	{
		if (_source is null) return;
		foreach (var id in _registered)
			UnregisterHotKey(_source.Handle, id);
		_registered.Clear();
	}

	private static FsModifiers ToFs(ModifierKeys m)
	{
		FsModifiers r = 0;
		if ((m & ModifierKeys.Alt) != 0) r |= FsModifiers.Alt;
		if ((m & ModifierKeys.Control) != 0) r |= FsModifiers.Control;
		if ((m & ModifierKeys.Shift) != 0) r |= FsModifiers.Shift;
		if ((m & ModifierKeys.Windows) != 0) r |= FsModifiers.Win;
		return r;
	}

	public void Dispose()
	{
		UnregisterAll();
		_source?.RemoveHook(WndProc);
		_source?.Dispose();
	}
}

/// <summary>
/// Represents a hotkey definition (modifiers + key) with string serialization
/// in the form "Ctrl+Alt+Up".
/// </summary>
public class HotKeyDefinition
{
	public ModifierKeys Modifiers { get; }
	public Key Key { get; }

	public HotKeyDefinition(ModifierKeys modifiers, Key key)
	{
		this.Modifiers = modifiers;
		this.Key = key;
	}

	public bool IsValid => Key != Key.None && (Modifiers != ModifierKeys.None || IsStandaloneAllowed(Key));

	private static bool IsStandaloneAllowed(Key key) => key >= Key.F13 && key <= Key.F24;

	public string Serialize()
	{
		if (!IsValid) return string.Empty;

		var sb = new StringBuilder();
		if ((Modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl+");
		if ((Modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt+");
		if ((Modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift+");
		if ((Modifiers & ModifierKeys.Windows) != 0) sb.Append("Win+");
		sb.Append(Key.ToString());
		return sb.ToString();
	}

	public override string ToString() => Serialize();

	public static HotKeyDefinition TryParse(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		ModifierKeys mods = ModifierKeys.None;
		Key key = Key.None;

		var parts = value.Split('+');
		foreach (var raw in parts)
		{
			var token = raw.Trim();
			if (token.Length == 0) continue;

			switch (token.ToLower(CultureInfo.InvariantCulture))
			{
				case "ctrl":
				case "control":
					mods |= ModifierKeys.Control;
					break;
				case "alt":
					mods |= ModifierKeys.Alt;
					break;
				case "shift":
					mods |= ModifierKeys.Shift;
					break;
				case "win":
				case "windows":
					mods |= ModifierKeys.Windows;
					break;
				default:
					if (Enum.TryParse<Key>(token, true, out var k))
						key = k;
					break;
			}
		}

		var def = new HotKeyDefinition(mods, key);
		return def.IsValid ? def : null;
	}
}
