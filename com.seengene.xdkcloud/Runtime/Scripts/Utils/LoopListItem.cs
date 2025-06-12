using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Seengene.XDK
{
    public class LoopListItem : MonoBehaviour
    {
        private LoopListView parentView;
        private int myDataIndex = -1;

        private RectTransform _rect;
        private Button button;
        private Text _text;
        private Action<LoopListItem> onItemClick;

        public int dataIndex
        {
            get
            {
                return myDataIndex;
            }
        }


        public void Init(LoopListView _View, Action<LoopListItem> callback)
        {
            parentView = _View;
            onItemClick = callback;
            _rect = GetComponent<RectTransform>();
            _text = GetComponentInChildren<Text>();

            ObjectTool.SetLeft(_rect, 0);
            ObjectTool.SetRight(_rect, 0);
            
            button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                onItemClick?.Invoke(this);
            });
        }


        public void ShowData(int _dataIndex, int fontSize, float hh)
        {
            myDataIndex = _dataIndex;
            var model = parentView.GetData(dataIndex);
            if(model.Type == LogType.Error)
            {
                _text.color = Color.red;
            }
            else if(model.Type == LogType.Warning)
            {
                _text.color = Color.yellow;
            }
            else
            {
                _text.color = Color.white;
            }
            _text.fontSize = fontSize;
            //Debug.Log("ShowData, fontsize=" + fontSize+ " _dataIndex="+ _dataIndex+" hh="+hh);
            _text.text = model.Condition;

            gameObject.SetActive(true);

            float offsetY = (hh + parentView.gap) * _dataIndex + parentView.gap;
            ObjectTool.SetHeight(_rect, hh);
            ObjectTool.SetPosY(_rect, -offsetY);
        }


    }
}
