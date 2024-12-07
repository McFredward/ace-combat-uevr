using AceCombat7_dotnet8.Properties;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using YawGLAPI;


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

		public Stream Logo => GetStream("logo.png"); // Logo for the main Library list

		public Stream SmallLogo => GetStream("recent.png"); // Logo for Library->Recent list

		public Stream Background => GetStream("wide.png"); // Wide logo for Description

		public string Description => Resources.desc;


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
						if (data.Length >= 20)
						{
							// Parse the first three floats: pitch, yaw, roll
							float pitch = BitConverter.ToSingle(data, 0);
							float yaw = BitConverter.ToSingle(data, 4);
							float roll = BitConverter.ToSingle(data, 8);

							float lastRumbleLeft = BitConverter.ToSingle(data, 12);
							float lastRumbleRight = BitConverter.ToSingle(data, 16);
							float normalizedCombinedRumble = ((lastRumbleLeft + lastRumbleRight) / 2.0f) / 65535.0f;

							string current_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
							//Debug.WriteLine($"{current_time} - yaw: {yaw}, pitch: {pitch}, roll: {roll}, rumble: {normalizedCombinedRumble}");

							// Forward the values to the app
							controller.SetInput(0, yaw);
							controller.SetInput(1, pitch);
							controller.SetInput(2, roll);
							controller.SetInput(3, normalizedCombinedRumble);
						}
						else
						{
							Debug.WriteLine($"Unexpected data size: {data.Length} bytes. Skipping packet.");
						}
					}
					catch (SocketException ex)
					{
						Debug.WriteLine($"Socket error: {ex.Message}");
						break; // Exit loop on socket error
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error: {ex.Message}");
					}

					
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

		Stream GetStream(string resourceName)
		{
			var assembly = GetType().Assembly;
			var rr = assembly.GetManifestResourceNames();
			string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
			return assembly.GetManifestResourceStream(fullResourceName);
		}
	}
}
