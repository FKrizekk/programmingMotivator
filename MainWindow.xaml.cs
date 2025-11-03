using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Reflection;

namespace SlideOverlay
{
    public partial class MainWindow : Window
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_LAYERED = 0x00080000;

        private string[] _imagePaths = Array.Empty<string>();
        private string[] motivationalQuotes;
        private int _currentIndex = 0;
        private int _currentQuoteIndex = 0;
        private readonly DispatcherTimer _cycleTimer;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            _cycleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _cycleTimer.Tick += CycleTimer_Tick;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // make window click‑through
            var hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);

            string exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // load image folder
            string imagesFolder = Path.Combine(exeFolder, "Images");
            string quotesFile = Path.Combine(exeFolder, "quotes.txt");
            if (File.Exists(quotesFile))
            {
                var externalQuotes = File.ReadAllLines(quotesFile)
                                        .Where(line => !string.IsNullOrWhiteSpace(line))
                                        .ToArray();
                motivationalQuotes = externalQuotes;
            }
            _imagePaths = Directory.GetFiles(imagesFolder, "*.png");
            var rnd = new Random();
            _imagePaths = _imagePaths.OrderBy(x => rnd.Next()).ToArray();
            motivationalQuotes = motivationalQuotes.OrderBy(x => rnd.Next()).ToArray();

            StartSlideIn();
            _cycleTimer.Start();
        }

        private void CycleTimer_Tick(object sender, EventArgs e)
        {
            StartSlideIn();
        }

        private void StartSlideIn()
        {
            if (_imagePaths.Length == 0)
                return;

            string path = _imagePaths[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _imagePaths.Length;

            // Set main image
            MyImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));

            // Set dynamic child elements
            var rnd = new Random(DateTime.Now.Second);
            OverlayText.Text = motivationalQuotes[rnd.Next(motivationalQuotes.Length)];

            // Delay to ensure layout done
            MainContainer.Dispatcher.BeginInvoke(new Action(() =>
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                double imgW = MainContainer.ActualWidth;
                double imgH = MainContainer.ActualHeight;

                if (imgW == 0 || imgH == 0)
                {
                    // fallback if layout not yet measured
                    imgW = MainContainer.Width;
                    imgH = MyImage.ActualHeight > 0 ? MyImage.ActualHeight : MyImage.Width;
                }

                double left = screenWidth - imgW*1.5;
                double startY = screenHeight + 300;
                double endY = screenHeight - imgH + 20;

                Canvas.SetLeft(MainContainer, left);
                Canvas.SetTop(MainContainer, startY);

                // animate slide up
                var animUp = new DoubleAnimation
                {
                    From = startY,
                    To = endY,
                    Duration = TimeSpan.FromSeconds(1.0),
                    FillBehavior = FillBehavior.HoldEnd
                };
                MainContainer.BeginAnimation(Canvas.TopProperty, animUp);

                // wait then slide down
                var waitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0 + 5.0) };
                waitTimer.Tick += (s2, e2) =>
                {
                    waitTimer.Stop();
                    var animDown = new DoubleAnimation
                    {
                        From = endY,
                        To = startY,
                        Duration = TimeSpan.FromSeconds(1.0),
                        FillBehavior = FillBehavior.Stop
                    };
                    MainContainer.BeginAnimation(Canvas.TopProperty, animDown);
                };
                waitTimer.Start();

            }), DispatcherPriority.Loaded);
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
