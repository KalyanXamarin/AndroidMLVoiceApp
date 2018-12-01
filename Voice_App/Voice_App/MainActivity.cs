using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using ApiAiSDK;
using System.Threading.Tasks;
using Android.Speech;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Support.V4.Content;
using Android.Content.PM;
using System.Collections.Generic;
using ApiAiSDK.Model;
using System;
using Voice_App.Adapters;
using Android.Support.V4.App;

namespace Voice_App
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IRecognitionListener , TextToSpeech.IOnInitListener
    {
        ApiAi apiAi;

        SpeechRecognizer speechRecognizer;
        TextToSpeech textToSpeechRecognizer;
        Intent recognizationIntent;
        ToggleButton speechButton;
        TextView partialSpeechTextView;
        List<VoiceString> speechStrings;
        RecyclerView voiceListview;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            speechStrings = new List<VoiceString>();

            var aiConfig = new AIConfiguration("33ea7cea8a484f1587e1d1ce65d8ad25", SupportedLanguage.English);
            apiAi = new ApiAi(aiConfig);

            speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this);
            textToSpeechRecognizer = new TextToSpeech(this, this);
            speechRecognizer.SetRecognitionListener(this);

            recognizationIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            recognizationIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            recognizationIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 5);
            //recognizationIntent.PutExtra(RecognizerIntent.ExtraLanguage, "sv");
            //recognizationIntent.PutExtra(RecognizerIntent.ExtraLanguagePreference, "sv");
            recognizationIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1000);
            recognizationIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true);

            voiceListview = FindViewById<RecyclerView>(Resource.Id.voice_listview);
            voiceListview.SetLayoutManager(new LinearLayoutManager(this, LinearLayoutManager.Vertical, true));
            voiceListview.SetAdapter(new VoiceTextAdapter(this, speechStrings));

            speechButton = FindViewById<ToggleButton>(Resource.Id.speechTogglebutton);
            partialSpeechTextView = FindViewById<TextView>(Resource.Id.partialSpeechTextView);
            speechButton.Enabled = false;
            partialSpeechTextView.Text = "In order to communicate with me please enable voice permissions";

            speechButton.CheckedChange += delegate
            {
                if (speechButton.Checked)
                {
                    if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.RecordAudio) == Android.Content.PM.Permission.Granted)
                    {
                        partialSpeechTextView.Text = string.Empty;
                        speechRecognizer.StartListening(recognizationIntent);
                    }
                    else
                    {
                         ActivityCompat.RequestPermissions(this,new string[] { Android.Manifest.Permission.RecordAudio }, 2);
                    }
                }
                else
                {
                    speechRecognizer.StopListening();
                }
            };

            if (ContextCompat.CheckSelfPermission(this,Android.Manifest.Permission.RecordAudio) == Android.Content.PM.Permission.Granted)
            {
                partialSpeechTextView.Text = string.Empty;
            }
            speechButton.Enabled = true;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == 2)
            {
                if(grantResults[0] == Permission.Granted)
                {
                    speechButton.Enabled = true;
                    partialSpeechTextView.Text = string.Empty;
                }
            }
        }

        

        #region speech-recognizer
        public void OnBeginningOfSpeech()
        {
        }

        public void OnBufferReceived(byte[] buffer)
        {
        }

        public void OnEndOfSpeech()
        {
            speechButton.Checked = false;
        }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            partialSpeechTextView.Text = string.Format("{0}\n{1}\n", partialSpeechTextView.Text, error.ToString());
            speechButton.Checked = false;
        }

        public void OnEvent(int eventType, Bundle @params)
        {
        }

        public void OnPartialResults(Bundle partialResults)
        {
            var stringResult = partialResults.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            partialSpeechTextView.Text = stringResult[0];
        }

        public void OnReadyForSpeech(Bundle @params)
        {
        }

        public void OnResults(Bundle results)
        {
            var stringResult = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            ProcessTheText(stringResult[0]);
        }

        public void OnRmsChanged(float rmsdB)
        {
        }

        public void OnInit([GeneratedEnum] OperationResult status)
        {
        }

        #endregion
        void ProcessTheText(string enteredString)
        {
            speechStrings.Insert(0, new VoiceString(enteredString, true));
           
            if (enteredString.ToLower().Contains("joke"))
            {
                speechStrings.Insert(0, new VoiceString("Hi,i am a Joke", false));
                SpeakTheText(speechStrings[0].VoiceLabel);
            }
            else
            {
                ProcessText();
            }
            voiceListview.GetAdapter().NotifyDataSetChanged();
            partialSpeechTextView.Text = string.Empty;
        }
        void ProcessText()
        {
            Task nlpTask = new Task(() =>
            {
                var result = apiAi.TextRequest(speechStrings[0].VoiceLabel);
                MakeReservationCall(result);

            });
            nlpTask.Start();
        }

        void MakeReservationCall(AIResponse aiResponse)
        {
            this.RunOnUiThread(() =>
            {
                switch (aiResponse.Result.Action)
                {
                    case "Create.Booking":
                        CreateBooking(aiResponse);
                        break;
                    case "My.Bookings":
                        break;
                    case "Room.Bookings":
                        break;
                    default:
                        if (aiResponse.Result.Fulfillment?.Speech != null)
                        {
                            AddTextListView(aiResponse.Result.Fulfillment.Speech, false);
                        }
                        break;
                }
            });
        }
        void CreateBooking(AIResponse aiResponse)
        {
            Object timePeriod = null;
            Object duration = null;
            Object date = null;
            Object room = null;
            DateTime fromDateTime = DateTime.Now;
            DateTime toDateTime = DateTime.Now;
            if (aiResponse.Result.Parameters.ContainsKey("time-period"))
            {
                aiResponse.Result.Parameters.TryGetValue("time-period", out timePeriod);
            }
            if (aiResponse.Result.Parameters.ContainsKey("duration"))
            {
                aiResponse.Result.Parameters.TryGetValue("duration", out duration);
            }
            if (aiResponse.Result.Parameters.ContainsKey("date"))
            {
                aiResponse.Result.Parameters.TryGetValue("date", out date);
            }
            if (aiResponse.Result.Parameters.ContainsKey("RoomName"))
            {
                aiResponse.Result.Parameters.TryGetValue("RoomName", out room);
            }

            if (string.IsNullOrEmpty(room.ToString()))
            {
                AddTextListView("Room name is not clear for me could you please repeat again",false);
                return;
            }
            if (string.IsNullOrEmpty(timePeriod.ToString()))
            {
                if (string.IsNullOrEmpty(duration.ToString()))
                {
                    AddTextListView("Time peroid is not clear for me colud you please repeat again",false);
                    return;
                }
            }
            else
            {
                var timePeriodInString = timePeriod.ToString();
                if (timePeriodInString.Contains("/"))
                {
                    var fromTime = timePeriodInString.Substring(0, timePeriodInString.IndexOf("/"));
                    var fromTimeSpan = TimeSpan.Parse(fromTime);
                    var toTime = timePeriodInString.Substring(timePeriodInString.IndexOf("/") + 1, timePeriodInString.Length - timePeriodInString.IndexOf("/") - 1);
                    var toTimeSpan = TimeSpan.Parse(toTime);
                    toDateTime = new DateTime(toDateTime.Year, toDateTime.Month, toDateTime.Day, toTimeSpan.Hours, toTimeSpan.Minutes, 0);
                    fromDateTime = new DateTime(fromDateTime.Year, fromDateTime.Month, fromDateTime.Day, fromTimeSpan.Hours, fromTimeSpan.Minutes, 0);
                }
            }
            if (!string.IsNullOrEmpty(date.ToString()))
            {
                var dateTime = DateTime.Parse(date.ToString());
                toDateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, toDateTime.Hour, toDateTime.Minute, 0);
                fromDateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, fromDateTime.Hour, fromDateTime.Minute, 0);
            }
            AddTextListView($"{room} booked for you from {fromDateTime} to {toDateTime}", false);
            //TODO
        }



        void SpeakTheText(string text)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                textToSpeechRecognizer.Speak(text, QueueMode.Flush, null, this.GetHashCode().ToString());
            }
            else
            {
                textToSpeechRecognizer.Speak(text, QueueMode.Flush, null);
            }
        }

        void AddTextListView(string Text,bool isFromUser)
        {
            if(!isFromUser)
            {
                SpeakTheText(Text);
            }
            speechStrings.Insert(0, new VoiceString(Text, isFromUser));
            voiceListview.GetAdapter().NotifyDataSetChanged();
        }

    }
    public class VoiceString
    {
        public string VoiceLabel;
        public bool IsFromUser;
        public VoiceString(string voiceLabel, bool isFromUser)
        {
            this.VoiceLabel = voiceLabel;
            this.IsFromUser = isFromUser;
        }
    }
}

