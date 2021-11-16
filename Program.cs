using System;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Timers;
using System.Globalization;
using System.IO;
using EAGetMail;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace BOT_Raviing
{
    class Program
    {

        private string token = "DISCORD_TOKEN_HERE";
        private ulong info_bot_channel_id = 901923588733546516;
        private ulong edt_bot_channel_id = 902906494528417812;
        private ulong guild_id = 850182424271912961;
        private string meteo_api_key = "API_KEY_HERE";
        private string url_Amiens = "https://api.openweathermap.org/data/2.5/weather?id=3037854&appid=" + API_KEY_HERE + "e&units=metric";
        private string url_eu = "https://api.openweathermap.org/data/2.5/weather?id=3019329&appid=" + API_KEY_HERE + "e&units=metric";
        private ulong admin_role = 886293186601967617;
        private DiscordSocketClient _client;
        private DateTimeOffset interest = DateTimeOffset.Now;
        private LogMessage logMessage;
        private bool refresupdated = false;
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        private async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Info,
            });

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            _client.Ready += Create_Commands;
            _client.InteractionCreated += Client_InteractionCreated;
            _client.ButtonExecuted += MyButtonHandler;
            System.Timers.Timer timerEmail = new System.Timers.Timer();
            timerEmail.Elapsed += async (sender, e) => await Timing(sender, e);
            timerEmail.Interval = 60000;
            timerEmail.Enabled = true;

            System.Timers.Timer timerInterest = new System.Timers.Timer();
            timerInterest.Elapsed += async (sender, e) => await TimerInterest(sender, e);
            timerInterest.Interval = 600000;
            timerInterest.Enabled = true;
            Thread.Sleep(1500);
            await RefreshSkyblockInterests();
            await Email();
            await Task.Delay(-1);
        }

        public async Task Create_Commands()
        {
            var guild = _client.GetGuild(guild_id);
            var liens = new SlashCommandBuilder();
            var meteo = new SlashCommandBuilder();
            var interest = new SlashCommandBuilder();
            var edt = new SlashCommandBuilder();
            var clear = new SlashCommandBuilder();

            liens.WithName("liens");
            liens.WithDescription("Te permet d'avoir tous les liens liés à l'IUT");

            meteo.WithName("meteo");
            meteo.WithDescription("Obtient la metéo du jour");
            meteo.AddOption("lieu", ApplicationCommandOptionType.String, "Entre le nom de la ville sans espaces", true);

            interest.WithName("interest");
            interest.WithDescription("Te donne dans combien de temps sont les prochains intérêts de la banque du Skyblock d'Hypixel");

            edt.WithName("edt");
            edt.WithDescription("Vous renvois l'EDT");
            edt.AddOption("numedt", ApplicationCommandOptionType.Integer, "Entre le numéro de la semaine", true);
            edt.AddOption("jour", ApplicationCommandOptionType.String, "Entre le jour que tu veux", false);

            clear.WithName("clear");
            clear.WithDescription("Clear x message sur le channel ou la commande est faite.");
            clear.AddOption("nombre", ApplicationCommandOptionType.Integer, "Entrez un nombre inférieur à 100");

            

            try
            {
                await guild.CreateApplicationCommandAsync(meteo.Build());
                await guild.CreateApplicationCommandAsync(liens.Build());
                await guild.CreateApplicationCommandAsync(interest.Build());
                await guild.CreateApplicationCommandAsync(edt.Build());
                await guild.CreateApplicationCommandAsync(clear.Build());
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            if (arg is SocketSlashCommand command)
            {
                ulong user_id = command.User.Id;
                SocketGuild guild = _client.GetGuild(guild_id);
                SocketGuildUser user = guild.GetUser(user_id);

                if (user != null)
                {
                    if(command.Data.Name == "liens")
                    {
                        var msgButton = new ComponentBuilder()
                       .WithButton("Moodle UPJV", null, ButtonStyle.Link, row: 0, url: "https://pedag.u-picardie.fr/moodle/upjv/")
                       .WithButton("WebMail", null, ButtonStyle.Link, row: 0, url: "https://webmail.etud.u-picardie.fr/")
                       .WithButton("Sconotes", null, ButtonStyle.Link, row: 0, url: "https://etud.iut-amiens.fr/notes/")
                       .WithButton("Ton site", null, ButtonStyle.Link, row: 0, url: "https://yannamours.fr/");
                        await command.Channel.SendMessageAsync($"Liens UPJV Related", component: msgButton.Build());
                        await command.RespondAsync("Commande éxécutée avec succés", ephemeral: true);
                    }
                    if(command.Data.Name == "meteo")
                    {
                        string lieu = (string)command.Data.Options.First().Value;
                        Console.WriteLine("Lieu: " + lieu);
                        string result;
                        if (lieu == "Amiens" || lieu == "amiens")
                        {
                            var httpRequest = (HttpWebRequest)WebRequest.Create(url_Amiens);

                            httpRequest.Accept = "application/json";


                            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                result = streamReader.ReadToEnd();
                            }

                            Console.WriteLine(httpResponse.StatusCode);

                            dynamic data = JsonConvert.DeserializeObject(result);

                            Console.WriteLine(data);

                            string etat_meteo = data.weather[0].main;
                            string temperature = data.main.temp;
                            string min_temperature = data.main.temp_min;
                            string max_temperature = data.main.temp_max;
                            string wind_speed = data.wind.speed;


                            EmbedBuilder embedbuilder = new EmbedBuilder()
                                .WithTitle("Meteo de Amiens")
                                .AddField("Actuellement: ", etat_meteo, false)
                                .AddField("Temperature: ", temperature + "°C", false)
                                .AddField("Vent: ", wind_speed + "km/h", false)
                                .WithFooter($"Envoyé le {DateTime.Now}")
                                .WithColor(Discord.Color.DarkMagenta);
                            Embed embed1 = embedbuilder.Build();
                            await arg.RespondAsync("",embed: embed1, isTTS: false);
                        }
                        else if(lieu == "Eu" || lieu == "eu")
                        {
                            var httpRequest = (HttpWebRequest)WebRequest.Create(url_eu);

                            httpRequest.Accept = "application/json";


                            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                result = streamReader.ReadToEnd();
                            }

                            dynamic data = JsonConvert.DeserializeObject(result);

                            Console.WriteLine(data);

                            string etat_meteo = data.weather[0].main;
                            string temperature = data.main.temp;
                            string min_temperature = data.main.temp_min;
                            string max_temperature = data.main.temp_max;
                            string wind_speed = data.wind.speed;


                            EmbedBuilder embedbuilder = new EmbedBuilder()
                                .WithTitle("Meteo de Eu")
                                .AddField("Actuellement: ", etat_meteo, false)
                                .AddField("Temperature: ", temperature + "°C", false)
                                .AddField("Vent: ", wind_speed + "km/h", false)
                                .WithFooter($"Envoyé le {DateTime.Now}")
                                .WithColor(Discord.Color.DarkMagenta);
                            Embed embed1 = embedbuilder.Build();
                            await arg.RespondAsync("", embed: embed1, isTTS: false);
                        }
                        
                    }
                    if(command.Data.Name == "interest")
                    {
                        TimeSpan sec = interest - DateTimeOffset.Now;
                        EmbedBuilder embedbuilder = new EmbedBuilder()
                               .WithTitle("Intérêts")
                               .AddField("Dans: ", sec.Days + " jours, " + sec.Hours + " heures, " + sec.Minutes + " minutes", false)
                               .AddField("A: ", "Le " + interest.ToLocalTime().Day + "/" + interest.ToLocalTime().Month + "/" + interest.ToLocalTime().Year + " à " + interest.ToLocalTime().Hour + "h" + interest.ToLocalTime().Minute, false)
                               .WithFooter($"Envoyé le {DateTime.Now}")
                               .WithColor(Discord.Color.Gold);
                        Embed embed1 = embedbuilder.Build();
                        await arg.RespondAsync("", embed: embed1, isTTS: false);
                    }
                    if(command.Data.Name == "edt")
                    {
                        // jour numEDT
                        Dictionary<string, string> options = new Dictionary<string, string>();
                        foreach (SocketSlashCommandDataOption option in command.Data.Options)
                        {
                            if(option.Value != null || (string)option.Value == "")
                            {
                                options.Add(option.Name, option.Value.ToString());
                                logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Command find value for " + option.Name + " = " + option.Value.ToString());
                                await Log(logMessage);
                            }
                        }
                        if(options.Count() == 2)
                        {
                            //Jour demandé
                            logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "2 Value detected");
                            await Log(logMessage);
                            bool success = false;
                            string edt_directory = Directory.GetCurrentDirectory() + @"\edt\";
                            foreach (string folder in Directory.GetDirectories(edt_directory))
                            {
                                Console.WriteLine(folder.Contains(options["numedt"]) + " - " + folder);
                                if (folder.Contains(options["numedt"]) && !folder.Contains(".pdf"))
                                {
                                    logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Folder finded = " + folder);
                                    await Log(logMessage);
                                    string jour = options["jour"];
                                    if (jour == "Lundi" ||
                                        jour == "Mardi" ||
                                        jour == "Mercredi" ||
                                        jour == "Jeudi" ||
                                        jour == "Vendredi")
                                    {
                                        logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Day = " + jour);
                                        await Log(logMessage);
                                        int day = 0;
                                        switch (jour)
                                        {
                                            case "Lundi":
                                                {
                                                    day = 1;
                                                    break;
                                                }
                                            case "Mardi":
                                                {
                                                    day = 2;
                                                    break;
                                                }
                                            case "Mercredi":
                                                {
                                                    day = 3;
                                                    break;
                                                }
                                            case "Jeudi":
                                                {
                                                    day = 4;
                                                    break;
                                                }
                                            case "Vendredi":
                                                {
                                                    day = 5;
                                                    break;
                                                }
                                            case "Samedi":
                                                {
                                                    day = 6;
                                                    break;
                                                }
                                        }
                                        await SendEDT(folder, day, _client.GetGuild(guild_id).GetTextChannel(arg.Channel.Id));
                                        success = true;
                                    }
                                    else
                                    {
                                        await arg.RespondAsync("Vous n'avez pas saisi un jour valide.");
                                    }
                                }
                            }
                            if (success)
                            {
                                logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Command successful !");
                                await Log(logMessage);
                            }
                            else
                            {
                                await arg.RespondAsync("Je n'ai pas trouvé le " + options["jour"] + " de l'EDT " + options["numedt"]);
                                logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Command failed !");
                                await Log(logMessage);
                            }
                        }
                        else if(options.Count() == 1)
                        {
                            //Semaine complete
                            bool success = false;
                            string edt_directory = Directory.GetCurrentDirectory() + @"\edt\";
                            foreach (string folder in Directory.GetDirectories(edt_directory))
                            {
                                Console.WriteLine(folder);
                                if (folder.Contains(options["numedt"]) && !folder.Contains(".pdf"))
                                {
                                    logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Folder finded... !");
                                    await Log(logMessage);
                                    logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Image find = " + folder);
                                    await Log(logMessage);
                                    await SendEDT(folder, 99, _client.GetGuild(guild_id).GetTextChannel(arg.Channel.Id));
                                    await arg.RespondAsync("Command réussi !");
                                    success = true;
                                }
                                else
                                {
                                    await arg.RespondAsync("Ce numéro d'EDT n'existe pas.");
                                }
                            }
                            if (success)
                            {
                                logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Command successful !");
                                await Log(logMessage);
                            }
                            else
                            {
                                logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Command failed !");
                                await Log(logMessage);
                            }
                        }
                    }
                    if(command.Data.Name == "clear")
                    {
                        int nb = Convert.ToInt32(command.Data.Options.First().Value);
                        if(nb < 100)
                        {
                            ITextChannel channel = _client.GetGuild(guild_id).GetTextChannel(arg.Channel.Id);

                            IAsyncEnumerable<IMessage> messages = channel.GetMessagesAsync(nb).Flatten();
                            await arg.RespondAsync("Commande éxécutée", ephemeral: true);
                            logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "Deleting " +nb +" messages...");
                            await Log(logMessage);
                            if (await messages.CountAsync() != 0)
                            {
                                Thread thread = new Thread(new ThreadStart(removeMsgAsync));

                            }
                            else
                            {
                                await arg.RespondAsync("Pas de messages à supprimer", ephemeral: true);
                            }
                        }
                        else
                        {
                            await arg.RespondAsync("Nombre de messages trop grand, max 99");
                        }
                    }
                }
                else
                    await command.RespondAsync($"Erreur, impossible d'avoir l'utilisateur.");
            }
        }

        public async Task MyButtonHandler(SocketMessageComponent component)
        {
            SocketGuildUser user = (SocketGuildUser)component.User;
            SocketGuild guild = user.Guild;

            switch (component.Data.CustomId)
            {
                case "moodle":
                    {
                        break;
                    }
                case "webmail":
                    {
                        break;
                    }
                case "sconotes":
                    {
                        break;
                    }
            }
        }
        public async Task Timing(object source, ElapsedEventArgs e)
        {
            logMessage = new LogMessage(LogSeverity.Info, "Bot", "Checking new emails...");
            await Email();
        }
        public async Task TimerInterest(object source, ElapsedEventArgs e)
        {
            logMessage = new LogMessage(LogSeverity.Info, "Bot", "Checking interest date...");
            await RefreshSkyblockInterests();
        }

        public async Task Email()
        {
            try
            {
                var date = DateTime.Now;
                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);

                MailServer oServer = new MailServer("imap.gmail.com", "EMAIL_HERE", "PASSWORD_HERE", ServerProtocol.Imap4);

                oServer.SSLConnection = true;

                oServer.Port = 993;

                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);

                oClient.GetMailInfosParam.Reset();
                oClient.GetMailInfosParam.GetMailInfosOptions = GetMailInfosOptionType.NewOnly;

                MailInfo[] infos = oClient.GetMailInfos();
                logMessage = new LogMessage(LogSeverity.Info, "Bot", "Total " + infos.Length + " unread email(s)");
                await Log(logMessage);
                for (int i = 0; i < infos.Length; i++)
                {
                    MailInfo info = infos[i];
                    logMessage = new LogMessage(LogSeverity.Info, "Bot", "Index:" + info.Index + " Size: " + info.Size + "; UIDL: " + info.UIDL);
                    await Log(logMessage);
                    // Receive email from IMAP4 server
                    Mail oMail = oClient.GetMail(info);
                    logMessage = new LogMessage(LogSeverity.Info, "Bot", "From: " + oMail.From.ToString());
                    await Log(logMessage);
                    logMessage = new LogMessage(LogSeverity.Info, "Bot", "Subject: " + oMail.Subject);
                    await Log(logMessage);
                    EmbedBuilder embed = new EmbedBuilder();
                    SocketRole admin = _client.GetGuild(guild_id).GetRole(admin_role);

                    if (oMail.Subject.Contains("EDT"))
                    {
                        await _client.GetGuild(guild_id).GetTextChannel(info_bot_channel_id).SendMessageAsync("Nouvel EDT " + admin.Mention);
                    }

                    var msgButton = new ComponentBuilder()
                       .WithButton("Gmail", null, ButtonStyle.Link, row: 0, url: "https://mail.google.com/mail/u/1/#inbox")
                       .WithButton("WebMail", null, ButtonStyle.Link, row: 0, url: "https://webmail.etud.u-picardie.fr/");
                    embed.WithTitle("Nouveau Mail !")
                        .AddField("De: ", oMail.From.ToString(), false)
                        .AddField("Sujet: ", oMail.Subject.ToString().Remove(oMail.Subject.ToString().Length - 15, 15), false);
                    if (oMail.TextBody.Length < 1024 && oMail.TextBody.Length == 0)
                    {
                        embed.AddField("Content: ", oMail.TextBody.ToString(), false);
                    }
                    else if (oMail.TextBody.ToString() == "" || oMail.TextBody.ToString() == null)
                        embed.AddField("Content: (Reduced)", oMail.TextBody.ToString().Substring(0, 1020) + "...");

                    embed.WithFooter($"Envoyé le {oMail.SentDate}").WithColor(Discord.Color.Blue);

                    await _client.GetGuild(guild_id).GetTextChannel(info_bot_channel_id).SendMessageAsync(" ", false, embed.Build(), component: msgButton.Build());

                    EAGetMail.Attachment[] attachments = oMail.Attachments;
                    string edt_name = "";
                    bool new_edt = false;
                    if (attachments.Count() > 0)
                    {
                        try
                        {
                            foreach (EAGetMail.Attachment attachment in attachments)
                            {
                                if (attachment.Name.Contains("Semaine") || attachment.Name.Contains("SEMAINE") || attachment.Name.Contains("semaine"))
                                {
                                    logMessage = new LogMessage(LogSeverity.Info, "Bot - EDT", "EDT detecte");
                                    await Log(logMessage);
                                    attachment.SaveAs("edt/" + attachment.Name, true);
                                    attachment.SaveAs("inbox/" + attachment.Name, true);
                                    new_edt = true;
                                    edt_name = attachment.Name;
                                }
                                else
                                {
                                    attachment.SaveAs("inbox/" + attachment.Name, true);
                                }
                            }
                        }
                        catch (Exception er)
                        {
                            Console.WriteLine(er.Message);
                            throw;
                        }
                    }
                    string currentdirectory = Directory.GetCurrentDirectory() + @"\inbox\";
                    List<string> attachmentpath = new List<string>();
                    if (Directory.GetFiles(currentdirectory).Length > 0)
                    {
                        foreach (string file in Directory.GetFiles(currentdirectory))
                        {
                            if (new FileInfo(file).Length > 8000000)
                            {
                                await _client.GetGuild(guild_id).GetTextChannel(info_bot_channel_id).SendMessageAsync("Il existe une piéce jointe mais trop lourde pour être envoyé ici");
                            }
                            else
                                await _client.GetGuild(guild_id).GetTextChannel(info_bot_channel_id).SendFileAsync(file, "");
                            File.Delete(file);
                        }
                    }
                    if (new_edt)
                    {
                        string edt_folder_path = "";
                        if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\edt\"))
                        {
                            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\edt\");
                        }
                        foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory() + @"\edt\"))
                        {
                            if (file.Contains(".pdf"))
                            {
                                edt_folder_path = file.Remove(file.Length - 4) + @"\";
                                if (!Directory.Exists(edt_folder_path))
                                {
                                    logMessage = new LogMessage(LogSeverity.Info, "Bot - EDT", "New EDT find " + file);
                                    await Log(logMessage);
                                    try
                                    {
                                        Directory.CreateDirectory(edt_folder_path);
                                        logMessage = new LogMessage(LogSeverity.Info, "Bot - EDT", "Loading file: " + file);
                                        await Log(logMessage);
                                        var document = PdfiumViewer.PdfDocument.Load(file);
                                        for (int x = 0; x < document.PageCount; x++)
                                        {
                                            var image = document.Render(x, 300, 300, true);
                                            image.Save(edt_folder_path + edt_name.Remove(edt_name.Length - 4) + " - " + x + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                            logMessage = new LogMessage(LogSeverity.Info, "Bot - EDT", edt_name + " - Page " + x + " saved at: " + edt_folder_path + @"\edt\" + edt_name + x + ".png");
                                            await Log(logMessage);
                                        }
                                        await SendEDT(edt_folder_path, 99, _client.GetGuild(guild_id).GetTextChannel(edt_bot_channel_id));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                            }
                        }
                    }
                    if (!info.Read)
                    {
                        oClient.MarkAsRead(info, true);
                    }
                }
                // Quit and expunge emails marked as deleted from IMAP4 server.
                oClient.Quit();
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
                Console.WriteLine(ep.StackTrace);
                Console.WriteLine(ep.InnerException);
            }
        }

        public async Task RefreshSkyblockInterests()//Permet d'être notifier quand le cooldown d'une fonctionnalité d'un jeu vidéo est remis à 0.
        {
            var date = DateTime.Now;
            date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
            var httpRequest = (HttpWebRequest)WebRequest.Create("https://hypixel-api.inventivetalent.org/api/skyblock/bank/interest/estimate");

            httpRequest.Accept = "application/json";
            string result;

            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            dynamic data = JsonConvert.DeserializeObject(result);
            long ms = data.estimate;
            DateTimeOffset due = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            if (DateTime.Now.ToLocalTime() > interest.ToLocalTime() && refresupdated)
            {
                await _client.GetGuild(guild_id).GetTextChannel(info_bot_channel_id).SendMessageAsync("Les intêrets sont prêts sur le Skyblock d'Hypixel");
            }
            else
                refresupdated = true;
            interest = due;
            logMessage = new LogMessage(LogSeverity.Info, "Bot ", "Interest Resreshed: " + interest.ToLocalTime());
            await Log(logMessage);
        }

        public async Task SendEDT(string folder_path, int page_number, ITextChannel textChannel)
        {
            if(page_number == 99)
            {
                try
                {
                    if (Directory.Exists(folder_path))
                    {
                        foreach (string page in Directory.GetFiles(folder_path))
                        {
                            logMessage = new LogMessage(LogSeverity.Info, "Bot - EDT", "Folder has been found");
                            await Log(logMessage);
                            await _client.GetGuild(guild_id).GetTextChannel(edt_bot_channel_id).SendFileAsync(page, "");
                        }
                    }
                }
                catch (Exception ep)
                {
                    Console.WriteLine(ep.Message);
                    Console.WriteLine(ep.StackTrace);
                    Console.WriteLine(ep.InnerException);
                    throw;
                }
            }
            else
            {
                try
                {
                    if (Directory.Exists(folder_path))
                    {
                        foreach (string page in Directory.GetFiles(folder_path))
                        {
                            string number = page.Remove(0, page.Length - 5);
                            string final_number = number.Substring(0,1);
                            if (page_number == Convert.ToInt64(final_number))
                            {
                                logMessage = new LogMessage(LogSeverity.Info, "Bot - EDT", "Page number" + final_number + " has been found");
                                await Log(logMessage);
                                await textChannel.SendFileAsync(page, "");
                            }
                        }
                    }
                }
                catch (Exception ep)
                {
                    Console.WriteLine(ep.Message);
                    Console.WriteLine(ep.StackTrace);
                    Console.WriteLine(ep.InnerException);
                    throw;
                }
            }
        }

        public async Task removeMsgAsync()
        {
            IAsyncEnumerable<IMessage> messages
            int nb = 0;
            await foreach (IMessage msg in messages)
            {
                logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "A message have successfully been deleted");
                Log(logMessage);
                msg.DeleteAsync();
                nb++;
                Thread.Sleep(1000);
            }
            logMessage = new LogMessage(LogSeverity.Info, "Bot - CMD", "I have delete " + nb + " messages");
            Log(logMessage);
        }

    }
}