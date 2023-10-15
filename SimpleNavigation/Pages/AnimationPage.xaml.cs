using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.FileProperties;

namespace SimpleNavigation;

public sealed partial class AnimationPage : Page
{
	double xSpeed = 5;
	double ySpeed = 5;
	DispatcherTimer? timer = null;
	List<ImageProps> images = new();

	public AnimationPage()
	{
		this.InitializeComponent();
		this.Loaded += AnimationPage_Loaded;
	}

	void AnimationPage_Loaded(object? sender, RoutedEventArgs e)
	{
		if (timer != null)
			return;

		// Initialize properties for multiple images.
		for (int i = 0; i < 50; i++)
		{
			var prop = new ImageProps {
				XCoord = 1,	YCoord = 1,
				Width = (double)Random.Shared.Next(20, 111),
				Height = (double)Random.Shared.Next(20, 111),
				XSpeed = (double)Random.Shared.Next(2, 11),
				YSpeed = (double)Random.Shared.Next(2, 11),
			};
			images.Add(prop);

			// Create Image element dynamically
			var image = new Image();
			
			image.Opacity = 0.62;
			image.Source = new BitmapImage(new Uri("ms-appx:///Assets/Sphere.png"));
			image.Width = prop.Width;
			image.Height = prop.Height;

			// Add image to canvas
			canvas.Children.Add(image);

			// Set ToolTip for each Image element
			ToolTipService.SetToolTip(canvas.Children[i], $"Sphere #{i + 1}");
		}

		// Create a timer for animation.
		timer = new DispatcherTimer();
		timer.Interval = TimeSpan.FromMilliseconds(12); // ~50 FPS
		timer.Tick += AnimationTimer_Tick;
		timer.Start();
	}

	void AnimationTimer_Tick(object? sender, object e)
	{
		if (canvas.ActualWidth == double.NaN || canvas.ActualHeight == double.NaN)
			return;

		// We want to avoid oversubscription. In the event that
		// you're doing too much inside the tick event we'll stop
		// the timer while we operate and then restart it.
		timer?.Stop();

		// Move a single Image control
		//MoveImage(image);

		// Move a group of Image controls
		for (int i = 0; i < images.Count; i++)
			MoveImage(images[i], canvas.Children[i] as Image);

		timer?.Start();
	}

	/// <summary>
	/// Update a single image.
	/// </summary>
	void MoveImage(Image image)
	{
		double x = Canvas.GetLeft(image);
		double y = Canvas.GetTop(image);

		// Update position
		x += xSpeed;
		y += ySpeed;

		// Bounce off the edges
		if (x < 0 || x > canvas.ActualWidth - image.ActualWidth) xSpeed *= -1;
		if (y < 0 || y > canvas.ActualHeight - image.ActualHeight) ySpeed *= -1;

		// Handle resizing issues.
		if (x < -1) x = 1;
		if (x > (canvas.ActualWidth - (image.Width - 9))) x -= 8;
		if (y < -1) y = 1;
		if (y > (canvas.ActualHeight - (image.Height - 9))) y -= 8;

		// Update image position
		Canvas.SetLeft(image, x);
		Canvas.SetTop(image, y);
	}

	/// <summary>
	/// Update a single image based on its <see cref="ImageProps"/>.
	/// </summary>
	void MoveImage(ImageProps properties, Image? image)
	{
		if (image == null)
			return;

		double x = Canvas.GetLeft(image);
		double y = Canvas.GetTop(image);

		// Update position
		x += properties.XSpeed;
		y += properties.YSpeed;

		// Bounce off the edges
		if (x < 0 || x > canvas.ActualWidth - image.ActualWidth) properties.XSpeed *= -1;
		if (y < 0 || y > canvas.ActualHeight - image.ActualHeight) properties.YSpeed *= -1;

		// Handle resizing issues.
		if (x < -1) x = 1;
		if (x > (canvas.ActualWidth - (image.Width - 9))) x -= 8;
		if (y < -1) y = 1;
		if (y > (canvas.ActualHeight - (image.Height - 9))) y -= 8;

		// Update image position
		Canvas.SetLeft(image, x);
		Canvas.SetTop(image, y);
	}
}

public class ImageProps
{
	public double XCoord { get; set; }
	public double YCoord { get; set; }
	public double XSpeed { get; set; }
	public double YSpeed { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }
}
