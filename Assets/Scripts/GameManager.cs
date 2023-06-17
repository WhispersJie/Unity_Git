using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using Directory = System.IO.Directory;
using File = System.IO.File;

public class GameManager : MonoBehaviour
{
    #region 创建单例
    private static GameManager instance;    
    public static GameManager Instance
    { 
        get 
        { 
            if (instance != null)
            return instance;
            instance = FindObjectOfType<GameManager>();
            if (instance == null)
            {
                instance = new GameManager();
            }
            else
            {
                instance.Init();
            }
            return instance;
        } 
    }
    private void Init()
    {
        slider=GameObject.Find("Canvas/Slider").GetComponent<Slider>();
    }
    #endregion
    #region 成员变量
    public string jsonPath;
    public Slider slider;

    private float downCount = 0;
         
    #endregion

    private void Awake()
    {
        instance = this;
        Init();
    }
    private void Start()
    {
        LoadResource(true);
    }
    #region METHOD方法

    /// <summary>
    /// 加载AssetBunlde包
    /// </summary>
    /// <param name="abName">AB包名</param>
    /// <param name="objName">AB包中物体名</param>
    /// <param name="mode">加载模式</param>
    /// <returns></returns>
    public GameObject LoadAssetBundle(string abName,string objName,LoadMode mode=LoadMode.GameObject)
    {
        StringBuilder abPath = new StringBuilder(Application.streamingAssetsPath);
        abPath.Append("/AssetBundles/" + abName);
        var myAssetBundle=AssetBundle.LoadFromFile(abPath.ToString());
        if (myAssetBundle == null)
        {
            Debug.LogWarning($"没有加载到 {abPath.ToString()} 的文件");
            return null;
        }
        var prefab = myAssetBundle.LoadAsset<GameObject>(objName);
        return prefab;
    }
    private void LoadResource(bool downloading=false)
    {
        if (downloading)
        {
            downCount = 0;
            //获取所有需要下载的资源，存为数组
            UnityWebRequestJson[] jsonDatas;
            if(!ReadJson(jsonPath, out jsonDatas))
                return;
            //遍历所有需要下载的资源的名字
            foreach (var data in jsonDatas)
            {
                //下载资源
                StartCoroutine(DownLoadAssetBunlde(data.asset_url, data.asset_name, () =>
                {
                    //资源下载完成后的回调
                    downCount++;
                    Debug.Log("下载进度 : " + (downCount / (float)jsonDatas.Length * 100) + "%");
                    slider.value = downCount / (float)jsonDatas.Length;
                }));
            }
        }
        else
        {
            Debug.Log(ReadJson(jsonPath));
        }
    }

    private string ReadJson(string filePath)
    {
        string json = string.Empty;
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                json = sr.ReadToEnd().ToString();
            }
        }
        return json;
    }
    private bool ReadJson(string filePath,out UnityWebRequestJson[] jsons)
    {
        if (File.Exists(filePath))
        {
            string jsonStr=File.ReadAllText(filePath);
            //jsons=JsonUtility.FromJson<UnityWebRequestJson[]>(jsonStr);
            jsons = JsonConvert.DeserializeObject<UnityWebRequestJson[]>(jsonStr);
            return true;
        }
        jsons = default(UnityWebRequestJson[]);
        return false;
    }

    /// <summary>
    /// 从服务器获取资源
    /// </summary>
    /// <param name="url">资源地址</param>
    /// <param name="fileName">文件名</param>
    /// <param name="Download">匿名函数</param>
    /// <returns></returns>
    private IEnumerator DownLoadAssetBunlde(string url,string fileName,Action Download=null)
    {
        //服务器文件地址路径
        string originPath = url +"/"+ fileName;
        using (UnityWebRequest request = UnityWebRequest.Get(originPath))
        {
            yield return request.SendWebRequest();

            //下载完成后执行回调
            if (request.isDone)
            {
                byte[] results = request.downloadHandler.data;
                string savePath=Application.dataPath + "/" + "Download";

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }
                FileInfo fileInfo = new FileInfo(savePath+"/"+ fileName);
                FileStream fs=fileInfo.Create();
                //fs.Write(字节数组, 开始位置, 数据长度);
                fs.Write(results, 0, results.Length);
                fs.Flush(); //文件写入存储到硬盘
                fs.Close(); //关闭文件流对象
                fs.Dispose(); //销毁文件对象
                if (Download != null)
                    Download();
            }
        }
    }
    private void OnApplicationQuit()
    {
        AssetBundle.UnloadAllAssetBundles(true);
    }
    #endregion

    #region ASSOCIATION关联
    public enum LoadMode
    {
        None,
        GameObject,
    }

    public struct UnityWebRequestJson
    {
        public int asset_id;
        public string asset_url;
        public string asset_name;
    }
    #endregion
}
