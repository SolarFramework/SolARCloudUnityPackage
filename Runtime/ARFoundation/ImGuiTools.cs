using System;
using UniRx;
using UnityEngine;

namespace ArTwin
{
    public static class ImGuiTools
    {
        public static IDisposable Enable(bool enabled)
        {
            var d = Disposable.CreateWithState(GUI.enabled, v => GUI.enabled = v);
            GUI.enabled = enabled;
            return d;
        }

        public static IDisposable ChangeCheck
        {
            get
            {
                GUI.changed = false;
                return Disposable.Empty;
            }
        }
    }
}
