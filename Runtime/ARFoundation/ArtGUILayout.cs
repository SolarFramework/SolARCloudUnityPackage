using System;
using UnityEngine;

namespace ArTwin
{
    public static class ArtGUILayout
    {
        public static void PrefixLabel(string label)
        {
            GUILayout.Label(label, GUILayout.Width(150));
        }

        static IDisposable Header(string label)
        {
            var d = new GUILayout.HorizontalScope();
            PrefixLabel(label);
            return d;
        }

        public static T EnumPopup<T>(string label, T value, params GUILayoutOption[] options)
            where T : struct, Enum
        {
            using (Header(label)) return EnumPopup(value, options);
        }
        public static T EnumPopup<T>(T value, params GUILayoutOption[] options)
            where T : struct, Enum
        {
            var type = typeof(T);
            var texts = Enum.GetNames(type);
            var values = Enum.GetValues(type);
            int index = Array.IndexOf(values, value);
            if (index == -1)
            {
                GUILayout.Label("Not in Enum");
                return value;
            }

            string availableSelection = "";

            string maxText = "";
            float maxWidth = 0;
            foreach (string text in texts)
            {
                float textWidth = GUI.skin.button.CalcSize(new GUIContent(text)).x;
                if (textWidth > maxWidth)
                {
                    maxWidth = textWidth;
                    maxText = text;
                }
                availableSelection += text + "\n ";
            }

            if (availableSelection == "")
            {
                GUILayout.Label("No selection");
                return value;
            }

            var labelRect = GUILayoutUtility.GetRect(new GUIContent(maxText), GUI.skin.button, options);
            GUI.Label(labelRect, new GUIContent(texts[index]), GUI.skin.button);

            if (GUILayout.Button("<", GUILayout.Width(25)))
            {
                --index;
            }
            if (GUILayout.Button(">", GUILayout.Width(25)))
            {
                ++index;
            }

            if (index > (texts.Length - 1)) { index = 0; }
            if (index < 0) { index = texts.Length - 1; }

            value = (T)values.GetValue(index);
            return value;
        }

        public static int IntSlider(string label, int value, int leftValue, int rightValue)
        {
            using (Header(label)) return IntSlider(value, leftValue, rightValue);
        }
        public static int IntSlider(int value, int leftValue, int rightValue)
        {
            return Mathf.RoundToInt(GUILayout.HorizontalSlider(value, leftValue, rightValue));
        }

        public static int IntField(string label, int value)
        {
            using (Header(label)) return IntField(value);
        }
        public static int IntField(int value)
        {
            var str = value.ToString();
            using (ImGuiTools.ChangeCheck)
            {
                str = GUILayout.TextField(str);
                if (GUI.changed)
                    int.TryParse(str, out value);
            }
            return value;
        }

        public static float FloatField(string label, float value)
        {
            using (Header(label)) return FloatField(value);
        }
        public static float FloatField(float value)
        {
            var str = value.ToString();
            using (ImGuiTools.ChangeCheck)
            {
                str = GUILayout.TextField(str);
                if (GUI.changed)
                    float.TryParse(str, out value);
            }
            return value;
        }

        public static string TextField(string label, string value)
        {
            using (Header(label)) return GUILayout.TextField(value);
        }

        public static Vector3 Vector3Field(Vector3 value)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = FloatField(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = FloatField(value.y);
                GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                value.z = FloatField(value.z);
                return value;
            }
        }
    }
}
