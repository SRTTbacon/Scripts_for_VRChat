#if UNITY_EDITOR
using SRTTbacon.VRCScript;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Assets.SRTTbacon.Editor
{
    public class VRC_FixTexture : EditorWindow
    {
        public List<string> Texture_List = new List<string>();
        private GUIContent[] Setting_ComboBox;
        private Vector2 _scrollPosition = Vector2.zero;
        private static string Execute_File = "";
        private static string Execute_Dir = "";
        private int Setting_Index = 0;
        private int Crunch_Value = 50;
        private bool IsLoaded = false;
        private bool IsCrunchMode = true;
        private bool IsExecuted = false;
        [MenuItem("SRTTbacon/テクスチャ解像度向上")]
        public static void Init()
        {
            VRC_FixTexture window = (VRC_FixTexture)GetWindow(typeof(VRC_FixTexture));
            window.titleContent = new GUIContent("VRC用テクスチャきれいきれいスクリプト");
            window.Show();
            string[] Execute_Files = Directory.GetFiles(Application.dataPath, "Improve_Resolution_for_VRChat.exe", SearchOption.AllDirectories);
            if (Execute_Files.Length > 0)
            {
                Execute_File = Execute_Files[0];
                Execute_Dir = Path.GetDirectoryName(Execute_File);
            }
        }
        public void OnGUI()
        {
            if (!IsLoaded)
            {
                Setting_ComboBox = new[]
                {
                    new GUIContent("2倍"),
                    new GUIContent("3倍"),
                    new GUIContent("4倍")
                };
                IsLoaded = true;
            }
            bool IsExistNull = false;
            foreach (string gb in Texture_List)
            {
                if (gb == "")
                {
                    IsExistNull = true;
                    break;
                }
            }
            if (!IsExistNull)
                Texture_List.Add("");
            GUIStyle centerbold = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            centerbold.normal.textColor = EditorStyles.label.normal.textColor;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false, GUI.skin.button, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ーVRChat用テクスチャ解像度向上スクリプトー", centerbold, GUILayout.Width(position.size.x));
            EditorGUILayout.LabelField("---Made by SRTTbacon---");
            EditorGUILayout.Space();
            string Message_01 = "※元のフォルダ内に～_x" + (Setting_Index + 2) + ".png(.jpg)が生成されます。";
            EditorGUILayout.LabelField(Message_01);
            EditorGUILayout.Space();
            VRC_Utility.GUI_List(this, nameof(Texture_List));
            EditorGUILayout.Space();
            Object[] DragObjects = DragAndDropAreaUtility.GetObjects("ここにテクスチャファイルをドラッグ");
            if (DragObjects != null && DragObjects.Length > 0)
            {
                if (!(DragObjects[0] as GameObject))
                {
                    string Temp = AssetDatabase.GetAssetPath(DragObjects[0]);
                    if (File.Exists(Temp) && !Texture_List.Contains(Temp))
                    {
                        if (Texture_List[Texture_List.Count - 1] == "")
                            Texture_List[Texture_List.Count - 1] = Temp;
                        else
                            Texture_List.Add(Temp);
                    }
                }
            }
            EditorGUILayout.Space(12f);
            Setting_Index = EditorGUILayout.Popup(new GUIContent("解像度: 元の解像度の"), Setting_Index, Setting_ComboBox);
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("画像生成後、クランチ圧縮を実行する");
                IsCrunchMode = EditorGUILayout.Toggle(IsCrunchMode);
            }
            if (IsCrunchMode)
                Crunch_Value = EditorGUILayout.IntSlider("圧縮品質(デフォルト50)", Crunch_Value, 0, 100);
            EditorGUILayout.Space(12f);
            if (!IsCanExecute(out string Error_Message))
                EditorGUI.BeginDisabledGroup(true);
            if (GUILayout.Button("実行"))
            {
                EditorUtility.DisplayProgressBar("VRChat用スクリプト -解像度向上-", "準備しています...", 0f);
                AssetDatabase.StartAssetEditing();
                try
                {
                    Execute_FixTexture();   //実行
                }
                catch (System.Exception e)
                {
                    if (e != null)
                        Debug.LogError(e.Message);
                    Error_Message = "実行中にエラーが発生しました。詳しくはLogをご確認ください。";
                }
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                foreach (string FileNow in Texture_List)
                {
                    if (FileNow == "")
                        continue;
                    string To_File = Path.GetDirectoryName(FileNow) + "/" + Path.GetFileNameWithoutExtension(FileNow) + "_x" + (Setting_Index + 2) + Path.GetExtension(FileNow);
                    TextureImporter To_Tex = AssetImporter.GetAtPath(To_File) as TextureImporter;
                    TextureImporter From_Tex = AssetImporter.GetAtPath(FileNow) as TextureImporter;
                    if (From_Tex != null && To_Tex != null)
                    {
                        object[] args = new object[2] { 0, 0 };
#pragma warning disable UNT0018 // System.Reflection features in performance critical messages
                        MethodInfo method = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
#pragma warning restore UNT0018 // System.Reflection features in performance critical messages
                        method.Invoke(To_Tex, args);
                        int Width = (int)args[0];
                        int Height = (int)args[1];
                        int Max_Size = Width >= Height ? Width : Height;
                        int Size = 0;
                        int[] Sizes = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
                        foreach (int Now in Sizes)
                        {
                            if (Max_Size <= Now)
                            {
                                Size = Max_Size;
                                break;
                            }
                        }
                        if (Size == 0)
                            Size = Sizes[Sizes.Length - 1];
                        To_Tex.CopyFrom(From_Tex);
                        if (IsCrunchMode)
                        {
                            To_Tex.maxTextureSize = Size;
                            To_Tex.crunchedCompression = true;
                        }
                        To_Tex.textureCompression = TextureImporterCompression.Compressed;
                        To_Tex.compressionQuality = Crunch_Value;
                        AssetDatabase.ImportAsset(To_Tex.assetPath);
                    }
                }
                EditorUtility.ClearProgressBar();
            }
            if (Error_Message != "")
            {
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
                GUIStyle Pos = new GUIStyle
                {
                    alignment = TextAnchor.LowerCenter,
                    wordWrap = true
                };
                Pos.normal.textColor = Color.white;
                EditorGUILayout.LabelField(Error_Message, Pos);
            }
            EditorGUILayout.EndScrollView();
        }
        private bool IsCanExecute(out string Error_Message)
        {
            Error_Message = "";
            if (IsExecuted)
                Error_Message = "処理が完了しました。もう一度実行する場合はウィンドウを閉じてください。";
            else if (!File.Exists(Execute_File))
                Error_Message = "Improve_Resolution_for_VRChat.exeが存在しません。";
            else if (!File.Exists(Execute_Dir + "\\Improve_Resolution_for_VRChat.dll"))
                Error_Message = "Improve_Resolution_for_VRChat.dllが存在しません。";
            else if (!File.Exists(Execute_Dir + "\\Improve_Resolution_for_VRChat.runtimeconfig.json"))
                Error_Message = "Improve_Resolution_for_VRChat.runtimeconfig.jsonが存在しません。";
            else
            {
                bool IsExist = false;
                foreach (string Texture_File in Texture_List)
                {
                    if (Texture_File == "")
                        continue;
                    if (!Texture_File.StartsWith("Assets/"))
                    {
                        Error_Message = "パスがAssets以外から始まっています。";
                        break;
                    }
                    if (!File.Exists(Texture_File))
                    {
                        Error_Message = "'" + Texture_File + "'が見つかりません。";
                        break;
                    }
                    string Ex = Path.GetExtension(Texture_File);
                    if (Ex != ".png" && Ex != ".jpg")
                    {
                        Error_Message = "拡張子が.pngまたは.jpg以外のファイルが選択されています。";
                        break;
                    }
                    string Name_Only = Path.GetFileNameWithoutExtension(Texture_File);
                    string Dir_and_Name = Path.GetDirectoryName(Texture_File) + "/" + Name_Only;
                    if (File.Exists(Dir_and_Name + "_x" + (Setting_Index + 2) + Ex))
                    {
                        Error_Message = "'" + Name_Only + "_x" + (Setting_Index + 2) + Ex + "'が既に存在します。";
                        break;
                    }
                    IsExist = true;
                }
                if (!IsExist && Error_Message == "")
                    Error_Message = "テクスチャが選択されていません。";

            }
            return Error_Message == "";
        }
        private void Execute_FixTexture()
        {
            IsExecuted = false;
            string From_Files = "";
            foreach (string Texture_File in Texture_List)
            {
                if (Texture_File == "")
                    continue;
                if (From_Files == "")
                    From_Files = Texture_File;
                else
                    From_Files += "|" + Texture_File;
            }
            EditorUtility.DisplayProgressBar("VRChat用スクリプト -解像度向上-", "高解像度の画像を生成しています...", 0.2f);
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Execute_File,
                Arguments = Setting_Index + 2 + " \"" + From_Files + "\"",
                WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                UseShellExecute = true
            };
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(processStartInfo);
            p.WaitForExit();
            IsExecuted = true;
        }
    }
}
#endif