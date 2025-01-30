using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using TinyPng;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ARK.EditorTools.TinyPNG
{
    public class TinyPngWindow : OdinEditorWindow
    {

        [MenuItem("ARK_Tools/TinyPNG")]
        private static void OpenWindow()
        {
            GetWindow<TinyPngWindow>().Show();
        }


        private const string SETTINGS_PATH = "Assets/TinyPng/TinyConfig.asset";

        private TinyConfig tinyConfig;


        [ProgressBar(0, 500), LabelText("已使用數量")]
        public int textureLimitCount = 0;

        [LabelText("TinyPNG API Key")]
        public string TinyPNG_APIKey = "";


        private int   curProcessCount   = 0;
        private int   totalProcessCount = 0;
        private float progress          = 0f;

        public List<Texture2D> Texture2Ds = new List<Texture2D>();


        [TableList]
        public List<TextureResizeInfo> TextureResizeInfos = new List<TextureResizeInfo>();


        public void CreateNewSettings()
        {
            tinyConfig = CreateInstance<TinyConfig>();
            AssetDatabase.CreateAsset(tinyConfig, SETTINGS_PATH);
        }

        protected override void OnImGUI()
        {
            SirenixEditorGUI.Title("TinyPNG", "將圖片拖曳後按開始壓縮圖片即可", TextAlignment.Center, true);
            EditorGUILayout.Space();

            if(GUILayout.Button("開始壓縮圖片"))
            {
                StartCompress();
            }
            if(GUILayout.Button("清空資料"))
            {
                CleanAll();
            }

            DrawDragDropArea();

            base.OnImGUI();
        }

        private void CleanAll()
        {
            Texture2Ds.Clear();
            TextureResizeInfos.Clear();
        }

        private void DrawDragDropArea()
        {
            Event evt       = Event.current;
            Rect  drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, "拖曳到此處");

            switch(evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if(!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if(evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach(var dragged_object in DragAndDrop.objectReferences)
                        {
                            if(dragged_object is Texture2D)
                            {
                                Texture2Ds.Add((Texture2D)dragged_object);
                            }
                        }
                    }
                    break;
            }
        }


        public async void StartCompress()
        {
            var pathHeal = Application.dataPath.Replace("Assets", "");

            if(TinyPNG_APIKey == "")
            {
                Debug.LogError("請先輸入API Key");
                return;
            }

            curProcessCount   = 0;
            totalProcessCount = Texture2Ds.Count;

            for(var i = 0; i < Texture2Ds.Count; i++)
            {
                curProcessCount++;
                progress = (float)curProcessCount / totalProcessCount;
                EditorUtility.DisplayProgressBar("壓縮圖片", Texture2Ds[i].name, progress);
                var path = pathHeal + AssetDatabase.GetAssetPath(Texture2Ds[i]);
                await CompressProcess(Texture2Ds[i].name, path);
            }
            EditorUtility.ClearProgressBar();
        }


        public async Task CompressProcess(string textureName, string path)
        {
            var textureResizeInfo = new TextureResizeInfo();

            // create an instance of the TinyPngClient
            var png = new TinyPngClient(TinyPNG_APIKey);

            textureResizeInfo.texture      = textureName;
            textureResizeInfo.OriginKbSize = GetFileSizeKB(path);

            // Create a task to compress an image.
            // this gives you the information about your image as stored by TinyPNG
            // they don't give you the actual bits (as you may want to chain this with a resize
            // // operation without caring for the originally sized image).
            EditorUtility.DisplayProgressBar("壓縮圖片", "上傳伺服器壓縮", progress);
            var compressImageTask = png.Compress(path);

            //
            //
            // // or `CompressFromUrl` if compressing from a remotely hosted image.
            // // var compressFromUrlImageTask = png.CompressFromUrl("image url");
            //
            // // If you want to actually save this compressed image off
            // // it will need to be downloaded 
            EditorUtility.DisplayProgressBar("壓縮圖片", "下載壓縮檔", progress);
            var compressedImage = await compressImageTask.Download();

            // Debug.Log("下載");
            //
            // // you can then get the bytes
            // var bytes = await compressedImage.GetImageByteData();
            //
            // // get a stream instead
            // var stream = await compressedImage.GetImageStreamData();
            //
            // // or just save to disk
            EditorUtility.DisplayProgressBar("壓縮圖片", "覆蓋原檔", progress);
            await compressedImage.SaveImageToDisk(path);

            // Debug.Log("儲存");

            // Putting it all together
            // await png.Compress(path)
            //    .Download()
            //    .SaveImageToDisk(path);

            var resultKB = GetFileSizeKB(path);
            textureResizeInfo.compressedSize   = resultKB;
            textureResizeInfo.compressedResult = resultKB;
            TextureResizeInfos.Add(textureResizeInfo);
            EditorUtility.DisplayProgressBar("壓縮圖片", "完成", progress);

            textureLimitCount = compressedImage.CompressionCount;
        }


        private int GetFileSizeKB(string path)
        {
            var length = new FileInfo(path).Length;

            return Mathf.FloorToInt(length / 1024f);
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            tinyConfig = AssetDatabase.LoadAssetAtPath(SETTINGS_PATH, typeof(TinyConfig)) as TinyConfig;
            if(tinyConfig == null)
            {
                CreateNewSettings();
            }
        }

        private void OnDisable()
        {
            EditorUtility.UnloadUnusedAssetsImmediate(true);
        }

    }
}