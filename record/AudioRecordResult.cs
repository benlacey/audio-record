namespace record
{
	public class AudioRecordResult
	{
		public bool IsSuccess { get; set; }

		public string ErrorMessage { get; set; }

		public byte[] AudioBytes { get; set; }

		public AudioRecordResult(bool isSuccess)
		{
			IsSuccess = isSuccess;
		}

		public AudioRecordResult(byte[] bytes)
		{
			IsSuccess = true;
			AudioBytes = bytes;
		}

		public AudioRecordResult(string error)
		{
			IsSuccess = false;
			ErrorMessage = error;
		}
	}
}
