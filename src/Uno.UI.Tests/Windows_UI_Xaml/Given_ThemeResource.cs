﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ThemeResource
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_System_ThemeResource_Light()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Color.FromArgb(0xDE, 0x00, 0x00, 0x00), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
		}

		[TestMethod]
		public async Task When_System_ThemeResource_Dark()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			try
			{
				await SwapSystemTheme();

				var control = new Test_Control();
				app.HostView.Children.Add(control);

				Assert.AreEqual(Color.FromArgb(0xDE, 0xFF, 0xFF, 0xFF), (control.TestTextBlock.Foreground as SolidColorBrush).Color);
			}
			finally
			{
				//ensure the light theme is reset
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public void When_App_ThemeResource_Default()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(Colors.LemonChiffon, (control.TestBorder.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_App_ThemeResource_Light()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			Assert.AreEqual(ApplicationTheme.Light, app.RequestedTheme);
			Assert.AreEqual(Colors.RosyBrown, (control.TestBorder.BorderBrush as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Inherited_In_Template_Applied()
		{
			var control = new Test_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			control.ApplyTemplate();

			var text2InlineBefore = control.InlineTemplateControl.TextBlock2.Text;
			Assert.AreEqual("LocalVisualTree", text2InlineBefore);
			var text2ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock2.Text;
			Assert.AreEqual("OuterVisualTree", text2ResourceTemplateBefore);

			var text4InlineBefore = control.InlineTemplateControl.TextBlock4.Text;
			Assert.AreEqual("ApplicationLevel", text4InlineBefore);
			var text4ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock4.Text;
			Assert.AreEqual("ApplicationLevel", text4ResourceTemplateBefore);
		}

		[TestMethod]
		public void When_Inherited_In_Template_Applied_XAML_Scope()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();

			control.InlineTemplateControl.ApplyTemplate();
			control.TemplateFromResourceControl.ApplyTemplate();

			var text2InlineBefore = control.InlineTemplateControl.TextBlock2.Text;
			var text2ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock2.Text;

			var text4InlineBefore = control.InlineTemplateControl.TextBlock4.Text;
			var text4ResourceTemplateBefore = control.TemplateFromResourceControl.TextBlock4.Text;

			app.HostView.Children.Add(control);

			var text2InlineAfter = control.InlineTemplateControl.TextBlock2.Text;
			var text2ResourceTemplateAfter = control.TemplateFromResourceControl.TextBlock2.Text;

			Assert.AreEqual("LocalVisualTree", text2InlineBefore);
			Assert.AreEqual("OuterVisualTree", text2ResourceTemplateBefore);

			Assert.AreEqual("ApplicationLevel", text4InlineBefore);
			Assert.AreEqual("ApplicationLevel", text4ResourceTemplateBefore);

			Assert.AreEqual("LocalVisualTree", text2InlineAfter);
			Assert.AreEqual("LocalVisualTree", text2ResourceTemplateAfter);
		}

		[TestMethod]
		public async Task When_Theme_Changed()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;

			Assert.AreEqual("LocalVisualTreeLight", textLightThemeMarkup);

			try
			{
				if (await SwapSystemTheme())
				{
					var textDarkThemeMarkup = control.TemplateFromResourceControl.TextBlock6.Text;
					Assert.AreEqual("LocalVisualTreeDark", textDarkThemeMarkup); //ThemeResource markup change lookup uses the visual tree (rather than original XAML namescope)
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_Static()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_Control();
			app.HostView.Children.Add(control);

			control.Measure(new Size(1000, 1000));

			var textLightStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;

			Assert.AreEqual("ApplicationLevelLight", textLightStaticMarkup);

			try
			{
				if (await SwapSystemTheme())
				{
					var textDarkStaticMarkup = control.TemplateFromResourceControl.TextBlock5.Text;
					Assert.AreEqual("ApplicationLevelLight", textDarkStaticMarkup); //StaticResource markup doesn't change
					;
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_ContentControl()
		{
			var control = new ContentControl() { Content = "Unstyled control" };
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(control);
			AssertEx.AssertHasColor(control.Foreground, Colors.Black);

			try
			{
				if (await SwapSystemTheme())
				{
					if (control.Parent == null)
					{
						app.HostView.Children.Add(control); // On UWP the control may have been removed by another test after the async swap
					}

					AssertEx.AssertHasColor(control.Foreground, Colors.White);
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_From_Setter()
		{
			var button = new Button() { Content = "Bu'on" };
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(button);
			AssertEx.AssertHasColor(button.Foreground, Colors.Black);

			try
			{
				if (await SwapSystemTheme())
				{
					if (button.Parent == null)
					{
						app.HostView.Children.Add(button); // On UWP the control may have been removed by another test after the async swap
					}

					AssertEx.AssertHasColor(button.Foreground, Colors.White);
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Theme_Changed_From_Setter_Library()
		{
			var page = new Test_Page();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(page);

			var myExtControl = page.MyExtControl;
			var textLightFromThemeResource = myExtControl.MyTagThemed1;
			var textLightFromStaticResource = myExtControl.MyTagThemed2;

			Assert.AreEqual("ExtLight", textLightFromThemeResource);
			Assert.AreEqual("ExtLight", textLightFromStaticResource);

			try
			{
				if (await SwapSystemTheme())
				{
					if (page.Parent == null)
					{
						app.HostView.Children.Add(page); // On UWP the control may have been removed by another test after the async swap
					}

					var textDarkFromThemeResource = myExtControl.MyTagThemed1;
					var textDarkFromStaticResource = myExtControl.MyTagThemed2;

					Assert.AreEqual("ExtDark", textDarkFromThemeResource);
					Assert.AreEqual("ExtLight", textDarkFromStaticResource);
				}
			}
			finally
			{
				await SwapSystemTheme();
			}
		}

		[TestMethod]
		public async Task When_Direct_Assignment_Incompatible()
		{
			var page = new ThemeResource_Direct_Assignment_Incompatible_Page();
			var transform = page.Resources["MyTransform"] as TranslateTransform;

			Assert.AreEqual(115, transform.Y); //Standard double resource
			Assert.AreEqual(490, transform.X); // Resource is actually a Thickness!
		}

		private static async Task<bool> SwapSystemTheme()
		{
			var currentTheme = Application.Current.RequestedTheme;
			var targetTheme = currentTheme == ApplicationTheme.Light ?
				ApplicationTheme.Dark :
				ApplicationTheme.Light;
#if NETFX_CORE
			if (!UnitTestsApp.App.EnableInteractiveTests)
			{
				return false;
			}

			var dialog = new ContentDialog { Content = "Set default app mode as 'dark' in settings", CloseButtonText = "Done" };
			await dialog.ShowAsync();
#else
			Application.Current.SetRequestedTheme(targetTheme); 
#endif
			Assert.AreEqual(targetTheme, Application.Current.RequestedTheme);
			return true;
		}
	}
}
