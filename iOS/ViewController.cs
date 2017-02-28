using System;
using System.Diagnostics;
using System.IO;
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

		public ViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Perform any additional setup after loading the view, typically from a nib.
			Button.AccessibilityIdentifier = "myButton";
			Button.TouchUpInside += delegate
			{
				OutputLabel.Text = $"Recording";
				_recorder.StartRecording();
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
			bytes = _recorder.StopRecording();

			OutputLabel.Text = $"Recorded {bytes.Length} bytes";
		}

		void Play_TouchUpInside(object sender, EventArgs e)
		{
			OutputLabel.Text = $"Playing";
			using (var data = NSData.FromArray(bytes))
			{
				NSError err;
				AVAudioSession.SharedInstance().OverrideOutputAudioPort(AVAudioSessionPortOverride.Speaker, out err);

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
}
