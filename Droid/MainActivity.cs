using Android.App;
using Android.Widget;
using Android.OS;
using Android.Media;
using Java.IO;
using System.Threading.Tasks;

namespace record.Droid
{
	[Activity(Label = "record", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		int count = 1;
		AndroidAudioRecorder _recorder;

		TextView _output;
		File _tmpFile;

		private AudioRecordOptions _options = new AudioRecordOptions()
		{
			StreamFormat = AudioRecordOptions.Format.Flac
		};

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			Button start = FindViewById<Button>(Resource.Id.start_button);
			Button stop = FindViewById<Button>(Resource.Id.stop_button);
			Button play = FindViewById<Button>(Resource.Id.play_button);

			_output = FindViewById<TextView>(Resource.Id.output);

			start.Click += Start_Click;
			stop.Click += Stop_Click;
			play.Click += Play_Click;
		}

		async void Start_Click(object sender, System.EventArgs e)
		{
			if (_recorder != null)
			{
				return;
			}

			_recorder = new AndroidAudioRecorder();
			var result = await _recorder.Record(_options);

			_output.Text = $"Recorded {result.AudioBytes.Length} bytes";

			// playback file
			_tmpFile = File.CreateTempFile("TCL", "wav", CacheDir);
			_tmpFile.DeleteOnExit();

			if (_options.StreamFormat == AudioRecordOptions.Format.Wave)
			{
				var fos = new FileOutputStream(_tmpFile);
				fos.Write(result.AudioBytes);
				fos.Close();
			}
			else
			{
				using (var ms = new System.IO.MemoryStream(result.AudioBytes))
				{
					using (var fos = System.IO.File.OpenWrite(_tmpFile.AbsolutePath))
					{
						using (var f = new FlacBox.WaveOverFlacStream(ms, FlacBox.WaveOverFlacStreamMode.Decode, false))
						{
							f.CopyTo(fos);
						}
					}
				}
			}

			_recorder = null;
		}

		void Stop_Click(object sender, System.EventArgs e)
		{
			if (_recorder == null)
			{
				return;
			}

			_recorder.Stop();
		}

		void Play_Click(object sender, System.EventArgs e)
		{
			if (_tmpFile == null)
			{
				return;
			}

			MediaPlayer mediaPlayer = new MediaPlayer();

			FileInputStream playFile = new FileInputStream(_tmpFile);
			mediaPlayer.SetDataSource(playFile.FD);

			mediaPlayer.Prepare();
			mediaPlayer.Start();
		}
	}
}

