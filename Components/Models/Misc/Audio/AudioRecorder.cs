using NAudio.Wave;

namespace MousyHub.Components.Models.Misc.Audio
{
    public class AudioRecorder
    {
        private WaveInEvent waveIn;
        private WaveFileWriter writer;
        private string outputFilePath;
        public bool IsRecording { get; private set; } = false;

        public AudioRecorder(string filePath)
        {
            outputFilePath = filePath;
        }

        public void StartRecording()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16bit, Mono

            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);

            waveIn.StartRecording();
            IsRecording = true;
        }

        public void StopRecording()
        {
            waveIn.StopRecording();
            IsRecording = false;
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (writer != null)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            if (waveIn != null)
            {
                waveIn.Dispose();
                waveIn = null;
            }
        }
    }
}
