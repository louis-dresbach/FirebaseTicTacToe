using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Firebase.Database;
using Firebase.Database.Query;

namespace FirebaseTicTacToe
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        private string playerid;

        static string auth = "Qbkk42UeqGeuCTIlRMHrg1JQA1EwrdbEdPcb04E3";
        FirebaseClient fc = new FirebaseClient("https://project-92573-default-rtdb.europe-west1.firebasedatabase.app/", new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(auth) });

        public Page1()
        {
            InitializeComponent();

            fc.Child("Games").AsObservable<Game>().Subscribe(game =>
            {
                if (game.Key == string.Empty || game.Object == null)
                    return;

                game.Object.id = game.Key;

                switch (game.EventType)
                {
                    case Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate:
                        listBoxGames.Dispatcher.Invoke(new Action(() => { listBoxGames.Items.Add(game.Object); }));
                        break;
                    case Firebase.Database.Streaming.FirebaseEventType.Delete:
                        listBoxGames.Dispatcher.Invoke(new Action(() => { listBoxGames.Items.Remove(game.Object); }));
                        break;
                }
            });

            Logon();

            this.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            fc.Child("Players").Child(this.playerid).DeleteAsync();
        }

        private async void Logon()
        {
            FirebaseObject<Player> fo = await fc.Child("Players").PostAsync(new Player { name = "player" });
            playerid = fo.Key;
        }

        private async void btnHost_Click(object sender, RoutedEventArgs e)
        {
            FirebaseObject<Game> fo = await fc.Child("Games").PostAsync(new Game { name = txtBoxName.Text });
            ConnectToGame(fo.Key);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxGames.SelectedItem == null)
                return;

            ConnectToGame(((Game)listBoxGames.SelectedItem).id);
        }

        private void ConnectToGame(string key)
        {
            Page2 p = new Page2(key, playerid);
            NavigationService.Navigate(p);
        }
    }
}
