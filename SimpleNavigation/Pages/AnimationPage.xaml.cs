using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace SimpleNavigation;

public sealed partial class AnimationPage : Page, INotifyPropertyChanged
{
    #region [Props]
	double xSpeed = 4;
	double ySpeed = 4;
	const int maxObjects = 21;
	const double gravityFactor = 0.5;
    const double maxGravitySpeed = 25;
	DispatcherTimer? timer = null;
	List<ImageProps> images = new();
    List<Uri> assets = new();
	bool use60FPS = true;
	bool useBoundingBox = true;
	bool insideUpdate = false;
	// For framerate diagnostics.
    static int counter = 0;
	static double elapsed = 0;
	static ValueStopwatch vsw = ValueStopwatch.StartNew();

    /// <summary>
    /// An event that the main page can subscribe to.
    /// </summary>
    public static event EventHandler<Message>? PostMessageEvent;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
    }

	private bool gravityEnable = false;
	public bool GravityEnable
	{
		get => gravityEnable;
		set
		{
			if (gravityEnable != value)
			{
				gravityEnable = value;
				OnPropertyChanged();
				if (!validating)
					ValidateCombination();
			}
		}
	}

    private bool magneticEnable = false;
    public bool MagneticEnable
    {
        get => magneticEnable;
        set
        {
			if (magneticEnable != value)
			{
				magneticEnable = value;
				OnPropertyChanged();
				if (!validating)
					ValidateCombination();
			}
        }
    }

    private bool collisionEnable = false;
    public bool CollisionEnable
    {
        get => collisionEnable;
        set
        {
			if (collisionEnable != value)
			{
				collisionEnable = value;
				OnPropertyChanged();
				if (!validating)
					ValidateCombination();
			}
        }
    }

    private int selectedAssetIdx = 0;
    public int SelectedAssetIdx
    {
        get => selectedAssetIdx;
        set
        {
            if (selectedAssetIdx != value)
            {
                selectedAssetIdx = value;
                OnPropertyChanged();
                PopulateCanvasImages(assets[selectedAssetIdx]);
            }
        }
    }
    #endregion

    public AnimationPage()
	{
		this.InitializeComponent();
		this.Loaded += AnimationPage_Loaded;
        this.Unloaded += AnimationPage_Unloaded;
		LoadLocalAssets();
    }

    /// <summary>
    /// Load our image roster.
    /// </summary>
    void LoadLocalAssets()
    {
		string path = string.Empty;
        if (!App.IsPackaged)
            path = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
		else
            path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Assets");

        foreach (var f in Directory.GetFiles(path, "*_.png", SearchOption.TopDirectoryOnly))
        {
            assets.Add(new Uri($"ms-appx:///Assets/{Path.GetFileName(f)}"));
        }

		cmbAssets.ItemsSource = assets;
    }

    /// <summary>
    /// Handle any parameter passed.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null && e.Parameter is SystemState sys)
        {
            Debug.WriteLine($"You sent '{sys.Title}'");
            PostMessageEvent?.Invoke(this, new Message
            {
                Content = $"OnNavigatedTo ⇨ {sys.Title}",
                Severity = InfoBarSeverity.Informational,
            });
        }
        else
        {
            Debug.WriteLine($"Parameter is not of type '{nameof(SystemState)}'");
        }
        base.OnNavigatedTo(e);
    }

	void PopulateCanvasImages(Uri? img)
	{
		if (img == null)
			return;

        // Clear the previous setup.
        if (images.Count > 0)
        {
            canvas.Children.Clear();
            images.Clear();
        }

        // Up to 1250 images can be rendered without performance degradation.
        for (int i = 0; i < maxObjects; i++)
        {
            int x = 1; int y = 1;
            int w = 90; int h = 90;

            if (canvas.ActualWidth > 0)
                x = Random.Shared.Next(1, (int)canvas.ActualWidth / 2);
            else
                x = Random.Shared.Next(1, w * 3);

            if (canvas.ActualHeight > 0)
                y = Random.Shared.Next(1, (int)canvas.ActualHeight / 2);
            else
                y = Random.Shared.Next(1, h * 3);

            var prop = new ImageProps
            {
                XCoord = x,
                YCoord = y,
                Width = (double)w,
                Height = (double)h,
                XSpeed = (double)Random.Shared.Next(1, 5),
                YSpeed = (double)Random.Shared.Next(1, 5),
				Rotation = (float)Random.Shared.Next(1, 5) + 0.1f,
				Clockwise = Extensions.CoinFlip(),
				Name = $"Image #{i + 1}"
            };
            images.Add(prop);

            // Create Image element dynamically.
            Image? image = new();
            image.Source = new BitmapImage(img);
            image.Width = prop.Width;
            image.Height = prop.Height;
			// Be sure to set the center point for rotation animations.
            image.CenterPoint = new System.Numerics.Vector3(w / 2, h / 2, 0);

            // Add image to canvas.
            canvas.Children.Add(image);

            // Update image position based on random coords.
            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, y);

            // Set tooltip for each Image element.
            ToolTipService.SetToolTip(canvas.Children[i], prop.Name);
        }
    }

	/// <summary>
	/// <see cref="Page"/> event.
	/// </summary>
    void AnimationPage_Loaded(object? sender, RoutedEventArgs e)
	{
		if (timer != null || assets.Count == 0 || canvas.Children.Count > 0)
			return;

		if (assets.Count > 0)
		{
			SelectedAssetIdx = Random.Shared.Next(0, assets.Count);
			cmbAssets.Text = $"{assets[selectedAssetIdx]}";
			PopulateCanvasImages(assets[selectedAssetIdx]);
		}

        if (use60FPS)
		{
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}
		else
		{
			// Use 30 frames per second.
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(16); // DispatchTimer does not have this resolution, but we'll ask for it anyway.
			timer.Tick += AnimationTimer_Tick;
			timer.Start();
		}
	}

    /// <summary>
    /// <see cref="Page"/> event.
    /// </summary>
    void AnimationPage_Unloaded(object sender, RoutedEventArgs e)
    {
		if (images.Count > 0)
		{
			var hits = images.Where(o => o.Bangs > 0).OrderByDescending(o => o.Bangs);
			foreach (var ip in hits)
			{
				Debug.WriteLine($"{ip.Name} 🡒 {ip.Bangs} collisions");
			}

            // Deferred Query
            //IOrderedEnumerable<ImageProps>? deferred = from p in images where p.Bangs > 0 orderby p.Bangs select p;
            
			// Immediate Query
            //List<ImageProps>? immediate = (from p in images where p.Bangs > 0 orderby p.Bangs select p).ToList();
        }
    }

    /// <summary>
    /// The <see cref="DispatcherTimer"/> in WinUI3 has a resolution of approximately 33.34 milliseconds 
    /// per tick, which corresponds to a frame rate of around 30 frames per second. This means that 
    /// the timer ticks at a frequency of approximately 30 times per second, with each tick occurring 
    /// roughly every 33.34 milliseconds.
    /// The <see cref="DispatcherTimer"/> is based on the UI thread's message pump, which is why its 
	/// accuracy is bound to the UI thread's message processing rate. In most scenarios, especially for 
	/// UI-related tasks and animations, a frame rate of 60 FPS (16.67 milliseconds per frame) or slightly 
	/// above is generally best for a smooth user experience.
    /// If you need a higher update rate or a more accurate timer, you might consider using alternative 
    /// timer mechanisms. One such alternative is the <see cref="CompositionTarget.Rendering"/> event, 
    /// which is triggered each time a new frame is rendered.This event is tightly synchronized with the 
    /// display's refresh rate, providing a more accurate timer for animations.
    /// </summary>
    void CompositionTarget_Rendering(object? sender, object e)
	{
		if (!insideUpdate)
		{
			try
			{
				insideUpdate = true;

				// If the user is on another page the canvas size will not be usable.
				if (canvas.ActualWidth == 0 || canvas.ActualWidth == double.NaN)
					return;

                // Move a group of Image controls
                for (int i = 0; i < images.Count; i++)
                    MoveImageWithCollisions(images[i], canvas.Children[i] as Image);

                #region [Framerate Diagnostics]
                // Our first sample will be off by a fractional amount due to page load
                // and object render events, but after that it should be accurate.
                elapsed += vsw.GetElapsedTime().TotalMilliseconds;
                vsw = ValueStopwatch.StartNew();
                if (++counter >= 60)
                {
                    var cycle = elapsed / 60d;
                    Debug.WriteLine($"Cycle average: {cycle:N1} milliseconds ({(1000 / cycle):N1} FPS)");
                    counter = 0; elapsed = 0d;
                }
                #endregion
            }
            finally
			{
                insideUpdate = false;
            }
        }
		else
		{
			// We should never get here.
            Debug.WriteLine($"🡒 Frame skipped!");
        }
    }

    /// <summary>
    /// The <see cref="DispatcherTimer"/> in WinUI3 has a resolution of approximately 33.34 milliseconds 
    /// per tick, which corresponds to a frame rate of around 30 frames per second. This means that 
    /// the timer ticks at a frequency of approximately 30 times per second, with each tick occurring 
    /// roughly every 33.34 milliseconds.
    /// The <see cref="DispatcherTimer"/> is based on the UI thread's message pump, which is why its 
	/// accuracy is bound to the UI thread's message processing rate. In most scenarios, especially for 
	/// UI-related tasks and animations, a frame rate of 60 FPS (16.67 milliseconds per frame) or slightly 
	/// above is generally best for a smooth user experience.
    /// If you need a higher update rate or a more accurate timer, you might consider using alternative 
    /// timer mechanisms. One such alternative is the <see cref="CompositionTarget.Rendering"/> event, 
    /// which is triggered each time a new frame is rendered.This event is tightly synchronized with the 
    /// display's refresh rate, providing a more accurate timer for animations.
    /// </summary>
    void AnimationTimer_Tick(object? sender, object e)
	{
        // If the user is on another page the canvas size will not be usable.
        if (canvas.ActualWidth == 0 || canvas.ActualWidth == double.NaN)
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

		#region [Framerate Diagnostics]
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
	/// This is a more expensive method, but it can account for object collisions.
	/// </summary>
	void MoveImageWithCollisions(ImageProps properties, Image? image)
	{
		if (image == null)
			return;

		// Update rotation.
		if (properties.Clockwise && image.Rotation < 359.9)
			image.Rotation += properties.Rotation;
        else if (properties.Clockwise && image.Rotation > 359.9)
            image.Rotation = 0.0f;
        else if (!properties.Clockwise && image.Rotation > 0)
            image.Rotation -= properties.Rotation;
        else if (!properties.Clockwise && image.Rotation <= 0)
            image.Rotation = 360.0f;

        double x = Canvas.GetLeft(image);
		double y = Canvas.GetTop(image);

		// Update position.
        x += properties.XSpeed;
        y += properties.YSpeed;

        // Bounce off the edges.
        if (canvas.ActualWidth > 0)
		{
			if (x < 0 || x > canvas.ActualWidth - properties.Width)
			{
				if (x < 0)
					properties.Clockwise = true;
				if (x > canvas.ActualWidth - properties.Width)
					properties.Clockwise = false;

                // Reverse the X-axis speed.
                properties.XSpeed *= -1;
                x = Math.Clamp(x, 0, canvas.ActualWidth - properties.Width);
			}
		}

		if (canvas.ActualHeight > 0)
		{
			if (y < 0 || y > canvas.ActualHeight - properties.Height)
			{
				// Reverse the Y-axis speed.
				properties.YSpeed *= -1;
                y = Math.Clamp(y, 0, canvas.ActualHeight - properties.Height);
				// Add attenuation for each bounce (friction)
				if (GravityEnable && (properties.YSpeed <= -2))
					properties.YSpeed += Random.Shared.NextDouble() + (gravityFactor * 4.5);
			}
		}
		
		if (collisionEnable)
		{
			image.Opacity = 0.99;

			if (!magneticEnable)
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
							// Record the collision.
							otherImage.Bangs += 1;

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
			}
			else
			{
				#region [Check for collisions with other images using bounding boxes]
				foreach (var otherImage in images)
				{
					if (otherImage != properties)
					{
						double otherX = otherImage.XCoord;
						double otherY = otherImage.YCoord;
				
						// Check for collision using bounding boxes
						if (x < otherX + otherImage.Width && x + image.ActualWidth > otherX && y < otherY + otherImage.Height && y + image.ActualHeight > otherY)
						{
							// Record the collision.
							otherImage.Bangs += 1;
				
							// Handle collision: swap speeds
							double tempXSpeed = properties.XSpeed;
							double tempYSpeed = properties.YSpeed;
							properties.XSpeed = otherImage.XSpeed;
							properties.YSpeed = otherImage.YSpeed;
							otherImage.XSpeed = tempXSpeed;
							otherImage.YSpeed = tempYSpeed;
				
							// Adjust positions to avoid overlap. (needs improvement)
							//x = Math.Clamp(x, otherX - image.Width, otherX + otherImage.Width);
							//y = Math.Clamp(y, otherY - image.Height, otherY + otherImage.Height);

                            // Adjust positions to avoid overlap. (magnet effect)
                            double angle = Math.Atan2(y - otherY, x - otherX);
                            x = otherX + Math.Cos(angle) * properties.Width;
                            y = otherY + Math.Sin(angle) * properties.Width;
                        }
                    }
				}
				#endregion
			}
        }
		else
		{
			// Since the images will be passing in front of or behind
			// each other we'll want the user to be able to see them.
            image.Opacity = 0.68;
        }

		if (gravityEnable)
		{
			// Apply gravity if enabled.
			properties.YSpeed += gravityFactor;
			
			// Clamp the gravity speed.
			properties.YSpeed = Math.Min(properties.YSpeed, maxGravitySpeed);

			// If collisions are not enabled then we'll check for stagnant objects.
			if (!collisionEnable)
			{
				if (properties.YSpeed >= 0 && properties.YSpeed < 1.01 && (y >= canvas.ActualHeight - (properties.Height + 1)))
				{
					var factor = ((canvas.ActualHeight / properties.Height) * 2.9) + 0.5;
                    properties.YSpeed = Math.Min(factor, maxGravitySpeed) * -1;
					// Help images that are closely overlaid by adding a random X component.
					if (properties.XSpeed > 0 && properties.XSpeed < 3 && Extensions.CoinFlip())
						properties.XSpeed += (double)Random.Shared.Next(0, 5);
					else if (properties.XSpeed < 0 && properties.XSpeed > -3 && Extensions.CoinFlip())
						properties.XSpeed -= (double)Random.Shared.Next(0, 5);
					else if (properties.XSpeed > 3 && Extensions.CoinFlip())
						properties.XSpeed -= (double)Random.Shared.Next(0, 3);
					else if (properties.XSpeed < -3 && Extensions.CoinFlip())
						properties.XSpeed += (double)Random.Shared.Next(0, 3);
				}
			}
        }

		// Update image position.
		Canvas.SetLeft(image, x);
		Canvas.SetTop(image, y);

		// Update image properties.
		properties.XCoord = x;
		properties.YCoord = y;
	}

	/// <summary>
	/// Update a single image.
	/// </summary>
	void MoveImage(Image? image)
	{
        if (image == null)
            return;

        double x = Canvas.GetLeft(image);
		double y = Canvas.GetTop(image);

		// Update position
		x += xSpeed;
		y += ySpeed;

		// Bounce off the edges.
		if (x < 0 || x > canvas.ActualWidth - image.ActualWidth) xSpeed *= -1;
		if (y < 0 || y > canvas.ActualHeight - image.ActualHeight) ySpeed *= -1;

		// Handle resizing issues.
		if (x < -1) x = 1;
		if (x > (canvas.ActualWidth - (image.Width - 9))) x -= 8;
		if (y < -1) y = 1;
		if (y > (canvas.ActualHeight - (image.Height - 9))) y -= 8;

		// Update image position.
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

		// Update position.
		x += properties.XSpeed;
		y += properties.YSpeed;

		// Bounce off the edges.
		if (canvas.ActualWidth > 0)
			if (x < 0 || x > canvas.ActualWidth - image.ActualWidth) properties.XSpeed *= -1;
		if (canvas.ActualHeight > 0)
			if (y < 0 || y > canvas.ActualHeight - image.ActualHeight) properties.YSpeed *= -1;

		// Handle resizing issues.
		if (x < -1) x = 1;
		if (x > (canvas.ActualWidth - (image.Width - 9))) x -= 8;
		if (y < -1) y = 1;
		if (y > (canvas.ActualHeight - (image.Height - 9))) y -= 8;

		// Update image position.
		Canvas.SetLeft(image, x);
		Canvas.SetTop(image, y);
	}

    static bool validating = false;
    void ValidateCombination()
    {
        try
        {
            validating = true;

			// The magnet effect is coupled with the collision system.
			if (magneticEnable is true && collisionEnable is false)
			{
				CollisionEnable = true;
                // The magnet effect should not be coupled with the gravity system.
                GravityEnable = false;
            }
            
			if (magneticEnable is true && gravityEnable is true)
			{
                // The magnet effect should not be coupled with the gravity system.
                GravityEnable = false;
            }

            if (magneticEnable is true && collisionEnable is true && gravityEnable is true)
            {
                // The magnet effect should not be coupled with the gravity system.
                GravityEnable = false;
            }
        }
        finally
        {
            validating = false;
        }
    }
}

/// <summary>
/// A simple data model for image objects.
/// </summary>
public class ImageProps
{
	public int Bangs { get; set; }
	public bool Clockwise { get; set; }
	public float Rotation { get; set; }
    public string? Name { get; set; }
	public double XCoord { get; set; }
	public double YCoord { get; set; }
	public double XSpeed { get; set; }
	public double YSpeed { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }
}
