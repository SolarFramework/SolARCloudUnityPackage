
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
using System.Timers;

namespace Com.Bcom.Solar
{
    public class FpsCounter
    {
        public event Action OnNotify;

        private int valueOverall = 0;
        private int valuesSinceLastCheck = 0;
        private DateTime StartTime;
        private DateTime LastCheckedTime;

        private System.Timers.Timer timer;

        public void Inc()
        {
            valueOverall++;
        }
        public int Count()
        {
            return valueOverall;
        }
        public void Start(DateTime start)
        {
            StartTime = start;
            LastCheckedTime = start;
            timer = new System.Timers.Timer(2000);
            timer.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) => OnNotify?.Invoke());
            timer.Enabled = true;
        }
        public void Reset()
        {
            valueOverall = 0;
            valuesSinceLastCheck = 0;
        }

        public float GetRelocAttemptsFpsOverall()
        {
            return (float)(valueOverall / (DateTime.Now - StartTime).TotalSeconds);
        }

        public float GetRelocAttemptsFpsSinceLastCheckPoint()
        {
            var result = (float)((valueOverall - valuesSinceLastCheck) / (DateTime.Now - LastCheckedTime).TotalSeconds);
            LastCheckedTime = DateTime.Now;
            valuesSinceLastCheck = valueOverall;
            return result;
        }
    }
}
