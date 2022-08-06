﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using YagihataItems.YagiUtils;

namespace YagihataItems.RadialInventorySystemV4
{
    public abstract class EditorTab
    {
        protected class MaterialOverrideData
        {
            public IndexPair index;
            public List<Material> materials;
        }
        public abstract void InitializeTab(ref Avatar risAvatar);
        public abstract void DrawTab(ref Avatar risAvatar, Rect position, bool showingVerticalScroll);
        public abstract string[] CheckErrors(ref Avatar risAvatar);
        protected abstract void BuildFXLayer(ref Avatar risAvatar, string autoGeneratedFolder);
        protected abstract void BuildExpressionParameters(ref Avatar risAvatar, string autoGeneratedFolder);
        protected abstract void BuildExpressionsMenu(ref Avatar risAvatar, string autoGeneratedFolder);
        public virtual void ApplyToAvatar(ref Avatar risAvatar)
        {
            var autoGeneratedFolder = RIS.AutoGeneratedFolderPath + risAvatar.UniqueID + "/";
            if (!AssetDatabase.IsValidFolder(autoGeneratedFolder))
                UnityUtils.CreateFolderRecursively(autoGeneratedFolder);
            BuildExpressionParameters(ref risAvatar, autoGeneratedFolder);
            BuildExpressionsMenu(ref risAvatar, autoGeneratedFolder);
            BuildFXLayer(ref risAvatar, autoGeneratedFolder);



            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Radial Inventory System", "ビルド終了！", "OK");
        }
        protected void TryAddParam(ref Avatar risAvatar, string name, float defaultValue, bool saved, VRCExpressionParameters.ValueType valueType = VRCExpressionParameters.ValueType.Bool)
        {
            var avatar = risAvatar.GetAvatarRoot();

            if (avatar == null)
                return;

            var expParams = avatar.expressionParameters;
            if (risAvatar.OptimizeParameters)
            {
                var existParam = expParams.FindParameter(name);
                if (existParam != null)
                {
                    existParam.saved = saved;
                    existParam.valueType = valueType;
                    existParam.defaultValue = defaultValue;
                }
                else
                {
                    expParams.AddParameter(name, valueType, saved, defaultValue);
                }

            }
            else
                expParams.AddParameter(name, valueType, saved, defaultValue);

        }
        protected void CheckParam(VRCAvatarDescriptor avatar, AnimatorController controller, string paramName, bool defaultEnabled)
        {
            var param = controller.parameters.FirstOrDefault(n => n.name == paramName);
            if (param == null)
            {
                controller.AddParameter(paramName, AnimatorControllerParameterType.Bool);
                param = controller.parameters.FirstOrDefault(n => n.name == paramName);
            }
            param.type = AnimatorControllerParameterType.Bool;
            param.defaultBool = defaultEnabled;
        }
        protected void CheckParam(VRCAvatarDescriptor avatar, AnimatorController controller, string paramName, int defaultValue)
        {
            var param = controller.parameters.FirstOrDefault(n => n.name == paramName);
            if (param == null)
            {
                controller.AddParameter(paramName, AnimatorControllerParameterType.Int);
                param = controller.parameters.FirstOrDefault(n => n.name == paramName);
            }
            param.type = AnimatorControllerParameterType.Int;
            param.defaultInt = defaultValue;
        }
        public virtual void RemoveFromAvatar(ref Avatar risAvatar)
        {
            var avatar = risAvatar.GetAvatarRoot();

            if (avatar == null)
                return;

            var autoGeneratedFolder = RIS.AutoGeneratedFolderPath + risAvatar.UniqueID + "/";
            UnityUtils.DeleteFolder(autoGeneratedFolder + "Animations/");
            UnityUtils.DeleteFolder(autoGeneratedFolder + "SubMenus/");
            var fxLayer = avatar.GetFXLayer(autoGeneratedFolder + "AutoGeneratedFXLayer.controller", false);
            if (fxLayer != null)
            {
                foreach (var name in fxLayer.layers.Where(n => n.name.StartsWith("RIS")).Select(n => n.name))
                    fxLayer.TryRemoveLayer(name);
                foreach (var name in fxLayer.parameters.Where(n => n.name.StartsWith("RIS")).Select(n => n.name))
                    fxLayer.TryRemoveParameter(name);
            }
            if (avatar.expressionsMenu != null)
            {
                var controls = avatar.expressionsMenu.controls;
                controls.RemoveAll(n => n.name == "Radial Inventory");
                avatar.expressionsMenu.controls = controls;
                EditorUtility.SetDirty(avatar.expressionsMenu);
            }
            if (avatar.expressionParameters != null)
            {
                var parameters = avatar.expressionParameters.parameters;
                foreach (var name in parameters.Where(n => n.name.StartsWith("RIS")).Select(n => n.name))
                    avatar.expressionParameters.TryRemoveParameter(name);
                parameters = avatar.expressionParameters.parameters;
                avatar.expressionParameters.parameters = parameters;
                EditorUtility.SetDirty(avatar.expressionParameters);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Radial Inventory System", "削除処理終了！", "OK");

        }
        public virtual void CalculateMemoryCost(ref Avatar risAvatar, out int costSum, out int costNow, out int costAdd)
        {
            var paramsTemp = new List<VRCExpressionParameters.Parameter>();
            foreach (var groupIndex in Enumerable.Range(0, risAvatar.Groups.Count))
            {
                var group = risAvatar.Groups[groupIndex];
                if (risAvatar.MenuMode == RIS.MenuModeType.Simple && group.UseResetButton)
                    paramsTemp.Add(new VRCExpressionParameters.Parameter() { name = $"{RIS.Prefix}-G{groupIndex}RESET", valueType = VRCExpressionParameters.ValueType.Bool });
                foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    paramsTemp.Add(new VRCExpressionParameters.Parameter() { name = $"{RIS.Prefix}-G{groupIndex}P{propIndex}", valueType = VRCExpressionParameters.ValueType.Bool });
            }
            paramsTemp.Add(new VRCExpressionParameters.Parameter() { name = $"{RIS.Prefix}-Initialize", valueType = VRCExpressionParameters.ValueType.Bool });
            var expressionParameter = risAvatar.GetAvatarRoot()?.GetExpressionParameters(RIS.AutoGeneratedFolderPath + risAvatar.UniqueID + "/", false);
            expressionParameter.CalculateMemoryCount(out costNow, out costSum, paramsTemp, risAvatar.OptimizeParameters, RIS.Prefix);
            costAdd = paramsTemp.Sum(n => VRCExpressionParameters.TypeCost(n.valueType));
        }
    }
}
