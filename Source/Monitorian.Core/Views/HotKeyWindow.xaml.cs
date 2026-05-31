using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views;

public partial class HotKeyWindow : Window
{
	private readonly FloatWindowMover _mover;
	private readonly AppControllerCore _controller;

	public HotKeyWindow(AppControllerCore controller, Rect pivot)
	{
		LanguageService.Switch();

		InitializeComponent();

		this._controller = controller;
		this.DataContext = new HotKeyWindowViewModel(controller);

		UpBox.Text = controller.Settings.BrightnessUpHotKey ?? string.Empty;
		DownBox.Text = controller.Settings.BrightnessDownHotKey ?? string.Empty;

		// Select the matching step item.
		int step = controller.Settings.HotKeyBrightnessStep;
		foreach (System.Windows.Controls.ComboBoxItem item in StepBox.Items)
		{
			if (int.TryParse(item.Tag?.ToString(), out int v) && v == step)
			{
				StepBox.SelectedItem = item;
				break;
			}
		}

		_mover = new FloatWindowMover(this, pivot);

		controller.WindowPainter.Add(this);
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		FlowElement.EnsureFlowDirection(this);
	}

	private void UpBox_HotKeyCaptured(object sender, HotKeyCapturedEventArgs e)
	{
		var value = e.IsClear ? string.Empty : new HotKeyDefinition(e.Modifiers, e.Key).Serialize();
		UpBox.Text = value;
		_controller.Settings.BrightnessUpHotKey = value;
	}

	private void DownBox_HotKeyCaptured(object sender, HotKeyCapturedEventArgs e)
	{
		var value = e.IsClear ? string.Empty : new HotKeyDefinition(e.Modifiers, e.Key).Serialize();
		DownBox.Text = value;
		_controller.Settings.BrightnessDownHotKey = value;
	}

	private void StepBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
	{
		if (StepBox.SelectedItem is System.Windows.Controls.ComboBoxItem item
			&& int.TryParse(item.Tag?.ToString(), out int step))
		{
			_controller.Settings.HotKeyBrightnessStep = step;
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.Key is Key.Escape && !UpBox.IsKeyboardFocused && !DownBox.IsKeyboardFocused)
			OnCloseTriggered(this, EventArgs.Empty);
	}

	#region Close

	private bool _isClosing = false;

	protected void OnCloseTriggered(object sender, EventArgs e)
	{
		if (!_isClosing && this.IsLoaded)
			this.Close();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		if (!_isClosing)
			this.Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!e.Cancel)
		{
			_isClosing = true;

			if (this.DataContext is IDisposable disposable)
				disposable.Dispose();
		}

		base.OnClosing(e);
	}

	#endregion
}
