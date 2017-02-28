using System;
using System.IO;
using AVFoundation;
using Foundation;

namespace record.iOS
{
	public class iOSAudioRecorder
	{
		AVAudioRecorder recorder; 
		NSError error; 
		NSUrl url; 
		NSDictionary settings;
		string _audioFilePath;

		public void StartRecording()
		{
			var audioSession = AVAudioSession.SharedInstance(); 
			var err = audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord); 

			if (err != null) { Console.WriteLine("audioSession: {0}", err); return; }

			err = audioSession.SetActive(true); 

			if (err != null) { Console.WriteLine("audioSession: {0}", err); return; }

			//Declare string for application temp path and tack on the file extension
			string fileName = string.Format ("Myfile{0}.wav", DateTime.Now.ToString ("yyyyMMddHHmmss")); 
			_audioFilePath = Path.Combine (Path.GetTempPath(), fileName);

			Console.WriteLine("Audio File Path: " + _audioFilePath);

			url = NSUrl.FromFilename(_audioFilePath); 

			//set up the NSObject Array of values that will be combined with the keys to make the NSDictionary 
			NSObject[] values = new NSObject[] { 
				NSNumber.FromFloat (16000.0f), //Sample Rate 
				NSNumber.FromInt32 ((int)AudioToolbox.AudioFormatType.LinearPCM), //AVFormat 
				NSNumber.FromInt32 (1), //Channels
				NSNumber.FromInt32 (16), //PCMBitDepth 
				NSNumber.FromBoolean (false), //IsBigEndianKey 
				NSNumber.FromBoolean (false) //IsFloatKey 
			};

			//Set up the NSObject Array of keys that will be combined with the values to make the NSDictionary 
			NSObject[] keys = new NSObject[] { 
				AVAudioSettings.AVSampleRateKey, 
				AVAudioSettings.AVFormatIDKey, 
				AVAudioSettings.AVNumberOfChannelsKey, 
				AVAudioSettings.AVLinearPCMBitDepthKey, 
				AVAudioSettings.AVLinearPCMIsBigEndianKey, 
				AVAudioSettings.AVLinearPCMIsFloatKey 
			};

			//Set Settings with the Values and Keys to create the NSDictionary 
			settings = NSDictionary.FromObjectsAndKeys (values, keys);

			//Set recorder parameters 
			recorder = AVAudioRecorder.Create(url, new AudioSettings(settings), out error);

			//Set Recorder to Prepare To Record
			recorder.PrepareToRecord();
			recorder.Record();
		}

		public byte[] StopRecording()
		{
			recorder.Stop();

			// read file
			var f = File.ReadAllBytes(_audioFilePath);

			File.Delete(_audioFilePath);
			return f;
		}
	}
}
