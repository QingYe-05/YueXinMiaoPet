using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace YueXinMiaoPet.Controls
{
    public class AnimatedGifImage : System.Windows.Controls.Image
    {
        public static readonly DependencyProperty GifPathProperty =
            DependencyProperty.Register(
                "GifPath",
                typeof(string),
                typeof(AnimatedGifImage),
                new PropertyMetadata(string.Empty, OnGifPathChanged));

        private readonly DispatcherTimer _timer;
        private readonly List<GifFrame> _frames;
        private int _frameIndex;

        public event EventHandler AnimationCycleCompleted;

        public string GifPath
        {
            get { return (string)GetValue(GifPathProperty); }
            set { SetValue(GifPathProperty, value); }
        }

        public TimeSpan CurrentCycleDuration { get; private set; }

        public AnimatedGifImage()
        {
            _frames = new List<GifFrame>();
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Tick += OnTimerTick;
            CurrentCycleDuration = TimeSpan.FromSeconds(3);
            Unloaded += delegate { StopAnimation(); };
            Loaded += delegate { StartAnimation(); };
        }

        private static void OnGifPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AnimatedGifImage control = d as AnimatedGifImage;
            if (control != null)
            {
                control.LoadGif(e.NewValue as string);
            }
        }

        private void LoadGif(string path)
        {
            StopAnimation();
            _frames.Clear();
            _frameIndex = 0;
            Source = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using (MemoryStream stream = new MemoryStream(bytes))
                using (System.Drawing.Image gif = System.Drawing.Image.FromStream(stream))
                {
                    FrameDimension dimension = new FrameDimension(gif.FrameDimensionsList[0]);
                    int frameCount = gif.GetFrameCount(dimension);
                    int[] delays = ReadDelays(gif, frameCount);
                    TimeSpan total = TimeSpan.Zero;

                    for (int i = 0; i < frameCount; i++)
                    {
                        gif.SelectActiveFrame(dimension, i);
                        using (Bitmap bitmap = new Bitmap(gif.Width, gif.Height, PixelFormat.Format32bppPArgb))
                        {
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.Clear(System.Drawing.Color.Transparent);
                                graphics.DrawImage(gif, 0, 0, gif.Width, gif.Height);
                            }

                            TimeSpan delay = TimeSpan.FromMilliseconds(Math.Max(60, delays[i] * 10));
                            _frames.Add(new GifFrame(ConvertBitmap(bitmap), delay));
                            total += delay;
                        }
                    }

                    CurrentCycleDuration = total.TotalMilliseconds > 0 ? total : TimeSpan.FromSeconds(3);
                }

                if (_frames.Count > 0)
                {
                    Source = _frames[0].Source;
                    StartAnimation();
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("加载 GIF 失败：" + path, ex);
                Source = null;
            }
        }

        private int[] ReadDelays(System.Drawing.Image gif, int frameCount)
        {
            int[] delays = new int[frameCount];
            for (int i = 0; i < delays.Length; i++)
            {
                delays[i] = 10;
            }

            try
            {
                PropertyItem item = gif.GetPropertyItem(0x5100);
                byte[] values = item.Value;
                for (int i = 0; i < frameCount; i++)
                {
                    int offset = i * 4;
                    if (values.Length >= offset + 4)
                    {
                        delays[i] = BitConverter.ToInt32(values, offset);
                        if (delays[i] <= 0)
                        {
                            delays[i] = 10;
                        }
                    }
                }
            }
            catch
            {
            }

            return delays;
        }

        private BitmapSource ConvertBitmap(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        private void StartAnimation()
        {
            if (_frames.Count == 0)
            {
                return;
            }

            _timer.Interval = _frames[_frameIndex].Delay;
            _timer.Start();
        }

        private void StopAnimation()
        {
            _timer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_frames.Count == 0)
            {
                StopAnimation();
                return;
            }

            _frameIndex++;
            if (_frameIndex >= _frames.Count)
            {
                _frameIndex = 0;
                EventHandler handler = AnimationCycleCompleted;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }

            Source = _frames[_frameIndex].Source;
            _timer.Interval = _frames[_frameIndex].Delay;
        }

        private class GifFrame
        {
            public BitmapSource Source { get; private set; }
            public TimeSpan Delay { get; private set; }

            public GifFrame(BitmapSource source, TimeSpan delay)
            {
                Source = source;
                Delay = delay;
            }
        }
    }
}
