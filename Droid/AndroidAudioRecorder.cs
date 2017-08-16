using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;
using FlacBox;

namespace record.Droid
{
	public class AndroidAudioRecorder : IAudioRecorder
	{
		private AudioRecord _recorder;

		private static ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
		private static Encoding RECORDER_AUDIO_ENCODING = Encoding.Pcm16bit;

		private int _bufferSize;
		private bool _isRecording;

		private MemoryStream _ms;
		private CancellationTokenSource _timeoutToken;
		private TaskCompletionSource<AudioRecordResult> _tcs;
		private AudioRecordOptions _options;

		public Task<AudioRecordResult> Record(AudioRecordOptions options = null)
		{
			_options = options ?? AudioRecordOptions.Empty;
			_tcs = new TaskCompletionSource<AudioRecordResult>();

			_bufferSize = AudioRecord.GetMinBufferSize(_options.SampleRate, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING) * 3;

			_recorder = new AudioRecord(AudioSource.VoiceRecognition, _options.SampleRate, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING, _bufferSize);

			if (_recorder.State == State.Initialized)
			{
				_recorder.StartRecording();
			}
			else
			{
				return Task.FromResult(new AudioRecordResult($"AudioRecord initialisation returned unexpected state ({_recorder.State})"));
			}

			_isRecording = true;
			_timeoutToken = new CancellationTokenSource();

			Task.Run(() => RecordAudio());
			Task.Run(() => Timeout());

			return _tcs.Task;
		}

		public void Stop()
		{
			if (_isRecording && _recorder == null)
			{
				_tcs.TrySetResult(new AudioRecordResult($"Not recording"));
			}

			_timeoutToken?.Cancel();
			_isRecording = false;

			if (_recorder.State == State.Initialized)
			{
				_recorder.Stop();
			}

			_recorder.Release();

			// Android audio is raw stream content, so add WAV header
			var wavstream = new MemoryStream();

			WriteWaveFileHeader(wavstream, _ms.Length, _options.SampleRate, 1);

			_ms.Seek(0, SeekOrigin.Begin);
			_ms.CopyTo(wavstream);
			_ms.Close();
			_ms.Dispose();
			_ms = null;

			if (_options.StreamFormat == AudioRecordOptions.Format.Wave)
			{
				_tcs.TrySetResult(new AudioRecordResult(wavstream.ToArray()));
			}
			else if (_options.StreamFormat == AudioRecordOptions.Format.Flac)
			{
				// encode audio into flac
				using (var ms = new MemoryStream())
				{
					using (var what = new WaveOverFlacStream(ms, WaveOverFlacStreamMode.Encode, true))
					{
						wavstream.Seek(0, SeekOrigin.Begin);
						wavstream.CopyTo(what);
					}

					ms.Flush();
					ms.Seek(0, SeekOrigin.Begin);
					_tcs.TrySetResult(new AudioRecordResult(ms.ToArray()));
				}
			}
		}

		private void RecordAudio()
		{
			var data = new byte[_bufferSize];

			_ms = new MemoryStream();

			int read = 0;
			while (_isRecording)
			{
				read = _recorder.Read(data, 0, _bufferSize);

				if (read != (int)RecordStatus.ErrorInvalidOperation)
				{
					_ms.Write(data, 0, data.Length);
				}
			}

			_ms.Flush();
		}

		private void WriteWaveFileHeader(MemoryStream ms, long totalAudioLen, long longSampleRate, int channels)
		{
			long byteRate = 16 * longSampleRate * 1 / 8;
			long totalDataLen = totalAudioLen + 36;

			byte[] header = new byte[44];

			header[0] = (byte)'R';  // RIFF/WAVE header
			header[1] = (byte)'I';
			header[2] = (byte)'F';
			header[3] = (byte)'F';
			header[4] = (byte)(totalDataLen & 0xff);
			header[5] = (byte)((totalDataLen >> 8) & 0xff);
			header[6] = (byte)((totalDataLen >> 16) & 0xff);
			header[7] = (byte)((totalDataLen >> 24) & 0xff);
			header[8] = (byte)'W';
			header[9] = (byte)'A';
			header[10] = (byte)'V';
			header[11] = (byte)'E';
			header[12] = (byte)'f';  // 'fmt ' chunk
			header[13] = (byte)'m';
			header[14] = (byte)'t';
			header[15] = (byte)' ';
			header[16] = 16;  // 4 bytes: size of 'fmt ' chunk
			header[17] = 0;
			header[18] = 0;
			header[19] = 0;
			header[20] = 1;  // format = 1
			header[21] = 0;
			header[22] = (byte)channels;
			header[23] = 0;
			header[24] = (byte)(longSampleRate & 0xff);
			header[25] = (byte)((longSampleRate >> 8) & 0xff);
			header[26] = (byte)((longSampleRate >> 16) & 0xff);
			header[27] = (byte)((longSampleRate >> 24) & 0xff);
			header[28] = (byte)(byteRate & 0xff);
			header[29] = (byte)((byteRate >> 8) & 0xff);
			header[30] = (byte)((byteRate >> 16) & 0xff);
			header[31] = (byte)((byteRate >> 24) & 0xff);
			header[32] = (byte)(2 * 16 / 8);  // block align
			header[33] = 0;
			header[34] = 16;  // bits per sample
			header[35] = 0;
			header[36] = (byte)'d';
			header[37] = (byte)'a';
			header[38] = (byte)'t';
			header[39] = (byte)'a';
			header[40] = (byte)(totalAudioLen & 0xff);
			header[41] = (byte)((totalAudioLen >> 8) & 0xff);
			header[42] = (byte)((totalAudioLen >> 16) & 0xff);
			header[43] = (byte)((totalAudioLen >> 24) & 0xff);

			ms.Write(header, 0, 44);
		}

		private async Task Timeout()
		{
			try
			{
				await Task.Delay(10000, _timeoutToken.Token);

				System.Diagnostics.Debug.WriteLine("TIMEOUT REACHED");
				_isRecording = false;
				_tcs.TrySetResult(new AudioRecordResult("Timeout reached"));
			}
			catch (TaskCanceledException)
			{
				// user stopped recording before timeout reached
			}
		}
	}
}
