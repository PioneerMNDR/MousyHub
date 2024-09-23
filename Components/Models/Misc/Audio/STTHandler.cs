using MongoDB.Bson;
using Newtonsoft.Json;
using Vosk;

namespace MousyHub.Components.Models.Misc.Audio
{

    public class STTHandler
    {

        private AudioRecorder AudioRecorder { get; set; }
        Vosk.Model VoskModel {  get; set; }
        private string tempFileName = AppDomain.CurrentDomain.BaseDirectory + "VoskAudio.wav";
        public bool isRecord {get { return AudioRecorder.IsRecording; } private set { } }
        public bool isVoskExist = false; 
        public STTHandler()
        {
            AudioRecorder = new AudioRecorder(tempFileName);
        }

        private bool CheckModelFiles(string PathToVoskModel)
        {
            string check1 = Util.FindFileRecursive(PathToVoskModel, "model.conf",1);
           string check2 = Util.FindFileRecursive(PathToVoskModel, "mfcc.conf",1);
           return check1 != string.Empty && check2 != string.Empty;
        }

        public void RunVoskModel(string PathToVoskModel)
        {
            
            isVoskExist = CheckModelFiles(PathToVoskModel);
            if (isVoskExist)
            {
                VoskModel = new Vosk.Model(PathToVoskModel);
            }
            else
            {
                Console.WriteLine("Vosk: wrong model path");
            }
        }

        public async Task<string> Inference(Vosk.Model model)
        {
            try
            {
                VoskRecognizer rec = new VoskRecognizer(model, 16000.0f);
                using (Stream source = File.OpenRead(tempFileName))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        float[] fbuffer = new float[bytesRead / 2];
                        for (int i = 0, n = 0; i < fbuffer.Length; i++, n += 2)
                        {
                            fbuffer[i] = BitConverter.ToInt16(buffer, n);
                        }
                        rec.AcceptWaveform(fbuffer, fbuffer.Length);

                    }
                }
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(rec.FinalResult());
                var textValue = result["text"];
                return textValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
 
        }

        public void Record()
        {
            if (AudioRecorder.IsRecording == false)
            {
                AudioRecorder.StartRecording();
            }

        }
        public async Task<string> StopRecording()
        {
            if (AudioRecorder.IsRecording)
            {
                AudioRecorder.StopRecording();
                if (isVoskExist)
                {
                    await Task.Delay(100);
                    string result =  await Inference(VoskModel);
                    return result;
                }
                return string.Empty;
            }
            return string.Empty;
        }




    }
}
