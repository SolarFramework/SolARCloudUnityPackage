using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class CalibrationSO : ScriptableObject
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/CALIBRATION_SO")]
    public static void CreateMyAsset()
    {
        var asset = CreateInstance<CalibrationSO>();
        UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/NewScripableObject.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = asset;
    }
#endif
    [ContextMenu("Read Xml")]
    public void ReadXml()
    {
        foreach (var resolution in resolutions)
            resolution.ReadXml();
    }

    public string deviceUniqueIdentifier;
    public string deviceName;
    public string deviceModel;

    [Serializable]
    public class Calib
    {
        public Vector2Int resolution;

        public Vector2 focals;
        public Vector2 pPoint;
        public Vector4 distortions;

        [ContextMenuItem("Read Xml", "ReadXml")]
        [SerializeField]
        public string filePath;

        public void ReadXml()
        {
            var serializer = new XmlSerializer(typeof(GmlCalibProject));
            GmlCalibProject gmlProject;
            using (var stream = File.OpenRead(filePath))
            {
                gmlProject = (GmlCalibProject)serializer.Deserialize(stream);
            }
            if (gmlProject == null) return;
            var results = gmlProject.results;
            focals = results.Focal;
            pPoint = results.Principal;
            distortions = results.Distortion;
        }

        [XmlRoot("CalibrationProject")]
        public class GmlCalibProject
        {
            public Results results;
            public class Results
            {
                public int ImageCount;
                public float focus_lenX;
                public float focus_lenY;
                public float PrincipalX;
                public float PrincipalY;
                public float Dist1;
                public float Dist2;
                public float Dist3;
                public float Dist4;
                public float focus_lenX_er;
                public float focus_lenY_er;
                public float PrincipalX_er;
                public float PrincipalY_er;
                public float Dist1_er;
                public float Dist2_er;
                public float Dist3_er;
                public float Dist4_er;
                public float dc_AllImage_errX;
                public float dc_AllImage_errY;
                public string Calib_Date;
                public int Calib_Type;

                [XmlIgnore] public Vector2 Focal => new Vector4(focus_lenX, focus_lenY);
                [XmlIgnore] public Vector2 Principal => new Vector4(PrincipalX, PrincipalY);
                [XmlIgnore] public Vector4 Distortion => new Vector4(Dist1, Dist2, Dist3, Dist4);
            }
        }
    }

    public Calib[] resolutions;
}
