using UnityEditor;
using UnityEngine;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using Unity.EditorCoroutines.Editor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AiKodexDeepVoicePro
{
    public class DeepVoiceProEditor : EditorWindow
    {
        string text = "";
        string instructions = "";
        public enum Model
        {
            DeepVoice_Instruct,
            DeepVoice_Multi,
        };
        public static Model model = Model.DeepVoice_Instruct;
        public enum Voice
        {
            Jessie, Harry, Glinda, Clyde, Callum, Charlotte, Dave, Fin, Freya, Batman, Andrew, Hailey, Arthur, Anime_Girl, Valentina, Wayne, Jan, Noah, Lily, Lily_Narrator, Ethan, Sophia, Olivia, Ruby, Lucas, John, Werner, Mark, Alex, Brittney, Hope, Dakota, Amelia, Allison, Heather, Arabella, Nanchan, Jane, Ember, Anika, Lilian, Olufun, Bonnie, Taro, Eva, Nadya, Nik, Leoni, Pablo, Bjorn, James, Char_Old, Archer_Old, Fin_Old, Dave_Old, Lucas_Old, Clyde_Old
        };
        public static Voice voice = Voice.Jessie;
        public enum InstructVoice
        {
            Archer, Breeze, Cadence, Dune, Flare, Glimmer, Halo, Iris, Jade, Karma, Lyric
        }
        public static InstructVoice instructVoice = InstructVoice.Archer;
        float pace = 0.5f, variability = 0.3f, clarity = 0.75f;
        private bool initDone = false;
        private GUIStyle StatesLabel, styleError;
        public static bool running = false;
        private Vector2 mainScroll;
        string responseFromServer;
        float postProgress;
        bool postFlag;
        bool autoPath = true;
        string _directoryPath, fileName, bodyName = "Voice", voiceName = Voice.Noah.ToString(), take = "0";
        UnityEngine.Object currentAudioClip, lastAudioClip;
        Texture2D audioWaveForm, disabledWaveForm, audioSlider, previewClip;
        float scrubber = 0;
        bool updateScrubber;
        Texture button_play, button_pause, button_stop;
        float editorDeltaTime = 0f, lastTimeSinceStartup = 0f;
        bool fileExists;
        bool foldTrimmer = false, foldJoiner = false, foldEqualizer = false;
        AudioClip clipToTrim;
        float trimMin, trimMax;
        string trimmedClipFileName;
        bool trimFileExists;
        string combinedClipFileName;
        string invoice;
        bool combineFileExists;
        [SerializeField]
        List<AudioClip> audioJoinList = new List<AudioClip>();
        AudioClip clipToEqualize;
        float volume, pitch;
        float[] bandFreqs = { 100f, 230f, 910f, 3600f, 14000f, 18000f };
        List<float> gains = new List<float> { 0.7f, 0.7f, 0.7f, 0.7f, 0.7f, 0.7f };
        string equalizedClipFileName;
        bool equalizeFileExists;
        bool previewVoices = false;
        int selGridIV = -1, selGridMV = -1;
        int lastSelGridNV = -1, lastSelGridMV = -1;
        string[] previewInstructVoicesString = { "Archer", "Breeze", "Cadence", "Dune", "Flare", "Glimmer", "Halo", "Iris", "Jade", "Karma", "Lyric" };
        string[] previewMultiVoicesString = { "Jessie", "Harry", "Glinda", "Clyde", "Freya", "Fin", "Dave", "Charlotte", "Callum", "Batman", "Andrew", "Hailey", "Arthur", "Anime_Girl", "Valentina", "Wayne", "Jan", "Noah", "Lily", "Lily_Narrator", "Ethan", "Sophia", "Olivia", "Ruby", "Lucas", "John", "Werner", "Mark", "Alex", "Brittney", "Hope", "Dakota", "Amelia", "Allison", "Heather", "Arabella", "Nanchan", "Jane", "Ember", "Anika", "Lilian", "Olufun", "Bonnie", "Taro", "Eva", "Nadya", "Nik", "Leoni", "Pablo", "Bjorn", "James" };


        void InitStyles()
        {
            initDone = true;
            StatesLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(),
                padding = new RectOffset(),
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }


        void Awake()
        {
            //Check all files names on startup to inform the user of a possible overwrite. 
            if (model == Model.DeepVoice_Instruct)
                voiceName = instructVoice.ToString();
            else if (model == Model.DeepVoice_Multi)
                voiceName = voice.ToString();
            fileName = $"{voiceName}_{bodyName}_{take}";
            fileExists = false;
            _directoryPath = "Assets/DeepVoicePro/Voices";
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
            var info = new DirectoryInfo(_directoryPath);
            var fileInfo = info.GetFiles();
            foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
            {
                if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                {
                    fileExists = true;
                }
            }
            invoice = PlayerPrefs.GetString("DeepVoicePro_Invoice");
        }
        // create menu item and window
        [MenuItem("Window/DeepVoice Pro")]
        static void Init()
        {
            DeepVoiceProEditor window = (DeepVoiceProEditor)EditorWindow.GetWindow(typeof(DeepVoiceProEditor));
            window.titleContent.text = "DeepVoice Pro";
            window.minSize = new Vector2(350, 300);
            running = true;
        }
        void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
            if (!initDone)
                InitStyles();
            GUIStyle style = new GUIStyle("WhiteLargeLabel");
            GUIStyle smallStyle = new GUIStyle("wordWrappedMiniLabel");
            GUIStyle sectionTitle = style;
            GUIStyle subStyle = new GUIStyle("Label");
            subStyle.fontSize = 10;
            subStyle.normal.textColor = Color.white;
            sectionTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle headStyle = new GUIStyle("BoldLabel");
            headStyle.fontSize = 18;
            headStyle.normal.textColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            Texture logo = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Logo.png", typeof(Texture));
            Texture infoToolTip = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Info.png", typeof(Texture));
            Texture2D disabledWaveForm = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/DisabledWaveform.png", typeof(Texture));
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("            DeepVoice Pro  ", headStyle);
            EditorGUILayout.LabelField("                Version 3.0.7", subStyle);
            EditorGUILayout.EndVertical();
            GUI.DrawTexture(new Rect(10, 3, 45, 45), logo, ScaleMode.StretchToFill, true, 10.0F);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Voice Generator", sectionTitle);
            var tempCenter = GUILayoutUtility.GetLastRect().center.x;
            EditorGUILayout.Space(10);
            invoice = EditorGUILayout.TextField(new GUIContent("Invoice Number  ", infoToolTip, "Enter Invoice number. Invoice numbers are 14 digits long. You can find them under Order History on the store. For a more detailed explaination, please refer to the documentation."), invoice);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("");
            if (GUILayout.Button("Verify", GUILayout.MaxWidth(48), GUILayout.MaxHeight(17)))
                this.StartCoroutine(Verify("https://dvpro.aikodex.com/verify", "{\"invoice\":\"" + invoice + "\"}"));
            if (GUILayout.Button("Save", GUILayout.MaxWidth(48), GUILayout.MaxHeight(17)))
                SaveInvoice();
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            EditorStyles.textArea.wordWrap = true;
            text = GUILayout.TextArea(text, EditorStyles.textArea);
            EditorGUILayout.BeginHorizontal();
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 10 };
            if (model == Model.DeepVoice_Instruct)
                EditorGUILayout.TextField($"Supports: AF, AR, HY, AZ, BE, BS, BG, CA, ZH, HR, CS, DA, NL, EN, ET, FI, FR, GL, DE, EL, HE, HI, HU, IS, ID, IT, JA, KN, KK, KO, LV, LT, MK, MS, MR, MI, NE, NO, FA, PL, PT, RO, RU, SR, SK, SL, ES, SW, SV, TL, TA, TH, TR, UK, UR, VI, CY.", smallStyle, GUILayout.MinHeight(50), GUILayout.MaxWidth(500));
            else if (model == Model.DeepVoice_Multi)
                EditorGUILayout.LabelField($"Supports: EN, JA, DE, HI, FR, KO, PT, IT, ES, ID, NL, TR, FIL, PL, SV, BG, RO, AR, CS, EL, FI, HR, MS, SK, DA, TA, UK", smallStyle, GUILayout.MaxWidth(800));
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
            styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
            styleError.normal.textColor = Color.red;
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (2500 - text.Length >= 0)
                EditorGUILayout.LabelField($"{2500 - text.Length} char", style, GUILayout.MaxWidth(80));
            else
                EditorGUILayout.LabelField($"{2500 - text.Length} char", styleError);
            if (GUILayout.Button("Status", GUILayout.MaxWidth(48), GUILayout.MaxHeight(17)))
                this.StartCoroutine(Status("https://dvpro.aikodex.com/status", "{\"invoice\":\"" + invoice + "\"}"));
            GUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Voice Model Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginChangeCheck();
            model = (Model)EditorGUILayout.EnumPopup(new GUIContent("Model", infoToolTip, "Select the text-to-speech (TTS) model file to use. The Multi model accept parameters such as pace, variability and clarity to offer improved customization for the output. The instruct model provide  is based on instructions for how text should sound like."), model);
            if (model == Model.DeepVoice_Instruct)
                instructVoice = (InstructVoice)EditorGUILayout.EnumPopup(new GUIContent("Voice", infoToolTip, "Selects the Voice ID to use for the given model. Choose from a variety of different voices and find the best fit for your character."), instructVoice);
            else if (model == Model.DeepVoice_Multi)
                voice = (Voice)EditorGUILayout.EnumPopup(new GUIContent("Voice", infoToolTip, "Selects the Voice ID to use for the given model. Choose from a variety of different voices and find the best fit for your character."), voice);
            if (EditorGUI.EndChangeCheck())
            {
                if (model == Model.DeepVoice_Instruct)
                    voiceName = instructVoice.ToString();
                else if (model == Model.DeepVoice_Multi)
                    voiceName = voice.ToString();
                fileName = $"{voiceName}_{bodyName}_{take}";
            }
            if (model == Model.DeepVoice_Instruct)
            {
                EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
                EditorStyles.textArea.wordWrap = true;
                instructions = EditorGUILayout.TextArea(instructions, EditorStyles.textArea);
                EditorGUILayout.LabelField("Examples Copy / Paste", EditorStyles.centeredGreyMiniLabel);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("German Accent, Precise, Clear", GUILayout.MaxWidth(Screen.width / 4)))
                {
                    instructions = @"Accent: Distinct but soft German accent, clear and precise, with a structured and logical approach to description.
Tone: Serious but engaging, with a methodical delivery that makes even complex artistic concepts easy to grasp.
Pacing: Moderate, deliberate, ensuring clarity and understanding while maintaining a steady rhythm.
Emotion: Controlled enthusiasm, appreciative and respectful of arts complexities, without excessive flair.
Pronunciation: Crisp and accurate, particularly with European names and terms related to technique and history.
Personality Affect: Intelligent, disciplined, and knowledgeable, making art feel both fascinating and intellectually rewarding.";
                }
                if (GUILayout.Button("Italian Accent, Expressive, Slow", GUILayout.MaxWidth(Screen.width / 4)))
                {
                    instructions = @"Accent: A rich, melodic Italian accent, full of warmth and expressive emotion, rolling through words with natural elegance.
Tone: Romantic and passionate, as if describing a long-lost love affair with each painting or sculpture.
Pacing: Slow, savoring every word, ensuring the audience absorbs the beauty and depth of the subject matter.
Emotion: Deeply passionate, each description feels like an affectionate ode to the art and its creators.
Pronunciation: Emphasizes the beauty of Italian and European names, pronouncing them with musicality and reverence.
Personality Affect: Warm, charismatic, and intensely devoted, drawing listeners into the emotional soul of the artwork";
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Spanish Accent, Smooth, Slow", GUILayout.MaxWidth(Screen.width / 4)))
                {
                    instructions = @"Accent: A rich, lyrical Spanish accent, flowing smoothly with a slightly musical quality.
Tone: Romantic and poetic, speaking with a deep appreciation for the soul of the artwork.
Pacing: Slow and deliberate, lingering on key phrases to create a sense of depth and reflection.
Emotion: Passionate yet serene, emotion is conveyed through subtle intonations rather than overt excitement.
Pronunciation: Spanish names and words are spoken with natural elegance, rolling off the tongue beautifully.
Personality Affect: Thoughtful, evocative, and slightly mysterious—guiding listeners into the deeper emotional layers of art.";
                }
                if (GUILayout.Button("Russian Accent, Deep, Slow", GUILayout.MaxWidth(Screen.width / 4)))
                {
                    instructions = @"Accent: A deep, slightly gravelly Russian accent, adding an air of gravitas and intensity to each description.
Tone: Serious and dramatic, as if every piece of art holds profound philosophical meaning.
Pacing: Slow and weighty, letting each word settle, adding to the impact of the storytelling.
Emotion: Deep, restrained passion, each word feels intentional, as if revealing hidden truths within the art.
Pronunciation: Strong, deliberate enunciation, with an emphasis on crisp consonants.
Personality Affect: Introspective, intellectual, and slightly enigmatic—making the art feel like an unfolding mystery.";
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

            }
            else
            {
                pace = EditorGUILayout.Slider(new GUIContent("Pace", infoToolTip, "Sets the pace of generation. Higher values mkaes the speech rate faset and lower values result in a slower rate of speech."), pace, 0.7f, 1.2f);
                variability = EditorGUILayout.Slider(new GUIContent("Variability", infoToolTip, "Sets a tone of the voice which allows for experimentation. Increasing variability can make speech more expressive with output varying between re-generations. However, it can also lead to instabilities."), variability, 0, 1);
                clarity = EditorGUILayout.Slider(new GUIContent("Clarity", infoToolTip, "High values boost overall voice clarity and target speaker similarity. Very high values can cause artifacts, so adjusting this setting to find the optimal value is encouraged."), clarity, 0, 1);
            }
            EditorGUILayout.Space(10);
            previewVoices = FoldOuts.FoldOut("Preview Voices", previewVoices);
            if (previewVoices)
            {

                Color originalBackgroundColor = GUI.backgroundColor;
                GUIStyle centeredHeaderStyle = new GUIStyle(EditorStyles.boldLabel); // Start with bold label style
                centeredHeaderStyle.alignment = TextAnchor.MiddleCenter; // Center the text horizontally and vertically within the label's rect
                centeredHeaderStyle.fontSize = 14; // Increase font size (default is often 11 or 12)
                                                   // You could also add padding if needed: centeredHeaderStyle.padding = new RectOffset(0, 0, 5, 5);

                try // Use a try-finally block to ensure the color is always restored
                {
                    // --- Instruct Voices Section ---
                    GUI.backgroundColor = new Color(0.8f, 0.9f, 1f, 1f); // Light Blue Tint
                    GUILayout.BeginVertical("Window");

                    // Centered Title using Flexible Space and the custom style
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace(); // Add space before the label
                    GUILayout.Label("Instruct Voices", centeredHeaderStyle); // Use the custom style
                    GUILayout.FlexibleSpace(); // Add space after the label
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal("Box");
                    selGridIV = GUILayout.SelectionGrid(selGridIV, previewInstructVoicesString, 5, GUILayout.MinWidth(100));
                    if (selGridIV != lastSelGridNV)
                    {
                        StopAllClips();
                        AudioClip clipToPlay = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Previews/" + previewInstructVoicesString[selGridIV] + ".wav", typeof(AudioClip));
                        if (clipToPlay != null)
                        {
                            PlayClip(clipToPlay, 0, false);
                        }
                        else
                        {
                            Debug.LogError("Failed to load audio clip: " + previewInstructVoicesString[selGridIV]);
                        }
                        lastSelGridNV = selGridIV;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    // Add some spacing between the sections (optional)
                    GUILayout.Space(10);

                    // --- Multi Voices Section ---
                    GUI.backgroundColor = new Color(1f, 0.95f, 0.8f, 1f); // Light Yellow/Orange Tint
                    GUILayout.BeginVertical("Window");

                    // Centered Title using Flexible Space and the custom style
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace(); // Add space before the label
                    GUILayout.Label("Multi Voices", centeredHeaderStyle); // Use the custom style
                    GUILayout.FlexibleSpace(); // Add space after the label
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal("Box");
                    selGridMV = GUILayout.SelectionGrid(selGridMV, previewMultiVoicesString, 5, GUILayout.MinWidth(100));
                    if (selGridMV != lastSelGridMV)
                    {
                        StopAllClips();
                        AudioClip clipToPlay = (AudioClip)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Previews/" + previewMultiVoicesString[selGridMV] + ".wav", typeof(AudioClip));
                        if (clipToPlay != null)
                        {
                            PlayClip(clipToPlay, 0, false);
                        }
                        else
                        {
                            Debug.LogError("Failed to load audio clip: " + previewMultiVoicesString[selGridMV]);
                        }
                        lastSelGridMV = selGridMV;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                finally // Ensures the color is reset even if an error occurs
                {
                    // Restore the original background color
                    GUI.backgroundColor = originalBackgroundColor;
                }
            }
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            voiceName = EditorGUILayout.TextField(new GUIContent("File Name", infoToolTip, "Automatically assigns the file name based on the selected voice. Additionally, increments the take field by +1 upon voice processing"), voiceName);
            bodyName = EditorGUILayout.TextField(bodyName);
            take = EditorGUILayout.TextField(take);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{voiceName}_{bodyName}_{take}.wav", style);
            fileName = $"{voiceName}_{bodyName}_{take}";
            if (EditorGUI.EndChangeCheck())
            {
                //Check all files for name existence

                fileExists = false;
                var info = new DirectoryInfo(_directoryPath);
                var fileInfo = info.GetFiles();
                foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                {
                    if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                    {
                        fileExists = true;
                    }
                }
            }

            if (fileExists)
            {
                styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                styleError.normal.textColor = Color.red;
                EditorGUILayout.LabelField(new GUIContent("[Overwrite Name]", infoToolTip, "This file name already exists. Clicking on generate will overwrite and replace the current file. Proceed with precaution."), styleError, GUILayout.Width(100));
            }
            else
            {
                styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                styleError.normal.textColor = Color.green;
                EditorGUILayout.LabelField(new GUIContent("[Available Name]", infoToolTip, "This file name is available to use."), styleError, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(autoPath == true);
            if (autoPath)
                _directoryPath = EditorGUILayout.TextField("Voices Folder", "Assets/DeepVoicePro/Voices");
            else
                _directoryPath = EditorGUILayout.TextField("Voices Folder", _directoryPath);
            if (GUILayout.Button(". . /", GUILayout.MaxWidth(50)))
                _directoryPath = EditorUtility.OpenFolderPanel("", "", "");
            EditorGUI.EndDisabledGroup();
            autoPath = EditorGUILayout.ToggleLeft("Auto", autoPath, GUILayout.MaxWidth(50));

            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginDisabledGroup(text == "");
            if (GUILayout.Button("Generate Voice", GUILayout.Height(30)))
            {
                postFlag = true;
                postProgress = 0;

                if (model == Model.DeepVoice_Instruct)
                    SendInstructPayload(text, "instruct", invoice, voiceName.ToString(), instructions);
                else if (model == Model.DeepVoice_Multi)
                    SendMultiPayload(text, "multi", invoice, voiceName.ToString(), pace.ToString(), variability.ToString(), clarity.ToString());

            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);
            Rect loading = GUILayoutUtility.GetRect(9, 9);
            if (postFlag)
            {
                Repaint();
                EditorGUI.ProgressBar(loading, Mathf.Sqrt(++postProgress) * 0.009f, "");
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);
            currentAudioClip = Selection.activeObject;
            EditorGUI.BeginDisabledGroup(Selection.activeObject == null || !Selection.activeObject.GetType().Equals(typeof(AudioClip)) || currentAudioClip == null);
            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField(new GUIContent("Preview", infoToolTip, "The preview section helps you preview the sound files without leaving this interface. To access it, single-click on a file in the project and hover over this panel. You will see this section enabled. Scrub the playhead to preview different sections of the audio."), sectionTitle);
            GUILayout.Space(100);


            if (Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)) && lastAudioClip != currentAudioClip)
            {
                AudioClip sound = (AudioClip)Selection.activeObject;
                audioWaveForm = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 0.55f, 0), false, 0);

                scrubber = 0;
                audioSlider = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / sound.length);
            }

            lastAudioClip = currentAudioClip;


            if (Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)) && currentAudioClip != null)
            {
                GUI.DrawTexture(new Rect(tempCenter - Screen.width * 0.2f, GUILayoutUtility.GetLastRect().y, Screen.width * 0.4f, 100), audioWaveForm, ScaleMode.StretchToFill, true, 1);
                GUI.DrawTexture(new Rect(tempCenter - Screen.width * 0.2f, GUILayoutUtility.GetLastRect().y, Screen.width * 0.4f, 100), audioSlider, ScaleMode.StretchToFill, true, 1);
                AudioClip sound = (AudioClip)Selection.activeObject;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(sound.name.ToString() + ", " + sound.frequency.ToString() + "Hz, " + sound.length.ToString().Substring(0, 4) + "s", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                this.button_play = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Play.png", typeof(Texture));
                this.button_pause = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Pause.png", typeof(Texture));
                this.button_stop = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Stop.png", typeof(Texture));

                if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (!updateScrubber)
                    {
                        PlayClip((AudioClip)currentAudioClip, Mathf.CeilToInt((scrubber / sound.length) * sound.samples), false);
                        if (Mathf.Approximately(scrubber, sound.length))
                            scrubber = 0;
                    }
                    updateScrubber = true;
                }
                if (GUILayout.Button(new GUIContent(button_pause), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                    updateScrubber = false;
                }
                if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                    updateScrubber = false;
                    scrubber = 0;
                    audioSlider = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / sound.length);
                }
                if (!updateScrubber)
                    lastTimeSinceStartup = 0f;
                if (scrubber > sound.length)
                {
                    updateScrubber = false;
                }
                if (updateScrubber)
                {
                    if (lastTimeSinceStartup == 0f)
                        lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
                    editorDeltaTime = (float)EditorApplication.timeSinceStartup - lastTimeSinceStartup;
                    lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
                    scrubber += editorDeltaTime;
                    audioSlider = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / sound.length);

                    //Prevent edge case for the sound clip looping if the scrubber is played while it is at the end of the playhead (~16000 samples)
                    if (sound.samples - Mathf.CeilToInt((scrubber / sound.length) * sound.samples) < 100)
                        StopAllClips();

                    Repaint();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

            }

            else
            {
                GUI.DrawTexture(new Rect(tempCenter - 100, GUILayoutUtility.GetLastRect().y, 200, 100), disabledWaveForm, ScaleMode.ScaleToFit, true, 1.5f);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select an audio file from the project", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                this.button_play = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Play.png", typeof(Texture));
                this.button_pause = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Pause.png", typeof(Texture));
                this.button_stop = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoicePro/Editor/Resources/Stop.png", typeof(Texture));
                GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25));
                GUILayout.Button(new GUIContent(button_pause), GUILayout.Width(25), GUILayout.Height(25));
                GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();


            }
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Playhead");

            if (Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)))
            {
                AudioClip clip = (AudioClip)Selection.activeObject;
                EditorGUI.BeginChangeCheck();
                scrubber = EditorGUILayout.Slider(scrubber, 0, clip.length, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    audioSlider = PaintWaveformSpectrum(clip, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / clip.length);
                    updateScrubber = false;
                    StopAllClips();
                }
            }
            else
                scrubber = EditorGUILayout.Slider(scrubber, 0, 1, GUILayout.Width(120));
            GUILayout.Label("s");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUI.EndDisabledGroup();
            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Audio Utility", sectionTitle);
            GUILayout.Space(10);

            GUILayout.Space(10);

            foldTrimmer = FoldOuts.FoldOut("Audio Trimmer", foldTrimmer);
            if (foldTrimmer)
            {
                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();
                clipToTrim = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Clip To Trim", infoToolTip, "Select an audio file you wish to trim. Once selected, use the slider to cut portions of the audio. When satisfied, save the audio by entering a valid name for the audio file. Click on \"Active Clip\" button to select the clip active in the project. To remove the selection, simply click on the x button on the right side of the clip selection field."), clipToTrim, typeof(AudioClip), true);

                if (GUILayout.Button("Active Clip", GUILayout.MaxWidth(80)) && Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)))
                {
                    clipToTrim = (AudioClip)Selection.activeObject;
                    trimMax = 1;
                }

                if (GUILayout.Button("×", EditorStyles.boldLabel, GUILayout.MaxWidth(20)))
                {
                    clipToTrim = null;
                    previewClip = null;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(120);
                if (EditorGUI.EndChangeCheck() && clipToTrim != null)
                {
                    previewClip = PaintWaveformSpectrum(clipToTrim, Screen.width / 4, 100, new Color(1, 0.55f, 0), false, 0);
                }
                EditorGUI.BeginDisabledGroup(previewClip == null);
                if (previewClip != null)
                {
                    float trimLastRectY = GUILayoutUtility.GetLastRect().y + 20;
                    GUI.DrawTexture(new Rect(tempCenter - Screen.width * 0.2f, trimLastRectY, Screen.width * 0.4f, 100), previewClip, ScaleMode.StretchToFill, true, 1);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.MinMaxSlider(ref trimMin, ref trimMax, 0, 1, GUILayout.MaxWidth(Screen.width * 0.705f));
                    GUILayout.FlexibleSpace();
                    GUI.Box(new Rect(Screen.width * 0.04f, trimLastRectY, Screen.width * trimMin * 0.6f, 100), "");
                    GUI.Box(new Rect(Screen.width * trimMax * 0.6f + Screen.width * 0.04f, trimLastRectY, Screen.width * 0.6f * (1 - trimMax), 100), "");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Start:");
                    EditorGUILayout.FloatField(trimMin * clipToTrim.length, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("End:");
                    EditorGUILayout.FloatField(trimMax * clipToTrim.length, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        AudioClip ac = AudioClip.Create("Temp", Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples), clipToTrim.channels, clipToTrim.frequency, false);
                        float[] samples = new float[Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples)];
                        clipToTrim.GetData(samples, Mathf.CeilToInt(trimMin * clipToTrim.samples));
                        ac.SetData(samples, 0);
                        if (!Directory.Exists(_directoryPath + "/Temp_data")) Directory.CreateDirectory(_directoryPath + "/Temp_data");
                        WaveUtils.Save("TempTrim", ac, _directoryPath + "/Temp_data", false);
                        AssetDatabase.Refresh();

                        AudioClip temp = (AudioClip)AssetDatabase.LoadAssetAtPath(_directoryPath + "/Temp_data/TempTrim.wav", typeof(AudioClip));
                        //System.Reflection cannot play audio samples stored in local variables hence the method above 
                        PlayClip(temp, 0, false);
                    }
                    if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        StopAllClips();
                    }
                    if (GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(25)))
                    {
                        trimMin = 0;
                        trimMax = 1;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();
                    trimmedClipFileName = EditorGUILayout.TextField("File Name", trimmedClipFileName);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{trimmedClipFileName}.wav", style);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //Check all files for name existence
                        fileName = $"{trimmedClipFileName}";

                        trimFileExists = false;
                        var info = new DirectoryInfo(_directoryPath);
                        var fileInfo = info.GetFiles();
                        foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                        {
                            if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                            {
                                trimFileExists = true;
                            }
                        }
                    }

                    if (trimFileExists)
                    {
                        styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                        styleError.normal.textColor = Color.red;
                        EditorGUILayout.LabelField($"[Overwrite Name]", styleError, GUILayout.Width(100));
                    }
                    else
                    {
                        styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                        if (trimmedClipFileName == "" || trimmedClipFileName == null)
                        {
                            styleError.normal.textColor = Color.red;
                            EditorGUILayout.LabelField($"[Cannot be Empty]", styleError, GUILayout.Width(100));
                        }
                        else
                        {
                            styleError.normal.textColor = Color.green;
                            EditorGUILayout.LabelField($"[Available Name]", styleError, GUILayout.Width(100));
                        }
                    }
                    GUILayout.EndHorizontal();
                    EditorGUI.BeginDisabledGroup(trimmedClipFileName == "" || trimmedClipFileName == null);
                    if (GUILayout.Button("Save Trimmed Audio", GUILayout.Height(30)))
                    {
                        AudioClip ac = AudioClip.Create("Temp", Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples), clipToTrim.channels, clipToTrim.frequency, false);
                        float[] samples = new float[Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples)];
                        clipToTrim.GetData(samples, Mathf.CeilToInt(trimMin * clipToTrim.samples));
                        ac.SetData(samples, 0);
                        WaveUtils.Save(trimmedClipFileName, ac, _directoryPath, false);
                        AssetDatabase.Refresh();
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(10);

                }
                else
                {
                    GUI.DrawTexture(new Rect(tempCenter - Screen.width * 0.25f, GUILayoutUtility.GetLastRect().y + 20, Screen.width * 0.5f, 100), disabledWaveForm, ScaleMode.ScaleToFit, true, 1.5f);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.MinMaxSlider(ref trimMin, ref trimMax, 0, 1, GUILayout.MaxWidth(Screen.width * 0.725f));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Start:");
                    EditorGUILayout.FloatField(trimMin * 0, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("End:");
                    EditorGUILayout.FloatField(trimMax * 1, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25));
                    GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25));
                    GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(25));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Button("Save Trimmed Audio", GUILayout.Height(30));
                }

                EditorGUI.EndDisabledGroup();

            }
            foldJoiner = FoldOuts.FoldOut("Audio Joiner", foldJoiner);
            if (foldJoiner)
            {
                string[] selectionOfAudioClips = Selection.assetGUIDs;
                EditorGUI.BeginDisabledGroup(selectionOfAudioClips.Length < 2);
                ScriptableObject target = this;
                SerializedObject so = new SerializedObject(target);
                SerializedProperty stringsProperty = so.FindProperty("audioJoinList");
                GUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(stringsProperty, new GUIContent("Audio Clips to Join", infoToolTip, "Select two or more audio files you wish to combine. Select the audio files from the project and click on \"Set Selected\" to auto populate the queue with the selected files. Please note that you cannot manually assign clips using the editor, you may only use the Set Selected Button to assign clips in this version of the asset. You can rearrange the audio clips in the hierarchy by dragging the clips. Once satisfied with the arrangement of the clips, enter a suitable name and save the file. You can clear the queue using the x button on the right of the Set Selected Button."), true, GUILayout.MaxWidth(300));
                so.ApplyModifiedProperties();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(new GUIContent("Set Selected", null, "Select two or more clips in the project to enable button")))
                {
                    audioJoinList.Clear();
                    for (int i = 0; i < selectionOfAudioClips.Length; i++)
                    {
                        audioJoinList.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(selectionOfAudioClips[i])));
                    }

                }
                EditorGUI.EndDisabledGroup();
                bool clearList = false;
                if (GUILayout.Button("×", EditorStyles.boldLabel, GUILayout.MaxWidth(20)))
                {
                    clearList = true;
                }
                GUILayout.EndHorizontal();
                bool anyClipNull = false;
                float combinedTime = 0;
                for (int i = 0; i < audioJoinList.Count; i++)
                {
                    if (audioJoinList[i] == null)
                        anyClipNull = true;
                }
                if (!anyClipNull)
                {
                    for (int i = 0; i < audioJoinList.Count; i++)
                    {
                        combinedTime += audioJoinList[i].length;
                        GUILayout.Label(audioJoinList[i].length.ToString() + "s", GUILayout.Height(18));
                    }
                }
                EditorGUI.BeginDisabledGroup(stringsProperty.arraySize == 0);
                GUILayout.Label(combinedTime.ToString().Length > 5 ? combinedTime.ToString().Substring(0, 5) + "s [Total]" : "Total Time", EditorStyles.boldLabel, GUILayout.Height(18));
                EditorGUI.EndDisabledGroup();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                if (clearList)
                    audioJoinList.Clear();

                EditorGUI.BeginDisabledGroup(audioJoinList.Count < 2 || anyClipNull);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    int totalSamplesCount = 0;

                    for (int i = 0; i < audioJoinList.Count; i++)
                        totalSamplesCount += audioJoinList[i].samples;


                    AudioClip ac = AudioClip.Create("Temp", totalSamplesCount, audioJoinList[0].channels, audioJoinList[0].frequency, false);

                    float[] concatenatedSamples = audioJoinList.Select(audioClip =>
                    {
                        float[] samples = new float[audioClip.samples * audioClip.channels];
                        audioClip.GetData(samples, 0);
                        return samples;
                    })
                    .SelectMany(x => x)
                    .ToArray();

                    ac.SetData(concatenatedSamples, 0);
                    if (!Directory.Exists(_directoryPath + "/Temp_data")) Directory.CreateDirectory(_directoryPath + "/Temp_data");
                    WaveUtils.Save("TempCombine", ac, _directoryPath + "/Temp_data", false);
                    AssetDatabase.Refresh();

                    AudioClip temp = (AudioClip)AssetDatabase.LoadAssetAtPath(_directoryPath + "/Temp_data/TempCombine.wav", typeof(AudioClip));
                    PlayClip(temp, 0, false);

                }
                if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                EditorGUI.BeginChangeCheck();
                combinedClipFileName = EditorGUILayout.TextField("File Name", combinedClipFileName);
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{combinedClipFileName}.wav", style);
                if (EditorGUI.EndChangeCheck())
                {
                    //Check all files for name existence
                    fileName = $"{combinedClipFileName}";

                    combineFileExists = false;
                    var info = new DirectoryInfo(_directoryPath);
                    var fileInfo = info.GetFiles();
                    foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                    {
                        if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                        {
                            combineFileExists = true;
                        }
                    }
                }

                if (combineFileExists)
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    styleError.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"[Overwrite Name]", styleError, GUILayout.Width(100));
                }
                else
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    if (combinedClipFileName == "" || combinedClipFileName == null)
                    {
                        styleError.normal.textColor = Color.red;
                        EditorGUILayout.LabelField($"[Cannot be Empty]", styleError, GUILayout.Width(100));
                    }
                    else
                    {
                        styleError.normal.textColor = Color.green;
                        EditorGUILayout.LabelField($"[Available Name]", styleError, GUILayout.Width(100));
                    }
                }
                GUILayout.EndHorizontal();



                EditorGUI.BeginDisabledGroup(combinedClipFileName == "" || combinedClipFileName == null);
                if (GUILayout.Button("Save Combined Audio", GUILayout.Height(30)))
                {
                    int totalSamplesCount = 0;

                    for (int i = 0; i < audioJoinList.Count; i++)
                        totalSamplesCount += audioJoinList[i].samples;


                    AudioClip ac = AudioClip.Create("Temp", totalSamplesCount, audioJoinList[0].channels, audioJoinList[0].frequency, false);

                    float[] concatenatedSamples = audioJoinList.Select(audioClip =>
                    {
                        float[] samples = new float[audioClip.samples * audioClip.channels];
                        audioClip.GetData(samples, 0);
                        return samples;
                    })
                    .SelectMany(x => x)
                    .ToArray();

                    ac.SetData(concatenatedSamples, 0);
                    WaveUtils.Save(combinedClipFileName, ac, _directoryPath, false);
                    AssetDatabase.Refresh();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(10);

            }


            foldEqualizer = FoldOuts.FoldOut("Audio Equalizer", foldEqualizer);
            if (foldEqualizer)
            {
                GUILayout.BeginHorizontal();
                clipToEqualize = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Clip to Equalize", infoToolTip, "Select an audio file you wish to equalize. You can adjust the sliders to make the voice loud, low, bassy or shrill. Once satisfied with the changes, enter a suitable name and save the file. You can reset the settings for the equalizer using the reset button."), clipToEqualize, typeof(AudioClip), true);

                if (GUILayout.Button("Active Clip", GUILayout.MaxWidth(80)) && Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)))
                {
                    clipToEqualize = (AudioClip)Selection.activeObject;
                }

                if (GUILayout.Button("×", EditorStyles.boldLabel, GUILayout.MaxWidth(20)))
                {
                    clipToEqualize = null;
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(clipToEqualize == null);
                volume = EditorGUILayout.Slider("Gain Volume", volume, -10, 10, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("dB", style, GUILayout.MaxWidth(20));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                pitch = EditorGUILayout.Slider("Pitch", pitch, -12, 12);
                EditorGUILayout.LabelField("ST", style, GUILayout.MaxWidth(20));
                EditorGUILayout.EndHorizontal();
                sectionTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Parametric EQ", sectionTitle);
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int i = 0; i < 6; i++)
                {
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" ", GUILayout.MaxWidth((bandFreqs[i].ToString().Length - 2) * (bandFreqs[i].ToString().Length - 1))); // Done for allignment purposes
                    gains[5 - i] = GUILayout.VerticalSlider(gains[5 - i], 0.2f, 1.2f, GUILayout.Height(100));
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label(bandFreqs[i].ToString());
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                {

                    float[] samples = new float[clipToEqualize.samples * clipToEqualize.channels];
                    clipToEqualize.GetData(samples, 0);

                    int numBands = 6;
                    float[] bandQs = { 1f, 1f, 1f, 1f, 1f, 1f };

                    for (int i = 0; i < numBands; i++)
                    {
                        float freq = bandFreqs[i];
                        float q = bandQs[i];
                        float gain = gains[i];

                        float w0 = 2 * Mathf.PI * freq / clipToEqualize.frequency;
                        float alpha = Mathf.Sin(w0) / (2 * q);

                        float b0 = 1 + alpha * gain;
                        float b1 = -2 * Mathf.Cos(w0);
                        float b2 = 1 - alpha * gain;
                        float a0 = 1 + alpha / gain;
                        float a1 = -2 * Mathf.Cos(w0);
                        float a2 = 1 - alpha / gain;

                        float x1 = 0;
                        float x2 = 0;
                        float y1 = 0;
                        float y2 = 0;

                        for (int j = 0; j < samples.Length; j++)
                        {
                            float x0 = samples[j];
                            float y0 = (b0 / a0) * x0 + (b1 / a0) * x1 + (b2 / a0) * x2
                                       - (a1 / a0) * y1 - (a2 / a0) * y2;

                            x2 = x1;
                            x1 = x0;
                            y2 = y1;
                            y1 = y0;

                            samples[j] = y0;
                        }
                    }

                    for (int i = 0; i < samples.Length; i++)
                    {
                        samples[i] *= Mathf.Pow(10, volume / 20); //Decibel Conversion
                    }

                    // Apply the pitch shift to the sample data
                    float[] pitchedSamples = new float[Mathf.CeilToInt(samples.Length * Mathf.Pow(2, -pitch / 12))];
                    for (int i = 0; i < pitchedSamples.Length; i++)
                    {
                        float oldIndex = (float)i / Mathf.Pow(2, -pitch / 12); //Semitone Conversion
                        int index = Mathf.FloorToInt(oldIndex);
                        float t = oldIndex - index;

                        if (index >= samples.Length - 1)
                        {
                            pitchedSamples[i] = samples[samples.Length - 1];
                        }
                        else
                        {
                            pitchedSamples[i] = Mathf.Lerp(samples[index], samples[index + 1], t);
                        }
                    }


                    AudioClip ac = AudioClip.Create("Temp", pitchedSamples.Length, clipToEqualize.channels, clipToEqualize.frequency, false);
                    ac.SetData(pitchedSamples, 0);
                    if (!Directory.Exists(_directoryPath + "/Temp_data")) Directory.CreateDirectory(_directoryPath + "/Temp_data");
                    WaveUtils.Save("TempEqualize", ac, _directoryPath + "/Temp_data", false);
                    AssetDatabase.Refresh();
                    AudioClip temp = (AudioClip)AssetDatabase.LoadAssetAtPath(_directoryPath + "/Temp_data/TempEqualize.wav", typeof(AudioClip));
                    PlayClip(temp, 0, false);

                }
                if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                }
                if (GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(25)))
                {
                    volume = 0;
                    pitch = 0;
                    for (int i = 0; i < 6; i++)
                        gains[i] = 1;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                EditorGUI.BeginChangeCheck();
                equalizedClipFileName = EditorGUILayout.TextField("File Name", equalizedClipFileName);
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{equalizedClipFileName}.wav", style);
                if (EditorGUI.EndChangeCheck())
                {
                    //Check all files for name existence
                    fileName = $"{equalizedClipFileName}";

                    equalizeFileExists = false;
                    var info = new DirectoryInfo(_directoryPath);
                    var fileInfo = info.GetFiles();
                    foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                    {
                        if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                        {
                            equalizeFileExists = true;
                        }
                    }
                }

                if (equalizeFileExists)
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    styleError.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"[Overwrite Name]", styleError, GUILayout.Width(100));
                }
                else
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    if (equalizedClipFileName == "" || equalizedClipFileName == null)
                    {
                        styleError.normal.textColor = Color.red;
                        EditorGUILayout.LabelField($"[Cannot be Empty]", styleError, GUILayout.Width(100));
                    }
                    else
                    {
                        styleError.normal.textColor = Color.green;
                        EditorGUILayout.LabelField($"[Available Name]", styleError, GUILayout.Width(100));
                    }
                }
                GUILayout.EndHorizontal();



                EditorGUI.BeginDisabledGroup(equalizedClipFileName == "" || equalizedClipFileName == null);
                if (GUILayout.Button("Save Equalized Audio", GUILayout.Height(30)))
                {
                    float[] samples = new float[clipToEqualize.samples * clipToEqualize.channels];
                    clipToEqualize.GetData(samples, 0);

                    int numBands = 6;
                    float[] bandQs = { 1f, 1f, 1f, 1f, 1f, 1f };

                    for (int i = 0; i < numBands; i++)
                    {
                        float freq = bandFreqs[i];
                        float q = bandQs[i];
                        float gain = gains[i];

                        float w0 = 2 * Mathf.PI * freq / clipToEqualize.frequency;
                        float alpha = Mathf.Sin(w0) / (2 * q);

                        float b0 = 1 + alpha * gain;
                        float b1 = -2 * Mathf.Cos(w0);
                        float b2 = 1 - alpha * gain;
                        float a0 = 1 + alpha / gain;
                        float a1 = -2 * Mathf.Cos(w0);
                        float a2 = 1 - alpha / gain;

                        float x1 = 0;
                        float x2 = 0;
                        float y1 = 0;
                        float y2 = 0;

                        for (int j = 0; j < samples.Length; j++)
                        {
                            float x0 = samples[j];
                            float y0 = (b0 / a0) * x0 + (b1 / a0) * x1 + (b2 / a0) * x2
                                       - (a1 / a0) * y1 - (a2 / a0) * y2;

                            x2 = x1;
                            x1 = x0;
                            y2 = y1;
                            y1 = y0;

                            samples[j] = y0;
                        }
                    }

                    for (int i = 0; i < samples.Length; i++)
                    {
                        samples[i] *= Mathf.Pow(10, volume / 20); //Decibel Conversion
                    }

                    // Apply the pitch shift to the sample data
                    float[] pitchedSamples = new float[Mathf.CeilToInt(samples.Length * Mathf.Pow(2, -pitch / 12))];
                    for (int i = 0; i < pitchedSamples.Length; i++)
                    {
                        float oldIndex = (float)i / Mathf.Pow(2, -pitch / 12); //Semitone Conversion
                        int index = Mathf.FloorToInt(oldIndex);
                        float t = oldIndex - index;

                        if (index >= samples.Length - 1)
                        {
                            pitchedSamples[i] = samples[samples.Length - 1];
                        }
                        else
                        {
                            pitchedSamples[i] = Mathf.Lerp(samples[index], samples[index + 1], t);
                        }
                    }


                    AudioClip ac = AudioClip.Create("Temp", pitchedSamples.Length, clipToEqualize.channels, clipToEqualize.frequency, false);
                    ac.SetData(pitchedSamples, 0);
                    WaveUtils.Save(equalizedClipFileName, ac, _directoryPath, false);
                    AssetDatabase.Refresh();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(10);

            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();



        }

        private void SaveInvoice()
        {
            PlayerPrefs.SetString("DeepVoicePro_Invoice", invoice);
        }
        [Serializable]
        public class MultiInvoicePayload
        {
            public string text;
            public string model;
            public string invoice;
            public string name;
            public string pace;
            public string variability;
            public string clarity;
        }
        public void SendMultiPayload(string text, string model, string invoice, string voice, string pace, string variability, string clarity)
        {
            // 1. Create an instance of your payload class
            MultiInvoicePayload payload = new MultiInvoicePayload
            {
                text = text,
                model = model,
                invoice = invoice,
                name = voice,
                pace = pace,
                variability = variability,
                clarity = clarity
            };
            // 2. Serialize the object to a JSON string
            string jsonPayload = JsonUtility.ToJson(payload);
            // 3. Start the coroutine with the serialized JSON
            this.StartCoroutine(Post("https://dvpro.aikodex.com/invoice", jsonPayload));
        }

        [Serializable]
        public class InstructInvoicePayload
        {
            public string text;
            public string model;
            public string invoice;
            public string name;
            public string instructions;
        }
        public void SendInstructPayload(string text, string model, string invoice, string voice, string instructions)
        {
            // 1. Create an instance of your payload class
            InstructInvoicePayload payload = new InstructInvoicePayload
            {
                text = text,
                model = model,
                invoice = invoice,
                name = voice,
                instructions = instructions
            };
            // 2. Serialize the object to a JSON string
            string jsonPayload = JsonUtility.ToJson(payload);
            // 3. Start the coroutine with the serialized JSON
            this.StartCoroutine(Post("https://dvpro.aikodex.com/invoice", jsonPayload));
        }

        public Texture2D PaintWaveformSpectrum(AudioClip audio, int width, int height, Color col, bool slider, float sliderValue)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float[] samples = new float[audio.samples];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            int packSize = (audio.samples / width) + 1;
            int s = 0;
            for (int i = 0; i < audio.samples; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }


            for (int i = 1; i < waveform.Length; i++)
            {
                var start = (i - 2 > 0 ? i - 2 : 0);
                var end = (i + 2 < waveform.Length ? i + 2 : waveform.Length);

                float sum = 0;

                for (int j = start; j < end; j++)
                {
                    sum += waveform[j];
                }

                var avg = sum / (end - start);
                waveform[i] = avg;

            }


            //Transparent BG
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            if (!slider)
            {
                for (int x = 0; x < waveform.Length; x = x + 2)
                {
                    for (int y = 0; y <= waveform[x] * height; y++)
                    {
                        tex.SetPixel(x, (height / 2) + y, col);
                        tex.SetPixel(x, (height / 2) - y, col);
                    }
                }
            }
            else
            {
                for (int x = 0; x < waveform.Length; x = x + 2)
                {
                    for (int y = 0; y <= waveform[x] * height; y++)
                    {
                        if (x < waveform.Length * sliderValue)
                        {
                            tex.SetPixel(x, (height / 2) + y, col);
                            tex.SetPixel(x, (height / 2) - y, col);
                        }
                    }
                }
            }
            tex.Apply();

            return tex;
        }

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            method.Invoke(
                null,
                new object[] { clip, startSample, loop }
            );
        }

        public static void StopAllClips()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { },
                null
            );

            method.Invoke(
                null,
                new object[] { }
            );
        }


        IEnumerator Post(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.result);
                Debug.Log("There was an error in generating the voice. Please check your invoice number and try again or check the documentation for more information.");
                if (request.responseCode == 400)
                {
                    Debug.Log("Error in text field: Please check your prompt for quotes (\"\") and line breaks at the end of the prompt. There could also be special formatting in your text. Please remove any special formatting by pasting as plain text in a notepad and then pasting the text here. Inclusion of any special formatting or illegal characters will result in an error such as this. For best results, please use a combination of letters, periods and commas and make sure there are no line breaks in between or at the end. If you must use quotes or line breaks, please prepend them with a backslash. Please do not press enter in the text field before clicking on generate.");
                }
            }
            else
            {
                if (request.responseCode == 400)
                {
                    Debug.Log("Error in text field: Please check your prompt for quotes (\"\") and line breaks at the end of the prompt. There could also be special formatting in your text. Please remove any special formatting by pasting as plain text in a notepad and then pasting the text here. Inclusion of any special formatting or illegal characters will result in an error such as this. For best results, please use a combination of letters, periods and commas and make sure there are no line breaks in between or at the end. If you must use quotes or line breaks, please prepend them with a backslash. Please do not press enter in the text field before clicking on generate.");
                }
                if (request.downloadHandler.text == "Invalid Response")
                    Debug.Log("Invalid Invoice Number. Please check your invoice number and try again.");
                else if (request.downloadHandler.text == "Limit Reached")
                    Debug.Log("It seems that you may have reached the limit. To check your character usage, please click on the Status button. Please wait until 30th/31st of the month to get a renewed character count. Thank you for using DeepVoice.");
                else
                {
                    byte[] soundBytes = System.Convert.FromBase64String(request.downloadHandler.text);
                    File.WriteAllBytes($"{_directoryPath}/{fileName}.wav", soundBytes);
                    AssetDatabase.Refresh();
                    Selection.activeObject = (AudioClip)AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{fileName}.wav");
                    take = (int.Parse(take) + 1).ToString();
                }
            }

            request.Dispose();
        }
        IEnumerator Status(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                if (request.downloadHandler.text == "Invalid Invoice Number")
                    Debug.Log("You do not have any generations or your invoice number is invalid. Please click on verify to verify your purchase.");
                else
                    Debug.Log("You have used " + request.downloadHandler.text + " characters.");
            }

            request.Dispose();
        }
        IEnumerator Verify(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                if (request.downloadHandler.text == "Not Verified")
                    Debug.Log("Invoice number verification unsuccessful. Please check your invoice number and try again or contact the publisher on the email given in the documentation.");
                else
                {
                    if (invoice.Length == 13)
                        Debug.Log("Your invoice number is verified, however, please use your invoice number to access the voice generator. Please check the documentation to get your invoice number. An invoice number is a 13 digit number you can find on the \"My Orders\" page on the asset store page.");
                    else
                        Debug.Log("Your invoice is verified. Thank you for choosing DeepVoice!");
                }
            }
            request.Dispose();
        }
    }

}
