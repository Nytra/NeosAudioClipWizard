using BaseX;
using CodeX;
using FrooxEngine;
using FrooxEngine.UIX;
using NeosModLoader;
using System.Reflection;
using System.Collections.Generic;

namespace NeosAudioClipWizard
{
	public class NeosAudioClipWizard : NeosMod
	{
		public override string Name => "AudioClipWizard";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/NeosAudioClipWizard";
		const string WIZARD_TITLE = "Audio Clip Wizard (Mod)";
		public override void OnEngineInit()
		{
			Engine.Current.RunPostInit(AddMenuOption);
		}
		void AddMenuOption()
		{
			DevCreateNewForm.AddAction("Editor", WIZARD_TITLE, (x) => AudioClipWizard.CreateWizard(x));
		}

		class AudioClipWizard
		{
			public static AudioClipWizard CreateWizard(Slot x)
			{
				return new AudioClipWizard(x);
			}
			Slot WizardSlot;
			readonly ReferenceField<Slot> processingRoot;
			readonly Button convertButton;
			readonly Button normalizeButton;
			readonly Button extractSidesButton;
			readonly Button denoiseButton;
			readonly Button trimSilenceButton;
			readonly Button trimStartSilenceButton;
			readonly Button trimEndSilenceButton;
			readonly Button trimStartButton;
			readonly Button trimEndButton;
			readonly Button fadeInButton;
			readonly Button fadeOutButton;
			readonly Button makeLoopableButton;

            readonly ValueField<int> radioField;
			readonly Text statusText;
			readonly FloatTextEditorParser parser1;
			readonly FloatTextEditorParser parser2;

            void UpdateStatusText(string info)
			{
				statusText.Content.Value = info;
			}

			bool ValidateWizard()
			{
				if (processingRoot.Reference.Target == null)
				{
					UpdateStatusText("No processing root provided!");
					return false;
				}
				return true;
			}

			void DriveButton(Button b, string str)
			{
                var boolField = b.Slot.AttachComponent<ValueField<bool>>();
                boolField.Value.Value = true;
                var stringField = b.Slot.AttachComponent<ValueField<string>>();
                stringField.Value.Value = str;
                var stringCopy = b.Slot.AttachComponent<ValueCopy<string>>();
                stringCopy.Source.Target = stringField.Value;
                stringCopy.Target.Target = b.LabelTextField;
                var boolCopy = b.Slot.AttachComponent<ValueCopy<bool>>();
                boolCopy.Source.Target = boolField.Value;
                boolCopy.Target.Target = b.EnabledField;
            }

            AudioClipWizard(Slot x)
			{
				WizardSlot = x;
				WizardSlot.Tag = "Developer";
				//WizardSlot.OnPrepareDestroy += Slot_OnPrepareDestroy;
				WizardSlot.PersistentSelf = false;

				NeosCanvasPanel canvasPanel = WizardSlot.AttachComponent<NeosCanvasPanel>();
				canvasPanel.Panel.AddCloseButton();
				canvasPanel.Panel.AddParentButton();
				canvasPanel.Panel.Title = WIZARD_TITLE;
				canvasPanel.Canvas.Size.Value = new float2(300f, 648f);

				//canvasPanel.Canvas.Slot.AttachComponent<Image>().Tint.Value = new color(1f, 0.2f);

				Slot Data = WizardSlot.AddSlot("Data");
				processingRoot = Data.AddSlot("processingRoot").AttachComponent<ReferenceField<Slot>>();
				//processingRoot.Reference.Target = WizardSlot.World.RootSlot;
				radioField = Data.AddSlot("radioField").AttachComponent<ValueField<int>>();

				UIBuilder UI = new UIBuilder(canvasPanel.Canvas);
				UI.Canvas.MarkDeveloper();
				UI.Canvas.AcceptPhysicalTouch.Value = false;

				VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				UI.Style.MinHeight = 24f;
				UI.Style.PreferredHeight = 24f;
				UI.Style.PreferredWidth = 400f;
				UI.Style.MinWidth = 400f;

				UI.Text("Processing Root:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				UI.Next("Root");
				UI.Current.AttachComponent<RefEditor>().Setup(processingRoot.Reference);

				UI.Spacer(24f);

                normalizeButton = UI.Button();
                normalizeButton.LocalPressed += NormalizePressed;
                DriveButton(normalizeButton, "Normalize");

                extractSidesButton = UI.Button();
                extractSidesButton.LocalPressed += ExtractSidesPressed;
                DriveButton(extractSidesButton, "Extract Sides");

                denoiseButton = UI.Button();
                denoiseButton.LocalPressed += DenoisePressed;
                DriveButton(denoiseButton, "Denoise");

				UI.Spacer(24f);

                UI.Style.MinWidth = 90f;
                UI.Style.PreferredWidth = 90f;

                UI.HorizontalLayout(4f);

				UI.Text("Amplitude Threshold:");
                parser1 = UI.FloatField(0f, 1f, 4);
                parser1.ParsedValue.Value = 0.002f;

				UI.NestOut();

                UI.HorizontalLayout(4f);

                trimSilenceButton = UI.Button();
                trimSilenceButton.LocalPressed += TrimSilencePressed;
                DriveButton(trimSilenceButton, "Trim Silence");

                trimStartSilenceButton = UI.Button();
                trimStartSilenceButton.LocalPressed += TrimStartSilencePressed;
                DriveButton(trimStartSilenceButton, "Trim Start Silence");

                trimEndSilenceButton = UI.Button();
                trimEndSilenceButton.LocalPressed += TrimEndSilencePressed;
                DriveButton(trimEndSilenceButton, "Trim End Silence");

				UI.NestOut();

				UI.HorizontalLayout(4f);

                UI.Text("Position/Duration (in seconds):");
                parser2 = UI.FloatField(0f, float.MaxValue, 4);
                parser2.ParsedValue.Value = 0.1f;

				UI.NestOut();

                UI.HorizontalLayout(4f);

                trimStartButton = UI.Button();
                trimStartButton.LocalPressed += TrimStartPressed;
                DriveButton(trimStartButton, "Trim Start");

                trimEndButton = UI.Button();
                trimEndButton.LocalPressed += TrimEndPressed;
                DriveButton(trimEndButton, "Trim End");

				UI.NestOut();

				UI.HorizontalLayout(4f);

                fadeInButton = UI.Button();
                fadeInButton.LocalPressed += FadeInPressed;
                DriveButton(fadeInButton, "Fade In");

                fadeOutButton = UI.Button();
                fadeOutButton.LocalPressed += FadeOutPressed;
                DriveButton(fadeOutButton, "Fade Out");

				UI.NestOut();

                makeLoopableButton = UI.Button();
                makeLoopableButton.LocalPressed += MakeLoopablePressed;
                DriveButton(makeLoopableButton, "Make Loopable");

                UI.Spacer(24f);

                UI.HorizontalElementWithLabel("Convert to WAV", 0.942f, () => UI.ValueRadio(radioField.Value, 0));
				UI.HorizontalElementWithLabel("Convert to OGG Vorbis", 0.942f, () => UI.ValueRadio(radioField.Value, 1));
				UI.HorizontalElementWithLabel("Convert to FLAC", 0.942f, () => UI.ValueRadio(radioField.Value, 2));

				UI.Spacer(24f);

				convertButton = UI.Button();
				convertButton.LocalPressed += ConvertPressed;
				DriveButton(convertButton, "Convert Audio");

                UI.Spacer(24f);

				UI.Text("Status:");
				statusText = UI.Text("---");

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
			}

			void ConvertPressed(IButton btn, ButtonEventData data)
			{
				if (!ValidateWizard())
				{
					return;
				}

				List<StaticAudioClip> clips = processingRoot.Reference.Target.GetComponentsInChildren<StaticAudioClip>();
				if (clips.Count == 0)
				{
					UpdateStatusText("No clips found.");
					return;
				}
				UpdateStatusText($"Converting {clips.Count} clips...");
				foreach (StaticAudioClip c in clips)
				{
					switch (radioField.Value.Value)
					{
						case 0:
							MethodInfo wavMethod = typeof(StaticAudioClip).GetMethod("ConvertToWAV", BindingFlags.NonPublic | BindingFlags.Instance);
							wavMethod.Invoke(c, new object[] { btn, data, null });
							break;
						case 1:
							MethodInfo vorbisMethod = typeof(StaticAudioClip).GetMethod("ConvertToVorbis", BindingFlags.NonPublic | BindingFlags.Instance);
							vorbisMethod.Invoke(c, new object[] { btn, data, null });
							break;
						case 2:
							MethodInfo flacMethod = typeof(StaticAudioClip).GetMethod("ConvertToFLAC", BindingFlags.NonPublic | BindingFlags.Instance);
							flacMethod.Invoke(c, new object[] { btn, data, null });
							break;
						default:
							break;
					}
				}
				UpdateStatusText($"Converted {clips.Count} clips!");
			}

            void NormalizePressed(IButton btn, ButtonEventData data)
			{
                InvokeMethod(btn, data, "Normalize");
            }

			void ExtractSidesPressed(IButton btn, ButtonEventData data)
			{
                InvokeMethod(btn, data, "ExtractSides");
            }

			void DenoisePressed(IButton btn, ButtonEventData data)
			{
                InvokeMethod(btn, data, "Denoise");
            }

			void TrimSilencePressed(IButton btn, ButtonEventData data)
			{
				InvokeMethod(btn, data, parser1, "TrimSilence");
			}

            void TrimStartSilencePressed(IButton btn, ButtonEventData data)
            {
                InvokeMethod(btn, data, parser1, "TrimStartSilence");
            }

            void TrimEndSilencePressed(IButton btn, ButtonEventData data)
            {
                InvokeMethod(btn, data, parser1, "TrimEndSilence");
            }

			void TrimStartPressed(IButton btn, ButtonEventData data)
			{
				InvokeMethod(btn, data, parser2, "TrimStart");
			}

            void TrimEndPressed(IButton btn, ButtonEventData data)
            {
                InvokeMethod(btn, data, parser2, "TrimEnd");
            }

            void FadeInPressed(IButton btn, ButtonEventData data)
            {
                InvokeMethod(btn, data, parser2, "FadeIn");
            }

            void FadeOutPressed(IButton btn, ButtonEventData data)
            {
                InvokeMethod(btn, data, parser2, "FadeOut");
            }

            void MakeLoopablePressed(IButton btn, ButtonEventData data)
            {
                InvokeMethod(btn, data, parser2, "MakeLoopable");
            }

            void InvokeMethod(IButton btn, ButtonEventData data, FloatTextEditorParser parser, string methodName)
			{
				if (!ValidateWizard())
				{
					return;
				}

				List<StaticAudioClip> clips = processingRoot.Reference.Target.GetComponentsInChildren<StaticAudioClip>();
				if (clips.Count == 0)
				{
					UpdateStatusText("No clips found.");
					return;
				}

                MethodInfo method = typeof(StaticAudioClip).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

                UpdateStatusText($"Calling {methodName} for {clips.Count} clips...");
				foreach (StaticAudioClip c in clips)
				{
					method.Invoke(c, new object[] { btn, data, parser });
				}
				UpdateStatusText($"{methodName} called for {clips.Count} clips!");
			}

			void InvokeMethod(IButton btn, ButtonEventData data, string methodName)
            {
                if (!ValidateWizard())
                {
                    return;
                }

                List<StaticAudioClip> clips = processingRoot.Reference.Target.GetComponentsInChildren<StaticAudioClip>();
                if (clips.Count == 0)
                {
                    UpdateStatusText("No clips found.");
                    return;
                }

                MethodInfo method = typeof(StaticAudioClip).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

                UpdateStatusText($"Calling {methodName} for {clips.Count} clips...");
                foreach (StaticAudioClip c in clips)
                {
                    method.Invoke(c, new object[] { btn, data });
                }
                UpdateStatusText($"{methodName} called for {clips.Count} clips!");
            }
        }
	}
}
