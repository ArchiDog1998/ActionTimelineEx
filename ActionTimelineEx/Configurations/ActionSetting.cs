﻿using ActionTimelineEx.Helpers;
using ECommons.DalamudServices;
using ImGuiNET;
using System.Numerics;
using XIVConfigUI.Attributes;

namespace ActionTimelineEx.Configurations;

internal class ActionSettingAttribute() : ListUIAttribute(0)
{
    public override uint GetIcon(object obj)
    {
        if (obj is not ActionSetting setting)
            return base.GetIcon(obj);
        return setting.IconId;
    }

    public override void OnClick(object obj)
    {
        base.OnClick(obj);
        if (obj is not ActionSetting setting) return;

        //TODO: Change the acion ID...
    }

    public override string GetDescription(object obj)
    {
        if (obj is not ActionSetting setting) return base.GetDescription(obj);
        return setting.DisplayName;
    }
}

public enum ActionSettingType : byte
{
    Action,
    Item,
}

[ActionSetting]
public class ActionSetting
{
    internal uint IconId { get; private set; } = 0;
    internal bool IsGCD { get; private set; } = false;

    internal string DisplayName { get; private set; } = "";

    private uint _actionId;

    public uint ActionId 
    {
        get => _actionId;
        set
        {
            if (value == _actionId) return;
            _actionId = value;

            Update();
        }
    }
    [JsonIgnore, UI("Id")]
    public int Id { get => (int)ActionId; set => ActionId = (uint) value; }
    private ActionSettingType _type;

    [UI("Type")]
    public ActionSettingType Type
    {
        get => _type;
        set
        {
            if (value == _type) return;
            _type = value;

            Update();
        }
    }

    private void Update()
    {
        ClearData();

        switch (Type)
        {
            case ActionSettingType.Action:
                UpdateAction();
                return;

            case ActionSettingType.Item:
                UpdateItem();
                return;
        }

        void UpdateItem()
        {
            var item = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.GetRow(ActionId);
            if (item == null) return;

            IconId = item.Icon;
            DisplayName = item.Name;
        }

        void UpdateAction()
        {
            var action = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?.GetRow(ActionId);
            if (action == null) return;

            IsGCD = action.CooldownGroup == 58 || action.AdditionalCooldownGroup == 58;

            IconId = GetActionIcon(action);
            DisplayName = $"{action.Name} ({(IsGCD ? "GCD" : "Ability")})";
        }

        void ClearData()
        {
            IconId = 0;
            DisplayName = string.Empty;
            IsGCD = false;
        }
    }

    public void Draw(ImDrawListPtr drawList, Vector2 point, float size)
    {
        drawList.DrawActionIcon(IconId, Type is ActionSettingType.Item, point, size);
        if (!string.IsNullOrEmpty(DisplayName) && DrawHelper.IsInRect(point, new Vector2(size))) ImGui.SetTooltip(DisplayName);
    }

    private static uint GetActionIcon(Lumina.Excel.GeneratedSheets.Action action)
    {
        var isGAction = action.ActionCategory.Row is 10 or 11;
        if (!isGAction) return action.Icon;

        var gAct = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.GeneralAction>()?.FirstOrDefault(g => g.Action.Row == action.RowId);

        if (gAct == null) return action.Icon;
        return (uint)gAct.Icon;
    }
}
