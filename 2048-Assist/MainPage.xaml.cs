using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Input;

namespace TwentyFortyEightAssist
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Url of Home page
        private string GameUri = "/Html/game.html";

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            Browser.IsScriptEnabled = true;
            // Add your URL here
            Browser.Navigate(new Uri(GameUri, UriKind.Relative));
        }

        // Handle navigation failures.
        private void Browser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            MessageBox.Show("Game could not be loaded. Try killing the game and restarting it.");
        }

        private void ApplicationBarHelp_Click(object sender, EventArgs e)
        {
            Browser.InvokeScript("showHelp");
        }

        private void WebPage_Loaded(object sender, NavigationEventArgs e)
        {
            //Call JS layer that the browser has been loaded, so that the game manager can start the game.
            Browser.InvokeScript("webPageLoaded");
            //Register for game state messages from JS layer; JS layer calls window.external.notify to notify the game state for determining the next move.
            Browser.ScriptNotify += (objectSender, args) =>
            {
                try
                {
                    Board board = new Board(args.Value);//setup the board with the values obtained from JS.
                    string direction = Solver.FindNextMove(board);
                    Browser.InvokeScript("GetDirectionFromNative", direction);//callback the JS layer with results
                }
                catch (Exception)
                {
                    MessageBox.Show("App crashed. Sorry for the trouble");
                }
            };
        }
    }
}
