using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Seengene.XDK
{
    public class RuntimeConsole : MonoBehaviour
    {
        public static bool Listening = true;

        [SerializeField]
        [Tooltip("Max count for log items, old ones will be removed")]
        private int MaxLogsCount = 2000;

        [SerializeField]
        private LoopListView loopList;

        [SerializeField]
        private ScrollRect DetailView;

        [SerializeField]
        private GridLayoutGroup buttonContainer;

        private const string SaveKey = "runtime_console_font_size";

        // All the messages.
        private List<LoopListModel> m_Messages = new List<LoopListModel>();
        private List<LoopListModel> m_cache = new List<LoopListModel>();

        private int fontSize;

        void Awake()
        {
            RuntimeConsole otherConsole = FindObjectOfType<RuntimeConsole>(true);
            if (otherConsole != null && otherConsole.transform != this.transform)
            {
                Destroy(gameObject);
            }
            if (PlayerPrefs.HasKey(SaveKey))
            {
                fontSize = PlayerPrefs.GetInt(SaveKey);
            }
            if (fontSize < 12)
            {
                fontSize = 12;
            }

            loopList.gameObject.SetActive(false);
            DetailView.gameObject.SetActive(false);
            buttonContainer.gameObject.SetActive(false);
            loopList.onItemExpend = OnLogItemClicked;

            var contentRect = DetailView.content.GetComponent<RectTransform>();
            ObjectTool.SetLeft(contentRect, 0);
            ObjectTool.SetRight(contentRect, 20);

            findButton("btnClose", OnBtnClicked);
            findButton("btnShowAll", OnBtnClicked);
            findButton("btnShowErrors", OnBtnClicked);
            findButton("btnShowLogs", OnBtnClicked);
            findButton("btnFontSizeAdd", OnBtnClicked);
            findButton("btnFontSizeMinus", OnBtnClicked);
            findButton("btnClear", OnBtnClicked);
        }

        private void OnLogItemClicked(LoopListModel model)
        {
            DetailView.gameObject.SetActive(true);
            var text = DetailView.content.GetComponentInChildren<Text>(true);
            var textRect = text.GetComponent<RectTransform>();
            ObjectTool.SetLeft(textRect, 0);
            ObjectTool.SetRight(textRect, 0);
            text.fontSize = fontSize;
            string colorStr;
            if (model.Type == LogType.Error)
            {
                colorStr = "<color=#ff0000>";
            }
            else if (model.Type == LogType.Warning)
            {
                colorStr = "<color=#ffff00>";
            }
            else
            {
                colorStr = "<color=#ffffff>";
            }
            text.text = colorStr + model.Condition + "</color>\n<color=#ffffff>" + model.StackTrace+"</color>";
            
            ObjectTool.SetHeight(textRect, text.preferredHeight);
            ObjectTool.SetPosY(textRect, -text.preferredHeight * 0.5f);

            var contentRect = DetailView.content.GetComponent<RectTransform>();
            ObjectTool.SetHeight(contentRect, text.preferredHeight);

            StartCoroutine(DelayToSetHeight());
        }

        private IEnumerator DelayToSetHeight()
        {
            yield return new WaitForSeconds(0.3f);
            var text = DetailView.content.GetComponentInChildren<Text>(true);
            var textRect = text.GetComponent<RectTransform>();
            ObjectTool.SetHeight(textRect, text.preferredHeight);
            ObjectTool.SetPosY(textRect, -text.preferredHeight * 0.5f);

            var contentRect = DetailView.content.GetComponent<RectTransform>();
            ObjectTool.SetHeight(contentRect, text.preferredHeight);
        }

        private void findButton(string str, System.Action<string> onClick)
        {
            var child = buttonContainer.transform.Find(str);
            if (child)
            {
                var btn = child.GetComponent<Button>();
                btn.onClick.AddListener(() => { onClick?.Invoke(str); });
            }
            else
            {
                Debug.Log("Can not find btn, name="+str);
            }
        }

        private void OnBtnClicked(string btnName)
        {
            if (string.Equals(btnName, "btnClose"))
            {
                loopList.gameObject.SetActive(false);
                DetailView.gameObject.SetActive(false);
                buttonContainer.gameObject.SetActive(false);
            }
            else if (string.Equals(btnName, "btnShowAll"))
            {
                ShowTypeList(m_Messages);
            }
            else if (string.Equals(btnName, "btnShowErrors"))
            {
                List<LoopListModel> list = new List<LoopListModel>();
                foreach (var item in m_Messages)
                {
                    if (item.Type == LogType.Error)
                    {
                        list.Add(item);
                    }
                }
                ShowTypeList(list);
            }
            else if (string.Equals(btnName, "btnShowLogs"))
            {
                List<LoopListModel> list = new List<LoopListModel>();
                foreach (var item in m_Messages)
                {
                    if (item.Type == LogType.Log)
                    {
                        list.Add(item);
                    }
                }
                ShowTypeList(list);
            }
            else if (string.Equals(btnName, "btnFontSizeAdd"))
            {
                if (loopList)
                {
                    fontSize += 2;
                    if (fontSize < 12)
                    {
                        fontSize = 12;
                    }
                    loopList.fontSize = fontSize;
                    PlayerPrefs.SetInt(SaveKey, fontSize);
                }
            }
            else if (string.Equals(btnName, "btnFontSizeMinus"))
            {
                if (loopList)
                {
                    fontSize = fontSize - 2;
                    if (fontSize < 12)
                    {
                        fontSize = 12;
                    }
                    loopList.fontSize = fontSize;
                    PlayerPrefs.SetInt(SaveKey, fontSize);
                }
            }
            else if (string.Equals(btnName, "btnClear"))
            {
                m_Messages.Clear();
                ShowTypeList(m_Messages);
            }
        }

        
        private void OnEnable()
        {
            // Subscribe to the log message received event.
            Application.logMessageReceived += OnGetLogMessage;
        }

        private void OnDisable()
        {
            // Unsubscribe to the log message received event.
            Application.logMessageReceived -= OnGetLogMessage;
        }

        private void OnGetLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!Listening)
            {
                return;
            }
            if (type == LogType.Warning)
            {
                return;
            }
            if (type == LogType.Assert)
            {
                return;
            }
            string tt = System.DateTime.Now.ToString("HH:mm:ss.fff");
            condition = $"[{tt}] {condition}";

            // Trim the condition and stack trace.
            condition = condition.Trim();
            stackTrace = stackTrace.Trim();

            // Add the message.
            m_Messages.Add(new LoopListModel(condition, stackTrace, type));
            while (m_Messages.Count > MaxLogsCount)
            {
                m_Messages.RemoveAt(0);
            }
        }




        public void ShowLogs()
        {
            ShowTypeList(m_Messages);
        }

        private void ShowTypeList(List<LoopListModel> list)
        {
            if (loopList)
            {
                loopList.fontSize = fontSize;
                loopList.SetModels(list);
                loopList.gameObject.SetActive(true);
            }
            if (buttonContainer)
            {
                buttonContainer.gameObject.SetActive(true);
            }
            if (DetailView)
            {
                DetailView.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Exports all logs to Application.dataPath/logs
        /// </summary>
        public void ExportLogs()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_Messages.Count; i++)
            {
                // Add the message type to the line.
                switch (m_Messages[i].Type)
                {
                    case LogType.Error:
                        sb.Append("[ERROR] ");
                        break;
                    case LogType.Assert:
                        sb.Append("[ASSERT] ");
                        break;
                    case LogType.Warning:
                        sb.Append("[WARNING] ");
                        break;
                    case LogType.Log:
                        sb.Append("[LOG] ");
                        break;
                    case LogType.Exception:
                        sb.Append("[EXCEPTION] ");
                        break;
                    default:
                        sb.Append("[UNKNOWN TYPE] ");
                        break;
                }

                // Append the condition.
                sb.Append(m_Messages[i].Condition);
                // Start a new line.
                sb.AppendLine();
                // Add the stack trace.
                sb.AppendLine(m_Messages[i].StackTrace);
                // Add a new empty line.
                sb.AppendLine();
            }

            // Make sure the output directory exists.
            if (!Directory.Exists(Application.dataPath + "/output_logs/"))
                Directory.CreateDirectory(Application.dataPath + "/output_logs/");

            // Create the file name and then the file.
            string fileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.WriteAllText(Application.persistentDataPath + "/output_logs/" + fileName, sb.ToString());
        }
    }
}
