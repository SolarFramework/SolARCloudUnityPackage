/**
 * @copyright Copyright (c) 2023 B-com http://www.b-com.com/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;

using UnityEngine;


namespace Com.Bcom.Solar
{
    public class LogConsole
    {
        public readonly Queue<string> logs;


        private LogLevel level;
        private int size;

        public LogConsole(int size = 40, bool connectToAppLogs = false, LogLevel logLevel = LogLevel.WARNING)
        {
            this.size = size;
            logs = new Queue<string>(capacity:size);
            if (connectToAppLogs)
                Application.logMessageReceivedThreaded += LogMessageReceived;
        }

        public void Log(LogLevel type, string message)
        {
            if (type > level) return;
            LogMessageReceived(type: type.ToUnityLogType(), condition: message, stackTrace: "");
        }

        public void Clear()
        {
            lock(this)
            {
                logs.Clear();
            }
        }

        private void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            lock(this)
            {
                if (logs.Count >= size) logs.Dequeue();
                logs.Enqueue($"<color={GetColor(type)}>[{DateTime.Now}]{condition}{((stackTrace != null && stackTrace != "") ? "\n" + stackTrace : "")}</color>");
            }
        }

        static string GetColor(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error: return "#FF0000";
                case LogType.Assert: return "#FF00FF";
                case LogType.Warning: return "#FFFF00";
                case LogType.Log: return "#FFFFFF";
                case LogType.Exception: return "#FF3333";
                default: throw new NotImplementedException(logType.ToString());
            }
        }
    }
}
