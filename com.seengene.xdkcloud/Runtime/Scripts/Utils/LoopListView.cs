using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Seengene.XDK
{
    [RequireComponent(typeof(ScrollRect))]
    public class LoopListView : MonoBehaviour
    {
        private List<LoopListModel> _models;
        private ScrollRect scrollRect;

        [SerializeField]
        private GameObject itemPrefab;

        private List<LoopListItem> idles = new();

        private float itemHeight;
        private Vector2 scrollNow;
        private bool isDirty;
        private bool inited;
        internal float gap = 3f;

        private int _fontSize;
        public int fontSize
        {
            get{
                return _fontSize;
            }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    isDirty = true;
                    //Debug.Log("set fontSize="+_fontSize);
                    itemHeight = _fontSize + 10;
                    RefreshContentHeight();
                    ChangeValue(scrollNow);
                }
            }
        }


        public Action<LoopListModel> onItemExpend;

        void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (inited)
            {
                return;
            }
            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
            }
            if (_models == null)
            {
                _models = new List<LoopListModel>();
            }
            if (scrollRect.content.childCount > 0)
            {
                itemPrefab = scrollRect.content.GetChild(0).gameObject;
            }
            itemPrefab.transform.SetParent(transform);
            itemPrefab.gameObject.SetActive(false);

            var text = itemPrefab.GetComponentInChildren<Text>(true);
            if (_fontSize == 0)
                _fontSize = text.fontSize;
            itemHeight = fontSize + 10;

            SpawnItems(itemPrefab, 25);

            scrollRect.onValueChanged.AddListener(ChangeValue);

            inited = true;
        }

        private void ChangeValue(Vector2 data)
        {
            if (_models == null)
            {
                return;
            }
            scrollNow = data;
            var viewHeight = GetViewHeight();
            var contentY = scrollRect.content.localPosition.y;
            //Debug.Log("--- data.y=" + data.y.ToString("f4")+ " contentY=" + contentY);
            for (int i = 0; i < _models.Count; i++)
            {
                var model = _models[i];
                float offsetY = (itemHeight + gap) * i + gap;
                if (contentY - offsetY - itemHeight > 0)
                {
                    RecycleDataShower(model);
                    continue; // 对象在上边缘以上
                }
                if (contentY - offsetY < -viewHeight)
                {
                    RecycleDataShower(model);
                    continue; // 对象在下边缘以下
                }
                
                if (model.shower == null)
                {
                    var item = GetIdleItem();
                    item.ShowData(i, fontSize, itemHeight);
                    model.shower = item;
                }
                else if(model.shower.dataIndex != i || isDirty)
                {
                    model.shower.ShowData(i, fontSize, itemHeight);
                }
            }
            isDirty = false;
        }


        private void RecycleDataShower(LoopListModel model)
        {
            if (model.shower != null)
            {
                PutIntoIdles(model.shower);
            }
            model.shower = null;
        }

        private void PutIntoIdles(LoopListItem item)
        {
            item.gameObject.SetActive(false);
            if (idles.Contains(item))
            {
                return;
            }
            idles.Add(item);
        }

        private LoopListItem GetIdleItem()
        {
            if (idles.Count > 0)
            {
                var idle = idles[0];
                idles.RemoveAt(0);
                return idle;
            }
            GameObject go = Instantiate(itemPrefab);
            go.transform.SetParent(scrollRect.content);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.name = "item" + scrollRect.content.childCount;
            go.SetActive(true);
            LoopListItem item = go.GetComponent<LoopListItem>();
            item.Init(this, OnItemClick);
            return item;
        }

        private void SpawnItems(GameObject itemPrefab, int itemCount)
        {
            for (int i = 0; i < itemCount; i++)
            {
                GameObject go = Instantiate(itemPrefab);
                go.transform.SetParent(scrollRect.content);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.name = "item" + i;
                go.SetActive(false);
                LoopListItem item = go.GetComponent<LoopListItem>();
                item.Init(this, OnItemClick);
                PutIntoIdles(item);
            }
        }

        private void OnItemClick(LoopListItem item)
        {
            var model = _models[item.dataIndex];
            Debug.Log("click log index=" + item.dataIndex);
            onItemExpend?.Invoke(model);
        }


        public void SetModels(List<LoopListModel> models)
        {
            Init();

            ObjectTool.SetLeft(scrollRect.content, 0);
            ObjectTool.SetRight(scrollRect.content, 20);

            idles.Clear();
            for (int i = 0; i < scrollRect.content.childCount; i++)
            {
                var child = scrollRect.content.GetChild(i);
                var item = child.GetComponent<LoopListItem>();
                item.gameObject.SetActive(false);
                idles.Add(item);
            }

            _models.Clear();
            _models.AddRange(models);
            foreach (var model in models)
            {
                model.shower = null;
            }

            RefreshContentHeight();
            ChangeValue(Vector2.zero);
        }

        public LoopListModel GetData(int index)
        {
            if (index < 0 || index > _models.Count - 1)
            {
                return null;
            }
            else
            {
                return _models[index];
            }
        }


        private float GetViewHeight()
        {
            var rect = GetComponent<RectTransform>();
            return rect.sizeDelta.y;
        }

        private void RefreshContentHeight()
        {
            if(_models == null)
            {
                return;
            }
            var viewHeight = GetViewHeight();
            float y = _models.Count * (itemHeight + gap) + gap;
            if (y < viewHeight)
            {
                y = viewHeight;
            }
            ObjectTool.SetHeight(scrollRect.content, y);
        }

    }
}
