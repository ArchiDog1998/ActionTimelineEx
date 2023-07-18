﻿using ActionTimeline.Helpers;
using ActionTimeline.Timeline;
using ActionTimelineEx.Configurations;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.Commands;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;

namespace ActionTimeline.Windows
{
    public class SettingsWindow : Window
    {
        private float _scale => ImGuiHelpers.GlobalScale;
        private Settings Settings => Plugin.Settings;

        public SettingsWindow() : base("ActionTimelineEx v" + typeof(SettingsWindow).Assembly.GetName().Version?.ToString() ?? string.Empty)
        {
            SizeCondition = ImGuiCond.FirstUseEver;
            Size = new Vector2(300, 490f);
            RespectCloseHotkey = true;
        }

        public override void OnClose()
        {
            Settings.Save();
            base.OnClose();
        }

        public override void Draw()
        {
            if (!ImGui.BeginTabBar("ActionTimelineEx Bar")) return;

            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralSetting();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Timeline"))
            {
                DrawTimelineSetting(Settings.TimelineSetting);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Help"))
            {
                DrawHelp();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawHelp() 
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Code.ToIconString()}##Github"))
            {
                Util.OpenLink("https://github.com/ArchiDog1998/ActionTimelineEx");
            }

            ImGui.SameLine();

            if (ImGui.Button($"{FontAwesomeIcon.History.ToIconString()}##ChangeLog"))
            {
                Util.OpenLink("https://github.com/ArchiDog1998/ActionTimelineEx/blob/release/CHANGELOG.md");
            }
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA5E5BFF);
            if (ImGui.Button($"{FontAwesomeIcon.Coffee.ToIconString()}##Support"))
            {
                Util.OpenLink("https://ko-fi.com/archited");
            }

            ImGui.PopStyleColor(3);
            ImGui.PopFont();

            if (ImGui.BeginChild("Help Information", new Vector2(0f, -1f), true))
            {
                CmdManager.DrawHelp();
                ImGui.EndChild();
            }
        }

        private ushort _aboutAdd = 0;
        private void DrawGeneralSetting()
        {
            ImGui.Checkbox("Show Only In Duty", ref Settings.ShowTimelineOnlyInDuty);
            ImGui.Checkbox("Show Only In Combat", ref Settings.ShowTimelineOnlyInCombat);

            //ImGui.NewLine();

            //ImGui.DragFloat("Status checking delay (seconds)", ref Settings.StatusCheckDelay, 0.01f, 0, 1);

            ImGui.NewLine();

            var index = 0;

            if(ImGui.CollapsingHeader("Showed Statuses"))
            {
                foreach (var statusId in TimelineManager.ShowedStatusId)
                {
                    var status = Svc.Data.GetExcelSheet<Status>()?.GetRow(statusId);
                    var texture = DrawHelper.GetTextureFromIconId(status?.Icon ?? 0);
                    if (texture != null)
                    {
                        ImGui.Image(texture.ImGuiHandle, new Vector2(18, 24));
                        var tips = $"{status?.Name ?? string.Empty} [{status?.RowId ?? 0}]";
                        DrawHelper.SetTooltip(tips);
                        if (++index % 10 != 0) ImGui.SameLine();
                    }
                }
            }

            ImGui.SameLine();
            ImGui.NewLine();

            ImGui.Text("Don't show these status.");

            if (ImGui.BeginChild("ExceptStatus", new Vector2(0f, -1f), true))
            {
                ushort removeId = 0, addId = 0;
                index = 0;
                foreach (var statusId in Plugin.Settings.HideStatusIds)
                {
                    var status = Svc.Data.GetExcelSheet<Status>()?.GetRow(statusId);
                    var texture = DrawHelper.GetTextureFromIconId(status?.Icon ?? 0);
                    if (texture != null)
                    {
                        ImGui.Image(texture.ImGuiHandle, new Vector2(24, 30));
                        DrawHelper.SetTooltip(status?.Name ?? string.Empty);
                        ImGui.SameLine();
                    }

                    var id = statusId.ToString();
                    ImGui.SetNextItemWidth(100 * _scale);
                    if (ImGui.InputText($"##Status{index++}", ref id, 8) && ushort.TryParse(id, out var newId))
                    {
                        removeId = statusId;
                        addId = newId;
                    }

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Ban.ToIconString()}##Remove{statusId}"))
                    {
                        removeId = statusId;
                    }
                    ImGui.PopFont();
                }
                var oneId = string.Empty;
                ImGui.SetNextItemWidth(100 * _scale);
                if (ImGui.InputText($"##AddOne", ref oneId, 8) && ushort.TryParse(oneId, out var newOneId))
                {
                    _aboutAdd = newOneId;
                }
                ImGui.SameLine();

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##AddNew"))
                {
                    addId = _aboutAdd;
                }
                ImGui.PopFont();

                if (removeId != 0)
                {
                    Plugin.Settings.HideStatusIds.Remove(removeId);
                }
                if (addId != 0)
                {
                    Plugin.Settings.HideStatusIds.Add(addId);
                }
                ImGui.EndChild();
            }
        }

        #region Timeline
        private void DrawTimelineSetting(DrawingSettings settings)
        {
            if (!ImGui.BeginTabBar("##Timeline_Settings_TabBar"))
            {
                return;
            }

            ImGui.PushItemWidth(80 * _scale);

            // general
            if (ImGui.BeginTabItem("General##Timeline_General"))
            {
                DrawGeneralTab(settings);
                ImGui.EndTabItem();
            }

            // icons
            if (ImGui.BeginTabItem("Icons##Timeline_Icons"))
            {
                DrawIconsTab(settings);
                ImGui.EndTabItem();
            }

            // casts
            if (ImGui.BeginTabItem("Bar##Timeline_Bar"))
            {
                DrawBarTab(settings);
                ImGui.EndTabItem();
            }

            // grid
            if (ImGui.BeginTabItem("Grid##Timeline_Grid"))
            {
                DrawGridTab(settings);
                ImGui.EndTabItem();
            }

            // gcd clipping
            if (ImGui.BeginTabItem("GCD Clipping##Timeline_GCD"))
            {
                DrawGCDClippingTab(settings);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        private void DrawGeneralTab(DrawingSettings settings)
        {
            ImGui.Checkbox("Enable", ref settings.Enable);
            ImGui.Checkbox("Is Rotation", ref settings.IsRotation);

            ImGui.NewLine();

            ImGui.Checkbox("Locked", ref settings.Locked);
            ImGui.ColorEdit4("Locked Color", ref settings.LockedBackgroundColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Unlocked Color", ref settings.UnlockedBackgroundColor, ImGuiColorEditFlags.NoInputs);

            ImGui.NewLine();

            ImGui.DragFloat("Size per second", ref settings.SizePerSecond, 0.3f, 20, 150);
            DrawHelper.SetTooltip("This is the width of every second drawn on the window.");

            if (!settings.IsRotation)
            {
                ImGui.DragInt("Offset Time (seconds)", ref settings.TimeOffsetSetting, 0.1f, 0, 10);
                DrawHelper.SetTooltip("This is the advanced time about action using");
            }
            ImGui.DragFloat("Drawing Center offset", ref settings.CenterOffset, 0.3f, -500, 500);
        }

        private void DrawIconsTab(DrawingSettings settings)
        {
            ImGui.DragInt("Icon Size", ref settings.GCDIconSize);

            ImGui.NewLine();
            ImGui.Checkbox("Show Off GCD", ref settings.ShowOGCD);

            if (settings.ShowOGCD)
            {
                ImGui.Indent();
                ImGui.DragInt("Off GCD Icon Size", ref settings.OGCDIconSize, 0.2f, 1, 100);
                ImGui.DragFloat("Iff GCD Vertical Offset", ref settings.OGCDOffset, 0.1f, 0, 1);
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Checkbox("Show Auto Attacks", ref settings.ShowAutoAttack);

            if (settings.ShowAutoAttack)
            {
                ImGui.Indent();
                ImGui.DragInt("Auto Attack Icon Size", ref settings.AutoAttackIconSize, 0.2f, 1, 100);
                ImGui.DragFloat("Auto Attack Vertical Offset", ref settings.AutoAttackOffset, 0.01f, 0, 1);
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Checkbox("Show Status Gain Lose", ref settings.ShowStatus);

            if (settings.ShowStatus)
            {
                ImGui.Indent();
                ImGui.DragInt("Status Icon Size", ref settings.StatusIconSize, 0.2f, 1, 100);
                ImGui.DragFloat("Status Icon Alpha", ref settings.StatusIconAlpha, 0.01f, 0, 1);
                ImGui.ColorEdit4("Status Gain Color", ref settings.StatusGainColor, ImGuiColorEditFlags.NoInputs);
                ImGui.ColorEdit4("Status Lose Color", ref settings.StatusLoseColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Checkbox("Show Damage Type", ref settings.ShowDamageType);
            if (settings.ShowDamageType)
            {
                ImGui.Indent();
                ImGui.ColorEdit4("Critical Color", ref settings.CriticalColor, ImGuiColorEditFlags.NoInputs);
                ImGui.ColorEdit4("Direct Color", ref settings.DirectColor, ImGuiColorEditFlags.NoInputs);
                ImGui.ColorEdit4("Critical Direct Color", ref settings.CriticalDirectColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }
        }

        private void DrawBarTab(DrawingSettings settings)
        {
            ImGui.ColorEdit4("Bar Background Color", ref settings.BackgroundColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("GCD Border Color", ref settings.GCDBorderColor, ImGuiColorEditFlags.NoInputs);
            ImGui.DragFloat("GCD Border Thickness", ref settings.GCDThickness, 0.01f, 0, 10);
            ImGui.DragFloat("GCD Border Round", ref settings.GCDRound, 0.01f, 0, 10);
            ImGui.DragFloatRange2("GCD Bar Height", ref settings.GCDHeightLow, ref settings.GCDHeightHigh, 0.01f, 0, 1);
            ImGui.NewLine();

            ImGui.ColorEdit4("Cast In Progress Color", ref settings.CastInProgressColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Cast Finished Color", ref settings.CastFinishedColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Cast Canceled Color", ref settings.CastCanceledColor, ImGuiColorEditFlags.NoInputs);

            ImGui.NewLine();

            ImGui.Checkbox("Show Animation Lock Time", ref settings.ShowAnimationLock);

            if (settings.ShowAutoAttack)
            {
                ImGui.Indent();
                ImGui.ColorEdit4("Animation Lock Color", ref settings.AnimationLockColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }

            ImGui.NewLine();

            ImGui.Checkbox("Show Status Line", ref settings.ShowStatusLine);

            if (settings.ShowAutoAttack)
            {
                ImGui.Indent();
                ImGui.DragFloat("Status Line Height", ref settings.StatusLineSize, 0.2f, 1, 100);
                ImGui.Unindent();
            }
        }

        private void DrawGridTab(DrawingSettings settings)
        {
            ImGui.Checkbox("Enabled", ref settings.ShowGrid);

            ImGui.DragFloat("Start Line Width", ref settings.GridStartLineWidth, 0.1f, 0.1f, 10);
            ImGui.ColorEdit4("Start Line Color", ref settings.GridStartLineColor, ImGuiColorEditFlags.NoInputs);

            if (!settings.ShowGrid) { return; }
            ImGui.NewLine();

            ImGui.Checkbox("Show Center Line", ref settings.ShowGridCenterLine);
            if (settings.ShowGridCenterLine)
            {
                ImGui.Indent();
                ImGui.DragFloat("Center Line Width", ref settings.GridCenterLineWidth, 0.1f, 0.1f, 10);
                ImGui.ColorEdit4("Center Line Color", ref settings.GridCenterLineColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }

            ImGui.NewLine();

            ImGui.DragFloat("Line Width", ref settings.GridLineWidth, 0.1f, 0.1f, 10);
            ImGui.ColorEdit4("Line Color", ref settings.GridLineColor, ImGuiColorEditFlags.NoInputs);

            ImGui.NewLine();
            ImGui.Checkbox("Divide By Seconds", ref settings.GridDivideBySeconds);

            if (!settings.GridDivideBySeconds) { return; }

            ImGui.Checkbox("Show Text", ref settings.GridShowSecondsText);

            ImGui.NewLine();
            ImGui.Checkbox("Sub-Divide By Seconds", ref settings.GridSubdivideSeconds);

            if (!settings.GridSubdivideSeconds) { return; }

            ImGui.DragInt("Sub-Division Count", ref settings.GridSubdivisionCount, 0.2f, 2, 8);
            ImGui.DragFloat("Sub-Division Line Width", ref settings.GridSubdivisionLineWidth, 0.5f, 1, 5);
            ImGui.ColorEdit4("Sub-Division Line Color", ref settings.GridSubdivisionLineColor, ImGuiColorEditFlags.NoInputs);
        }

        private void DrawGCDClippingTab(DrawingSettings settings)
        {
            ImGui.Checkbox("Enabled", ref settings.ShowGCDClippingSetting);
            DrawHelper.SetTooltip("This only shown when timeline is not rotation.");

            if (!settings.ShowGCDClipping) return;

            int clippingThreshold = (int)(settings.GCDClippingThreshold * 1000f);
            if (ImGui.DragInt("Threshold (ms)", ref clippingThreshold, 0.1f, 0, 1000))
            {
                settings.GCDClippingThreshold = (float)clippingThreshold / 1000f;
            }
            DrawHelper.SetTooltip("This can be used filter out \"false positives\" due to latency or other factors. Any GCD clipping detected that is shorter than this value will be ignored.\nIt is strongly recommended that you test out different values and find out what works best for your setup.");

            ImGui.DragInt("Max Time (seconds)", ref settings.GCDClippingMaxTime, 0.1f, 3, 60);
            DrawHelper.SetTooltip("Any GCD clip longer than this will be capped");

            ImGui.ColorEdit4("Color", ref settings.GCDClippingColor, ImGuiColorEditFlags.NoInputs);
        }
        #endregion
    }
}