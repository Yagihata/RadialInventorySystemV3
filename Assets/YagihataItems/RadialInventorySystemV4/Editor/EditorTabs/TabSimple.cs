﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using YagihataItems.YagiUtils;

namespace YagihataItems.RadialInventorySystemV4
{
    public class TabSimple : EditorTab
    {
        private ReorderableList propsReorderableList;
        private ReorderableList gameObjectsReorderableList;
        private ReorderableList propSetReorderableList;
        private const float zoomLevel = 0.6f;
        private GUIStyle buttonStyle = null;
        private Rect[] buttonRects = null;
        private short targetId = -1;
        private string[] targetName = { "頭のアイテム", "手のアイテム", "足のアイテム", "胸のアイテム", "腰のアイテム", "アクセサリー1", "アクセサリー2" };
        private string propSetTitle = "衣装セット";
        private Vector2 targetPosition = Vector2.zero;
        private GUIStyle targetNameLabelStyle = null;
        private Prop targetProp;
        private bool selectedPropIsChanged;
        private int bodyImageWidth = (int)(200 * zoomLevel);
        private int bodyImageHeight = (int)(480 * zoomLevel);
        private GUIStyle singleLinedLabelStyle = new GUIStyle(GUI.skin.label) { fixedHeight = EditorGUIUtility.singleLineHeight };
        private GUIStyle singleLinedButtonStyle = new GUIStyle(GUI.skin.button) { fixedHeight = EditorGUIUtility.singleLineHeight };

        private void SetTargetID(short id, Avatar risAvatar, bool forceEnable = false)
        {
            if (targetId == id && !forceEnable)
            {
                targetId = -1;
                InitializePropList(null, risAvatar);
            }
            else
            {
                targetId = id;
                if (risAvatar != null && targetId < risAvatar.Groups.Count)
                {
                    InitializePropList(risAvatar.Groups[targetId], risAvatar);
                }
            }
            targetProp = new Prop();
            InitializeGameObjectsList(risAvatar);
            InitializePropSetList(risAvatar);
        }
        private void DrawOutlinedLine(Vector2 dest, Vector2 mid, Vector2 src)
        {
            Drawing.DrawLine(dest, mid, Color.cyan, 5, true);
            Drawing.DrawLine(mid, src, Color.cyan, 5, true);
            Drawing.DrawLine(dest, mid, Color.white, 3, false, 2f);
            Drawing.DrawLine(mid, src, Color.white, 3, false, 2f);

        }
        private void InitializePropList(Group group, Avatar risAvatar)
        {
            List<Prop> props = null;
            if (group != null && group.Props != null)
                props = group.Props;
            else
                props = new List<Prop>();
            propsReorderableList = new ReorderableList(props, typeof(Prop))
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "アイテム一覧" + $": {props.Count}");
                    var position =
                        new Rect(
                            rect.x + rect.width - 20f,
                            rect.y,
                            20f,
                            13f
                        );
                    var maxPropCount = 8;
                    if (props.Count < maxPropCount && GUI.Button(position, ReorderableListStyle.AddContent, ReorderableListStyle.AddStyle))
                    {
                        var newProp = new Prop();
                        newProp.TargetObjects.Add(null);
                        group.Props.Add(newProp);
                    }
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    if (props.Count <= index)
                        return;
                    var rawPropName = props[index].GetPropName(risAvatar);
                    var propName = !string.IsNullOrEmpty(rawPropName) ? rawPropName : $"衣装{index}";
                    var style = GUI.skin.label;
                    style.fontSize = (int)(rect.height / 1.75f);
                    GUI.Label(rect, propName, style);
                    style.fontSize = 0;
                    rect.x = rect.x + rect.width - 20f;
                    rect.width = 20f;
                    if (GUI.Button(rect, ReorderableListStyle.SubContent, ReorderableListStyle.SubStyle))
                    {
                        props.RemoveAt(index);
                        if (index >= props.Count)
                            index = propsReorderableList.index = -1;
                    }
                },
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
                {
                    if (!isFocused)
                    {
                        var maxPropCount = 8;
                        if (risAvatar != null && group != null)
                        {
                            if (group.UseResetButton)
                                maxPropCount = 7;
                        }
                        if (index >= maxPropCount)
                        {
                            GUI.DrawTexture(rect, TexAssets.RedTexture);
                        }

                    }
                    else
                    {
                        GUI.DrawTexture(rect, TexAssets.BlueTexture);
                    }
                },
                drawFooterCallback = rect => { },
                footerHeight = 0f,
                elementHeightCallback = index =>
                {
                    if (props.Count <= index)
                        return 0;
                    return EditorGUIUtility.singleLineHeight * 1.55f;

                }

            };
        }
        private void InitializeGameObjectsList(Avatar risAvatar, Prop prop = null)
        {
            List<GUIDPathPair<GameObject>> gameObjects = null;
            var propFlag = prop != null && prop.TargetObjects != null;
            if (propFlag)
                gameObjects = prop.TargetObjects;
            else
                gameObjects = new List<GUIDPathPair<GameObject>>();
            var list = new ReorderableList(gameObjects, typeof(GameObject));
            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "オブジェクト一覧" + $": {gameObjects.Count}");
                var position =
                    new Rect(
                        rect.x + rect.width - 20f,
                        rect.y,
                        20f,
                        13f
                    );
                if (GUI.Button(position, ReorderableListStyle.AddContent, ReorderableListStyle.AddStyle) && propFlag)
                {
                    prop.TargetObjects.Add(null);
                }
            };
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (gameObjects.Count <= index)
                    return;
                rect.width -= 20;
                if (risAvatar.AvatarRoot?.GetObject() == null)
                    return;
                var parent = risAvatar.AvatarRoot?.GetObject()?.gameObject;
                var targetObject = gameObjects[index]?.GetObject(parent);
                EditorGUI.BeginChangeCheck();
                targetObject = (GameObject)EditorGUI.ObjectField(rect, targetObject, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    if (gameObjects[index] == null)
                        gameObjects[index] = new GUIDPathPair<GameObject>(ObjectPathStateType.RelativeFromObject, targetObject, parent);
                    else
                        gameObjects[index].SetObject(targetObject, parent);
                    if (gameObjects[index].GetObject(parent) == null && targetObject != null)
                    {
                        EditorUtility.DisplayDialog("Radial Inventory System", $"対象アバターとして指定しているオブジェクトの子オブジェクトのみ登録できます。", "OK");
                    }
                }
                rect.x = rect.x + rect.width;
                rect.width = 20f;
                if (GUI.Button(rect, ReorderableListStyle.SubContent, ReorderableListStyle.SubStyle))
                {
                    gameObjects.RemoveAt(index);
                    if (index >= gameObjects.Count)
                        index = list.index = -1;
                }
            };
            list.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
            {
                if (!isFocused)
                {
                    if (risAvatar.AvatarRoot?.GetObject() != null)
                    {
                        var parent = risAvatar.AvatarRoot?.GetObject()?.gameObject;
                        if (parent != null && index >= 0 && index < gameObjects.Count)
                        {

                            var targetObject = gameObjects[index]?.GetObject(parent);
                            if (targetObject != null && !targetObject.IsChildOf(parent))
                            {
                                GUI.DrawTexture(rect, TexAssets.RedTexture);
                            }
                        }
                    }

                }
                else
                {
                    GUI.DrawTexture(rect, TexAssets.BlueTexture);
                }
            };
            list.drawFooterCallback = rect => { };
            list.footerHeight = 0f;
            list.elementHeightCallback = index =>
            {
                if (gameObjects.Count <= index)
                    return 0;
                return EditorGUIUtility.singleLineHeight;
            };
            gameObjectsReorderableList = list;
        }
        private void InitializePropSetList(Avatar risAvatar, Prop prop = null)
        {
            List<IndexPair> propSets = null;
            var propFlag = prop != null && prop.PropSets != null;
            if (propFlag)
                propSets = prop.PropSets;
            else
                propSets = new List<IndexPair>();
            var list = new ReorderableList(propSets, typeof(IndexPair));
            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "衣装一覧" + $": {propSets.Count}");
                var position =
                    new Rect(
                        rect.x + rect.width - 20f,
                        rect.y,
                        20f,
                        13f
                    );
                if (GUI.Button(position, ReorderableListStyle.AddContent, ReorderableListStyle.AddStyle) && propFlag)
                {
                    prop.PropSets.Add(new IndexPair());
                }
            };
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (propSets.Count <= index)
                    return;
                rect.width -= 25;
                if (risAvatar.AvatarRoot?.GetObject() == null)
                    return;
                rect.y += 5;
                rect.height -= 10;
                using (new GUI.GroupScope(rect, GUI.skin.box))
                {
                    var propSet = propSets[index];
                    var subRect1 = new Rect(rect);
                    var subRect2 = new Rect(rect);
                    subRect1.x = subRect1.y = subRect2.x = 0;
                    subRect1.height = subRect1.height = subRect2.y = EditorGUIUtility.singleLineHeight;


                    subRect1.width = 120;
                    GUI.Label(subRect1, "アイテムの種類", singleLinedLabelStyle);
                    subRect1.x += subRect1.width;
                    subRect1.width = rect.width - subRect1.width;
                    EditorGUI.BeginChangeCheck();
                    var selectedGroup = propSet.GroupIndex;
                    selectedGroup = EditorGUI.Popup(subRect1, selectedGroup, targetName);
                    if (EditorGUI.EndChangeCheck())
                        propSet.GroupIndex = selectedGroup;
                    var propSetNames = new string[] { "0: ", "1: ", "2: ", "3: ", "4: ", "5: ", "6: ", "7: " };
                    if (selectedGroup < risAvatar.Groups.Count)
                    {
                        var group = risAvatar.Groups[selectedGroup];
                        for (int i = 0; i < group.Props.Count; i++)
                        {
                            var propName = group.Props[i].GetPropName(risAvatar);
                            if (propName != null)
                                propSetNames[i] = propSetNames[i] + propName;
                        }
                    }

                    subRect2.width = 120;
                    GUI.Label(subRect2, "有効化するプロップ", singleLinedLabelStyle);
                    subRect2.x += subRect2.width;
                    subRect2.width = rect.width - subRect2.width;
                    var selectedProp = propSet.PropIndex;
                    EditorGUI.BeginChangeCheck();
                    selectedProp = EditorGUI.Popup(subRect2, selectedProp, propSetNames);
                    if(EditorGUI.EndChangeCheck())
                        propSet.PropIndex = selectedProp;
                }
                rect.x = rect.x + rect.width + 5;
                rect.width = 20f;
                if (GUI.Button(rect, ReorderableListStyle.SubContent, ReorderableListStyle.SubStyle))
                {
                    propSets.RemoveAt(index);
                    if (index >= propSets.Count)
                        index = list.index = -1;
                }
            };
            list.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
            {
                if (isFocused)
                {
                    GUI.DrawTexture(rect, TexAssets.BlueTexture);
                }
            };
            list.drawFooterCallback = rect => { };
            list.footerHeight = 0f;
            list.elementHeightCallback = index =>
            {
                if (propSets.Count <= index)
                    return 0;
                return EditorGUIUtility.singleLineHeight * 2 + 10;
            };
            propSetReorderableList = list;
        }

        public override void InitializeTab(ref Avatar risAvatar)
        {
            SetTargetID(0, risAvatar, true);
        }
        public override void DrawTab(ref Avatar risAvatar, Rect position, bool showingVerticalScroll)
        {
            var cellWidth = position.width- 45f - bodyImageWidth * 2.5f;
            if (propsReorderableList == null)
                InitializePropList(null, risAvatar);
            if (gameObjectsReorderableList == null)
                InitializeGameObjectsList(risAvatar);
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle();
                buttonStyle.normal.background = null;
            }
            if (targetNameLabelStyle == null)
            {
                targetNameLabelStyle = new GUIStyle("ProjectBrowserHeaderBgTop");
                targetNameLabelStyle.fontSize = (int)(EditorGUIUtility.singleLineHeight * 1.3f);
                targetNameLabelStyle.alignment = TextAnchor.MiddleLeft;
                targetNameLabelStyle.fixedHeight = EditorGUIUtility.singleLineHeight * 2f;
            }

            using (new GUILayout.VerticalScope())
            {
                var rect = GUILayoutUtility.GetRect(0, 0);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Box(TexAssets.BodyTexture, GUILayout.Width(bodyImageWidth), GUILayout.Height(bodyImageHeight));
                    using (new GUILayout.VerticalScope())
                    {
                        var infoAreaRect = GUILayoutUtility.GetRect(0, 0);
                        if (targetId != -1)
                        {
                        }
                        var height = (int)(EditorGUIUtility.singleLineHeight * 2f);
                        var labelText = "アイテム未選択";
                        if (targetId == 7)
                            labelText = propSetTitle;
                        else if (targetId >= 0)
                            labelText = targetName[targetId];
                        GUILayout.Label($" {labelText}", targetNameLabelStyle, GUILayout.Height(height));
                        targetPosition.x = infoAreaRect.x;
                        targetPosition.y = infoAreaRect.y + (height / 2f) + 2;
                        if (targetId != -1 && risAvatar.Groups.Count <= targetId)
                        {
                            var count = targetId - risAvatar.Groups.Count;
                            for (int i = 0; i <= count; i++)
                                risAvatar.Groups.Add(new Group());
                            InitializePropList(risAvatar.Groups[targetId], risAvatar);
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledGroupScope(targetId == -1))
                            {
                                using (var scope = new EditorGUILayout.HorizontalScope(GUILayout.Width(bodyImageWidth * 1.5f)))
                                {
                                    var scopeWidth = bodyImageWidth * 1.5f;
                                    var propListHeight = propsReorderableList.GetHeight();
                                    using (new GUILayout.HorizontalScope(GUILayout.Height(propListHeight), GUILayout.Width(scopeWidth)))
                                    {
                                        var oldSelectedIndex = propsReorderableList.index;
                                        propsReorderableList.DoList(new Rect(scope.rect.x, scope.rect.y, scopeWidth, propListHeight));
                                        if (targetId != -1 && risAvatar.Groups.Count > targetId)
                                            risAvatar.Groups[targetId].Props = (List<Prop>)propsReorderableList.list;
                                        selectedPropIsChanged = oldSelectedIndex != propsReorderableList.index;
                                        GUILayout.Space(0);
                                    }
                                }
                            }
                            var propIndex = propsReorderableList.index;
                            var propIsSelected = risAvatar != null && risAvatar.Groups.Count >= 1 && targetId != -1 && risAvatar.Groups.Count > targetId && risAvatar.Groups[targetId].Props.Count > propsReorderableList.index && propsReorderableList.index >= 0 && propIndex >= 0;

                            if (propIsSelected && selectedPropIsChanged)
                            {
                                targetProp = risAvatar.Groups[targetId].Props[propsReorderableList.index];
                            }
                            else if (selectedPropIsChanged || targetProp == null)
                            {
                                targetProp = new Prop();
                            }
                            using (new EditorGUI.DisabledGroupScope(!propIsSelected))
                            {
                                using (new GUILayout.VerticalScope(GUILayout.Width(cellWidth)))
                                {
                                    if (targetId != 7)
                                    {
                                        //プロップ編集画面
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            using (new GUILayout.VerticalScope(GUILayout.Width(EditorGUIUtility.singleLineHeight * 2.5f + 5f)))
                                            {
                                                GUILayout.Label("アイコン");
                                                var icon = targetProp.Icon?.GetObject();
                                                EditorGUI.BeginChangeCheck();
                                                icon = (Texture2D)EditorGUILayout.ObjectField(icon, typeof(Texture2D), false, GUILayout.Width(EditorGUIUtility.singleLineHeight * 2.5f), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2.5f));
                                                if (EditorGUI.EndChangeCheck())
                                                    targetProp.Icon.SetObject(icon);
                                            }
                                            using (new GUILayout.VerticalScope())
                                            {
                                                GUILayout.Space(7);
                                                EditorGUIUtility.labelWidth = 110;
                                                targetProp.Name = EditorGUILayout.TextField("プロップ名", targetProp.Name);
                                                targetProp.AttachedBone = (RIS.BoneType)EditorGUILayout.EnumPopup("アイテムの追従先", targetProp.AttachedBone, GUILayout.MinWidth(1));
                                                targetProp.IsDefaultEnabled = EditorGUILayout.Toggle("はじめから出しておく", targetProp.IsDefaultEnabled);
                                                EditorGUIUtility.labelWidth = 0;
                                            }
                                        }
                                        GUILayout.Space(5);
                                        if (gameObjectsReorderableList == null || selectedPropIsChanged)
                                        {
                                            InitializeGameObjectsList(risAvatar, targetProp);
                                        }
                                        EditorGUI.BeginChangeCheck();
                                        gameObjectsReorderableList.DoLayoutList();
                                        if (EditorGUI.EndChangeCheck())
                                            targetProp.TargetObjects = (List<GUIDPathPair<GameObject>>)gameObjectsReorderableList.list;
                                    }
                                    else
                                    {
                                        //プロップ編集画面
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            using (new GUILayout.VerticalScope(GUILayout.Width(EditorGUIUtility.singleLineHeight * 2.5f + 5f)))
                                            {
                                                GUILayout.Label("アイコン");
                                                var icon = targetProp.Icon?.GetObject();
                                                EditorGUI.BeginChangeCheck();
                                                icon = (Texture2D)EditorGUILayout.ObjectField(icon, typeof(Texture2D), false, GUILayout.Width(EditorGUIUtility.singleLineHeight * 2.5f), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2.5f));
                                                if (EditorGUI.EndChangeCheck())
                                                    targetProp.Icon.SetObject(icon);
                                            }
                                            using (new GUILayout.VerticalScope())
                                            {
                                                GUILayout.Space(7);
                                                EditorGUIUtility.labelWidth = 110;
                                                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                                                targetProp.Name = EditorGUILayout.TextField("プロップ名", targetProp.Name);
                                                EditorGUI.BeginChangeCheck();
                                                targetProp.IsDefaultEnabled = EditorGUILayout.Toggle("はじめから出しておく", targetProp.IsDefaultEnabled);
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    var group = risAvatar.Groups[targetId];
                                                    for (int i = 0; i < group.Props.Count; ++i)
                                                    {
                                                        if (propIndex != i)
                                                            group.Props[i].IsDefaultEnabled = false;
                                                    }
                                                }
                                                EditorGUIUtility.labelWidth = 0;
                                            }
                                        }
                                        GUILayout.Space(5);

                                        if (propSetReorderableList == null || selectedPropIsChanged)
                                            InitializePropSetList(risAvatar, targetProp);
                                        EditorGUI.BeginChangeCheck();
                                        propSetReorderableList.DoLayoutList();
                                        if (EditorGUI.EndChangeCheck())
                                            targetProp.PropSets = (List<IndexPair>)propSetReorderableList.list;
                                    }
                                }
                            }
                        }
                    }
                }
                if (buttonRects == null)
                {
                    buttonRects = new Rect[]
                    {
                        new Rect(rect.x + 7, rect.y + 20, 30, 30),
                        new Rect(rect.x + 76, rect.y + 130, 30, 30),
                        new Rect(rect.x + 18, rect.y + 220, 30, 30),
                        new Rect(rect.x + 12, rect.y + 90, 30, 30),
                        new Rect(rect.x + 12, rect.y + 160, 30, 30),
                        new Rect(rect.x + 80, rect.y + 180, 30, 30),
                        new Rect(rect.x + 80, rect.y + 210, 30, 30),
                        new Rect(rect.x + 80, rect.y + 250, 30, 30)
                    };
                }
                else
                {
                    buttonRects[0].x = rect.x + 7;
                    buttonRects[1].x = rect.x + 76;
                    buttonRects[2].x = rect.x + 18;
                    buttonRects[3].x = rect.x + 12;
                    buttonRects[4].x = rect.x + 12;
                    buttonRects[5].x = rect.x + 80;
                    buttonRects[6].x = rect.x + 80;
                    buttonRects[7].x = rect.x + 80;

                    buttonRects[0].y = rect.y + 20;
                    buttonRects[1].y = rect.y + 130;
                    buttonRects[2].y = rect.y + 220;
                    buttonRects[3].y = rect.y + 90;
                    buttonRects[4].y = rect.y + 160;
                    buttonRects[5].y = rect.y + 180;
                    buttonRects[6].y = rect.y + 210;
                    buttonRects[7].y = rect.y + 250;
                }
                if (GUI.Button(buttonRects[0], TexAssets.CircleIcon, buttonStyle))
                    SetTargetID(0, risAvatar);
                if (GUI.Button(buttonRects[1], TexAssets.CircleIcon, buttonStyle))
                    SetTargetID(1, risAvatar);
                if (GUI.Button(buttonRects[2], TexAssets.CircleIcon, buttonStyle))
                    SetTargetID(2, risAvatar);
                if (GUI.Button(buttonRects[3], TexAssets.CircleIcon, buttonStyle))
                    SetTargetID(3, risAvatar);
                if (GUI.Button(buttonRects[4], TexAssets.CircleIcon, buttonStyle))
                    SetTargetID(4, risAvatar);
                if (GUI.Button(buttonRects[5], TexAssets.Accessory1Icon, buttonStyle))
                    SetTargetID(5, risAvatar);
                if (GUI.Button(buttonRects[6], TexAssets.Accessory2Icon, buttonStyle))
                    SetTargetID(6, risAvatar);
                if (GUI.Button(buttonRects[7], TexAssets.DresserIcon, buttonStyle))
                    SetTargetID(7, risAvatar);
                if (targetId != -1 && targetPosition != Vector2.zero)
                {
                    var midPosition = targetPosition;
                    midPosition.x += -20;
                    DrawOutlinedLine(buttonRects[targetId].center, midPosition, targetPosition);
                }
            }
        }

        public override string[] CheckErrors(ref Avatar risAvatar)
        {
            if (risAvatar == null)
                return new string[] { };

            var errors = new List<string>();
            if (risAvatar.Groups.Count == 0)
                errors.Add("グループを追加してください。");

            foreach (var groupIndex in Enumerable.Range(0, risAvatar.Groups.Count))
            {
                var group = risAvatar.Groups[groupIndex];

                var maxPropsCount = 8;

                if (groupIndex == 7)
                {
                    if (group.Props.Count > maxPropsCount)
                        errors.Add(string.Format($"{propSetTitle}のプロップ数がオーバーしています。(最大{0})", maxPropsCount));

                    foreach (var prop in group.Props)
                    {
                        var propName = prop.GetPropName(risAvatar);
                        if (string.IsNullOrEmpty(propName))
                            propName = "衣装" + group.Props.IndexOf(prop);

                        if (risAvatar.GetAvatarRoot() != null)
                        {
                            var parentGameObject = risAvatar.GetAvatarRoot()?.gameObject;
                            if (!prop.PropSets.Any(v => v != null))
                                errors.Add($"衣装セット[{propName}]に衣装が登録されていません。");
                        }
                    }
                }
                else
                {
                    var groupName = targetName[groupIndex];
                    if (group.Props.Count > maxPropsCount)
                        errors.Add(string.Format($"{groupName}のプロップ数がオーバーしています。(最大{0})", maxPropsCount));

                    foreach (var prop in group.Props)
                    {
                        var propName = prop.GetPropName(risAvatar);
                        if (string.IsNullOrEmpty(propName))
                            propName = "Prop" + group.Props.IndexOf(prop);
                        if (risAvatar.GetAvatarRoot() != null)
                        {
                            var parentGameObject = risAvatar.GetAvatarRoot()?.gameObject;
                            if (prop.TargetObjects.Count <= 0 || prop.TargetObjects[0]?.GetObject(parentGameObject) == null)
                                errors.Add($"{groupName}のプロップ" + $"[{propName}]にオブジェクトが登録されていません。");
                        }
                    }
                }
            }


            return errors.ToArray();
        }

        protected override void BuildFXLayer(ref Avatar risAvatar, string autoGeneratedFolder)
        {
            Dictionary<GameObject, List<MaterialOverrideData>> materialOverrideDatas = new Dictionary<GameObject, List<MaterialOverrideData>>();
            List<IndexPair>[] exclusiveGroupIndexes = Enumerable.Range(0, 8).Select(n => new List<IndexPair>()).ToArray();

            var avatar = risAvatar.GetAvatarRoot();
            if (avatar == null)
                return;
            avatar.customizeAnimationLayers = true;
            var fxLayer = avatar.GetFXLayer(autoGeneratedFolder);
            var animationsFolder = autoGeneratedFolder + "Animations/";
            UnityUtils.ReCreateFolder(animationsFolder);
            foreach (var name in fxLayer.layers.Where(n => n.name.StartsWith("RIS")).Select(n => n.name))
                fxLayer.TryRemoveLayer(name);
            var parameters = fxLayer.parameters;
            foreach (var name in fxLayer.parameters.Where(n => n.name.StartsWith("RIS")).Select(n => n.name))
                fxLayer.TryRemoveParameter(name);
            fxLayer.parameters = parameters;
            var fallbackClip = new AnimationClip();
            var fallbackParamName = $"{RIS.Prefix}-Initialize";
            foreach (var groupIndex in Enumerable.Range(0, risAvatar.Groups.Count))
            {
                var group = risAvatar.Groups[groupIndex];
                var groupName = group.Name;
                if (string.IsNullOrWhiteSpace(groupName))
                    groupName = "Group" + groupIndex.ToString();
                if (groupIndex == 7 && group.Props.Any(v1 => v1.PropSets.Any(v2 => v2 != null)))
                {
                    var layerName = $"{RIS.Prefix}-PROPSET";

                    var paramName = $"{RIS.Prefix}-SET";
                    CheckParam(avatar, fxLayer, paramName, 0);

                    var layer = fxLayer.FindAnimatorControllerLayer(layerName);
                    if (layer == null)
                        layer = fxLayer.AddAnimatorControllerLayer(layerName);
                    var stateMachine = layer.stateMachine;
                    stateMachine.Clear();

                    var stateName = "DEFAULT";
                    var defaultState = stateMachine.AddState(stateName, new Vector3(300, 150, 0));
                    defaultState.writeDefaultValues = risAvatar.UseWriteDefaults;
                    var defaultTransition = stateMachine.MakeAnyStateTransition(defaultState);
                    stateMachine.defaultState = defaultState;
                    defaultTransition.AddCondition(AnimatorConditionMode.Equals, paramName, 0, false, true);

                    var risParams = avatar.expressionParameters.parameters.Where(n => n.name.StartsWith($"{RIS.Prefix}-G")).ToArray();

                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    {
                        var prop = group.Props[propIndex];

                        var propSets = prop.PropSets;
                        if (propSets.Any(v => v != null))
                        {
                            //TryAddParam(ref risAvatar, $"{RIS.Prefix}-SET", prop.IsDefaultEnabled ? 1f : 0f, true);

                            stateName = $"PROPSET-{propIndex}";
                            var state = stateMachine.AddState(stateName, new Vector3(300, 200 + (propIndex * 50), 0));
                            state.writeDefaultValues = risAvatar.UseWriteDefaults;
                            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                            var transition = stateMachine.MakeAnyStateTransition(state);
                            transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, propIndex + 1, false, true);
                            foreach (var risParamIndex in Enumerable.Range(0, risParams.Length))
                            {
                                var param = risParams[risParamIndex];
                                var enabled = propSets.Any(v => param.name == $"{RIS.Prefix}-G{v.GroupIndex}P{v.PropIndex}") ? 1 : 0;
                                var driverParameters = driver.parameters;
                                driverParameters.Add(new VRC_AvatarParameterDriver.Parameter()
                                {
                                    name = param.name,
                                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                                    value = enabled
                                });
                                driver.parameters = driverParameters;
                            }


                            EditorUtility.SetDirty(driver);
                            EditorUtility.SetDirty(layer.stateMachine);
                        }
                        /*if (exclusiveMode != RIS.ExclusiveModeType.None)
                        {
                            var driver = onState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                            foreach (var subPropIndex in Enumerable.Range(0, group.Props.Count))
                            {
                            }
                        }*/
                    }
                }
                else
                {
                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    {
                        var prop = group.Props[propIndex];
                        var targetObject = prop?.GetFirstTargetObject(risAvatar);
                        var relativePath = "";
                        if (targetObject != null)
                        {
                            if (risAvatar.ApplyEnableDefault)
                                targetObject.SetActive(prop.IsDefaultEnabled);
                            relativePath = targetObject.GetRelativePath(avatar.gameObject);
                            var curve = new AnimationCurve();
                            var frameValue = prop.IsDefaultEnabled ? 1 : 0;
                            curve.AddKey(0f, frameValue);
                            curve.AddKey(1f / fallbackClip.frameRate, frameValue);
                            fallbackClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", curve);
                        }
                        var propName = prop.GetPropName(risAvatar);
                        if (string.IsNullOrWhiteSpace(propName))
                            propName = "Prop" + propIndex.ToString();
                        var layerName = $"{RIS.Prefix}-MAIN-G{groupIndex}P{propIndex}";

                        var paramName = $"{RIS.Prefix}-G{groupIndex}P{propIndex}";
                        CheckParam(avatar, fxLayer, paramName, prop.IsDefaultEnabled);
                        if (prop.IsLocalOnly)
                            CheckParam(avatar, fxLayer, "IsLocal", false);

                        var layer = fxLayer.FindAnimatorControllerLayer(layerName);
                        if (layer == null)
                            layer = fxLayer.AddAnimatorControllerLayer(layerName);
                        var stateMachine = layer.stateMachine;
                        stateMachine.Clear();
                        var exclusiveMode = risAvatar.GetExclusiveMode((RIS.ExclusiveGroupType)groupIndex);
                        var onState = stateMachine.AddState("PropON", new Vector3(300, 100, 0));
                        onState.writeDefaultValues = risAvatar.UseWriteDefaults;
                        var offState = stateMachine.AddState("PropOFF", new Vector3(300, 200, 0));
                        offState.writeDefaultValues = risAvatar.UseWriteDefaults;

                        var transition = stateMachine.MakeAnyStateTransition(onState);
                        transition.CreateSingleCondition(AnimatorConditionMode.If, paramName, 1f, prop.IsLocalOnly && !prop.IsDefaultEnabled, true);
                        transition = stateMachine.MakeAnyStateTransition(offState);
                        transition.CreateSingleCondition(AnimatorConditionMode.IfNot, paramName, 1f, prop.IsLocalOnly && prop.IsDefaultEnabled, true);

                        var clipName = "G" + groupIndex.ToString() + "P" + propIndex.ToString();
                        relativePath = targetObject.GetRelativePath(avatar.gameObject);

                        stateMachine.defaultState = prop.IsDefaultEnabled ? onState : offState;

                        onState.motion = UnityUtils.CreateAnimationClip(relativePath, 1, typeof(GameObject), "m_IsActive", $"{clipName}ON", animationsFolder);
                        offState.motion = UnityUtils.CreateAnimationClip(relativePath, 0, typeof(GameObject), "m_IsActive", $"{clipName}OFF", animationsFolder);
                        if (stateMachine.states.Length <= 0)
                        {
                            fxLayer.TryRemoveLayer(layerName);
                            EditorUtility.SetDirty(fxLayer);
                        }
                        else
                        {
                            layer.stateMachine = stateMachine;
                            EditorUtility.SetDirty(layer.stateMachine);
                        }
                    }
                }
            }


            if (avatar.baseAnimationLayers == null)
                avatar.baseAnimationLayers = new VRCAvatarDescriptor.CustomAnimLayer[5];
            if (avatar.baseAnimationLayers.Count() != 5)
                Array.Resize(ref avatar.baseAnimationLayers, 5);
            avatar.baseAnimationLayers[4].animatorController = fxLayer;
            EditorUtility.SetDirty(avatar.baseAnimationLayers[4].animatorController);
            EditorUtility.SetDirty(avatar);
            AssetDatabase.SaveAssets();
        }

        protected override void BuildExpressionParameters(ref Avatar risAvatar, string autoGeneratedFolder)
        {
            var avatar = risAvatar.GetAvatarRoot();

            if (avatar == null)
                return;

            var expParams = avatar.GetExpressionParameters(autoGeneratedFolder);
            if (risAvatar.OptimizeParameters)
                expParams.OptimizeParameter();

            var parameters = expParams.parameters;
            foreach (var name in parameters.Where(n => n.name.StartsWith("RIS")).Select(n => n.name))
                expParams.TryRemoveParameter(name);
            expParams.parameters = parameters;

            foreach (var groupIndex in Enumerable.Range(0, risAvatar.Groups.Count))
            {
                var group = risAvatar.Groups[groupIndex];
                if (groupIndex == 7)
                {
                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    {
                        var prop = group.Props[propIndex];
                        if (prop.PropSets.Any(v => v != null))
                            TryAddParam(ref risAvatar, $"{RIS.Prefix}-SET", prop.IsDefaultEnabled ? 1f : 0f, true, VRCExpressionParameters.ValueType.Int);
                    }
                }
                else
                {
                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    {
                        var prop = group.Props[propIndex];
                        var v2Mode = false;
                        if (risAvatar.GetExclusiveMode((RIS.ExclusiveGroupType)groupIndex) == RIS.ExclusiveModeType.LegacyExclusive)
                            v2Mode = true;
                        TryAddParam(ref risAvatar, $"{RIS.Prefix}-G{groupIndex}P{propIndex}", prop.IsDefaultEnabled && !v2Mode ? 1f : 0f, prop.UseSaveParameter);
                    }
                }
            }
            TryAddParam(ref risAvatar, $"{RIS.Prefix}-Initialize", 1f, true);
            avatar.expressionParameters = expParams;
            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(avatar.expressionParameters);
            AssetDatabase.SaveAssets();

        }

        protected override void BuildExpressionsMenu(ref Avatar risAvatar, string autoGeneratedFolder)
        {
            var avatar = risAvatar.AvatarRoot?.GetObject();

            if (avatar == null)
                return;

            VRCExpressionsMenu menu = null;
            avatar.customExpressions = true;
            var rootMenu = avatar.expressionsMenu;
            if (rootMenu == null)
                rootMenu = avatar.expressionsMenu = UnityUtils.TryGetAsset(autoGeneratedFolder + "AutoGeneratedMenu.asset", typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;

            //v3.1までのtypoメニューを消す処理
            var controls = rootMenu.controls;
            var risControl = controls.FirstOrDefault(n => n.name == "Radial Inventory");
            if (risControl == null)
                controls.Add(risControl = new VRCExpressionsMenu.Control() { name = "Radial Inventory" });
            rootMenu.controls = controls;

            risControl.icon = TexAssets.MenuIcon;
            risControl.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
            risControl.subMenu = menu = UnityUtils.TryGetAsset(autoGeneratedFolder + $"RadInvMainMenu.asset", typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;

            var subMenuFolder = autoGeneratedFolder + "SubMenus/";
            UnityUtils.ReCreateFolder(subMenuFolder);
            var menuControls = menu.controls;
            menuControls.Clear();
            foreach (var groupIndex in Enumerable.Range(0, risAvatar.Groups.Count))
            {
                var group = risAvatar.Groups[groupIndex];
                if (groupIndex == 7 && group.Props.Any(v1 => v1.PropSets.Any(v2 => v2 != null)))
                {
                    var groupName = propSetTitle;

                    VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control();
                    control.name = groupName;
                    control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                    control.icon = group.Icon?.GetObject();
                    if (control.icon == null)
                        control.icon = TexAssets.GroupIcon;
                    VRCExpressionsMenu subMenu = control.subMenu = UnityUtils.TryGetAsset(subMenuFolder + $"Group{groupIndex}Menu.asset", typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;
                    var subMenuControls = subMenu.controls;
                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    {
                        var prop = group.Props[propIndex];
                        var propName = prop.GetPropName(risAvatar);
                        if (string.IsNullOrWhiteSpace(propName))
                            propName = "セット" + propIndex.ToString();
                        var propControl = new VRCExpressionsMenu.Control();
                        propControl.name = propName;
                        propControl.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                        propControl.value = propIndex + 1;
                        propControl.parameter = new VRCExpressionsMenu.Control.Parameter() { name = $"{RIS.Prefix}-SET" };
                        propControl.icon = prop.Icon?.GetObject();
                        if (propControl.icon == null)
                            propControl.icon = TexAssets.BoxIcon;
                        subMenuControls.Add(propControl);
                    }
                    subMenu.controls = subMenuControls;
                    control.subMenu = subMenu;
                    menuControls.Add(control);
                    EditorUtility.SetDirty(control.subMenu);
                }
                else if(group.Props.Count > 0)
                {

                    var groupName = targetName[groupIndex];

                    VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control();
                    control.name = groupName;
                    control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                    control.icon = group.Icon?.GetObject();
                    if (control.icon == null)
                        control.icon = TexAssets.GroupIcon;
                    VRCExpressionsMenu subMenu = control.subMenu = UnityUtils.TryGetAsset(subMenuFolder + $"Group{groupIndex}Menu.asset", typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;
                    var subMenuControls = subMenu.controls;
                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                    {
                        var prop = group.Props[propIndex];
                        var propName = prop.GetPropName(risAvatar);
                        if (string.IsNullOrWhiteSpace(propName))
                            propName = "Prop" + propIndex.ToString();
                        var propControl = new VRCExpressionsMenu.Control();
                        propControl.name = propName;
                        propControl.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                        propControl.value = 1f;
                        propControl.parameter = new VRCExpressionsMenu.Control.Parameter() { name = $"{RIS.Prefix}-G{groupIndex}P{propIndex}" };
                        propControl.icon = prop.Icon?.GetObject();
                        if (propControl.icon == null)
                            propControl.icon = TexAssets.BoxIcon;
                        subMenuControls.Add(propControl);
                    }
                    subMenu.controls = subMenuControls;
                    control.subMenu = subMenu;
                    menuControls.Add(control);
                    EditorUtility.SetDirty(control.subMenu);
                }
            }
            menu.controls = menuControls;
            avatar.expressionsMenu = rootMenu;
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(avatar.expressionsMenu);
            EditorUtility.SetDirty(avatar);
            AssetDatabase.SaveAssets();
        }
        public override void CalculateMemoryCost(ref Avatar risAvatar, out int costSum, out int costNow, out int costAdd)
        {
            var paramsTemp = new List<VRCExpressionParameters.Parameter>();
            foreach (var groupIndex in Enumerable.Range(0, risAvatar.Groups.Count))
            {
                var group = risAvatar.Groups[groupIndex];
                if (groupIndex == 7)
                {
                    if (group.Props.Any(v1 => v1.PropSets.Any(v2 => v2 != null)))
                        paramsTemp.Add(new VRCExpressionParameters.Parameter() { name = $"{RIS.Prefix}-SET", valueType = VRCExpressionParameters.ValueType.Int });
                }
                else
                {
                    foreach (var propIndex in Enumerable.Range(0, group.Props.Count))
                        paramsTemp.Add(new VRCExpressionParameters.Parameter() { name = $"{RIS.Prefix}-G{groupIndex}P{propIndex}", valueType = VRCExpressionParameters.ValueType.Bool });
                }
            }
            paramsTemp.Add(new VRCExpressionParameters.Parameter() { name = $"{RIS.Prefix}-Initialize", valueType = VRCExpressionParameters.ValueType.Bool });
            var expressionParameter = risAvatar.GetAvatarRoot()?.GetExpressionParameters(RIS.AutoGeneratedFolderPath + risAvatar.UniqueID + "/", false);
            expressionParameter.CalculateMemoryCount(out costNow, out costSum, paramsTemp, risAvatar.OptimizeParameters, RIS.Prefix);
            costAdd = paramsTemp.Sum(n => VRCExpressionParameters.TypeCost(n.valueType));
        }
    }
}