using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Tournabot
{
    public class Scrim
    {
        private ulong organizerId, messageId;
        private Timer timer;
        private Program program;
        private string region, code;
        private Dictionary<ulong, string> players, director;
        private int tolerance;
        private bool started;

        public Scrim(ulong id, string reg, ulong message, int tol, Program prog)
        {
            organizerId = id;
            region = reg;
            messageId = message;
            tolerance = tol;
            program = prog;
            code = "";
            started = false;
            director = new Dictionary<ulong, string>();
            players = new Dictionary<ulong, string>();
            timer = new Timer(300000);
            timer.Elapsed += TimerTick;
            timer.Start();
        }

        private async void TimerTick(object sender, ElapsedEventArgs e)
        {
            if(players.Count < tolerance || code == "" || started)
            {
                timer.Stop();
                timer.Dispose();
                await program.RemoveScrimInstance(organizerId);
            }
            else
            {
                await program.StartScrim(organizerId);
            }
        }

        public ulong GetScrimId()
        {
            return organizerId;
        }

        public string GetRegion()
        {
            return region;
        }

        public ulong GetMessageId()
        {
            return messageId;
        }

        public string GetCode()
        {
            return code;
        }

        public Dictionary<ulong, string> GetPlayers()
        {
            return players;
        }

        public Dictionary<ulong, string> GetDirector()
        {
            return director;
        }

        public async Task<string> AddPlayer(ulong id, string name)
        {
            string message = "";
            foreach (KeyValuePair<ulong, string> dir in director)
            {
                if (dir.Key == id)
                {
                    message = "You already signed up as a director!";
                    return message;
                }
            }
            if (players.ContainsKey(id))
            {
                message = "You already signed up for the scrim!";
            }
            else if (players.Count < 10)
            {
                players.Add(id, name);
                message = "You successfully signed up for the scrim!";
                await TriggerOption();
            }
            else
            {
                message = "The scrim is already filled.";
            }
            return message;
        }

        public async Task<string> AddDirector(ulong id, string name)
        {
            string message = "";
            foreach (KeyValuePair<ulong, string> player in players)
            {
                if (player.Key == id)
                {
                    message = "You already signed up as a player!";
                    return message;
                }
            }
            if (director.Count == 0)
            {
                director.Add(id, name);
                message = "You successfully signed up to direct the scrim!";
                await TriggerOption();
            }
            else
            {
                message = "The scrim already has a director.";
            }
            return message;
        }

        public string SetCode(string cd)
        {
            string message;
            if (cd.Length != 4)
            {
                message = "Incorrect code provided, must be 4 characters long.";
            }
            else if (code.Length != 0)
            {
                code = cd;
                message = "Code successfully changed! If you set this code to start another match with the same group. Go ahead and do !start when you are ready.";
            }
            else
            {
                code = cd;
                message = "Code set! Just wait for all players/directors to join and you will be notified when your scrim is filled. You can also do !start to begin the scrim at anytime.";
            }
            return message;
        }

        public async Task TriggerOption()
        { 
            if (players.Count == 10)
            {
                string message = "";
                if (code != "")
                {
                    if (director.Count != 0)
                    {
                        message = "You have a full match with a director! You can use !start right now to begin the scrim session.";
                    }
                    else
                    {
                        message = "You have a full match without a director. You can wait for a potential director, or you can just start the scrim without one by using the !start command.";
                    }
                }
                else
                {
                    message = "You have a full match, but have not provided a code yet. You can do so by using the command !code *lobby code*. Then you can use !start to begin the scrim.";
                }
                await program.AlertScrimOrganizer(organizerId, message);
            }
        }

        public void SetTimer(int mills)
        {
            started = true;
            timer.Stop();
            timer.Interval = mills;
            timer.Start();
        }

        public void EndScrim()
        {
            timer.Stop();
            timer.Dispose();
        }
    }
}