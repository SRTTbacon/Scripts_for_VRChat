#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace SRTTbacon.VRCScript
{
    //髪色RGBAのソースコード。改変・二次配布などご自由にどうぞ。
    public class VRC_HairColor : EditorWindow
    {
        IReadOnlyList<GameObject> Active_Hairs = null;
        GameObject Parent_Object = null;
        SkinnedMeshRenderer Face_Object = null;
        public List<GameObject> Hair_List = new List<GameObject>();
        string Save_Dir = "";
        bool IsBackupMode = true;
        bool IsChangedList = false;
        bool IsExecuted = false;
        [MenuItem("SRTTbacon/VRC用スクリプト/髪色RGBA")]
        public static void Init()
        {
            VRC_HairColor window = (VRC_HairColor)GetWindow(typeof(VRC_HairColor));
            window.titleContent = new GUIContent("VRC用髪色RGBAスクリプト");
            window.Show();
        }
        public void OnGUI()
        {
            bool IsExistNull = false;
            foreach (GameObject gb in Hair_List)
            {
                if (gb == null)
                {
                    IsExistNull = true;
                    break;
                }
            }
            if (!IsExistNull)
                Hair_List.Add(null);
            GUIStyle centerbold = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            centerbold.normal.textColor = EditorStyles.label.normal.textColor;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ーVRChat用髪色RGBAスクリプトー", centerbold, GUILayout.Width(position.size.x));
            EditorGUILayout.LabelField("---Made by SRTTbacon---");
            EditorGUILayout.Space();
            VRC_Utility.GUI_List(this, nameof(Hair_List));
            EditorGUILayout.Space(12f);
            int Material_Count = 0;
            //選択済みのオブジェクトを読み取り専用としてListに保存(Where関数で値がnullではないオブジェクトのみ抽出される)
            Active_Hairs = Hair_List.Where(h => h != null).ToList();
            if (!IsChangedList && Active_Hairs.Count > 0)
            {
                IsChangedList = true;
                GameObject Root = VRC_Utility.Get_Avatar_Object(Active_Hairs[0].transform);
                if (Root != null)
                {
                    Parent_Object = Root;
                    //親オブジェクトから顔のメッシュを取得(VRCAvatarDescriptor内に存在すれば)
                    if (Parent_Object.TryGetComponent(out VRCAvatarDescriptor VRCDes))
                        Face_Object = VRCDes.VisemeSkinnedMesh;
                }
            }
            else if (Material_Count == 0)
                IsChangedList = false;
            if (Save_Dir == "")
            {
                //顔のメッシュが存在するフォルダの1つ前のフォルダ+Generated(SRTTbacon)を保存先にする
                if (Face_Object != null && Face_Object.sharedMaterials.Length > 0)
                {
                    string Material_Dir = AssetDatabase.GetAssetPath(Face_Object.sharedMaterials[0]);
                    if (Material_Dir.Contains("/"))
                        Material_Dir = Material_Dir.Substring(0, Material_Dir.LastIndexOf("/"));
                    if (Material_Dir != "")
                        Save_Dir = Material_Dir.Substring(0, Material_Dir.LastIndexOf("/") + 1) + "Generated(SRTTbacon)";
                }
            }
            Save_Dir = EditorGUILayout.TextField("生成されるファイルの保存場所:", Save_Dir);
            //フォルダをドラッグすることで、そのフォルダを保存先として設定できるように
            Object[] DragObjects = DragAndDropAreaUtility.GetObjects("ここに保存先のフォルダをドラッグ" + (Save_Dir == "" ? "" : "(選択済み)"));
            if (DragObjects != null && DragObjects.Length > 0)
            {
                if (!(DragObjects[0] as GameObject))
                {
                    string Temp = AssetDatabase.GetAssetPath(DragObjects[0]);
                    Save_Dir = Directory.Exists(Temp) ? Temp : Path.GetDirectoryName(Temp).Replace("\\", "/");
                }
            }
            //バックアップを有効にすると、保存先にFXアニメーターとExpressionMenu(Parameters)をコピーする
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("念のためファイルのバックアップを作成");
                IsBackupMode = EditorGUILayout.Toggle("", IsBackupMode);
            }
            EditorGUILayout.Space();
            //実行できる状態か判定。実行不可であればボタンを押せないように
            if (!IsCanExecute(out string Error_Message))
                EditorGUI.BeginDisabledGroup(true);
            if (GUILayout.Button("実行"))
            {
                EditorUtility.DisplayProgressBar("VRChat用スクリプト -髪色RGBA-", "準備しています...", 0f);
                try
                {
                    Execute_Hair_Color_RGBA();   //実行
                }
                catch (System.Exception e)
                {
                    if (e != null)
                        Debug.LogError(e.Message);
                    Error_Message = "実行中にエラーが発生しました。詳しくはLogをご確認ください。";
                }
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
            //実行不可の理由を記述
            if (Error_Message != "")
            {
                EditorGUI.EndDisabledGroup();
                GUIStyle Pos = new GUIStyle
                {
                    alignment = TextAnchor.LowerCenter
                };
                Pos.normal.textColor = Color.white;
                EditorGUILayout.LabelField(Error_Message, Pos);
            }
        }
        //実行できるかを取得(Error_Messageに何も入らなかった場合実行可能)
        private bool IsCanExecute(out string Error_Message)
        {
            Error_Message = "";
            if (IsExecuted)
                Error_Message = "処理が完了しました。もう一度実行する場合はウィンドウを閉じてください。";
            else if (Active_Hairs == null || Active_Hairs.Count == 0)
                Error_Message = "髪のオブジェクトが設定されていません。";
            else if (Parent_Object == null)
                Error_Message = "アバター用の髪を選択してください。";
            else
            {
                if (Parent_Object.TryGetComponent(out VRCAvatarDescriptor VRCDes))
                {
                    bool IsExistFX = false;
                    bool IsExistHairColor = false;
                    bool IsExistHairParam = false;
                    bool IsEqualFXPos = false;
                    foreach (VRCAvatarDescriptor.CustomAnimLayer Anim in VRCDes.baseAnimationLayers)
                    {
                        if (Anim.type == VRCAvatarDescriptor.AnimLayerType.FX)
                        {
                            AnimatorController animator = Anim.animatorController as AnimatorController;
                            if (animator != null)
                            {
                                IsExistFX = true;
                                string Animator_Pos = AssetDatabase.GetAssetPath(animator);
                                if (Save_Dir + "/" + Path.GetFileName(Animator_Pos) == Animator_Pos)
                                {
                                    IsEqualFXPos = true;
                                    break;
                                }
                                foreach (AnimatorControllerLayer layer in animator.layers)
                                {
                                    if (layer.name == "HairColorR" || layer.name == "HairColorG" || layer.name == "HairColorB" || layer.name == "HairColorA")
                                    {
                                        IsExistHairColor = true;
                                        break;
                                    }
                                }
                                foreach (AnimatorControllerParameter Param in animator.parameters)
                                {
                                    if (Param.name == "HairColorR" || Param.name == "HairColorG" || Param.name == "HairColorB" || Param.name == "HairColorA")
                                    {
                                        IsExistHairParam = true;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                    string ExMenu_Pos = VRCDes.expressionsMenu != null ? AssetDatabase.GetAssetPath(VRCDes.expressionsMenu) : "";
                    string ExParam_Pos = VRCDes.expressionParameters != null ? AssetDatabase.GetAssetPath(VRCDes.expressionParameters) : "";
                    if (!IsExistFX)
                        Error_Message = "アバターにFXアニメーターが設定されていません。";
                    else if (IsEqualFXPos)
                        Error_Message = "保存先のフォルダ内にFXアニメーターが存在します。";
                    else if (IsExistHairColor)
                        Error_Message = "FXアニメーター内にHairColorという名前のレイヤーが既に存在します。";
                    else if (IsExistHairParam)
                        Error_Message = "FXアニメーター内にHairColorという名前のパラメーターが既に存在します。";
                    else if (VRCDes.expressionParameters == null)
                        Error_Message = "アバターにExpressionParameterが設定されていません。";
                    else if (VRCDes.expressionsMenu == null)
                        Error_Message = "アバターにExpressionMenuが設定されていません。";
                    else if (Save_Dir + "/" + Path.GetFileName(ExMenu_Pos) == ExMenu_Pos)
                        Error_Message = "保存先のフォルダ内にExpressionsMenuが存在します。";
                    else if (Save_Dir + "/" + Path.GetFileName(ExParam_Pos) == ExParam_Pos)
                        Error_Message = "保存先のフォルダ内にExpressionsParametersが存在します。";
                    else if (VRCDes.expressionParameters.CalcTotalCost() + 32 > 256)
                        Error_Message = "ExpressionParameter内にスペースが足りません。";
                    else if (VRCDes.expressionParameters.FindParameter("HairColorR") != null)
                        Error_Message = "ExpressionParameter内にHairColorR変数が既に存在します。";
                    else if (VRCDes.expressionParameters.FindParameter("HairColorG") != null)
                        Error_Message = "ExpressionParameter内にHairColorG変数が既に存在します。";
                    else if (VRCDes.expressionParameters.FindParameter("HairColorB") != null)
                        Error_Message = "ExpressionParameter内にHairColorB変数が既に存在します。";
                    else if (VRCDes.expressionParameters.FindParameter("HairColorA") != null)
                        Error_Message = "ExpressionParameter内にHairColorA変数が既に存在します。";
                    else if (Active_Hairs != null && Active_Hairs.Count > 0)
                    {
                        foreach (GameObject Hair_Object in Active_Hairs)
                        {
                            bool IsNotLiltoon = false;
                            if (Hair_Object.TryGetComponent(out SkinnedMeshRenderer MeshReader))
                            {
                                bool IsExistLiltoon = false;
                                foreach (Material mat in MeshReader.sharedMaterials)
                                {
                                    if (mat.shader.name.Contains("liltoon", System.StringComparison.OrdinalIgnoreCase))
                                    {
                                        IsExistLiltoon = true;
                                        break;
                                    }
                                }
                                if (!IsExistLiltoon)
                                {
                                    Error_Message = "髪のマテリアル(シェーダー)はliltoonである必要があります。";
                                    IsNotLiltoon = true;
                                }
                            }
                            else
                                Error_Message = "髪のオブジェクトにSkinnedMeshReadererが設定されていません。";
                            if (IsNotLiltoon)
                                break;
                        }
                    }
                }
                else
                    Error_Message = "親オブジェクトにVRCAvatarDescriptorが設定されていません。";
            }
            if (Error_Message == "" && Save_Dir == "")
                Error_Message = "保存先のフォルダが指定されていません。";
            return Error_Message == "";
        }
        private void Execute_Hair_Color_RGBA()
        {
            if (Save_Dir.EndsWith("/"))
                Save_Dir = Save_Dir.Substring(0, Save_Dir.Length - 1);
            if (!Directory.Exists(Save_Dir))
                Directory.CreateDirectory(Save_Dir);
            //FXアニメーターにレイヤーをセットする
            string[] HairLayer = { "HairColorR", "HairColorG", "HairColorB", "HairColorA" };
            VRCAvatarDescriptor VRCDes = Parent_Object.GetComponent<VRCAvatarDescriptor>();
            if (IsBackupMode)
            {
                AssetDatabase.StartAssetEditing();
                for (int Index = 0; Index < VRCDes.baseAnimationLayers.Length; Index++)
                {
                    if (VRCDes.baseAnimationLayers[Index].type == VRCAvatarDescriptor.AnimLayerType.FX)
                    {
                        string Anim_Name = VRCDes.baseAnimationLayers[Index].animatorController.name;
                        File.Delete(Save_Dir + "/" + Anim_Name + "_Copy.controller");
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(VRCDes.baseAnimationLayers[Index].animatorController), Save_Dir + "/" + Anim_Name + "_Copy.controller");
                        break;
                    }
                }
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(VRCDes.expressionsMenu), Save_Dir + "/" + VRCDes.expressionsMenu.name + "_Copy.asset");
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(VRCDes.expressionParameters), Save_Dir + "/" + VRCDes.expressionParameters.name + "_Copy.asset");
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            foreach (VRCAvatarDescriptor.CustomAnimLayer Anim in VRCDes.baseAnimationLayers)
            {
                if (Anim.type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    AnimatorController animator = Anim.animatorController as AnimatorController;
                    foreach (string LayerName in HairLayer)
                    {
                        EditorUtility.DisplayProgressBar("VRChat用スクリプト -髪色RGBA-", "FXアニメーターに" + LayerName + "を追加しています...", 0.5f);
                        animator.AddParameter(LayerName, AnimatorControllerParameterType.Float);
                        AnimatorControllerLayer layer = new AnimatorControllerLayer
                        {
                            name = LayerName,
                            defaultWeight = 1.0f,
                            blendingMode = AnimatorLayerBlendingMode.Override
                        };
                        AnimationClip clip = new AnimationClip
                        {
                            name = LayerName
                        };
                        layer.stateMachine = new AnimatorStateMachine();
                        string Material_Color = LayerName.Substring(LayerName.Length - 1).ToLower();
                        //マテリアル(オブジェクト)の数だけ色変更のモーションを追加
                        foreach (GameObject Hair_Object in Active_Hairs)
                        {
                            string Hair_Struct = VRC_Utility.Get_Structure(Hair_Object.transform, Parent_Object.transform);
                            clip.SetCurve(Hair_Struct, typeof(SkinnedMeshRenderer), "material._EmissionColor." + Material_Color, AnimationCurve.Linear(0f, 0f, 1f, 1f));
                            if (LayerName == "HairColorA")
                                clip.SetCurve(Hair_Struct, typeof(SkinnedMeshRenderer), "material._UseEmission", AnimationCurve.Linear(0f, 1f, 1f, 1f));
                        }
                        AnimatorState State = layer.stateMachine.AddState(LayerName);
                        AssetDatabase.CreateAsset(clip, Save_Dir + "/" + LayerName + ".anim");
                        State.motion = clip;
                        State.timeParameterActive = true;
                        State.timeParameter = LayerName;
                        State.writeDefaultValues = false;
                        layer.stateMachine.AddAnyStateTransition(State);
                        animator.AddLayer(layer);
                    }
                    break;
                }
            }
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -髪色RGBA-", "ExpressionsMenuを編集しています...", 0.75f);
            //髪色RGBAのExpressionsMenuを作成
            VRCExpressionsMenu Hair_Menu = CreateInstance<VRCExpressionsMenu>();
            string[] Color_Name = { "髪色_赤", "髪色_緑", "髪色_青", "髪色_透明度" };
            for (int Index = 0; Index < Color_Name.Length; Index++)
            {
                VRCExpressionsMenu.Control Control = new VRCExpressionsMenu.Control
                {
                    name = Color_Name[Index],
                    type = VRCExpressionsMenu.Control.ControlType.RadialPuppet
                };
                VRCExpressionsMenu.Control.Parameter Param = new VRCExpressionsMenu.Control.Parameter
                {
                    name = HairLayer[Index]
                };
                Control.subParameters = new VRCExpressionsMenu.Control.Parameter[] { Param };
                Hair_Menu.controls.Add(Control);
            }
            AssetDatabase.CreateAsset(Hair_Menu, Save_Dir + "/髪色_RGBA.asset");
            //作成したExpressionsMenuをルートのExpressionsMenuに追加
            VRCExpressionsMenu.Control Root_Control = new VRCExpressionsMenu.Control
            {
                name = "髪色RGBA",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = Hair_Menu
            };
            VRCDes.expressionsMenu.controls.Add(Root_Control);
            List<VRCExpressionParameters.Parameter> Params = new List<VRCExpressionParameters.Parameter>(VRCDes.expressionParameters.parameters);
            foreach (string LayerName in HairLayer)
            {
                VRCExpressionParameters.Parameter Param = new VRCExpressionParameters.Parameter
                {
                    name = LayerName,
                    saved = true,
                    valueType = VRCExpressionParameters.ValueType.Float
                };
                if (LayerName == "HairColorA")
                    Param.defaultValue = 1f;
                else
                    Param.defaultValue = 0f;
                Params.Add(Param);
            }
            VRCDes.expressionParameters.parameters = Params.ToArray();
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -表情修正-", "処理が完了しました。", 1.0f);
            Hair_List.Clear();
            IsExecuted = true;
        }
    }
}
#endif