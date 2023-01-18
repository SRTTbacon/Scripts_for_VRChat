#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace SRTTbacon.VRCScript
{
    public class VRC_FaceFix : EditorWindow
    {
        GameObject Avatar_Object = null;
        SkinnedMeshRenderer Face_Object = null;
        string Face_Structure = "";
        string Save_Dir = "";
        bool IsBackupMode = true;
        bool IsExecuted = false;
        [MenuItem("SRTTbacon/VRC用スクリプト/表情ぐにゃぁ修正")]
        public static void Init()
        {
            VRC_FaceFix window = (VRC_FaceFix)GetWindow(typeof(VRC_FaceFix));
            window.titleContent = new GUIContent("VRC用表情アニメーション修正スクリプト");
            window.Show();
        }
        public void OnGUI()
        {
            GUIStyle centerbold = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            centerbold.normal.textColor = EditorStyles.label.normal.textColor;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ーVRChat用表情ぐにゃぁ修正スクリプトー", centerbold);
            EditorGUILayout.LabelField("---Made by SRTTbacon---");
            EditorGUILayout.Space();
            string Message_01 = "※表情関連の不具合は原因が様々なので、このスクリプトによって確実に修正される保証はありません。";
            EditorGUILayout.LabelField(Message_01);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("アバター名:" + (Avatar_Object == null ? "なし" : Avatar_Object.name), centerbold, GUILayout.Width(position.size.x));
            //フォルダをドラッグすることで、そのフォルダを保存先として設定できるように
            Object[] DragAvatarObjects = DragAndDropAreaUtility.GetObjects("ここにアバターのオブジェクトをドラッグ" + (Avatar_Object == null ? "" : "(選択済み)"));
            if (DragAvatarObjects != null && DragAvatarObjects.Length > 0)
            {
                if (DragAvatarObjects[0] is GameObject gb)
                {
                    Avatar_Object = VRC_Utility.Get_Avatar_Object(gb.transform);
                    if (Avatar_Object.TryGetComponent(out VRCAvatarDescriptor VRCDes))
                    {
                        Face_Object = VRCDes.VisemeSkinnedMesh;
                        Face_Structure = VRC_Utility.Get_Structure(Face_Object.transform, Avatar_Object.transform);
                    }
                }
            }
            if (Save_Dir == "")
            {
                //顔のメッシュが存在するフォルダの1つ前のフォルダ+Generated(SRTTbacon)/Backupsを保存先にする
                if (Face_Object != null && Face_Object.sharedMaterials.Length > 0)
                {
                    string Material_Dir = AssetDatabase.GetAssetPath(Face_Object.sharedMaterials[0]);
                    if (Material_Dir.Contains("/"))
                        Material_Dir = Material_Dir.Substring(0, Material_Dir.LastIndexOf("/"));
                    if (Material_Dir != "")
                        Save_Dir = Material_Dir.Substring(0, Material_Dir.LastIndexOf("/") + 1) + "Generated(SRTTbacon)/Backups";
                }
            }
            if (IsBackupMode)
            {
                Save_Dir = EditorGUILayout.TextField("バックアップの保存場所:", Save_Dir);
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
                EditorUtility.DisplayProgressBar("VRChat用スクリプト -表情修正-", "準備しています...", 0f);
                try
                {
                    Execute_Face_Fix();   //実行
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
        private bool IsCanExecute(out string Error_Message)
        {
            Error_Message = "";
            if (IsExecuted)
                Error_Message = "処理が完了しました。もう一度実行する場合はウィンドウを閉じてください。";
            else if (Avatar_Object == null)
                Error_Message = "アバターが選択されていません。";
            else if (Face_Object == null)
                Error_Message = "VRCAvatarDescriptorに表情が設定されていません。";
            else if (Face_Object.sharedMesh == null)
                Error_Message = "アバターに顔のメッシュが設定されていません。";
            else if (Face_Object.sharedMesh.blendShapeCount == 0)
                Error_Message = "アバターにBlendShapeが入っていません。";
            else
            {
                bool IsExistFX = false;
                bool IsExistFaceAnim = false;
                VRCAvatarDescriptor VRCDes = Avatar_Object.GetComponent<VRCAvatarDescriptor>();
                //FXアニメーター内に表情モーションが含まれているかを検知
                foreach (VRCAvatarDescriptor.CustomAnimLayer Anim in VRCDes.baseAnimationLayers)
                {
                    if (Anim.type == VRCAvatarDescriptor.AnimLayerType.FX)
                    {
                        AnimatorController animator = Anim.animatorController as AnimatorController;
                        if (animator != null)
                        {
                            IsExistFX = true;
                            foreach (AnimatorControllerLayer Layer in animator.layers)
                            {
                                if (Layer.name == "Left Hand" || Layer.name == "Right Hand")
                                {
                                    foreach (ChildAnimatorState State in Layer.stateMachine.states)
                                    {
                                        if (State.state.motion != null)
                                        {
                                            IsExistFaceAnim = true;
                                            break;
                                        }
                                    }
                                    if (IsExistFaceAnim)
                                        break;
                                }
                            }
                        }
                        break;
                    }
                }
                if (!IsExistFX)
                    Error_Message = "アバターにFXアニメーターが設定されていません。";
                else if (!IsExistFaceAnim)
                    Error_Message = "FXアニメーターに表情モーションが追加されていません。";
            }
            return Error_Message == "";
        }
        private void Execute_Face_Fix()
        {
            if (IsBackupMode)
            {
                if (Save_Dir.EndsWith("/"))
                    Save_Dir = Save_Dir.Substring(0, Save_Dir.Length - 1);
                if (!Directory.Exists(Save_Dir))
                    Directory.CreateDirectory(Save_Dir);
            }
            VRCAvatarDescriptor VRCDes = Avatar_Object.GetComponent<VRCAvatarDescriptor>();
            List<AnimationClip> Face_Animations = new List<AnimationClip>();
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -表情修正-", "表情アニメーションを取得しています...", 0.2f);
            foreach (VRCAvatarDescriptor.CustomAnimLayer Anim in VRCDes.baseAnimationLayers)
            {
                if (Anim.type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    AnimatorController animator = Anim.animatorController as AnimatorController;
                    foreach (AnimatorControllerLayer Layer in animator.layers)
                        if (Layer.name == "Left Hand" || Layer.name == "Right Hand")
                            foreach (ChildAnimatorState State in Layer.stateMachine.states)
                                if (State.state.motion != null && State.state.motion is AnimationClip animclip && !Face_Animations.Contains(animclip))
                                    Face_Animations.Add(animclip);
                    break;
                }
            }
            List<string> BlendShapes = new List<string>();
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -表情修正-", "使用している表情を検知しています...", 0.5f);
            foreach (AnimationClip AnimClip in Face_Animations)
                Add_Using_BlandShapes(AnimClip, BlendShapes);
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -表情修正-", "アニメーションを修正しています...", 0.8f);
            if (IsBackupMode)
            {
                AssetDatabase.StartAssetEditing();
                foreach (AnimationClip AnimClip in Face_Animations)
                    Fix_Animation_BlendShape(AnimClip, BlendShapes, true);
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            foreach (AnimationClip AnimClip in Face_Animations)
                Fix_Animation_BlendShape(AnimClip, BlendShapes, false);
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -表情修正-", "処理が完了しました。", 1.0f);
            Face_Animations.Clear();
            BlendShapes.Clear();
            IsExecuted = true;
        }
        private void Fix_Animation_BlendShape(AnimationClip motion, List<string> BlendShapes, bool IsOnlyBackup)
        {
            for (int Index = 0; Index < Face_Object.sharedMesh.blendShapeCount; Index++)
            {
                string New_Shape_Name = Face_Object.sharedMesh.GetBlendShapeName(Index);
                if (!BlendShapes.Contains(New_Shape_Name))
                    continue;
                bool IsExistShape = false;
                bool IsNoIncludeShape = true;
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(motion))
                {
                    string BlendShapeName = binding.propertyName.Substring(binding.propertyName.LastIndexOf(".") + 1);
                    if (binding.path == Face_Object.name && BlendShapeName == New_Shape_Name)
                    {
                        IsExistShape = true;
                        break;
                    }
                    if (binding.path == Face_Object.name)
                        IsNoIncludeShape = false;
                }
                if (IsExistShape || IsNoIncludeShape)
                    continue;
                if (IsOnlyBackup)
                {
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(motion), Save_Dir + "/" + motion.name + ".asset");
                    break;
                }
                else
                    motion.SetCurve(Face_Structure, typeof(SkinnedMeshRenderer), "blendShape." + New_Shape_Name, AnimationCurve.Linear(0f, 0f, motion.length, 0f));
            }
        }
        private void Add_Using_BlandShapes(AnimationClip motion, List<string> BlendShapes)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(motion))
            {
                string BlendShapeName = binding.propertyName.Substring(binding.propertyName.LastIndexOf(".") + 1);
                if (binding.path == Face_Object.name && binding.propertyName != "m_IsActive" && !BlendShapes.Contains(BlendShapeName))
                    BlendShapes.Add(BlendShapeName);
            }
        }
    }
}
#endif