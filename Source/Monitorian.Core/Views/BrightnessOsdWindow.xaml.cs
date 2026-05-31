using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monitorian.Core.Views;

public partial class BrightnessOsdWindow : Window
{
	private const int WS_EX_TRANSPARENT = 0x00000020;
	private const int WS_EX_TOOLWINDOW = 0x00000080;
	private const int WS_EX_NOACTIVATE = 0x08000000;
	private const int GWL_EXSTYLE = -20;

	[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
	private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
	private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
	private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
	private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	private readonly DispatcherTimer _hideTimer;

	public BrightnessOsdWindow()
	{
		InitializeComponent();

		_hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
		_hideTimer.Tick += (_, _) =>
		{
			_hideTimer.Stop();
			this.Hide();
		};

		this.SourceInitialized += OnSourceInitialized;
	}

	private void OnSourceInitialized(object sender, EventArgs e)
	{
		var helper = new WindowInteropHelper(this);
		var hwnd = helper.Handle;

		int current = (IntPtr.Size == 8)
			? (int)GetWindowLongPtr64(hwnd, GWL_EXSTYLE)
			: GetWindowLong32(hwnd, GWL_EXSTYLE);
		int updated = current | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
		if (IntPtr.Size == 8)
			SetWindowLongPtr64(hwnd, GWL_EXSTYLE, new IntPtr(updated));
		else
			SetWindowLong32(hwnd, GWL_EXSTYLE, updated);
	}

	protected override void OnClosed(EventArgs e)
	{
		_hideTimer.Stop();
		base.OnClosed(e);
	}

	public void ShowOsd(string monitorName, int value)
	{
		NameText.Text = monitorName ?? string.Empty;
		ValueText.Text = $"{value}%";
		Bar.Value = Math.Max(0, Math.Min(100, value));

		PositionAboveTaskbar();

		if (!this.IsVisible)
			this.Show();

		_hideTimer.Stop();
		_hideTimer.Start();
	}

	private void PositionAboveTaskbar()
	{
		// Use the screen containing the cursor.
		var pos = System.Windows.Forms.Cursor.Position;
		var screen = System.Windows.Forms.Screen.FromPoint(pos);

		var working = screen.WorkingArea;   // excludes taskbar, in physical pixels
		var bounds = screen.Bounds;          // full screen, in physical pixels

		var source = PresentationSource.FromVisual(this);
		var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

		// Convert to DIPs
		var workTopLeft = transform.Transform(new Point(working.Left, working.Top));
		var workBottomRight = transform.Transform(new Point(working.Right, working.Bottom));
		var boundsBottomRight = transform.Transform(new Point(bounds.Right, bounds.Bottom));

		double workWidth = workBottomRight.X - workTopLeft.X;
		double workHeight = workBottomRight.Y - workTopLeft.Y;

		this.UpdateLayout();
		double w = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
		double h = this.ActualHeight > 0 ? this.ActualHeight : 80;

		// Horizontally center within the work area (taskbar middle for normal layouts).
		double left = workTopLeft.X + (workWidth - w) / 2;

		// Place just above the taskbar: a small gap from the bottom of work area.
		const double gap = 12;
		double top = workBottomRight.Y - h - gap;

		// If the taskbar is at the top, drop below it instead.
		if (working.Top > bounds.Top)
			top = workTopLeft.Y + gap;

		this.Left = left;
		this.Top = top;
	}
}
