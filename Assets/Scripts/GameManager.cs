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
    #region ��������
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
    #region ��Ա����
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
    #region METHOD����

    /// <summary>
    /// ����AssetBunlde��
    /// </summary>
    /// <param name="abName">AB����</param>
    /// <param name="objName">AB����������</param>
    /// <param name="mode">����ģʽ</param>
    /// <returns></returns>
    public GameObject LoadAssetBundle(string abName,string objName,LoadMode mode=LoadMode.GameObject)
    {
        StringBuilder abPath = new StringBuilder(Application.streamingAssetsPath);
        abPath.Append("/AssetBundles/" + abName);
        var myAssetBundle=AssetBundle.LoadFromFile(abPath.ToString());
        if (myAssetBundle == null)
        {
            Debug.LogWarning($"û�м��ص� {abPath.ToString()} ���ļ�");
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
            //��ȡ������Ҫ���ص���Դ����Ϊ����
            UnityWebRequestJson[] jsonDatas;
            if(!ReadJson(jsonPath, out jsonDatas))
                return;
            //����������Ҫ���ص���Դ������
            foreach (var data in jsonDatas)
            {
                //������Դ
                StartCoroutine(DownLoadAssetBunlde(data.asset_url, data.asset_name, () =>
                {
                    //��Դ������ɺ�Ļص�
                    downCount++;
                    Debug.Log("���ؽ��� : " + (downCount / (float)jsonDatas.Length * 100) + "%");
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
    /// �ӷ�������ȡ��Դ
    /// </summary>
    /// <param name="url">��Դ��ַ</param>
    /// <param name="fileName">�ļ���</param>
    /// <param name="Download">��������</param>
    /// <returns></returns>
    private IEnumerator DownLoadAssetBunlde(string url,string fileName,Action Download=null)
    {
        //�������ļ���ַ·��
        string originPath = url +"/"+ fileName;
        using (UnityWebRequest request = UnityWebRequest.Get(originPath))
        {
            yield return request.SendWebRequest();

            //������ɺ�ִ�лص�
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
                //fs.Write(�ֽ�����, ��ʼλ��, ���ݳ���);
                fs.Write(results, 0, results.Length);
                fs.Flush(); //�ļ�д��洢��Ӳ��
                fs.Close(); //�ر��ļ�������
                fs.Dispose(); //�����ļ�����
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

    #region ASSOCIATION����
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
