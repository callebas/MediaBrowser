﻿using MediaBrowser.Model.DTO;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MediaBrowser.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Timer MouseIdleTimer { get; set; }
        private Timer BackdropTimer { get; set; }
        private Image BackdropImage { get; set; }
        private string[] CurrentBackdrops { get; set; }
        private int CurrentBackdropIndex { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            BackButton.Click += BtnApplicationBackClick;
            ExitButton.Click += ExitButtonClick;
            ForwardButton.Click += ForwardButtonClick;
            DragBar.MouseDown += DragableGridMouseDown;
            Loaded += MainWindowLoaded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private bool _isMouseIdle = true;
        public bool IsMouseIdle
        {
            get { return _isMouseIdle; }
            set
            {
                _isMouseIdle = value;
                OnPropertyChanged("IsMouseIdle");
            }
        }

        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = App.Instance;

            if (App.Instance.ServerConfiguration == null)
            {
                App.Instance.PropertyChanged += ApplicationPropertyChanged;
            }
            else
            {
                LoadInitialPage();
            }
        }

        void ForwardButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateForward();
        }

        void ExitButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void ApplicationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ServerConfiguration"))
            {
                App.Instance.PropertyChanged -= ApplicationPropertyChanged;
                LoadInitialPage();
            }
        }

        private async void LoadInitialPage()
        {
            await App.Instance.LogoutUser().ConfigureAwait(false);
        }

        private void DragableGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        void BtnApplicationBackClick(object sender, RoutedEventArgs e)
        {
            NavigateBack();
        }

        private Frame PageFrame
        {
            get
            {
                // Finding the grid that is generated by the ControlTemplate of the Button
                return TreeHelper.FindChild<Frame>(PageContent, "PageFrame");
            }
        }

        public void Navigate(Uri uri)
        {
            PageFrame.Navigate(uri);
        }

        /// <summary>
        /// Sets the backdrop based on an ApiBaseItemWrapper
        /// </summary>
        public void SetBackdrops(DtoBaseItem item)
        {
            SetBackdrops(UIKernel.Instance.ApiClient.GetBackdropImageUrls(item, null, null, 1920, 1080));
        }

        /// <summary>
        /// Sets the backdrop based on a list of image files
        /// </summary>
        public async void SetBackdrops(string[] backdrops)
        {
            // Don't reload the same backdrops
            if (CurrentBackdrops != null && backdrops.SequenceEqual(CurrentBackdrops))
            {
                return;
            }

            if (BackdropTimer != null)
            {
                BackdropTimer.Dispose();
            }

            BackdropGrid.Children.Clear();

            if (backdrops.Length == 0)
            {
                CurrentBackdrops = null;
                return;
            }

            CurrentBackdropIndex = GetFirstBackdropIndex();

            Image image = await App.Instance.GetImage(backdrops.ElementAt(CurrentBackdropIndex));
            image.SetResourceReference(Image.StyleProperty, "BackdropImage");

            BackdropGrid.Children.Add(image);

            CurrentBackdrops = backdrops;
            BackdropImage = image;

            const int backdropRotationTime = 7000;

            if (backdrops.Count() > 1)
            {
                BackdropTimer = new Timer(BackdropTimerCallback, null, backdropRotationTime, backdropRotationTime);
            }
        }

        public void ClearBackdrops()
        {
            if (BackdropTimer != null)
            {
                BackdropTimer.Dispose();
            }

            BackdropGrid.Children.Clear();

            CurrentBackdrops = null;
        }

        private void BackdropTimerCallback(object stateInfo)
        {
            // Need to do this on the UI thread
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var animFadeOut = new Storyboard();
                animFadeOut.Completed += AnimFadeOutCompleted;

                var fadeOut = new DoubleAnimation();
                fadeOut.From = 1.0;
                fadeOut.To = 0.5;
                fadeOut.Duration = new Duration(TimeSpan.FromSeconds(1));

                animFadeOut.Children.Add(fadeOut);
                Storyboard.SetTarget(fadeOut, BackdropImage);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Image.OpacityProperty));

                animFadeOut.Begin(this);
            });
        }

        async void AnimFadeOutCompleted(object sender, System.EventArgs e)
        {
            if (CurrentBackdrops == null)
            {
                return;
            }

            int backdropIndex = GetNextBackdropIndex();

            BitmapImage image = await App.Instance.GetBitmapImage(CurrentBackdrops[backdropIndex]);
            CurrentBackdropIndex = backdropIndex;

            // Need to do this on the UI thread
            BackdropImage.Source = image;
            Storyboard imageFadeIn = new Storyboard();

            DoubleAnimation fadeIn = new DoubleAnimation();

            fadeIn.From = 0.25;
            fadeIn.To = 1.0;
            fadeIn.Duration = new Duration(TimeSpan.FromSeconds(1));

            imageFadeIn.Children.Add(fadeIn);
            Storyboard.SetTarget(fadeIn, BackdropImage);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(Image.OpacityProperty));
            imageFadeIn.Begin(this);
        }

        private int GetFirstBackdropIndex()
        {
            return 0;
        }

        private int GetNextBackdropIndex()
        {
            if (CurrentBackdropIndex < CurrentBackdrops.Length - 1)
            {
                return CurrentBackdropIndex + 1;
            }

            return 0;
        }

        public void NavigateBack()
        {
            if (PageFrame.NavigationService.CanGoBack)
            {
                PageFrame.NavigationService.GoBack();
            }
        }

        public void NavigateForward()
        {
            if (PageFrame.NavigationService.CanGoForward)
            {
                PageFrame.NavigationService.GoForward();
            }
        }

        /// <summary>
        /// Shows the control bar then starts a timer to hide it
        /// </summary>
        private void StartMouseIdleTimer()
        {
            IsMouseIdle = false;

            const int duration = 10000;

            // Start the timer if it's null, otherwise reset it
            if (MouseIdleTimer == null)
            {
                MouseIdleTimer = new Timer(MouseIdleTimerCallback, null, duration, Timeout.Infinite);
            }
            else
            {
                MouseIdleTimer.Change(duration, Timeout.Infinite);
            }
        }

        /// <summary>
        /// This is the Timer callback method to hide the control bar
        /// </summary>
        private void MouseIdleTimerCallback(object stateInfo)
        {
            IsMouseIdle = true;
            
            if (MouseIdleTimer != null)
            {
                MouseIdleTimer.Dispose();
                MouseIdleTimer = null;
            }
        }

        /// <summary>
        /// Handles OnMouseMove to show the control box
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            StartMouseIdleTimer();
        }

        /// <summary>
        /// Handles OnKeyUp to provide keyboard based navigation
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (IsBackPress(e))
            {
                NavigateBack();
            }

            else if (IsForwardPress(e))
            {
                NavigateForward();
            }
        }

        /// <summary>
        /// Determines if a keypress should be treated as a backward press
        /// </summary>
        private bool IsBackPress(KeyEventArgs e)
        {
            if (e.Key == Key.BrowserBack || e.Key == Key.Back)
            {
                return true;
            }

            if (e.SystemKey == Key.Left && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a keypress should be treated as a forward press
        /// </summary>
        private bool IsForwardPress(KeyEventArgs e)
        {
            if (e.Key == Key.BrowserForward)
            {
                return true;
            }

            if (e.SystemKey == Key.RightAlt && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                return true;
            }

            return false;
        }
    }
}