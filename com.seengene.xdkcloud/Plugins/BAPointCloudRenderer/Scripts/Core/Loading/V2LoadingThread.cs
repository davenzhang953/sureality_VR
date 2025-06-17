using System;
using BAPointCloudRenderer.DataStructures;
using BAPointCloudRenderer.CloudData;
using System.Threading;
using UnityEngine;

namespace BAPointCloudRenderer.Loading {
    /// <summary>
    /// The Loading Thread of the V2-Rendering-System (see Bachelor Thesis chapter 3.2.6 "The Loading Thread").
    /// Responsible for loading the point data.
    /// </summary>
    class V2LoadingThread {

        private ThreadSafeQueue<Node> loadingQueue;
        private bool running = true;
        private V2Cache cache;
        private Thread thread;
        private int counter = 0;
        
        public V2LoadingThread(V2Cache cache) {
            loadingQueue = new ThreadSafeQueue<Node>();
            this.cache = cache;
        }

        public void Start() {
            running = true;
            thread = new Thread(Run);
            thread.Start();
        }

        private void Run() {
            try {
                while (running) {
                    counter++;
                    if (counter % 800 == 0)
                    {
                        Debug.Log("running V2LoadingThread, counter="+counter);
                    }
                    Node n;
                    if (loadingQueue.TryDequeue(out n)) {
                        Monitor.Enter(n);
                        if (!n.HasPointsToRender() && !n.HasGameObjects()) {
                            Monitor.Exit(n);
                            CloudLoader.LoadPointsForNode(n);
                            cache.Insert(n);
                        } else {
                            Monitor.Exit(n);
                        }
                    }
                    Thread.Sleep(10);
                }
            } catch (Exception ex) {
                if (running)
                {
                    Debug.Log("eee_eea "+ex);
                }
            }
        }

        public void Stop() {
            running = false;
            try
            {
                if (thread != null)
                {
                    thread.Abort();
                    thread = null;
                }
            }
            catch (Exception ee)
            {
                Debug.Log("Error " + ee);
            }
        }

        /// <summary>
        /// Schedules the given node for loading.
        /// </summary>
        /// <param name="node">not null</param>
        public void ScheduleForLoading(Node node) {
            if (loadingQueue.Contains(node))
            {
                return;
            }
            loadingQueue.Enqueue(node);
        }

    }
}
