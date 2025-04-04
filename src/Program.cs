﻿using Discord; // Discord API for bot in:eraction
using Discord.WebSocket; // Provides the WebSocket-based client
using DotNetEnv; // To load environment variables from a `.env` file
using System; // Basic system functionalities like Console output
using System.Threading.Tasks; // For asynchronous programming
using System.Threading;
using Discord.Commands;
using Microsoft.Win32.SafeHandles; // For command handling (though not used here)
using Database;

public class Program
{   
	public static List<User> users = new List<User>();
	public static DB dataBase = new DB();
	public static DiscordSocketClient _client;
	public static int ThreadCount = 0;

	// Main entry point of the application
	private static async Task Main(string[] args)
	{	
		Env.Load();
		// Configure the bot client with necessary intents to track guilds, members, and presence updates
		var socketConfig = new DiscordSocketConfig
		{ 		 
			GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
		};

		// Initialize the Discord socket client with the configuration
		_client = new DiscordSocketClient(socketConfig);

		// Retrieve the bot token from the environment variables
		string token = Env.GetString("TOKEN");

		// Subscribe to events that the bot should listen to
		_client.PresenceUpdated += ActivityHandler; // Event triggered when a user's presence changes (activity, status, etc.)
		_client.Log += LogMessage; // Event triggered for logging messages from the bot
		_client.Ready += CreateCommand;
		_client.SlashCommandExecuted += CommandHandler;

		// Log in the bot with the token a:wnd start the connection
		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();

		// Keep the bot running indefinitely
		await Task.Delay(-1);
	}

	// This method is called whenever a new log message is generated (for debugging or logging purposes)
	private static async Task LogMessage(LogMessage message)
	{
		// Print the log message to the console
		Console.WriteLine(message);

		await Task.CompletedTask;
	}

	//this method creates commands
	public static async Task CreateCommand()
	{
		SlashCommandBuilder testCommand = new SlashCommandBuilder();
		testCommand.WithName("weekgame");
		testCommand.WithDescription("shows most played games of week and times they were played");
		await _client.CreateGlobalApplicationCommandAsync(testCommand.Build());
	}

	//this method is more of a method that not handles but distributes handlers and commands
	public static async Task CommandHandler(SocketSlashCommand command)
	{
		switch(Convert.ToString(command.CommandName))
			{
				case "weekgame":
					WeekGame[] localWeekGame = await dataBase.FindMostPlayedGameWeek();
					Console.WriteLine(localWeekGame.Length);
					if(localWeekGame.Length == 0)
					{
						await command.RespondAsync("No records found");
						break;
					}
					Console.WriteLine(localWeekGame.Length);

					var some = CreateEmbedMessage(localWeekGame);
					await command.RespondAsync(embed: some);
					break;
			}
	}

	//this method is used for creating embed messages
	private static Embed CreateEmbedMessage(Array someArray) 
	{
		EmbedBuilder embed = new EmbedBuilder
			{
				Title = "Top " + someArray.Length + " games played in week",
			};

		foreach(WeekGame someValue in someArray)
		{
			embed.AddField(someValue.gameName, $"{someValue.playTime.Hours}h {someValue.playTime.Minutes}m");
		}
		
		var result = embed.Build();

		return result;
	}

	//This method is called when a user's activity or status changes (presence update)
	private static async Task ActivityHandler(SocketUser user, SocketPresence oldPresence, SocketPresence newPresence)
	{
		var oldPresenceVar = oldPresence.Activities.Where(e => Convert.ToString(e.Type) == "Playing");
		var newPresenceVar = newPresence.Activities.Where(e => Convert.ToString(e.Type) == "Playing");

		//variable to check if we should do if statement little down below
		bool shouldDo = false;
		string gameName = "";

		//just checks if user closed game and i am doing == Playing couse activity could also be listening to spotify
		if(
			oldPresenceVar.Count() > newPresenceVar.Count()
		)
		{
			for(int index = 0; index <= oldPresenceVar.Count(); index++)
			{
				if(newPresenceVar.Count() == index || oldPresenceVar.ToArray()[index].Name == newPresenceVar.ToArray()[index].Name) 
				{
					gameName = Convert.ToString(oldPresenceVar.ToArray()[index].Name);
					break;
				}
			}

			if(await dataBase.FindPlayer(Convert.ToString(user.AvatarId)) == false)
			{
				dataBase.AddPlayer(Convert.ToString(user.AvatarId));
			}

			if(await dataBase.FindGame(gameName) == false && await dataBase.FindPlayer(Convert.ToString(user.AvatarId)) == true && gameName != "")
			{
				dataBase.AddGame(gameName);
			}

			shouldDo = true;
		}

		//this is basically handler to closing game
		if(shouldDo == true)
		{
			Console.WriteLine("quit");
			//we create User object here and also get time game was open
			User localUser = new User(Convert.ToString(user), Convert.ToString(gameName));	
			if(users.Any(e => e.userName ==  localUser.userName && e.game == localUser.game))
			{	
				int index = users.FindIndex(u => u.userName == localUser.userName && u.game == localUser.game);
				Console.WriteLine(DateTime.Now.Subtract(users[index].date));
				dataBase.AddPlayerGame(await dataBase.FindGameId(gameName), user.AvatarId, DateTime.Now.Subtract(users[index].date));

				users.RemoveAt(index);
			}
		}

		if(shouldDo == false && user.Activities.Any(e => Convert.ToString(e.Type) == "Playing"))
		{	
			string newGame = Convert.ToString(newPresenceVar.First().Name);
			User localUser = new User(Convert.ToString(user), newGame);
			if(!users.Any(e => e.userName == localUser.userName && e.game == localUser.game) && !users.Any(e => e.userName == localUser.userName))
			{
				users.Add(localUser);
				Console.WriteLine("game added");
			}
		}
	}
}


//this classs is just used for calculating time that someone played the game
public class User
{
	public string userName {get; }
	public DateTime date {get; }
	public string game {get; }

	public User(string UserName, string Game)
	{
		userName = UserName;
		date = DateTime.Now;
		game = Game;
	}
}
