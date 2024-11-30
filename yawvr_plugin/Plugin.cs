using SamplePlugin.Properties;
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
			"PITCH","YAW","ROLL"
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
			new Thread(() =>
			{
				int port = 20777; // The UDP port to listen on
				UdpClient udpClient = new UdpClient(port);
				IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

				Console.WriteLine($"Listening for UDP packets on port {port}...");

				while (!tokenSource.IsCancellationRequested)
				{
					try
					{
						// Receive UDP data
						byte[] data = udpClient.Receive(ref remoteEndPoint);

						// Ensure the data has the expected length (5 floats = 20 bytes)
						if (data.Length == 20)
						{
							// Parse the first three floats: pitch, yaw, roll
							float pitch = BitConverter.ToSingle(data, 0);
							float yaw = BitConverter.ToSingle(data, 4);
							float roll = BitConverter.ToSingle(data, 8);
							float last_rumble_left = BitConverter.ToSingle(data, 12);
							float last_rumble_right = BitConverter.ToSingle(data, 16);

							(float pitch_noisy, float yaw_noisy, float roll_noisy) = AddRumbleToMotion(pitch, yaw, roll, last_rumble_left, last_rumble_right, 2.5f);

							// Forward the values to the app
							controller.SetInput(0, pitch_noisy);
							controller.SetInput(1, yaw_noisy);
							controller.SetInput(2, roll_noisy);
						}
						else
						{
							Console.WriteLine($"Unexpected data size: {data.Length} bytes. Skipping packet.");
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

					Thread.Sleep(20); // Maintain 50 Hz update rate
				}

				udpClient.Close();
			}).Start();
		}


		public (float finalPitch, float finalYaw, float finalRoll) AddRumbleToMotion(
			float pitch,
			float yaw,
			float roll,
			float lastRumbleLeft,
			float lastRumbleRight,
			float max_rumble_degree)
		{
			// Normalize rumble values (0 to 65535) to a range for vibration intensity (e.g., 0 to 1)
			double intensityLeft = lastRumbleLeft / 65535.0;
			double intensityRight = lastRumbleRight / 65535.0;

			// Average the intensities
			double combinedIntensity = (intensityLeft + intensityRight) / 2.0;

			// Maximum noise deviation (in degrees)
			double maxDeviation = max_rumble_degree * combinedIntensity;

			// Add random noise to yaw, pitch, and roll
			float noiseYaw = GetRandomNoise(maxDeviation);
			float noisePitch = GetRandomNoise(maxDeviation);
			float noiseRoll = GetRandomNoise(maxDeviation);

			// Apply noise
			float finalYaw = yaw + noiseYaw;
			float finalPitch = pitch + noisePitch;
			float finalRoll = roll + noiseRoll;

			return (finalYaw, finalPitch, finalRoll);
		}

		private float GetRandomNoise(double maxDeviation)
		{
			// Generate random noise in the range -maxDeviation to +maxDeviation
			return (float)((random.NextDouble() * 2 - 1) * maxDeviation);
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
