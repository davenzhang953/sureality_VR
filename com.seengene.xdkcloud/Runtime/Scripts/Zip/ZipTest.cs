using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Seengene.XDK
{
    public class ZipTest : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            string srcFile = Application.persistentDataPath + "/zip/output.zip";
            if (File.Exists(srcFile))
            {
                Debug.Log("file is ok, src=" + srcFile);
                string outFolder = Application.persistentDataPath + "/ply/zzz";


                ZipXiCallback unzipFileCallback = new ZipXiCallback();
                FileStream fs = File.OpenRead(srcFile);
                Debug.Log("begin unzipTheFile..... fileStream ok ");
                StartCoroutine(ZipUtility.UnzipFileAsync(fs, outFolder, null, null, null));
                Debug.Log("unzip succ? " + unzipFileCallback.success);
            }
            else
            {
                Debug.Log("file not exist, src=" + srcFile);
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
