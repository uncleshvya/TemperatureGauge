﻿using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TempGauge
{
	// Token: 0x02000004 RID: 4
	[StaticConstructorOnStartup]
	public class Building_TemperatureGauge : Building_Thermometer
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000231C File Offset: 0x0000051C
		public bool shouldSendAlert
		{
			get
			{
				return base.tempOutOfRange && this.alertState > AlertState.Off;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000008 RID: 8 RVA: 0x00002344 File Offset: 0x00000544
		private string alertGizmoLabel
		{
			get
			{
				string result;
				switch (this.alertState)
				{
				case AlertState.Off:
					result = Translator.Translate("AlertOffLabel");
					break;
				case AlertState.Normal:
					result = Translator.Translate("AlertNormalLabel");
					break;
				case AlertState.Critical:
					result = Translator.Translate("AlertCriticalLabel");
					break;
				default:
					result = Translator.Translate("AlertOffLabel");
					break;
				}
				return result;
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000023A2 File Offset: 0x000005A2
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<AlertState>(ref this.alertState, "alertState", AlertState.Normal, false);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000023C0 File Offset: 0x000005C0
		public override void Draw()
		{
			base.Draw();
			float temperature = this.GetRoom(RegionType.Set_Passable).Temperature;
			GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
			r.center = this.DrawPos + Vector3.up * 0.05f;
			r.size = new Vector2(0.55f, 0.2f);
			r.margin = 0.05f;
			r.fillPercent = Mathf.Clamp(Mathf.Abs(temperature), 1f, 50f) / 50f;
			r.unfilledMat = Building_TemperatureGauge.GaugeUnfilledMat;
			bool flag = temperature > 0f;
			if (flag)
			{
				r.filledMat = Building_TemperatureGauge.GaugeFillHotMat;
			}
			else
			{
				r.filledMat = Building_TemperatureGauge.GaugeFillColdMat;
			}
			Rot4 rotation = base.Rotation;
			rotation.Rotate(RotationDirection.Clockwise);
			r.rotation = rotation;
			GenDraw.DrawFillableBar(r);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000024A4 File Offset: 0x000006A4
		public override void TickRare()
		{
			base.TickRare();
			bool shouldSendAlert = this.shouldSendAlert;
			if (shouldSendAlert)
			{
				MoteMaker.ThrowMetaIcon(IntVec3Utility.ToIntVec3(GenThing.TrueCenter(this)), base.Map, ThingDefOf.Mote_IncapIcon);
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000024E4 File Offset: 0x000006E4
		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = base.GetType() == typeof(MinifiedThing);
            var tempInfoAdd = string.Empty;
            if (flag)
			{
				stringBuilder.Append(Translator.Translate("NotInstalled"));
				stringBuilder.AppendLine();
			}
            else
            {
                tempInfoAdd = Translator.Translate("CurrentTempIs");
                var currentTemp = this.GetRoom(RegionType.Set_Passable).Temperature;
                var niceTemp = (float)Math.Round(currentTemp * 10f) / 10f;
                tempInfoAdd += niceTemp.ToStringTemperature("F0");
            }
            bool flag2 = this.alertState == AlertState.Off;
			if (flag2)
			{
				stringBuilder.Append(Translator.Translate("AlertOffDesc"));
			}
			else
			{
				bool onHighTemp = this.onHighTemp;
				if (onHighTemp)
				{
					stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("AlertOnHighTemperatureDesc", base.targetTempString));
				}
				else
				{
					stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("AlertOnLowTemperatureDesc", base.targetTempString));
				}
            }
            if (!string.IsNullOrEmpty(tempInfoAdd))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(tempInfoAdd);
            }
            return stringBuilder.ToString();
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000025A0 File Offset: 0x000007A0
		public override IEnumerable<Gizmo> GetGizmos()
		{
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("UI/Commands/Alert_" + this.alertState.ToString(), true),
				defaultLabel = this.alertGizmoLabel,
				defaultDesc = Translator.Translate("AlertGizmoDesc"),
				action = delegate()
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					bool flag = this.alertState >= AlertState.Critical;
					if (flag)
					{
						this.alertState = AlertState.Off;
					}
					else
					{
						this.alertState++;
					}
				}
			};
			foreach (Gizmo g in base.GetGizmos())
			{
				yield return g;
			}
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings", true),
				defaultLabel = Translator.Translate("CommandCopyZoneSettingsLabel"),
				defaultDesc = Translator.Translate("CommandCopyZoneSettingsDesc"),
				action = delegate()
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					GaugeSettings_Clipboard.Copy(this.onHighTemp, this.CompTempControl.targetTemperature, this.alertState);
				},
				hotKey = KeyBindingDefOf.Misc4
			};
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings", true),
				defaultLabel = Translator.Translate("CommandPasteZoneSettingsLabel"),
				defaultDesc = Translator.Translate("CommandPasteZoneSettingsDesc"),
				action = delegate()
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					GaugeSettings_Clipboard.PasteInto(out this.onHighTemp, out this.CompTempControl.targetTemperature, out this.alertState);
				},
				hotKey = KeyBindingDefOf.Misc5
			};
			yield break;
		}

		// Token: 0x04000009 RID: 9
		private static readonly Material GaugeFillHotMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 0.5f, 0.2f), false);

		// Token: 0x0400000A RID: 10
		private static readonly Material GaugeFillColdMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.4f, 1f), false);

		// Token: 0x0400000B RID: 11
		private static readonly Material GaugeUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.1f, 0.1f, 0.1f), false);

		// Token: 0x0400000C RID: 12
		public AlertState alertState = AlertState.Normal;
	}
}
