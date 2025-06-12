using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seengene.XDK
{
    public class LoopListModel 
    {
        /// <summary> The condition text. </summary>
        public string Condition { get; set; }


        /// <summary> The stack trace. </summary>
        public string StackTrace { get; set; }


        /// <summary> The type of the log. </summary>
        public LogType Type { get; set; }


        public LoopListItem shower { get; set; }

        public LoopListModel(string condition, string stackTrace, LogType type)
        {
            Condition = condition;
            StackTrace = stackTrace;
            Type = type;
        }
    }
}
