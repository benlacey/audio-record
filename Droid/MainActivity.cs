using Android.App;
using Android.Widget;
using Android.OS;
using Android.Media;
using Java.IO;

namespace record.Droid
{
	[Activity(Label = "record", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		int count = 1;
		AndroidAudioRecorder _recorder;

		TextView _output;
		File _tmpFile;

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

		void Start_Click(object sender, System.EventArgs e)
		{
			if (_recorder != null)
			{
				return;
			}

			_recorder = new AndroidAudioRecorder();
			_recorder.StartRecording();

			_output.Text = $"started";
		}

		void Stop_Click(object sender, System.EventArgs e)
		{
			if (_recorder == null)
			{
				return;
			}

			var bytes = _recorder.StopRecording();

			_output.Text = $"Recorded {bytes.Length} bytes";

			// playback
			_tmpFile = File.CreateTempFile("TCL", "wav", CacheDir);
			_tmpFile.DeleteOnExit();

			var fos = new FileOutputStream(_tmpFile);
			fos.Write(bytes);
			fos.Close();

			_recorder = null;
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

