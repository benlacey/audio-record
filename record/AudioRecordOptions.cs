using System;
namespace record
{
	public class AudioRecordOptions
	{
		public static AudioRecordOptions Empty = new AudioRecordOptions();

		public Format StreamFormat { get; set; }

		public int SampleRate { get; set; } = 22050;

		public enum Format
		{
			Wave,
			Flac
		}
	}
}
