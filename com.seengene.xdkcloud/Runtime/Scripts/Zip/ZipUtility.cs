/******************************************************
* DESCRIPTION: Zip包的压缩与解压
*
*     Copyright (c) 2017, 谭伟俊 （TanWeijun）
*     All rights reserved
*
* CREATED: 2017.03.11, 08:37, CST
******************************************************/

using System.IO;
using System.Collections;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using System;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using System.Collections.Generic;

namespace Seengene.XDK
{
    public static class ZipUtility
    {
        #region ZipCallback
        public abstract class ZipCallback
        {
            /// <summary>
            /// 压缩单个文件或文件夹前执行的回调
            /// </summary>
            /// <param name="_entry"></param>
            /// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
            public virtual bool OnPreZip(ZipEntry _entry) {
                return true;
            }

            /// <summary>
            /// 压缩单个文件或文件夹后执行的回调
            /// </summary>
            /// <param name="_entry"></param>
            public virtual void OnPostZip(ZipEntry _entry) { }

            /// <summary>
            /// 压缩执行完毕后的回调
            /// </summary>
            /// <param name="_result">true表示压缩成功，false表示压缩失败</param>
            public virtual void OnFinished(bool _result) { }
        }
        #endregion

        #region UnzipCallback
        public abstract class UnzipCallback
        {
            /// <summary>
            /// 解压单个文件或文件夹前执行的回调
            /// </summary>
            /// <param name="_entry"></param>
            /// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
            public virtual bool OnPreUnzip(ZipEntry _entry) {
                return true;
            }

            /// <summary>
            /// 解压单个文件或文件夹后执行的回调
            /// </summary>
            /// <param name="_entry"></param>
            public virtual void OnPostUnzip(ZipEntry _entry) { }

            /// <summary>
            /// 解压执行完毕后的回调
            /// </summary>
            /// <param name="_result">true表示解压成功，false表示解压失败</param>
            public virtual void OnFinished(bool _result) { }
        }
        #endregion

        /// <summary>
        /// 压缩文件和文件夹
        /// </summary>
        /// <param name="_fileOrDirectoryArray">文件夹路径和文件名</param>
        /// <param name="_outputPathName">压缩后的输出路径文件名</param>
        /// <param name="_password">压缩密码</param>
        /// <param name="_zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool Zip(string[] _fileOrDirectoryArray, string _outputPathName, string _password = null, ZipCallback _zipCallback = null) {
            if ((null == _fileOrDirectoryArray) || string.IsNullOrEmpty(_outputPathName)) {
                if (null != _zipCallback)
                    _zipCallback.OnFinished(false);

                return false;
            }

            ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(_outputPathName));
            zipOutputStream.SetLevel(6);    // 压缩质量和压缩速度的平衡点
            if (!string.IsNullOrEmpty(_password))
                zipOutputStream.Password = _password;

            for (int index = 0; index < _fileOrDirectoryArray.Length; ++index) {
                bool result = false;
                string fileOrDirectory = _fileOrDirectoryArray[index];
                if (Directory.Exists(fileOrDirectory))
                    result = ZipDirectory(fileOrDirectory, string.Empty, zipOutputStream, _zipCallback);
                else if (File.Exists(fileOrDirectory))
                    result = ZipFile(fileOrDirectory, string.Empty, zipOutputStream, _zipCallback);

                if (!result) {
                    if (null != _zipCallback)
                        _zipCallback.OnFinished(false);

                    return false;
                }
            }

            zipOutputStream.Finish();
            zipOutputStream.Close();

            if (null != _zipCallback)
                _zipCallback.OnFinished(true);

            return true;
        }

        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="_filePathName">Zip包的文件路径名</param>
        /// <param name="_outputPath">解压输出路径</param>
        /// <param name="_password">解压密码</param>
        /// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool UnzipFile(string _filePathName, string _outputPath, string _password = null, UnzipCallback _unzipCallback = null) {
            if (string.IsNullOrEmpty(_filePathName) || string.IsNullOrEmpty(_outputPath)) {
                if (null != _unzipCallback)
                    _unzipCallback.OnFinished(false);

                return false;
            }

            try {
                return UnzipFile(File.OpenRead(_filePathName), _outputPath, _password, _unzipCallback);
            } catch (System.Exception _e) {
                Debug.LogError("[ZipUtility.UnzipFile]: " + _e.ToString());

                if (null != _unzipCallback)
                    _unzipCallback.OnFinished(false);

                return false;
            }
        }

        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="_fileBytes">Zip包字节数组</param>
        /// <param name="_outputPath">解压输出路径</param>
        /// <param name="_password">解压密码</param>
        /// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool UnzipFile(byte[] _fileBytes, string _outputPath, string _password = null, UnzipCallback _unzipCallback = null) {
            if ((null == _fileBytes) || string.IsNullOrEmpty(_outputPath)) {
                if (null != _unzipCallback)
                    _unzipCallback.OnFinished(false);

                return false;
            }

            bool result = UnzipFile(new MemoryStream(_fileBytes), _outputPath, _password, _unzipCallback);
            if (!result) {
                if (null != _unzipCallback)
                    _unzipCallback.OnFinished(false);
            }

            return result;
        }

        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="_inputStream">Zip包输入流</param>
        /// <param name="_outputPath">解压输出路径</param>
        /// <param name="_password">解压密码</param>
        /// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool UnzipFile(Stream _inputStream, string _outputPath, string _password = null, UnzipCallback _unzipCallback = null) {
            if ((null == _inputStream) || string.IsNullOrEmpty(_outputPath)) {
                if (null != _unzipCallback) {
                    _unzipCallback.OnFinished(false);
                }
                return false;
            }

            // 创建文件目录
            if (!Directory.Exists(_outputPath))
                Directory.CreateDirectory(_outputPath);

            // 解压Zip包
            ZipEntry entry = null;
            using (ZipInputStream zipInputStream = new ZipInputStream(_inputStream)) {
                if (!string.IsNullOrEmpty(_password)) {
                    zipInputStream.Password = _password;
                }

                while (null != (entry = zipInputStream.GetNextEntry())) {
                    if (string.IsNullOrEmpty(entry.Name)) {
                        continue;
                    }

                    if ((null != _unzipCallback) && !_unzipCallback.OnPreUnzip(entry)) {
                        continue;   // 过滤
                    }

                    string filePathName = Path.Combine(_outputPath, entry.Name);

                    // 创建文件目录
                    if (entry.IsDirectory) {
                        Directory.CreateDirectory(filePathName);
                        continue;
                    }

                    // 写入文件
                    try {
                        using (FileStream fileStream = File.Create(filePathName)) {
                            byte[] bytes = new byte[1024];
                            while (true) {
                                int count = zipInputStream.Read(bytes, 0, bytes.Length);
                                if (count > 0)
                                    fileStream.Write(bytes, 0, count);
                                else {
                                    if (null != _unzipCallback)
                                        _unzipCallback.OnPostUnzip(entry);

                                    break;
                                }
                            }
                        }
                    } catch (System.Exception _e) {
                        Debug.LogError("[ZipUtility.UnzipFile]: " + _e.ToString());

                        if (null != _unzipCallback)
                            _unzipCallback.OnFinished(false);

                        return false;
                    }
                }
            }

            if (null != _unzipCallback) {
                _unzipCallback.OnFinished(true);
            }

            return true;
        }


        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="_inputStream">Zip包输入流</param>
        /// <param name="_outputPath">解压输出路径</param>
        /// <param name="_password">解压密码</param>
        /// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static IEnumerator UnzipFileAsync(Stream _inputStream, string _outputPath, string _password = null, Action<string> onFinish = null, Action<float> onPorgress = null) {
            if ((null == _inputStream) || string.IsNullOrEmpty(_outputPath)) {
                onFinish?.Invoke("params error");
                yield break;
            }

            // 创建文件目录
            if (!Directory.Exists(_outputPath)) {
                Directory.CreateDirectory(_outputPath);
            }

            // 查看内部文件数量
            int total = 0;
            ZipEntry entry;
            ZipInputStream zipInputStream = new ZipInputStream(_inputStream);
            zipInputStream.IsStreamOwner = false;
            if (!string.IsNullOrEmpty(_password)) {
                zipInputStream.Password = _password;
            }
            while (null != (entry = zipInputStream.GetNextEntry())) {
                if (string.IsNullOrEmpty(entry.Name)) {
                    continue;
                }
                total++;
                if (total % 50 == 0) {
                    yield return new WaitForEndOfFrame();
                }
            }

            Debug.LogFormat("There are {0} entrys in zip file", total);
            if (total == 0) {
                onPorgress?.Invoke(1f);
                onFinish?.Invoke(null);
                yield break;
            }
            // 解压Zip包
            int succ = 0;
            _inputStream.Position = 0; // 
            zipInputStream = new ZipInputStream(_inputStream);
            if (!string.IsNullOrEmpty(_password)) {
                zipInputStream.Password = _password;
            }

            while (null != (entry = zipInputStream.GetNextEntry())) {
                if (string.IsNullOrEmpty(entry.Name)) {
                    continue;
                }

                string filePathName = Path.Combine(_outputPath, entry.Name);
                if (entry.IsDirectory) // 创建文件目录
                {
                    Directory.CreateDirectory(filePathName);
                    succ++;
                    onPorgress?.Invoke(succ * 1.0f / total);
                } else // 写入文件
                  {
                    FileStream fileStream = null;
                    try {
                        fileStream = File.Create(filePathName);
                    } catch (System.Exception ee) {
                        Debug.LogError("[ZipUtility.UnzipFile] 0 : " + ee.ToString());
                        succ++;
                        onPorgress?.Invoke(succ * 1.0f / total);
                        continue;
                    }
                    try {
                        byte[] bytes = new byte[1024];
                        while (true) {
                            int count = zipInputStream.Read(bytes, 0, bytes.Length);
                            if (count > 0) {
                                fileStream.Write(bytes, 0, count);
                            } else {
                                break;
                            }
                        }
                        fileStream.Flush();
                        fileStream.Close();
                        succ++;
                        onPorgress?.Invoke(succ * 1.0f / total);
                    } catch (System.Exception _e) {
                        Debug.LogError("[ZipUtility.UnzipFile] 1 : " + _e.ToString());

                        onFinish?.Invoke("fail to unzip");
                        yield break;
                    }
                }

                yield return new WaitForEndOfFrame();
            }
            zipInputStream.Dispose();
            onFinish?.Invoke(null);
        }

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="_filePathName">文件路径名</param>
        /// <param name="_parentRelPath">要压缩的文件的父相对文件夹</param>
        /// <param name="_zipOutputStream">压缩输出流</param>
        /// <param name="_zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        private static bool ZipFile(string _filePathName, string _parentRelPath, ZipOutputStream _zipOutputStream, ZipCallback _zipCallback = null) {
            //Crc32 crc32 = new Crc32();
            ZipEntry entry = null;
            FileStream fileStream = null;
            try {
                string entryName = _parentRelPath + '/' + Path.GetFileName(_filePathName);
                entry = new ZipEntry(entryName);
                entry.DateTime = System.DateTime.Now;

                if ((null != _zipCallback) && !_zipCallback.OnPreZip(entry))
                    return true;    // 过滤

                fileStream = File.OpenRead(_filePathName);
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                fileStream.Close();

                entry.Size = buffer.Length;

                //crc32.Reset();
                //crc32.Update(buffer);
                //entry.Crc = crc32.Value;

                _zipOutputStream.PutNextEntry(entry);
                _zipOutputStream.Write(buffer, 0, buffer.Length);
            } catch (System.Exception _e) {
                Debug.LogError("[ZipUtility.ZipFile]: " + _e.ToString());
                return false;
            } finally {
                if (null != fileStream) {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }

            if (null != _zipCallback)
                _zipCallback.OnPostZip(entry);

            return true;
        }

        /// <summary>
        /// 压缩文件夹
        /// </summary>
        /// <param name="_path">要压缩的文件夹</param>
        /// <param name="_parentRelPath">要压缩的文件夹的父相对文件夹</param>
        /// <param name="_zipOutputStream">压缩输出流</param>
        /// <param name="_zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        private static bool ZipDirectory(string _path, string _parentRelPath, ZipOutputStream _zipOutputStream, ZipCallback _zipCallback = null) {
            ZipEntry entry = null;
            try {
                string entryName = Path.Combine(_parentRelPath, Path.GetFileName(_path) + '/');
                entry = new ZipEntry(entryName);
                entry.DateTime = System.DateTime.Now;
                entry.Size = 0;

                if ((null != _zipCallback) && !_zipCallback.OnPreZip(entry))
                    return true;    // 过滤

                _zipOutputStream.PutNextEntry(entry);
                _zipOutputStream.Flush();

                string[] files = Directory.GetFiles(_path);
                for (int index = 0; index < files.Length; ++index)
                    ZipFile(files[index], Path.Combine(_parentRelPath, Path.GetFileName(_path)), _zipOutputStream, _zipCallback);
            } catch (System.Exception _e) {
                Debug.LogError("[ZipUtility.ZipDirectory]: " + _e.ToString());
                return false;
            }

            string[] directories = Directory.GetDirectories(_path);
            for (int index = 0; index < directories.Length; ++index) {
                if (!ZipDirectory(directories[index], Path.Combine(_parentRelPath, Path.GetFileName(_path)), _zipOutputStream, _zipCallback))
                    return false;
            }

            if (null != _zipCallback)
                _zipCallback.OnPostZip(entry);

            return true;
        }


        /// <summary>
        /// 文件解压
        /// </summary>
        /// <param name="zipPath">压缩文件路径</param>
        /// <param name="goalFolder">解压到的目录</param>
        /// <param name="onFinish">解压完成的回调</param>
        /// <param name="onPorgress">解压进度回调</param>
        /// <returns></returns>
        public static IEnumerator UnzipTarGzAsync(string zipPath, string goalFolder, Action<string> onFinish = null, Action<float> onPorgress = null) {
            Stream inStream;
            Stream gzipStream = null;
            TarArchive tarArchive = null;
            int fileDone = 0;

            // 逐个解压缩文件
            using (inStream = File.OpenRead(zipPath)) {
                long len = inStream.Length; // 压缩包总长度
                using (gzipStream = new GZipInputStream(inStream)) {
                    tarArchive = TarArchive.CreateInputTarArchive(gzipStream, null);
                    var fullDistDir = Path.GetFullPath(goalFolder);
                    TarInputStream tarIn = tarArchive.getTarIn();
                    long milli = System.DateTime.UtcNow.Ticks;
                    while (true) {
                        TarEntry entry = tarIn.GetNextEntry();
                        if (entry == null) {
                            break;
                        }
                        fileDone++;
                        if (entry.TarHeader.TypeFlag == TarHeader.LF_LINK || entry.TarHeader.TypeFlag == TarHeader.LF_SYMLINK) {
                            continue;
                        }
                        tarArchive.ExtractEntry(fullDistDir, entry, false);
                        long pos = inStream.Position; // 
                        long now = System.DateTime.UtcNow.Ticks;
                        if (now - milli > 2 * 1000000) {
                            //Debug.Log("progress=" + (pos * 1.0f / len)+" fileDone="+fileDone);
                            onPorgress?.Invoke(pos * 1.0f / len);
                            milli = now;
                            yield return null;
                        }
                    }
                }
            }
            if (null != tarArchive) {
                tarArchive.Close();
            }
            if (null != gzipStream) {
                gzipStream.Close();
            }
            if (null != inStream) {
                inStream.Close();
            }
            onPorgress?.Invoke(1.0f);
            onFinish?.Invoke(null);
        }


    }
}