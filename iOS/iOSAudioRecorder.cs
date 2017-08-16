using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using FlacBox;
using Foundation;

namespace record.iOS
{
	public class iOSAudioRecorder : IAudioRecorder
	{
		private AVAudioRecorder _recorder;
		private string _audioFilePath;
		private CancellationTokenSource _cts;
		private TaskCompletionSource<AudioRecordResult> _tcs;
		private AudioRecordOptions _options;

		public Task<AudioRecordResult> Record(AudioRecordOptions options = null)
		{
			_options = options ?? AudioRecordOptions.Empty;

			_tcs = new TaskCompletionSource<AudioRecordResult>();

			var audioSession = AVAudioSession.SharedInstance();

			var err = audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord);
			if (err != null)
			{
				return Task.FromResult(new AudioRecordResult($"AVAudioSession.SetCategory returned error '{err}'"));
			}

			err = audioSession.SetActive(true);
			if (err != null)
			{
				return Task.FromResult(new AudioRecordResult($"AVAudioSession.SetActive returned error '{err}'"));
			}

			_audioFilePath = Path.Combine(Path.GetTempPath(), $"audiorec_{DateTime.Now.Ticks}.wav");

			Console.WriteLine("Audio File Path: " + _audioFilePath);

			var url = NSUrl.FromFilename(_audioFilePath);

			var config = new Dictionary<NSObject, NSObject>
			{
				{ AVAudioSettings.AVSampleRateKey, NSNumber.FromFloat((float)_options.SampleRate) },
				{ AVAudioSettings.AVFormatIDKey, NSNumber.FromInt32((int)AudioToolbox.AudioFormatType.LinearPCM) },
				{ AVAudioSettings.AVNumberOfChannelsKey, NSNumber.FromInt32(1) },
				{ AVAudioSettings.AVLinearPCMBitDepthKey, NSNumber.FromInt32(16) },
				{ AVAudioSettings.AVLinearPCMIsBigEndianKey, NSNumber.FromBoolean(false) },
				{ AVAudioSettings.AVLinearPCMIsFloatKey, NSNumber.FromBoolean(false) }
			};

			var settings = NSDictionary.FromObjectsAndKeys(config.Keys.ToArray(), config.Values.ToArray());

			_recorder = AVAudioRecorder.Create(url, new AudioSettings(settings), out err);
			if (err != null)
			{
				return Task.FromResult(new AudioRecordResult($"AVAudioRecorder.Create returned error '{err}'"));
			}

			_recorder.PrepareToRecord();
			_recorder.Record();

			Task.Run(() => Timeout());

			return _tcs.Task;
		}

		public void Stop()
		{
			_cts?.Cancel();
			_recorder.Stop();

			byte[] audioBytes = null;
			if (_options.StreamFormat == AudioRecordOptions.Format.Wave)
			{
				audioBytes = File.ReadAllBytes(_audioFilePath);
			}
			else if (_options.StreamFormat == AudioRecordOptions.Format.Flac)
			{
				// encode audio into flac
				using (var fr = File.OpenRead(_audioFilePath))
				{
					using (var ms = new MemoryStream())
					{
						using (var what = new WaveOverFlacStream(ms, WaveOverFlacStreamMode.Encode, true))
						{
							fr.CopyTo(what);
						}

						ms.Flush();
						ms.Seek(0, SeekOrigin.Begin);
						audioBytes = ms.ToArray();
					}
				}
			}

			File.Delete(_audioFilePath);

			_tcs.TrySetResult(new AudioRecordResult(audioBytes));
		}

		private async Task Timeout()
		{
			try
			{
				await Task.Delay(5000, _cts.Token);

				System.Diagnostics.Debug.WriteLine("TIMEOUT REACHED");
				Stop();
				////_tcs.TrySetResult(new AudioRecordResult("Timeout reached"));
			}
			catch (TaskCanceledException)
			{
				// user stopped recording before timeout reached
			}
		}
	}
}
