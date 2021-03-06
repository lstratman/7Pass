﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using KeePass.Analytics;
using KeePass.Storage;
using KeePass.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace KeePass
{
    public partial class App
    {
        private bool _initialized;

        public new static App Current
        {
            get { return (App)Application.Current; }
        }

        public PhoneApplicationFrame RootFrame { get; private set; }

        public App()
        {
            UnhandledException += Application_UnhandledException;

            if (Debugger.IsAttached)
                Host.Settings.EnableFrameRateCounter = true;

            InitializeComponent();

            InitializePhoneApplication();
        }

        private void InitializePhoneApplication()
        {
            if (_initialized)
                return;

            RootFrame = new Delay.HybridOrientationChangesFrame
            {
                Duration = TimeSpan.FromSeconds(0.6)
            };

            RootFrame.Navigated += CompleteInitializePhoneApplication;
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            _initialized = true;
        }

        private void Application_Activated(
            object sender, ActivatedEventArgs e)
        {
            AnalyticsTracker.Track(
                "start", "activated");

            if (e.IsApplicationInstancePreserved)
            {
                var global = GlobalPassHandler.Instance;
                global.Reset();

                if (global.ShouldPromptGlobalPass)
                {
                    var root = RootFrame;
                    root.Dispatcher.BeginInvoke(() =>
                        root.Navigate(Navigation.GetPathTo
                            <GlobalPassVerify>()));
                }

                return;
            }

            ThemeData.Initialize();
            Cache.RestoreCache(RootFrame.Dispatcher);
        }

        private void Application_Closing(
            object sender, ClosingEventArgs e) {}

        private void Application_Deactivated(
            object sender, DeactivatedEventArgs e) {}

        private void Application_Launching(
            object sender, LaunchingEventArgs e)
        {
            AnalyticsTracker.Track(
                "start", "launch");
            ThemeData.Initialize();
        }

        private void Application_UnhandledException(
            object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject;

            AnalyticsTracker.Track(
                new TrackingEvent("error")
                {
                    {"type", ex.GetType().FullName},
                    {"stack", ex.StackTrace}
                });

            e.Handled = true;

            RootFrame.Dispatcher.BeginInvoke(() =>
            {
                if (!Debugger.IsAttached)
                {
                    var response = MessageBox.Show(
                        Properties.Resources.UnhandledExPrompt,
                        Properties.Resources.UnhandledExTitle,
                        MessageBoxButton.OKCancel);

                    if (response == MessageBoxResult.OK)
                        ErrorReport.Report(ex);
                }
                else
                    Debugger.Break();
            });
        }

        private void CompleteInitializePhoneApplication(
            object sender, NavigationEventArgs e)
        {
            RootVisual = RootFrame;
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private static void RootFrame_NavigationFailed(
            object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}