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
using System.Linq;

using Firebase.Database;
using Firebase.Database.Query;

namespace FirebaseTicTacToe
{
    /// <summary>
    /// Interaction logic for Page2.xaml
    /// </summary>
    public partial class Page2 : Page
    {
        static string auth = "Qbkk42UeqGeuCTIlRMHrg1JQA1EwrdbEdPcb04E3";
        FirebaseClient fc = new FirebaseClient("https://project-92573-default-rtdb.europe-west1.firebasedatabase.app/", new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(auth) });

        string gameid, playerid;
        Connection c;
        Assignment assignment;
        Assignment assignment_otherPlayer;
        char curTurn = 'O';

        public Page2(string gameid, string playerid)
        {
            InitializeComponent();
            DisableButtons();

            this.gameid = gameid;
            this.playerid = playerid;

            Connect();

            fc.Child("Plays").OrderBy("gameid").EqualTo(this.gameid).AsObservable<Play>().Subscribe(p =>
            {
                if (p.Key == string.Empty || p.Object == null)
                    return;

                switch(p.EventType)
                {
                    case Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate:
                        Played(p.Object.x, p.Object.y, p.Object.XorO);
                        switch(p.Object.XorO)
                        {
                            case 'O':
                                curTurn = 'X';
                                break;
                            default:
                                curTurn = 'O';
                                break;
                        }
                        if(curTurn == assignment.XorO)
                        {
                            this.Dispatcher.Invoke(new Action(() => {
                                lblStatus.Content = "Your turn! You are playing as " + this.assignment.XorO;
                                Enablebuttons();
                            }));
                        }
                        else
                        {
                            this.Dispatcher.Invoke(new Action(() => {
                                lblStatus.Content = "Waiting for opponents turn.";
                            }));
                        }

                        // Check for victory:
                        this.Dispatcher.Invoke(new Action(() => {
                            char winner = ' ';
                            string ba, bb, bc;
                            Button a, b, c;
                            // Columns
                            for (int x = 0; x < 3; x++)
                            {
                                a = ((Button)this.FindName("btn" + x.ToString() + "0"));
                                b = ((Button)this.FindName("btn" + x.ToString() + "1"));
                                c = ((Button)this.FindName("btn" + x.ToString() + "2"));
                                if(a.Content != null && b.Content != null && c.Content != null)
                                {
                                    ba = a.Content.ToString();
                                    bb = b.Content.ToString();
                                    bc = c.Content.ToString();
                                    if (ba == bb && bb == bc)
                                    {
                                        winner = ba[0];
                                    }
                                }
                            }
                            // Rows
                            for (int x = 0; x < 3; x++)
                            {
                                a = ((Button)this.FindName("btn0" + x.ToString()));
                                b = ((Button)this.FindName("btn1" + x.ToString()));
                                c = ((Button)this.FindName("btn2" + x.ToString()));
                                if (a.Content != null && b.Content != null && c.Content != null)
                                {
                                    ba = a.Content.ToString();
                                    bb = b.Content.ToString();
                                    bc = c.Content.ToString();
                                    if (ba == bb && bb == bc)
                                    {
                                        winner = ba[0];
                                    }
                                }
                            }
                            // Diagonal
                            a = ((Button)this.FindName("btn00"));
                            b = ((Button)this.FindName("btn11"));
                            c = ((Button)this.FindName("btn22"));
                            if (a.Content != null && b.Content != null && c.Content != null)
                            {
                                ba = a.Content.ToString();
                                bb = b.Content.ToString();
                                bc = c.Content.ToString();
                                if (ba == bb && bb == bc)
                                {
                                    winner = ba[0];
                                }
                            }
                            a = ((Button)this.FindName("btn02"));
                            b = ((Button)this.FindName("btn11"));
                            c = ((Button)this.FindName("btn20"));
                            if (a.Content != null && b.Content != null && c.Content != null)
                            {
                                ba = a.Content.ToString();
                                bb = b.Content.ToString();
                                bc = c.Content.ToString();
                                if (ba == bb && bb == bc)
                                {
                                    winner = ba[0];
                                }
                            }

                            if (winner != ' ')
                            {
                                lblStatus.Content = "Game over! " + winner + " won the game.";
                                DisableButtons();
                            }
                        }));

                        break;
                }
            });

            fc.Child("Assignments").OrderBy("gameid").EqualTo(this.gameid).AsObservable<Assignment>().Subscribe(p =>
            {
                if (p.Key == string.Empty || p.Object == null)
                    return;

                switch (p.EventType)
                {
                    case Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate:
                        if (p.Object.playerid == this.playerid)
                            this.assignment = p.Object;
                        else
                            this.assignment_otherPlayer = p.Object;

                        if(this.assignment != null && this.assignment_otherPlayer != null)
                        {
                            this.Dispatcher.Invoke(new Action(() => {
                                String t = "";
                                if (curTurn == assignment.XorO)
                                {
                                    t += "Your turn! ";
                                    Enablebuttons();
                                }
                                t += "You are playing as " + this.assignment.XorO;

                                lblStatus.Content = t;
                            }));
                        }
                        break;
                }

            });

            this.Unloaded += Page2_Unloaded;
            this.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Disconnect();

            // Remove player
            fc.Child("Players").Child(this.playerid).DeleteAsync();
        }

        private void Page2_Unloaded(object sender, RoutedEventArgs e)
        {
            fc.Child("Connections").Child(c.id).DeleteAsync();
            Disconnect();
        }

        private async void Disconnect()
        {
            fc.Child("Connections").Child(c.id).DeleteAsync();

            IReadOnlyCollection<FirebaseObject<Connection>> connections = await fc.Child("Connections").OrderBy("gameid").EqualTo(this.gameid).OnceAsync<Connection>();
            if(connections.Count == 0)
            {
                // We were the last player to disconnect, so delete the game
                fc.Child("Games").Child(this.gameid).DeleteAsync();

                // Delete all plays of the game
                IReadOnlyCollection<FirebaseObject<Play>> plays = await fc.Child("Plays").OrderBy("gameid").EqualTo(this.gameid).OnceAsync<Play>();
                foreach (FirebaseObject<Play> p in plays)
                {
                    fc.Child("Plays").Child(p.Key).DeleteAsync();
                }

                // Delete the assignments
                IReadOnlyCollection<FirebaseObject<Assignment>> ass = await fc.Child("Assignments").OrderBy("gameid").EqualTo(this.gameid).OnceAsync<Assignment>();
                foreach (FirebaseObject<Assignment> p in ass)
                {
                    fc.Child("Assignments").Child(p.Key).DeleteAsync();
                }
            }
        }

        private async void Connect()
        {
            //TODO: Check how many players are connected

            c = new Connection { gameid = this.gameid, playerid = this.playerid };
            FirebaseObject<Connection> fo = await fc.Child("Connections").PostAsync(c);
            c.id = fo.Key;


            IReadOnlyCollection<FirebaseObject<Connection>> connections = await fc.Child("Connections").OrderBy("gameid").EqualTo(this.gameid).OnceAsync<Connection>();

            if(connections.Count == 1) // We are the first
            {
                lblStatus.Content = "Waiting for second player.";
            }
            else if (connections.Count == 2) // We are the second player
            {
                Random r = new Random();
                Connection forO;
                Connection forX;
                if (r.Next(2) == 0)
                {
                    forO = connections.ElementAt(0).Object;
                    forX = connections.ElementAt(1).Object;
                }
                else
                {
                    forO = connections.ElementAt(1).Object;
                    forX = connections.ElementAt(0).Object;
                }
                Assignment a = new Assignment() { gameid = this.gameid, playerid = forO.playerid, XorO = 'O' };
                Assignment b = new Assignment() { gameid = this.gameid, playerid = forX.playerid, XorO = 'X' };

                await fc.Child("Assignments").PostAsync(a);
                await fc.Child("Assignments").PostAsync(b);
            }
            else // Game is full
            {
                Disconnect();
                NavigationService.Navigate(new Page1());
            }
        }

        private void Played(int x, int y, char player)
        {
            this.Dispatcher.Invoke(new Action(() => {
                Button b = ((Button)this.FindName("btn" + x.ToString() + y.ToString()));
                b.Content = player; 
            }));
        }

        private async void Play(int x, int y)
        {
            DisableButtons();

            Play p = new Play();
            p.gameid = this.gameid;
            p.playerid = this.playerid;
            p.x = x;
            p.y = y;
            p.XorO = this.assignment.XorO;

            await fc.Child("Plays").PostAsync(p);
        }

        private void Enablebuttons()
        {
            for(int x=0; x<3; x++)
            {
                for (int y=0; y<3; y++)
                {
                    Button b = ((Button)this.FindName("btn" + x.ToString() + y.ToString()));
                    if(b.Content == null || b.Content.ToString() == string.Empty)
                    {
                        b.IsEnabled = true;
                    }
                }
            }
        }

        private void DisableButtons()
        {

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Button b = ((Button)this.FindName("btn" + x.ToString() + y.ToString()));
                    b.IsEnabled = false;
                }
            }
        }

        private void btn00_Click(object sender, RoutedEventArgs e)
        {
            Play(0, 0);
        }

        private void btn01_Click(object sender, RoutedEventArgs e)
        {
            Play(0, 1);
        }

        private void btn02_Click(object sender, RoutedEventArgs e)
        {
            Play(0, 2);
        }

        private void btn10_Click(object sender, RoutedEventArgs e)
        {
            Play(1, 0);
        }

        private void btn11_Click(object sender, RoutedEventArgs e)
        {
            Play(1, 1);
        }

        private void btn12_Click(object sender, RoutedEventArgs e)
        {
            Play(1, 2);
        }

        private void btn20_Click(object sender, RoutedEventArgs e)
        {
            Play(2, 0);
        }

        private void btn21_Click(object sender, RoutedEventArgs e)
        {
            Play(2, 1);
        }

        private void btn22_Click(object sender, RoutedEventArgs e)
        {
            Play(2, 2);
        }
    }
}

public class Player
{
    public string name;
}

public class Connection
{
    public string id;
    public string gameid;
    public string playerid;
}

public class Play
{
    public string id;
    public string playerid;
    public string gameid;
    public int x;
    public int y;

    public char XorO;
}

public class Assignment
{
    public string playerid;
    public string gameid;
    public char XorO;
}