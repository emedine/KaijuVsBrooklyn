/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using InfinityCode.Zip;
using UnityEngine;

public static class RealWorldTerrainDownloader
{
    private const int maxDownloadItem = 16;

    public static int completeSize;
    public static bool finish;

    private static List<RealWorldTerrainDownloaderItem> activeItems;
    private static List<RealWorldTerrainDownloaderItem> items;
    private static List<RealWorldTerrainDownloaderItem> tempItems;
    private static int totalSize;

    public static int count
    {
        get
        {
            if (items == null) return -1;
            return items.Count;
        }
    }

    public static float progress
    {
        get
        {
            if (activeItems == null || activeItems.Count == 0) return 0;
            float localProgress = activeItems.Sum(i => i.progress * i.avarageSize) / totalSize;
            return completeSize  / (float)totalSize + localProgress;
        }
    }

    public static int totalSizeMB
    {
        get { return Mathf.RoundToInt(totalSize / (float) RealWorldTerrainUtils.Mb); }
    }

    public static RealWorldTerrainDownloaderItem Add(string url, string localFilename,
        RealWorldTerrainDownloadType downloadType, string title, int avarageSize, bool generateErrorFile = true)
    {
        if (items == null) items = new List<RealWorldTerrainDownloaderItem>();
        RealWorldTerrainDownloaderItem item = new RealWorldTerrainDownloaderItem(url, localFilename, downloadType, title,
            avarageSize);
        item.generateErrorFile = generateErrorFile;
        if (!item.exists) items.Add(item);
        return item;
    }

    public static RealWorldTerrainDownloaderItem Add(string url, string localFilename,
        RealWorldTerrainDownloadType downloadType, string title, int avarageSize, long failedCRC)
    {
        RealWorldTerrainDownloaderItem item = Add(url, localFilename, downloadType, title, avarageSize);
        item.failedCRC = failedCRC;
        return item;
    }

    public static RealWorldTerrainDownloaderItem AddTemp(string url, string localFilename,
        RealWorldTerrainDownloadType downloadType, string title, int avarageSize)
    {
        if (tempItems == null) tempItems = new List<RealWorldTerrainDownloaderItem>();
        RealWorldTerrainDownloaderItem item = new RealWorldTerrainDownloaderItem(url, localFilename, downloadType, title,
            avarageSize);
        if (!item.exists) tempItems.Add(item);
        return item;
    }

    public static RealWorldTerrainDownloaderItem AddTemp(string url, string localFilename,
        RealWorldTerrainDownloadType downloadType, string title, int avarageSize, long failedCRC)
    {
        RealWorldTerrainDownloaderItem item = AddTemp(url, localFilename, downloadType, title, avarageSize);
        item.failedCRC = failedCRC;
        return item;
    }

    public static void ApplyTemp()
    {
        if (tempItems == null) return;
        if (items == null) items = new List<RealWorldTerrainDownloaderItem>();
        items.AddRange(tempItems);
        tempItems = null;
    }

    public static void CheckComplete()
    {
        foreach (RealWorldTerrainDownloaderItem item in activeItems) item.CheckComplete();
        if (activeItems.RemoveAll(i => i.complete) > 0) GC.Collect();
        while (activeItems.Count < maxDownloadItem && items.Count > 0) StartNextDownload();
        if (activeItems.Count == 0 && items.Count == 0) finish = true;
    }

    public static void ClearTemp()
    {
        tempItems = null;
        GC.Collect();
    }

    public static void Dispose()
    {
        if (activeItems != null)
        {
            foreach (RealWorldTerrainDownloaderItem item in activeItems) item.Dispose();
            activeItems = null;
        }

        items = null;
        tempItems = null;
    }

    public static void Start()
    {
        finish = false;
        completeSize = 0;

        if (items == null || items.Count == 0)
        {
            finish = true;
            return;
        }

        activeItems = new List<RealWorldTerrainDownloaderItem>();

        try
        {
            totalSize = items.Sum(i => i.avarageSize);
        }
        catch
        {
            RealWorldTerrain.isCapturing = false;
            Dispose();
            return;
        }

        CheckComplete();
    }

    public static void StartNextDownload()
    {
        if (items == null || items.Count == 0) return;
        RealWorldTerrainDownloaderItem item = items[0];
        items.RemoveAt(0);
        if (item.exists) return;
        item.Start();
        activeItems.Add(item);
    }
}

public class RealWorldTerrainDownloaderItem
{
    public delegate void OnCompleteHandler(ref byte[] data);
    public event OnCompleteHandler OnComplete;

    public readonly int avarageSize;
    public readonly RealWorldTerrainDownloadType downloadType;
    public readonly string localFilename;
    public readonly string title;
    public readonly Uri url;

    public bool checkCRC;
    public bool complete = false;
    public bool generateErrorFile = true;

    public float progress
    {
        get
        {
            if (downloadType == RealWorldTerrainDownloadType.www) return www.progress;
            return _progress;
        }
    }

    private string _errorFilename;
    private long _failedCRC;
    private WebClient client;
    private WWW www;
    private float _progress = 0;
    


    private string errorFilename
    {
        get
        {
            if (string.IsNullOrEmpty(_errorFilename))
                _errorFilename = localFilename.Substring(0, localFilename.LastIndexOf(".") + 1) + "err";
            return _errorFilename;
        }
    }

    public bool exists
    {
        get { return File.Exists(localFilename) || File.Exists(errorFilename); }
    }

    public long failedCRC
    {
        get { return _failedCRC; }
        set
        {
            _failedCRC = value;
            checkCRC = true;
        }
    }

    public RealWorldTerrainDownloaderItem(string url, string localFilename, RealWorldTerrainDownloadType downloadType,
        string title, int avarageSize)
    {
        this.url = new Uri(url);
        this.localFilename = localFilename;
        this.downloadType = downloadType;
        this.title = title;
        this.avarageSize = avarageSize;
    }

    public void CheckComplete()
    {
        if (downloadType == RealWorldTerrainDownloadType.www)
        {
            if (www.isDone)
            {
                if (string.IsNullOrEmpty(www.error)) SaveWWWData();
                else Debug.Log("Download failed: " + www.url + "\n" + www.error);
                
                RealWorldTerrainDownloader.completeSize += avarageSize;
                complete = true;
            }
        }
    }

    public void CreateErrorFile()
    {
        if (!generateErrorFile) return;
        File.WriteAllBytes(errorFilename, new byte[] {});
    }

    public void DispatchCompete(ref byte[] data)
    {
        if (OnComplete != null) OnComplete(ref data);
    }

    public void Dispose()
    {
        if (downloadType == RealWorldTerrainDownloadType.www)
        {
            www.Dispose();
        }
        else
        {
            client.CancelAsync();
            
            if (downloadType == RealWorldTerrainDownloadType.file)
                RealWorldTerrainUtils.SafeDeleteFile(localFilename);
        }

        client = null;
        www = null;
    }

    private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            Debug.Log(e.Error);
            RealWorldTerrainUtils.SafeDeleteFile(localFilename);
            CreateErrorFile();
        }
        else if (new FileInfo(localFilename).Length == 0)
        {
            RealWorldTerrainUtils.SafeDeleteFile(localFilename);
            CreateErrorFile();
        }

        RealWorldTerrainDownloader.completeSize += avarageSize;
        _progress = 1;
        complete = true;
    }

    private void OnDownloadDataComplete(object sender, DownloadDataCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            Debug.Log(e.Error);
            CreateErrorFile();
        }
        else
        {
            byte[] data = e.Result;
            bool allowSave = true;
            if (data.Length == 0) allowSave = false;
            else if (checkCRC)
            {
                Crc32 crc = new Crc32();
                crc.Update(data, 0, data.Length);
                if (crc.Value == failedCRC) allowSave = false;
            }

            if (allowSave)
            {
                DispatchCompete(ref data);
                if (localFilename != string.Empty) File.WriteAllBytes(localFilename, data);
            }
            else
                CreateErrorFile();
        }

        RealWorldTerrainDownloader.completeSize += avarageSize;
        _progress = 1;
        complete = true;
    }

    private void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        _progress = e.ProgressPercentage / 100f;
    }

    public void SaveWWWData()
    {
        File.WriteAllBytes(localFilename, www.bytes);
    }

    public void Start()
    {
        if (downloadType == RealWorldTerrainDownloadType.file || downloadType == RealWorldTerrainDownloadType.data)
        {
            client = new WebClient();
            client.DownloadDataCompleted += OnDownloadDataComplete;
            client.DownloadFileCompleted += OnDownloadComplete;
            client.DownloadProgressChanged += OnProgressChanged;
        }

        if (downloadType == RealWorldTerrainDownloadType.file)
            client.DownloadFileAsync(url, localFilename);
        else if (downloadType == RealWorldTerrainDownloadType.data)
            client.DownloadDataAsync(url);
        else www = new WWW(url.AbsoluteUri);
    }
}