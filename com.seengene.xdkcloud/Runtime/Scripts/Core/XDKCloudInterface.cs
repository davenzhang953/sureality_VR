using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.Net.Sockets;
using Stopwatch = System.Diagnostics.Stopwatch;
using OpenCVForUnity.CoreModule;

namespace Seengene.XDK 
{ 
    public sealed class XDKCloudInterface : MonoBehaviour
    {
        private class FrameContainer
        {
            public XDKFrameData frameData;
            public XDKLBSData lbsData;
            public Mat imageMat;
            public Mat intrinsicsMat;
        }


        public static float totalTime = 0f;
        public static float maxTime = float.MinValue;
        public static float minTime = float.MaxValue;
        public static int totalCount = 0;
        public static int dropCount = 0;
        public static int authCount = 0;
        public static string currUrl = "";

        public Action<AuthorizationResponse> OnAuthorized;
        public Action<RelocationResponse> OnRelocated;

        private int serverFlag = -1; 
        private bool IsUploadingImage = false;
        private string session_ID;
        private string MyCookie;

        internal int requestCounter;
        internal XDKCloudSession xSession;
        private AuthorizationRequest authReq;
        private List<RelocationRequest> requestQueue = new();


        private string persistentDataPath;
        private string imageSavePath;

        private FrameContainer dataTank;

        private void Awake()
        {
            persistentDataPath = Application.persistentDataPath;
            serverFlag = UnityEngine.Random.Range(1, 1000);
        }



        private void OnDestroy()
        {
            StopRelocate();
        }


        /// <summary>
        /// 做认证
        /// </summary>
        public void Authorize()
        {
            IsUploadingImage = false;

            authReq = new AuthorizationRequest();
            authReq.mapId = xSession.currentMap.ToString();
            authReq.sdkVersion = XDKCloudSession.XDKVersion;
            authReq.deviceInfo = xSession.xdkHardware.GetName();
            authReq.bundleID = Application.identifier;
            authReq.secretKey = xSession.secretKey;

            var threadAuth = new Thread(AuthorizeAsync);
            threadAuth.IsBackground = true;
            threadAuth.Start();
        }


        /// <summary>
        /// 添加一个定位请求的数据
        /// </summary>
        /// <param name="frameData"></param>
        /// <param name="lbsData"></param>
        public void RelocateFrame(XDKFrameData frameData, XDKLBSData lbsData)
        {
            if (frameData.rgbTexture == null
                && frameData.textureDataR8 == null
                && frameData.textureDataRGB24 == null)
            {
                Debug.LogError("相机照片为空！本次图片无法进行定位");
            }
            else
            {
                // 这一步必须放到主线程里
                XDKTools.FrameDataToMat(frameData, out Mat image0, out Mat intrinsics0);
                dataTank = new FrameContainer();
                dataTank.frameData = frameData;
                dataTank.lbsData = lbsData;
                dataTank.imageMat = image0;
                dataTank.intrinsicsMat = intrinsics0;

                /// 使用单独的线程来处理图片
                var worker = new Thread(CreateRelocateRequest);
                worker.IsBackground = true;
                worker.Start();
            }
        }

        /// <summary>
        /// 停止访问定位服务
        /// </summary>
        public void StopRelocate()
        {
            Debug.Log("Seengene StopRelocate");
            IsUploadingImage = false;
            requestQueue.Clear();
        }

        /// <summary>
        /// 清空缓存的定位请求
        /// </summary>
        public void ClearRequests()
        {
            requestQueue.Clear();
        }


        /// <summary>
        /// 从队列中获取一个最新的定位请求
        /// <returns></returns>
        private RelocationRequest getUploadImageItem()
        {
            RelocationRequest item = null;
            for (int i = requestQueue.Count - 1; i >= 0; i--)
            {
                var temp = requestQueue[i];
                if (string.Equals(temp.sessionID, session_ID))
                {
                    if (item == null)
                    {
                        item = temp;
                    }
                    else
                    {
                        dropCount++;
                    }
                }
            }
            requestQueue.Clear();
            return item;
        }


        private void SendDebugInfo(DebugInfoType tt, string format, params object[] others)
        {
            if (XDKCloudSession.IfDebugOn)
            {
                Loom.QueueOnMainThread(() => {
                    string msg = string.Format(format, others);
                    xSession.SendDebugEvent(tt, msg);
                });
            }
        }



        private void DebugSocektInfo(HttpWebRequest webReq, string mark)
        {
            ServicePoint servicePoint = webReq.ServicePoint;
            //ReflectTool.DebugAllProperty(typeof(ServicePoint));
            string ipAddress = servicePoint.Address.IdnHost;
            int portNumber = servicePoint.Address.Port;
            Debug.LogFormat("XDK Request, mark={0} ipAddress={1}, port={2}", mark, ipAddress, portNumber);

            //int localPort = ((IPEndPoint)webReq.ServicePoint.BindIPEndPointDelegate(null, new IPEndPoint(IPAddress.Any, 0))).Port;
        }




        /// <summary>
        /// 向后台服务器请求session
        /// </summary>
        /// <param name="mapId"></param>
        /// <returns></returns>
        private void AuthorizeAsync()
        {
            MyCookie = null; // 清除原有Cookie

            string authUrl = getServerUrl();
            if (authUrl.EndsWith("/"))
            {
                authUrl += "requestMap";
            }
            else
            {
                authUrl += "/requestMap";
            }
            currUrl = authUrl;

            long startTicks = DateTime.Now.Ticks / 10000;
            byte[] postBytes = authReq.ToByteArray();
            authCount++;

            AuthorizationResponse authResponse = new AuthorizationResponse();
            authResponse.scale = 0;
            authResponse.status = (int)MapQueryStatus.MAP_FAIL;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            HttpWebRequest httpReq = null;
            try
            {
                httpReq = WebRequest.CreateHttp(new Uri(authUrl));
                httpReq.Headers.Set("ServerFlag", serverFlag.ToString());
                httpReq.Method = "POST";
                httpReq.KeepAlive = true; 
                httpReq.ContentType = "application/json";
                httpReq.Timeout = 5000; // 单位是毫秒
                httpReq.ContentLength = postBytes.Length;

                using (Stream reqStream = httpReq.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                    reqStream.Close();
                }

                using (HttpWebResponse webResponse = (HttpWebResponse)httpReq.GetResponse())
                {
                    ReadCookieFromHeader(webResponse);
                    //DebugSocektInfo(httpReq, "auth");
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        ParseAuthResponse(webResponse, authResponse);
                        session_ID = authResponse.sessionID;

                        bool threadOK = false;
                        if(authResponse.status == (int)MapQueryStatus.MAP_SUCCESS)
                        {
                            threadOK = SendImageThreadStart();
                        }
                        Debug.Log("XDK Request, threadOK=" + threadOK+ " session_ID="+ session_ID);
                    }
                    else
                    {
                        authResponse.status = (int)MapQueryStatus.MAP_FAIL;
                        authResponse.sessionID = webResponse.StatusCode.ToString();
                    }
                    webResponse.Close();
                }
            }
            catch (Exception ex)
            {
                authResponse.status = (int)MapQueryStatus.NET_ERROR;
                Debug.LogFormat("XDK Request, auth err:{0}", ex);
            }

            sw.Stop();
            totalTime += sw.ElapsedMilliseconds;
            totalCount++;
            if (maxTime < sw.ElapsedMilliseconds)
            {
                maxTime = sw.ElapsedMilliseconds;
            }
            if (minTime > sw.ElapsedMilliseconds)
            {
                minTime = sw.ElapsedMilliseconds;
            }

            Loom.QueueOnMainThread(() =>
            {
                try
                {
                    OnAuthorized?.Invoke(authResponse);
                }
                catch (Exception ee)
                {
                    Debug.Log("XDK Request, exception: " + ee);
                }
            });

        }



        /// <summary>
        /// 处理定位服务的反馈
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cameraPos"></param>
        /// <param name="cameraIntrinsics"></param>
        /// <param name="sessionID"></param>
        private void ParseAuthResponse(HttpWebResponse myHttpWebResponse, AuthorizationResponse response)
        {
            using (Stream responseStream = myHttpWebResponse.GetResponseStream())
            {
                if (responseStream.CanTimeout)
                {
                    responseStream.ReadTimeout = 5000;
                }
                byte[] buffer = HttpResponseStreamToBytes(responseStream);
                response.ReadFromBytes(buffer);

                SendDebugInfo(DebugInfoType.TimeInfo, "XDK Request, auth.status={0} session={1}",
                                response.status,
                                response.sessionID);

                responseStream.Close();
            }
        }
        /// <summary>
        /// 获取服务器地址，
        /// </summary>
        /// <returns></returns>
        private string getServerUrl()
        {
            string url = XDKCloudSession.XDKServer;
            if (string.IsNullOrEmpty(url))
            {
                url = xSession.relocateServer;
            }
            if (url.EndsWith("/requestMap"))
            {
                url = url.Substring(0, url.Length - 11);
            }
            return url;
        }



        /// <summary>
        /// Create Request, run in new thread
        /// 
        /// </summary>
        private void CreateRelocateRequest()
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            //byte[] jpegBytes0 = XDKTools.GrayImageMatToJpgBytes(dataTank.imageMat);
            //string intri0 = XDKTools.ConvertIntrinsicsMatToString(dataTank.intrinsicsMat);

            XDKTools.CheckForDistort(dataTank.imageMat, dataTank.intrinsicsMat, dataTank.frameData.distortData, out Mat image1, out Mat intrinsics1);
            
            XDKTools.CheckForScale(image1, intrinsics1, out Mat image2, out Mat intrinsics2);
            //byte[] jpegBytes1 = XDKTools.GrayImageMatToJpgBytes(image2);
            //string intri1 = XDKTools.ConvertIntrinsicsMatToString(intrinsics2);

            XDKTools.CheckForRotate(image2, intrinsics2, dataTank.frameData.IsPortraitImage, out Mat image3, out Mat intrinsics3);
            //byte[] jpegBytes2 = XDKTools.GrayImageMatToJpgBytes(image3);
            //string intri2 = XDKTools.ConvertIntrinsicsMatToString(intrinsics3);

            XDKTools.CheckForFlip(image3, intrinsics3, dataTank.frameData.flipHorizontal, dataTank.frameData.flipVertical, out Mat image4, out Mat intrinsics4);

            byte[] jpegBytes = XDKTools.GrayImageMatToJpgBytes(image4);
            if (jpegBytes == null)
            {
                UnityEngine.Debug.LogError("图片转单通道灰度图JPG格式时出错！");
                return;
            }
            //sw.Stop();
            float[,] intrinsics = XDKTools.ConvertIntrinsicsMatToFloatArray(intrinsics4);


            // 获取相机位姿Transform
            Vector3 positionIn = dataTank.frameData.cameraPosition;
            positionIn.z = -positionIn.z;
            Quaternion rotationIn = Quaternion.Euler(dataTank.frameData.cameraEuler);
            rotationIn.z = -rotationIn.z;
            rotationIn.w = -rotationIn.w;
            Matrix4x4 cameraTrans = Matrix4x4.TRS(positionIn, rotationIn, Vector3.one);
            cameraTrans = cameraTrans.transpose;


            requestCounter++;
            //UnityEngine.Debug.Log($"seq={requestCounter}, sw in ms="+sw.ElapsedMilliseconds);
            if (XDKCloudSession.IfSaveImages)
            {
                imageSavePath = Path.Combine(persistentDataPath, "seengene", session_ID);
                if (!Directory.Exists(imageSavePath))
                {
                    Debug.Log("create imageSavePath, path=" + imageSavePath);
                    Directory.CreateDirectory(imageSavePath);
                }
                SaveDataToLocal(requestCounter.ToString(), cameraTrans, intrinsics, positionIn, rotationIn, dataTank.frameData);
                SaveCameraImage(requestCounter.ToString(), jpegBytes);
                
                // // 临时测试
                // Texture2D texture = new Texture2D(dataTank.frameData.textureWidth, dataTank.frameData.textureHeight, TextureFormat.RGB24, false);
                // Color32[] pixels = texture.GetPixels32();
                // for (int i = 0; i < pixels.Length; i++)
                // {
                //     int index = i * 3;
                //     pixels[i] = new Color32(dataTank.frameData.textureDataRGB24[index], dataTank.frameData.textureDataRGB24[index+1], dataTank.frameData.textureDataRGB24[index+2],255);
                // }
                // texture.SetPixels32(pixels);
                // texture.Apply();
                //
                // byte[] jpgData = texture.EncodeToJPG();
                // SaveCameraImage(requestCounter.ToString(), jpgData);

                //SaveCameraImage(requestCounter+"_origin", jpegBytes0);
                //SaveCameraImage(requestCounter+"_afterScale", jpegBytes1);
                //SaveCameraImage(requestCounter+"_afterRotate", jpegBytes2);

                //WriteFileToDisk(requestCounter + "_origin.txt", intri0);
                //WriteFileToDisk(requestCounter + "_afterScale.txt", intri1);
                //WriteFileToDisk(requestCounter + "_afterRotate.txt", intri2);
            }

            RelocationRequest item = new RelocationRequest();
            item.sessionID = session_ID;
            item.seq = requestCounter;
            item.cameraPose = cameraTrans;
            item.cameraIntrinsics = intrinsics;
            item.imageBytes = jpegBytes;
            item.cameraPosition = dataTank.frameData.cameraPosition;
            item.cameraEulerAngles = dataTank.frameData.cameraEuler;
            item.frameIndex = dataTank.frameData.index;

            if (dataTank.lbsData != null)
            {
                item.isGpsOK = true;
                item.longitude = dataTank.lbsData.longitude;
                item.latitude = dataTank.lbsData.latitude;
                item.gpsPrecision = dataTank.lbsData.precision;
                item.gpsDirection = dataTank.lbsData.direction;
            }
            else
            {
                item.isGpsOK = false;
                item.longitude = 0;
                item.latitude = 0;
                item.gpsPrecision = 0;
                item.gpsDirection = 0;
            }

            requestQueue.Add(item);

            /// invoke in main thread
            Loom.QueueOnMainThread(() => {
                xSession.OnRelocateRequest(item);
            });
        }


        private void SaveCameraImage(string fileName, byte[] imageBuffer)
        {
            File.WriteAllBytes(Path.Combine(imageSavePath, fileName + ".jpg"), imageBuffer);
        }

        /// <summary>
        /// 将图片保存到本地
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="imageBuffer"></param>
        /// <param name="matrix"></param>
        /// <param name="intrinsics"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        private void SaveDataToLocal(string fileName, Matrix4x4 matrix, float[,] intrinsics, Vector3 position, Quaternion rotation, XDKFrameData frameData)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"upload_poseData \n{matrix.m00} {matrix.m01} {matrix.m02} {matrix.m03} \n{matrix.m10} {matrix.m11} {matrix.m12} {matrix.m13} \n{ matrix.m20} { matrix.m21} { matrix.m22} { matrix.m23} \n{matrix.m30} {matrix.m31} {matrix.m32} {matrix.m33}");
            sb.Append($"\n\nupload_intrinsics \n{intrinsics[0, 0]} {intrinsics[0, 1]} {intrinsics[0, 2]} \n{intrinsics[1, 0]} {intrinsics[1, 1]} {intrinsics[1, 2]} \n{ intrinsics[2, 0]} { intrinsics[2, 1]} { intrinsics[2, 2]} ");
            sb.Append($"\n\norigin_poseData: \n{position.x} {position.y} {-position.z} \n{rotation.x} {rotation.y} {-rotation.z} {-rotation.w}");
            sb.Append($"\n\norigin_intrinsics \n{frameData.focalLengthX} {0} {frameData.principalPointX} \n{0} {frameData.focalLengthY} {frameData.principalPointY} \n 0 0 1 ");

            if (frameData.distortData != null)
            {
                sb.Append($"\n\ndistortData \n{frameData.distortData.K1} {frameData.distortData.K2} {frameData.distortData.K3} {frameData.distortData.K4} ");
            }

            WriteFileToDisk(fileName + ".txt", sb.ToString());
        }


        private void WriteFileToDisk(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(imageSavePath, fileName), content);
        }

        /// <summary>
        /// 启动一个线程，一直运行。
        /// </summary>
        /// <returns></returns>
        private bool SendImageThreadStart()
        {
            try
            {
                IsUploadingImage = true;
                Thread threadUpload = new Thread(RelocUploadImageThread);
                threadUpload.IsBackground = true;
                threadUpload.Start();
                SendDebugInfo(DebugInfoType.TimeInfo, "XDK Request, SendImageThread Start success");
                return true;
            }
            catch (Exception e)
            {
                IsUploadingImage = false;
                SendDebugInfo(DebugInfoType.TimeInfo, "XDK Request, SendImageThread Start failed: {0}", e.Message);
            }
            return false;
        }

        /// <summary>
        /// 同步更新图片给云定位
        /// U3D端使用单独线程，为了减少主线程UI界面卡顿
        /// </summary>
        private void RelocUploadImageThread()
        {
            while (IsUploadingImage)
            {
                RelocationRequest item = getUploadImageItem();
                if (item != null)
                {
                    SendRelocateRequest(item);
                    Thread.Sleep(3);
                }
                else
                {
                    Thread.Sleep(30);
                }
            }
            //Debug.Log("XDK Request,  Thread run to the end.");
        }


        /// <summary>
        /// 给请求的header的cookie中添加token数据
        /// </summary>
        /// <param name="webRequest"></param>
        private void ReadCookieFromHeader(HttpWebResponse webResponse)
        {
            //int count = 0;
            string tempCookie = null;
            var headers = webResponse.Headers;
            foreach (var key in headers.AllKeys)
            {
                if (string.Equals(key, "Set-Cookie"))
                {
                    tempCookie = headers[key];
                    break;
                }
                //Debug.Log($"Header {count}, {key}={headers[key]}");
                //count++;
            }
            if (tempCookie != null)
            {
                if (string.Equals(MyCookie, tempCookie))
                {
                    //Debug.Log($"XDK Request, GetCookie tempCookie={tempCookie}, is same as old one");
                    return;
                }
                Debug.Log($"XDK Request, GetCookie MyCookie={MyCookie}, --> tempCookie={tempCookie}");
                MyCookie = tempCookie;
            }
        }

        /// <summary>
        /// 给请求的header的cookie中添加token数据
        /// </summary>
        /// <param name="webRequest"></param>
        private void SetCookieToRequest(HttpWebRequest webRequest)
        {
            if (string.IsNullOrEmpty(MyCookie))
            {
                return;
            }
            var cookie = webRequest.Headers.Get("Cookie");
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = MyCookie;
            }
            else
            {
                cookie += ";" + MyCookie;
            }
            webRequest.Headers.Set("Cookie", cookie);
            Debug.Log($"XDK Request, SetMyCookie -> {MyCookie}");
        }



        private void SendRelocateRequest(RelocationRequest uploadImageItem)
        {
            RelocationResponse response = new RelocationResponse();
            response.seq = uploadImageItem.seq;
            response.frameIndex = uploadImageItem.frameIndex;
            response.status = (int)RelocalizeQueryStatus.NET_ERROR;
            long startTicks = DateTime.Now.Ticks / 10000;
            string relocationUrl = getServerUrl();
            if (relocationUrl.EndsWith("/"))
            {
                relocationUrl += "relocalize";
            }
            else
            {
                relocationUrl += "/relocalize";
            }
            currUrl = relocationUrl;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            HttpWebRequest httpReq = null;
            try
            {
                byte[] postBytes = uploadImageItem.ToByteArray();
                httpReq = WebRequest.CreateHttp(new Uri(relocationUrl));
                httpReq.Headers.Set("ServerFlag", serverFlag.ToString());
                httpReq.Method = "POST";
                httpReq.KeepAlive = true; //告知服务器这是短连接，使用后就关闭
                httpReq.ContentType = "application/json";
                httpReq.Timeout = 5000; // 单位是毫秒
                httpReq.ContentLength = postBytes.Length;
                SetCookieToRequest(httpReq); // add cookie

                Debug.Log("sendRequest " + uploadImageItem.ToString());

                //SendDebugInfo(DebugInfoType.RelocateSucc, "XDK Request, SendReloteRequest seq:{0}; ServerFlag:{1}; sessionID:{2}",
                //    uploadImageItem.seq,
                //    serverFlag,
                //    uploadImageItem.sessionID);

                using (Stream reqStream = httpReq.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                    reqStream.Close();
                }

                using (HttpWebResponse webResponse = (HttpWebResponse)httpReq.GetResponse())
                {
                    ReadCookieFromHeader(webResponse);
                    //DebugSocektInfo(httpReq, "relocate");
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        ParseRelocationResponse(webResponse, response, uploadImageItem);
                    }
                    webResponse.Close();
                }
            }
            catch (Exception ex)
            {
                response.status = (int)RelocalizeQueryStatus.NET_ERROR;
                Debug.LogFormat("XDK Request, seq={0} err={1}", uploadImageItem.seq, ex);
            }
            finally
            {
                if (httpReq != null)
                {
                    httpReq.Abort();
                }
            }

            sw.Stop();
            totalTime += sw.ElapsedMilliseconds;
            totalCount++;
            if (maxTime < sw.ElapsedMilliseconds)
            {
                maxTime = sw.ElapsedMilliseconds;
            }
            if (minTime > sw.ElapsedMilliseconds)
            {
                minTime = sw.ElapsedMilliseconds;
            }

            Loom.QueueOnMainThread(() =>
            {
                try
                {
                    OnRelocated?.Invoke(response);
                }
                catch (Exception ee)
                {
                    Debug.Log("Seengene Invoke OnRelocated throw exception: " + ee);
                }
            });
        }



        /// <summary>
        /// 将 Stream 转成 byte[]
        /// </summary>
        /// <param name="responseStream"></param>
        /// <returns></returns>
        private byte[] HttpResponseStreamToBytes(Stream responseStream)
        {
            using (MemoryStream stmMemory = new MemoryStream())
            {
                byte[] buffer = new byte[64 * 1024];
                int i;
                while ((i = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stmMemory.Write(buffer, 0, i);
                }
                byte[] arraryByte = stmMemory.ToArray();
                stmMemory.Close();
                return arraryByte;
            }
        }




        /// <summary>
        /// 处理定位服务的反馈
        /// </summary>
        /// <param name="myHttpWebResponse"></param>
        /// <param name="response"></param>
        /// <param name="request"></param>
        private void ParseRelocationResponse(HttpWebResponse myHttpWebResponse, RelocationResponse response, RelocationRequest request)
        {
            using (Stream responseStream = myHttpWebResponse.GetResponseStream())
            {
                if (responseStream.CanTimeout)
                {
                    responseStream.ReadTimeout = 5000;
                }

                byte[] buffer = HttpResponseStreamToBytes(responseStream);
                response.ReadFromBytes(buffer);
                response.cameraPos = request.cameraPose;
                response.sessionID = request.sessionID;
                response.cameraIntrinsics = request.cameraIntrinsics;
                response.cameraPosition = request.cameraPosition;
                response.cameraEulerAngles = request.cameraEulerAngles;


                SendDebugInfo(DebugInfoType.TimeInfo, "XDK Request, seq={0} status={1} extra_msg={2}",
                                response.seq,
                                response.status,
                                response.extra_msg);

                responseStream.Close();
            }
        }


    }
}