using System;

using Monitorian.Core.Models;

namespace Monitorian.Core.ViewModels;

public class HotKeyWindowViewModel : ViewModelBase
{
	private readonly AppControllerCore _controller;
	public SettingsCore Settings => _controller.Settings;

	public HotKeyWindowViewModel(AppControllerCore controller)
	{
		this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
	}
}
