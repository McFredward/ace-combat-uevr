using AceCombat7Plugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Threading;
using YawGEAPI;


namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Ace Combat 7")]
    [ExportMetadata("Version", "1.0")]
    public class Plugin : Game
    {
		private Random random = new Random();
        public int STEAM_ID => 502500; // Game's SteamID. App will lauch game based on this

        public string PROCESS_NAME => "Ace7Game"; // The gameprocess name. App will wait/monitor this process for different features like autostart.

        public bool PATCH_AVAILABLE => false; // Tell app if patch is needed. "Patch" Button will appear -> Needed manually

        public string AUTHOR => "McFredward"; // Creator, will show this on plugin manager

        public System.Drawing.Image Logo => Resources.logo; // Logo for the main Library list

        public System.Drawing.Image SmallLogo => Resources.recent; // Logo for Library->Recent list

        public System.Drawing.Image Background => Resources.wide; // Wide logo for Description

        public string Description => "<h1>DescriptionText</h1>"; // Description HTML. App uses HTMLRenderer for this, color,links, headers, supported


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
			new Thread(async () =>
			{
				int port = 20777; // The UDP port to listen on
				UdpClient udpClient = new UdpClient(port);
				IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

				// Set a reasonable buffer size
				udpClient.Client.ReceiveBufferSize = 65536;

				Console.WriteLine($"Listening for UDP packets on port {port}...");

				// A task to handle receiving the most recent packet asynchronously
				byte[] latestPacket = null; // Store the most recent packet

				while (!tokenSource.IsCancellationRequested)
				{
					try
					{
						// Asynchronously receive data from the UDP socket
						UdpReceiveResult result = await udpClient.ReceiveAsync();

						// Ensure the data has the expected length (20 bytes)
						if (result.Buffer.Length == 20)
						{
							latestPacket = result.Buffer; // Only store the most recent packet
						}
						else
						{
							Console.WriteLine($"Unexpected data size: {result.Buffer.Length} bytes. Skipping packet.");
						}
					}
					catch (SocketException ex)
					{
						Console.WriteLine($"Socket error: {ex.Message}");
						break; // Exit loop on socket error
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error: {ex.Message}");
					}

					// Process the latest packet if available
					if (latestPacket != null)
					{
						float pitch = BitConverter.ToSingle(latestPacket, 0);
						float yaw = BitConverter.ToSingle(latestPacket, 4);
						float roll = BitConverter.ToSingle(latestPacket, 8);

						float lastRumbleLeft = BitConverter.ToSingle(latestPacket, 12);
						float lastRumbleRight = BitConverter.ToSingle(latestPacket, 16);
						float normalizedCombinedRumble = ((lastRumbleLeft + lastRumbleRight) / 2.0f) / 65535.0f;

						// Forward the values to the app
						controller.SetInput(0, yaw);
						controller.SetInput(1, pitch);
						controller.SetInput(2, roll);
						controller.SetInput(3, normalizedCombinedRumble);

						// Optionally reset latestPacket to null if you want to process it only once
						latestPacket = null; // Reset to avoid re-processing the same packet
					}

					Thread.Sleep(20); // Maintain 50 Hz update rate
				}

				udpClient.Close();
			}).Start();
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
