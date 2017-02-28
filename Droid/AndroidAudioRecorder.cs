using System;
using System.IO;
using System.Threading.Tasks;
using Android.Media;

namespace record.Droid
{
	public class AndroidAudioRecorder
	{
		private AudioRecord _recorder;

		private static int RECORDER_SAMPLERATE = 16000;
		private static ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
		private static Encoding RECORDER_AUDIO_ENCODING = Encoding.Pcm16bit;

		private int _bufferSize;
		private bool _isRecording;

		private MemoryStream _ms;

		public void StartRecording()
		{
			_bufferSize = AudioRecord.GetMinBufferSize(RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING) * 3;

			_recorder = new AudioRecord(AudioSource.VoiceRecognition, RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING, _bufferSize);

			if (_recorder.State == State.Initialized)
				_recorder.StartRecording();

			_isRecording = true;
			Task.Run(() => RecordAudio());
		}

		public byte[] StopRecording()
		{
			if (_isRecording && _recorder == null)
			{
				// uh oh
			}

			_isRecording = false;

			if (_recorder.State == State.Initialized)
				_recorder.Stop();
			_recorder.Release();

			// grab wav from memory stream
			var wavstream = new MemoryStream();

			WriteWaveFileHeader(wavstream,
								_ms.Length,
								RECORDER_SAMPLERATE,
								1);

			_ms.Seek(0, SeekOrigin.Begin);
			_ms.CopyTo(wavstream);
			_ms.Close();
			_ms.Dispose();
			_ms = null;

			return wavstream.ToArray();
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

		private void WriteWaveFileHeader(
		 MemoryStream ms, long totalAudioLen, long longSampleRate, int channels)
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
	}
}
