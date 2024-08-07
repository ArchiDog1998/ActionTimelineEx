﻿using ActionTimelineEx.Configurations;
using ActionTimelineEx.Configurations.Actions;
using ActionTimelineEx.Timeline;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Lumina.Excel.GeneratedSheets;
using XIVDrawer.ElementSpecial;

namespace ActionTimelineEx.Helpers;

internal static class RotationHelper
{
    private static DrawingHighlightHotbar? _highLight;
    public static ActionSetting? ActiveAction => RotationSetting.GetNextAction(GcdUsedCount, oGcdUsedCount);

    public static RotationSetting RotationSetting => Plugin.Settings.RotationHelper.RotationSetting;

    internal static readonly List<ActionSetting> SuccessActions = [];

    public static int GcdUsedCount { get; private set; }
    public static byte oGcdUsedCount { get; private set; }


    public static void Init()
    {
        ActionEffect.ActionEffectEvent += ActionFromSelf;
        Svc.DutyState.DutyWiped += DutyState_DutyWiped;
        Svc.DutyState.DutyCompleted += DutyState_DutyWiped;
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);
        _highLight = new();
        Svc.Framework.Update += Framework_Update;
    }

    private static void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        if (_highLight == null) return;
        _highLight.Color = Plugin.Settings.RotationHighlightColor;
        _highLight.HotbarIDs.Clear();

        if (!Plugin.Settings.DrawRotation) return;
        if (!Plugin.Settings.HighlightInHotbar) return;

        var action = ActiveAction;
        if (action == null) return;
        HotbarID? hotBar = null;

        switch (action.Type)
        {
            case ActionSettingType.Action:
                var isGAction = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?.GetRow(action.ActionId)?.ActionCategory.Row is 10 or 11;
                if (isGAction)
                {
                    var gAct = Svc.Data.GetExcelSheet<GeneralAction>()?.FirstOrDefault(g => g.Action.Row == action.ActionId);
                    if (gAct != null)
                    {
                        hotBar = new HotbarID(FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.HotbarSlotType.GeneralAction, gAct.RowId);
                        break;
                    }
                }

                hotBar = new HotbarID(FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.HotbarSlotType.Action, action.ActionId);
                break;

            case ActionSettingType.Item:
                hotBar = new HotbarID(FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.HotbarSlotType.Item, action.ActionId);
                break;
        }

        if (hotBar == null) return;
        _highLight.HotbarIDs.Add(hotBar.Value);
    }

    public static void Dispose()
    {
        ActionEffect.ActionEffectEvent -= ActionFromSelf;
        Svc.DutyState.DutyWiped -= DutyState_DutyWiped;
        Svc.DutyState.DutyCompleted -= DutyState_DutyWiped;
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Svc.Framework.Update -= Framework_Update;
    }

    private static void ClientState_TerritoryChanged(ushort obj)
    {
        Clear();
    }

    private static void DutyState_DutyWiped(object? sender, ushort e)
    {
        Clear();
    }

    private static void ActionFromSelf(ActionEffectSet set)
    {
        if (!Player.Available) return;
        if (set.Source.EntityId != Player.Object.EntityId) return;
        if ((ActionCate)(set.Action?.ActionCategory.Value?.RowId ?? 0) is ActionCate.AutoAttack) return; //Auto Attack.

        var actionSettingType = (ActionSettingType)(byte)set.Header.ActionType;
        if (!Enum.IsDefined(typeof(ActionSettingType), actionSettingType)) return;
        var actionId = set.Header.ActionID;

        if (Plugin.Settings.RecordRotation)
        {
            RecordRotation(set);
            return;
        }

        if (!Plugin.Settings.DrawRotation) return;

        if (IsIgnored(set)) return;

        var nextAction = RotationSetting.GetNextAction(GcdUsedCount, oGcdUsedCount);
        if (IsGcd(set))
        {
            oGcdUsedCount = 0;
            GcdUsedCount++;
        }
        else
        {
            oGcdUsedCount++;
        }

        if (nextAction == null) return;

        if (nextAction.IsMatched(actionId, actionSettingType))
        {
            SuccessActions.Add(nextAction);
        }
        else if(Plugin.Settings.ShowWrongClick)
        {
            Svc.Chat.PrintError($"Clicked the wrong action {set.Name} ({set.Header.ActionID})! You should Click {nextAction.DisplayName} ({nextAction.ActionID})!");
        }
    }

    private static bool IsIgnored(in ActionEffectSet set)
    {
        if (Plugin.Settings.IgnoreItems && set.Action == null) return true;

        if (set.Action != null)
        {
            var type = set.Action.GetActionType();

            switch (type)
            {
                case ActionType.SystemAction when Plugin.Settings.IgnoreSystemActions:
                case ActionType.RoleAction when Plugin.Settings.IgnoreRoleActions:
                case ActionType.LimitBreak when Plugin.Settings.IgnoreLimitBreaks:
                case ActionType.DutyAction when Plugin.Settings.IgnoreDutyActions:
                    return true;
            }
        }

        var actionSettingType = (ActionSettingType)(byte)set.Header.ActionType;
        var actionId = set.Header.ActionID;

        foreach (var act in RotationSetting.IgnoreActions)
        {
            if (act == null) continue;
            if (act.IsMatched(actionId, actionSettingType)) return true;
        }
        return false;
    }

    private static void RecordRotation(in ActionEffectSet set)
    {
        if (IsGcd(set))
        {
            RotationSetting.GCDs.Add(new GCDAction()
            {
                ActionId = set.Header.ActionID,
            });
        }
        else
        {
            var gcd = RotationSetting.GCDs.LastOrDefault();
            if (gcd == null)
            {
                RotationSetting.GCDs.Add(gcd = new GCDAction());
            }

            gcd.oGCDs.Add(new oGCDAction()
            {
                ActionId = set.Header.ActionID,
                ActionType = (ActionSettingType)(byte)set.Header.ActionType,
            });
        }
    }

    private static bool IsGcd(in ActionEffectSet set)
    {
        return set.Action?.IsGcd() ?? false;
    }

    public static void Clear()
    {
        GcdUsedCount = 0;
        oGcdUsedCount = 0;
        SuccessActions.Clear();
    }
}
