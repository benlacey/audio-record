using System.Threading.Tasks;

namespace record
{
	public interface IAudioRecorder
	{
		Task<AudioRecordResult> Record(AudioRecordOptions options = null);

		void Stop();
	}
}
