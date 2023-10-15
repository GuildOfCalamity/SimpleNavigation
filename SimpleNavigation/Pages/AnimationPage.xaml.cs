using System;
using System.Buffers.Text;
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
	static int counter = 0;
	static double elapsed = 0;
	static ValueStopwatch vsw = ValueStopwatch.StartNew();
	static int ignoreCollisionCheckCounter = 0;

	public AnimationPage()
	{
		this.InitializeComponent();
		this.Loaded += AnimationPage_Loaded;
	}

	void AnimationPage_Loaded(object? sender, RoutedEventArgs e)
	{
		if (timer != null)
			return;

		// If you want 60 FPS then use me for canvas updates.
		CompositionTarget.Rendering += CompositionTarget_Rendering;

		// Up to 1300 images can be rendered without performance degradation (on my rig).
		for (int i = 0; i < 30; i++) 
		{
			int x = 1; int y = 1;
			int w = 60; int h = 60;

			if (canvas.ActualWidth > 0)
				x = Random.Shared.Next(1, (int)canvas.ActualWidth - (w + 1));
			else
				x = Random.Shared.Next(1, 500);

			if (canvas.ActualHeight > 0)
				y = Random.Shared.Next(1, (int)canvas.ActualHeight - (h + 1));
			else
				y = Random.Shared.Next(1, 500);

			var prop = new ImageProps {
				XCoord = x,	YCoord = y,
				Width = (double)w,
				Height = (double)h,
				XSpeed = (double)Random.Shared.Next(2, 5),
				YSpeed = (double)Random.Shared.Next(2, 5),
			};
			images.Add(prop);

			// Create Image element dynamically
			var image = new Image();
			
			image.Opacity = 0.61;
			image.Source = new BitmapImage(new Uri("ms-appx:///Assets/Sphere.png"));
			image.Width = prop.Width;
			image.Height = prop.Height;
			image.Name = $"Sphere #{i + 1}";
			// Add image to canvas
			canvas.Children.Add(image);

			// Update image position
			Canvas.SetLeft(image, x);
			Canvas.SetTop(image, y);

			// Set ToolTip for each Image element
			ToolTipService.SetToolTip(canvas.Children[i], image.Name);
		}

		// Create a timer for animation.
		timer = new DispatcherTimer();
		timer.Interval = TimeSpan.FromMilliseconds(12); // ~50 FPS
		timer.Tick += AnimationTimer_Tick;
		timer.Start();
	}

	/// <summary>
	/// If you want 60 FPS then use me.
	/// </summary>
	void CompositionTarget_Rendering(object? sender, object e)
	{
		// Canvas update code here.
	}

	/// <summary>
	/// The DispatcherTimer in WinUI3 has a resolution of approximately 15.6 milliseconds per tick,
	/// which corresponds to a frame rate of around 64 frames per second (FPS). This means that the
	/// timer ticks at a frequency of approximately 64 times per second, with each tick occurring 
	/// roughly every 15.6 milliseconds.
	/// The DispatcherTimer is based on the UI thread's message pump, which is why its accuracy is bound 
	/// to the UI thread's message processing rate.In most scenarios, especially for UI-related tasks and 
	/// animations, a frame rate of 60 FPS (16.6 milliseconds per frame) or slightly above is generally 
	/// sufficient for smooth user experience.
	/// If you need a higher update rate or a more accurate timer, you might consider using alternative 
	/// timer mechanisms. One such alternative is the CompositionTarget.Rendering event, which is triggered 
	/// each time a new frame is rendered.This event is tightly synchronized with the display's refresh 
	/// rate, providing a more accurate timer for animations.
	/// You can move this code to the <see cref="CompositionTarget_Rendering(object?, object)"/> 
	/// event to attain 60 FPS performance.
	/// </summary>
	void AnimationTimer_Tick(object? sender, object e)
	{
		if (canvas.ActualWidth == double.NaN || canvas.ActualHeight == double.NaN)
			return;

		// We want to avoid oversubscription. In the event that
		// you're doing too much inside the tick event we'll stop
		// the timer while we operate and then restart it.
		// I believe the DispatchTimer handles this internally
		// for you but it's a good idea to follow best practices.
		timer?.Stop();

		// Move a single Image control
		//MoveImage(image);

		// Move a group of Image controls
		for (int i = 0; i < images.Count; i++)
			MoveImageWithCollisions(images[i], canvas.Children[i] as Image);

		#region [Framerate Debug]
		// Our first sample will be off by a fractional amount due to page load
		// and object render events, but after that it should be accurate.
		elapsed += vsw.GetElapsedTime().TotalMilliseconds;
		vsw = ValueStopwatch.StartNew();
		if (++counter >= 60)
		{
			var cycle = elapsed / 60d;
			System.Diagnostics.Debug.WriteLine($"Cycle average: {cycle:N1} milliseconds ({(1000 / cycle):N1} FPS)");
			counter = 0; elapsed = 0d;
		}
		#endregion

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
		if (canvas.ActualWidth > 0)
			if (x < 0 || x > canvas.ActualWidth - image.ActualWidth) properties.XSpeed *= -1;
		if (canvas.ActualHeight > 0)
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

	/// <summary>
	/// This is a more expensive method, but it can account for object collisions.
	/// </summary>
	void MoveImageWithCollisions(ImageProps properties, Image? image)
	{
		if (image == null)
			return;

		double x = Canvas.GetLeft(image);
		double y = Canvas.GetTop(image);

		// Update position
		x += properties.XSpeed;
		y += properties.YSpeed;

		// Bounce off the edges
		if (canvas.ActualWidth > 0)
		{
			if (x < 0 || x > canvas.ActualWidth - properties.Width)
			{
				properties.XSpeed *= -1;
				x = Math.Clamp(x, 0, canvas.ActualWidth - properties.Width);
			}
		}

		if (canvas.ActualHeight > 0)
		{
			if (y < 0 || y > canvas.ActualHeight - properties.Height)
			{
				properties.YSpeed *= -1;
				y = Math.Clamp(y, 0, canvas.ActualHeight - properties.Height);
			}
		}
		
		if (++ignoreCollisionCheckCounter > 4000) // Let some time go by before testing collisions.
		{
			#region [Check for collisions with other images using distance formula]
			foreach (var otherImage in images)
			{
				if (otherImage != properties)
				{
					double otherX = otherImage.XCoord;
					double otherY = otherImage.YCoord;
					double distance = Math.Sqrt(Math.Pow(x - otherX, 2) + Math.Pow(y - otherY, 2));
			
					// Assuming images are square, change to appropriate width for non-square images.
					if (distance < image.Width + 1)
					{
						// Handle collision by swapping their speeds.
						double tempXSpeed = properties.XSpeed;
						double tempYSpeed = properties.YSpeed;
						properties.XSpeed = otherImage.XSpeed;
						properties.YSpeed = otherImage.YSpeed;
						otherImage.XSpeed = tempXSpeed;
						otherImage.YSpeed = tempYSpeed;
			
						// Adjust positions to avoid overlap.
						double angle = Math.Atan2(y - otherY, x - otherX);
						x = otherX + Math.Cos(angle) * properties.Width;
						y = otherY + Math.Sin(angle) * properties.Width;
					}
				}
			}
			#endregion

			#region [Check for collisions with other images using bounding boxes]
			//foreach (var otherImage in images)
			//{
			//	if (otherImage != properties)
			//	{
			//		double otherX = otherImage.XCoord;
			//		double otherY = otherImage.YCoord;
			//
			//		// Check for collision using bounding boxes
			//		if (x < otherX + otherImage.Width &&
			//			x + image.ActualWidth > otherX &&
			//			y < otherY + otherImage.Height &&
			//			y + image.ActualHeight > otherY)
			//		{
			//			// Handle collision: swap speeds
			//			double tempXSpeed = properties.XSpeed;
			//			double tempYSpeed = properties.YSpeed;
			//			properties.XSpeed = otherImage.XSpeed;
			//			properties.YSpeed = otherImage.YSpeed;
			//			otherImage.XSpeed = tempXSpeed;
			//			otherImage.YSpeed = tempYSpeed;
			//
			//			// Adjust positions to avoid overlap
			//			x = Math.Clamp(x, otherX - image.Width, otherX + otherImage.Width);
			//			y = Math.Clamp(y, otherY - image.Height, otherY + otherImage.Height);
			//		}
			//	}
			//}
			#endregion
		}

		// Update image position
		Canvas.SetLeft(image, x);
		Canvas.SetTop(image, y);

		// Update properties
		properties.XCoord = x;
		properties.YCoord = y;
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
	public string Name { get; set; }
}
