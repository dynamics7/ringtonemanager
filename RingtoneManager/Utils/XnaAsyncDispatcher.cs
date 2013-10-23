using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Xna.Framework;

namespace RingtoneManager
{
    public class XNAFrameworkDispatcherService : IApplicationService
    {
        private readonly DispatcherTimer _frameworkDispatcherTimer;
        public XNAFrameworkDispatcherService()
        {
            FrameworkDispatcher.Update();
            _frameworkDispatcherTimer = new DispatcherTimer();
            _frameworkDispatcherTimer.Tick += FrameworkDispatcherTimer_Tick;
            _frameworkDispatcherTimer.Interval = TimeSpan.FromTicks(333333);
        }

        void IApplicationService.StartService(ApplicationServiceContext context)
        {
            _frameworkDispatcherTimer.Start();
        }

        void IApplicationService.StopService()
        {
            _frameworkDispatcherTimer.Stop();
        }

        private static void FrameworkDispatcherTimer_Tick(object sender, EventArgs e)
        {
            FrameworkDispatcher.Update();
        }
    }

}
