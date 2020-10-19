using System;
using System.Windows;
using System.Runtime.InteropServices;
using VMS.TPS.Common.Model.API;
using SpotStats;
 
namespace VMS.TPS
{
    public class Script
    {
        [DllImport("kernel32")]
        public static extern bool AllocConsole();
        public void Execute(ScriptContext context, Window window)
        {
            // AllocConsole();
            // Console.WriteLine("Start...");
            MainView mainView = new MainView(context);
            window.Title = "Scanning Spot Statistics";
            window.Content = mainView;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.ResizeMode = ResizeMode.NoResize;
        }
    }
}