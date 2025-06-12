using System.Collections;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using Seengene.XDK;
using static Seengene.XDK.ZipUtility;

namespace Seengene.XDK
{
    public class ZipXiCallback : UnzipCallback
    {
        [HideInInspector]
        public bool finished;

        [HideInInspector]
        public bool success;


        /// <summary>
        /// 解压单个文件或文件夹前执行的回调
        /// </summary>
        /// <param name="_entry"></param>
        /// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
        public override bool OnPreUnzip(ZipEntry _entry) {
            Debug.Log("unzip pre . name=" + _entry.Name + " index=" + _entry.ZipFileIndex);
            return true;
        }

        /// <summary>
        /// 解压单个文件或文件夹后执行的回调
        /// </summary>
        /// <param name="_entry"></param>
        public override void OnPostUnzip(ZipEntry _entry) {
            Debug.Log("unzip post. name=" + _entry.Name + " index=" + _entry.ZipFileIndex);
        }

        /// <summary>
        /// 解压执行完毕后的回调
        /// </summary>
        /// <param name="_result">true表示解压成功，false表示解压失败</param>
        public override void OnFinished(bool _result) {
            finished = true;
            success = _result;
            Debug.Log("unzip finished. succ=" + _result);
        }
    }
}
