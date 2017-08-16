using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using UIKit;

namespace record.iOS
{
	public partial class ViewController : UIViewController
	{
		int count = 1;
		iOSAudioRecorder _recorder = new iOSAudioRecorder();
		byte[] bytes;
		AVAudioPlayer player;
		private AudioRecordOptions _options = new AudioRecordOptions()
		{
			StreamFormat = AudioRecordOptions.Format.Flac
		};

		public ViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Perform any additional setup after loading the view, typically from a nib.
			Button.AccessibilityIdentifier = "myButton";
			Button.TouchUpInside += async delegate
			{
				OutputLabel.Text = $"Recording";

				Task.Run(async () =>
				{
					await Task.Delay(TimeSpan.FromSeconds(3));
					_recorder.Stop();
				});
				var result = await _recorder.Record(_options);

				bytes = result.AudioBytes;
				OutputLabel.Text = $"Recorded {bytes.Length} bytes";
			};

			Stop.TouchUpInside += Stop_TouchUpInside;
			Play.TouchUpInside += Play_TouchUpInside;
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.		
		}

		void Stop_TouchUpInside(object sender, EventArgs e)
		{
			_recorder.Stop();
		}

		void Play_TouchUpInside(object sender, EventArgs e)
		{
			OutputLabel.Text = $"Playing";

			var os = new MemoryStream();

			if (_options.StreamFormat == AudioRecordOptions.Format.Flac)
			{
				using (var fs = new MemoryStream(bytes))
				{
					using (var f = new FlacBox.WaveOverFlacStream(fs, FlacBox.WaveOverFlacStreamMode.Decode, false))
					{
						f.CopyTo(os);
						os.Seek(0, SeekOrigin.Begin);
					}
				}
			}
			else
			{
				os = new MemoryStream(bytes);
			}

			using (var data = NSData.FromStream(os))
			{
				PlayData(data);
			}
		}

		private void PlayData(NSData data)
		{
			NSError err;
			////AVAudioSession.SharedInstance().OverrideOutputAudioPort(AVAudioSessionPortOverride.Speaker, out err);

			player = AVAudioPlayer.FromData(data);
			player.Volume = 1.0f;

			player.DecoderError += (s, ev) =>
			{
				Debug.WriteLine(ev.Error.ToString());
			};

			player.FinishedPlaying += (s, ev) =>
			{
				Debug.WriteLine(OutputLabel.Text = "Finished");
			};

			player.PrepareToPlay();
			player.Play();
		}
	}
}
