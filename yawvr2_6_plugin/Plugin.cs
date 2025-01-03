using AceCombat7Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Threading;
using YawGEAPI;
using System.Threading.Tasks;
using System.Diagnostics;


namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Ace Combat 7")]
    [ExportMetadata("Version", "1.0")]
    public class Plugin : Game
    {

		private int port = 20777; // The UDP port to listen on
		private UdpClient udpClient = null;
		private Random random = new Random();
        public int STEAM_ID => 502500; // Game's SteamID. App will lauch game based on this

        public string PROCESS_NAME => "Ace7Game"; // The gameprocess name. App will wait/monitor this process for different features like autostart.

        public bool PATCH_AVAILABLE => false; // Tell app if patch is needed. "Patch" Button will appear -> Needed manually

        public string AUTHOR => "McFredward"; // Creator, will show this on plugin manager

        public System.Drawing.Image Logo => Resources.logo; // Logo for the main Library list

        public System.Drawing.Image SmallLogo => Resources.recent; // Logo for Library->Recent list

        public System.Drawing.Image Background => Resources.wide; // Wide logo for Description

		public string Description => @"
			<!DOCTYPE html>
			<html lang=""en"">
			<head>
				<meta charset=""UTF-8"">
				<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
				<title>UEVR Installation Guide</title>
			</head>
			<body>
				<h1>UEVR Installation Guide</h1>
				<h2>Installation Steps</h2>
				<ol>
					<li>Download <a href=""https://github.com/praydog/UEVR-nightly/releases"" target=""_blank"">the latest UEVR Nightly version</a> and extract it to any location.</li>
					<li>Download UESS from <a href=""https://www.nexusmods.com/acecombat7skiesunknown/mods/2474?tab=files"" target=""_blank"">this link</a> and extract the files into the game folder located at ""...\common\ACE COMBAT 7"".</li>
					<li>Create the following directories:
						<ul>
							<li>""ACE COMBAT 7\Game\Content\Paks\LogicMods""</li>
							<li>""ACE COMBAT 7\Game\Content\Paks\~LogicMods""</li>
						</ul>
					</li>
					<li>Download this mod from Nexus Mods: <a href=""https://www.nexusmods.com/acecombat7skiesunknown/mods/2387"" target=""_blank"">UEVR Compatibility Mod</a>.</li>
					<li>Extract the file ""UEVR_Compatibility_Mod_P.pak"" into ""ACE COMBAT 7\Game\Content\Paks\~LogicMods"".</li>
					<li>Download the UEVR Profile from <a href=""https://github.com/McFredward/ace-combat-uevr/releases"" target=""_blank"">this GitHub repository</a>.</li>
					<li>Launch UEVR and import the UEVR profile ""Ace7Game.zip"" using the ""Import Config"" option.</li>
				</ol>
				<h2>Starting the Game</h2>
				<ol>
					<li>Start UEVR.</li>
					<li>Launch the YawVR Game Engine.</li>
					<li>Select the Ace Combat 7 profile and start it.</li>
					<li>Launch Ace Combat 7.</li>
					<li>Choose ""Ace Combat 7"" in the Dropwdown Menu in UEVR and press ""Inject"".</li>
				</ol>
				<li>Made by McFredward based on the great work of keton, kosnag & praydog</li>
				<li><a href=""https://github.com/McFredward/ace-combat-uevr"" target=""_blank"">Sourcecode</a></li>
				<p>Happy Yaw'ing!</p>
			</body>
			</html>
			";


		private IMainFormDispatcher dispatcher; // this is our reference to the app. Features like showing dialog/notification can be used
        private IProfileManager controller; // this is our reference to profile manager. input values need to be passed to this


        //We'll provide these inputs to the app.. This can even be marshalled from a struct for example
        private string[] inputNames = new string[]
        {
			"YAW","PITCH","ROLL", "RUMBLE_INTENSITY"
        };

        
        private CancellationTokenSource tokenSource;
        /// <summary>
        /// Default LED profile
        /// </summary>
        public LedEffect DefaultLED()
        {
            // ask the dispatcher to convert our string to a LedEffect
            return dispatcher.JsonToLED(Resources.defaultProfile);
        }
        /// <summary>
        /// Default axis profile
        /// </summary>
        public List<Profile_Component> DefaultProfile()
        {
			// ask the dispatcher to convert our string to a axis profile
			return dispatcher.JsonToComponents(Resources.defaultProfile);
		}

        /// <summary>
        /// Will be called at plugin stop request
        /// </summary>
        public void Exit()
        {
            tokenSource?.Cancel();
			udpClient.Close();

		}

        /// <summary>
        /// App fetches available inputs through this
        /// </summary>
        /// <returns></returns>
        public string[] GetInputData()
        {
            return inputNames;
        }

		/// <summary>
		/// Features for the plugin. 
		/// These features can be setup to be called for different events like pushing arcade buttons
		/// </summary>
		public Dictionary<string, ParameterInfo[]> GetFeatures()
		{
			return new Dictionary<string, ParameterInfo[]>()
			{
			};
		}

		/// <summary>
		/// Will be called when app starts this plugin
		/// </summary>

		public void Init()
		{
			tokenSource = new CancellationTokenSource();
			Task.Run(() => StartListeningAsync(tokenSource.Token));
		}

		public async Task StartListeningAsync(CancellationToken cancellationToken)
		{
			try
			{
				udpClient = new UdpClient(port);
				udpClient.Client.ReceiveBufferSize = 262144; // Set to 256 KB
				IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
				Debug.WriteLine($"Listening for UDP packets on port {port}...");

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						// Receive UDP data asynchronously
						UdpReceiveResult result = await udpClient.ReceiveAsync();

						// Ensure the data has the expected length (5 floats = 20 bytes)
						if (result.Buffer.Length == 20)
						{
							float pitch = BitConverter.ToSingle(result.Buffer, 0);
							float yaw = BitConverter.ToSingle(result.Buffer, 4);
							float roll = BitConverter.ToSingle(result.Buffer, 8);

							float lastRumbleLeft = BitConverter.ToSingle(result.Buffer, 12);
							float lastRumbleRight = BitConverter.ToSingle(result.Buffer, 16);
							float normalizedCombinedRumble = ((lastRumbleLeft + lastRumbleRight) / 2.0f) / 65535.0f;

							string current_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
							Debug.WriteLine($"{current_time} - yaw: {yaw}, pitch: {pitch}, roll: {roll}, rumble: {normalizedCombinedRumble}");

							controller.SetInput(0, yaw);
							controller.SetInput(1, pitch);
							controller.SetInput(2, roll);
							controller.SetInput(3, normalizedCombinedRumble);
						}
						else
						{
							string current_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
							Debug.WriteLine($"{current_time}: Unexpected data size: {result.Buffer.Length} bytes. Skipping packet.");
						}
					}
					catch (OperationCanceledException)
					{
						Debug.WriteLine("UDP listening task was canceled.");
					}
					catch (SocketException ex)
					{
						Debug.WriteLine($"Socket error: {ex.Message}");
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error: {ex.Message}");
					}
				}
			}
			finally
			{
				// Ensure proper cleanup of UdpClient
				udpClient?.Dispose();
				Debug.WriteLine("Stopped listening for UDP packets.");
			}
		}

		public void PatchGame()
        {
            //Pass
        }


        /// <summary>
        /// The app will give us these references. We need to save them
        /// </summary>
        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
    }
}
